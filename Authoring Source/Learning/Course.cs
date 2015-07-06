using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Drawing;
using System.Media;
using System.Threading;

 /* The Course class handles the management of the course objects and files.
  * An instance of this class is created and used in the Slide class.
  * This class is persisted by serializing to an xml file.
  */

namespace Learning
{
    [XmlRoot("course")]
    public class Course
    {
        // the directory string is where all files are located and is also the name of the course
        private string directory;
        // the Control class instance persists navigation
        private Control control;
        // the UnDo class instance persists the undo/redo state
        private UnDo undo;
        // the Hash class instance persists the hash of the course.xml file
        private Hash hash;
        // a course consists of one or more sessions; this is never empty; there is always at least one
        private List<Session> sessions = new List<Session>();
        [XmlElement("sessions")]
        public List<Session> Sessions { get { return sessions; } set { sessions = value; } }
        // this is the delegate that serializes the Course class
        private XmlSerializer serializer = new XmlSerializer(typeof(Course));
        // the name of the course file
        private const string filename = "course.xml";
        // the name of the control file
        private const string controlname = "control.xml";
        // the name of the undo file
        private const string undoname = "undo.xml";
        // the name of the hash file
        private const string hashname = "hash.xml";
        // the name of the undo directory
        private const string undodir = "undo";
        // the name of the images directory (redundantly specified in Screen class)
        private const string imagedir = "images"; // conform this in Screen
        // the name of the sounds directory (redundantly specified in the Image class)
        private const string sounddir = "sounds"; 
        // hashes for the good.wav and bad.wav files that indicate right and wrong answers to questions
        // these hashes are checked when the sound files are called to prevent tampering
        private byte[] goodhash = { 108, 184, 145, 42, 125, 71, 205, 184, 232, 53, 99, 139, 236, 219, 154, 24, 244, 219, 117, 116, 6, 76, 13, 47, 54, 185, 227, 197, 46, 167, 174, 235 };
        private byte[] badhash = { 108, 255, 182, 95, 158, 19, 24, 174, 64, 192, 134, 111, 19, 139, 131, 76, 132, 34, 83, 47, 228, 27, 19, 56, 191, 97, 206, 54, 109, 199, 32, 235 };

        // for a new course, a new guid is created
        // for an existing course, the guid will be deserialized from course.xml
        // the guid is used to save navigation and window state information in the registry
        public Course() {
            guid = Guid.NewGuid().ToString();        
        }
        [XmlElement("guid")]
        public string guid; // used as course identifier in registry

        // re-opens the course using the current directory
        private Course open() {
            return Open(directory);
        }
        // opens a course in directory dir, which may be a new or existing course
        public Course Open(string dir) {
            return Open(dir, false);
        }
        // opens a course using directory dir
        //   if isnew is true, the directory must not already have a course in it
        //   returns course instance, or null if isnew is true and directory is not empty
        public Course Open(string dir, bool isnew) { // create new course directory named s or open existing course at beginning
            Directory = Path.GetFullPath(dir);
            Course c = null;  // initialized null in case of failure
            DirectoryInfo di = new DirectoryInfo(directory);
            di.Create();
            string file = directory + "/" + filename;
            FileInfo fi = new FileInfo(file);
            if (fi.Exists){ // open
                if (isnew) return c; // don't open new course over an old one
                getUnDo(); // create the UnDo instance
                // if currently viewing a past version from the undo folder,
                //      retrieve that version
                if (UnDoPos <= UnDoCtr && UnDoPos > -1){
                    string undofile = undoName(UnDoPos);
                    c = deserialize(undofile);
                    c.Directory = directory;
                    c.getControlUnDo(UnDoPos); // set the Control instance
                    c.getHashUnDo(UnDoPos); // set the Hash instance
                }
                else{ // viewing the current version 
                    c = deserialize(file);
                    c.Directory = directory;
                    c.getControl(); // set the Control instance
                    c.getHash(hashfile); // set the Hash instance
                }
                c.getUnDo(); // set the UnDo instance
            }
            else{ // no course exists here, create a new one
                di = new DirectoryInfo(directory + "/" + undodir);
                di.Create();
                sessions.Add(new Session("NewSession0",directory));
                getControl();
                getUnDo();
                getHash(hashfile);
                Save();
                c = this;
            }
            return c;
        }
        // write the control file
        private void serializeControl(){
            control.Serialize(controlfile);
        }
        // write the undo file
        private void serializeUnDo(){
            undo.Serialize(undofile);
        }
        // read the control file
        private void getControl(){
            getControl(directory + "/" + controlname);
        }
        private void getControl(string file){
            control = new Control();
            control.Guid = guid;
            if ((new FileInfo(file)).Exists)
                control = control.Deserialize(file);
            control.Session = Math.Min(control.Session, sessions.Count - 1);
            control.Screen = Math.Min(control.Screen, CurrentSession.ScreenCount - 1);
        }
        // read the control file from the undo directory
        private void getControlUnDo(int pos){
            getControl(undoName(pos, controlname));
        }
        // read the hash file
        private void getHash(string file){
            hash = new Hash();
            if ((new FileInfo(file)).Exists){
                hash = hash.Deserialize(file);
            }
        }
        // read the hash file from the undo directory
        private void getHashUnDo(int pos){
            getHash(undoName(pos, hashname));
        }
        // read the undo file
        private void getUnDo() {
            undo = new UnDo();
            if ((new FileInfo(undofile)).Exists)
                undo = undo.Deserialize(undofile);
        }
        // write the course.xml file
        private void serialize(string file){
            TextWriter writer = new StreamWriter(file);
            serializer.Serialize(writer, this);
            writer.Close();
        }
        // read the course.xml file and return a Course instance
        private Course deserialize(string file){
            StreamReader reader = new StreamReader(file);
            Course c = (Course)serializer.Deserialize(reader);
            reader.Close();
            return c;
        }
        // save the course after first saving the current files to the undo directory
        public void Save(){
            saveBackup();
            string file = directory + "/" + filename; 
            serialize(file);
            serializeControl();
            hash.HashCode = HashStat.ReadHash(file);
            hash.Serialize(hashfile);
        }
        // copy the current files to the undo directory
        private void saveBackup() {
            FileInfo fi = new FileInfo(directory + "/" + filename);
            if(fi.Exists){
                UnDoCtr++;
                UnDoPos = 1 + UnDoCtr; // reset after save
                (new DirectoryInfo(directory + @"\" + undodir)).Create();
                string file = undoName(UnDoCtr);
                fi.CopyTo(file);
                control.Serialize(undoName(UnDoCtr,controlname));
                hash.HashCode = HashStat.ReadHash(file);
                hash.Serialize(undoName(UnDoCtr, hashname));
            }
        }
        // utility to generate the next serial name of a file copied to the undo directory
        // this version is generalized to any file name, i.e. control.xml or hash.xml
        private string undoName(int ctr, string filename) {
            return directory + "/" + undodir + "/"
                + Path.GetFileNameWithoutExtension(filename)
                + String.Format("{00000}", ctr)
                + Path.GetExtension(filename);
        }
        // utility to generate next serial name of course.xml file copid to undo directory
        private string undoName(int ctr) {
            return undoName(ctr, filename);
        }
        // undo last change by decrementing the undo position pointer and re-opening the course
        public Course UnDo() { 
            if (UnDoCtr > -1 && UnDoPos > 0){
                UnDoPos--;
                return open();
            }
            else {
                return this;
            }
        }
        // redo last undo by incrementing undo position pointer and re-opening
        public Course ReDo() {
            if (UnDoPos <= UnDoCtr){
                UnDoPos++;
                return open();
            }
            else {
                return this;
            }
        }
        // list of session names used to populate listbox in session view
        [XmlIgnore]
        public string[] SessionItems { 
            get{
                string[] s = new string[sessions.Count];
                for(int i = 0; i < sessions.Count; i++)
                    s[i] = sessions[i].Title;
                return s;
            }
        }
        // "Slide n of p" message
        [XmlIgnore]
        public string SlideNumber {
            get { return "Slide " + (1+control.Screen).ToString() + " of " + sessions[control.Session].ScreenCount; }
        }
        // copy image to clipboard and delete it
        public void CutImage() {
            Screen s = CurrentScreen;
            if(s.ImageInstance.ImageFile.Length > 0){
                Clipboard.SetImage(s.Image);
                s.Image = null;
            }
        }
        // copy image to clipboard
        public void CopyImage()
        {
            Clipboard.SetImage((Bitmap)CurrentScreen.Image);
        }
        // enum used to indicate different past modes for images
        public enum ImagePlacement {
            fit,    // resize image to best fit in picture control
            upperLeft, // don't resize and align in upper left corner
            upperRight, // don't resize and align in upper right corner
            lowerLeft, // don't resize and align in lower left corner
            lowerRight // don't resize and align in lower right corner
        }
        // default call is to resize to fit
        public bool Paste() { return Paste(ImagePlacement.fit); }
        // past image depending on placement specification
        public bool Paste(ImagePlacement place) { 
            // accept file name, one only
            if(Clipboard.ContainsFileDropList()){
                StringCollection fdl = Clipboard.GetFileDropList();
                if(fdl.Count == 1){
                    string file = fdl[0].ToLower();
                    // only accept these file extensions...
                    switch (Path.GetExtension(file)) {
                        case".jpg":
                            CurrentScreen.ImageInstance.SetImage(new Bitmap(file), place);
                            return true;
                        case ".bmp":
                            CurrentScreen.ImageInstance.SetImage(new Bitmap(file), place);
                            return true;
                        case ".gif":
                            CurrentScreen.ImageInstance.SetImage(new Bitmap(file), place);
                            return true;
                        case ".wav":
                            CurrentScreen.SoundFile = file;
                            Sound(true);
                            return true;
                    }
                }
            }
            if (Clipboard.ContainsAudio()){
                Stream sound = Clipboard.GetAudioStream();
                CurrentScreen.SoundAudio = sound;
                Sound(true);
                return true;
            }
            // accept image
            if (Clipboard.ContainsImage())
            {
                CurrentScreen.ImageInstance.SetImage((Bitmap)Clipboard.GetImage(), place);
                return true;
            }
            return false;
        }
        // navigate to the next screen if possible
        public void Next(int choice) {
            int s;
            if (View != View.student){ // instructor view
                Screen++;
                if (Screen >= CurrentSession.ScreenCount){
                    CurrentSession.Screens.Add(new Screen(directory));
                    Save();
                }
            }
            else{ // student view
                if (!CurrentScreen.Question)
                    Screen++;
                else
                    if (CurrentScreen.Correct == choice)
                        Screen++;
                    else 
                        GBSound(false);
                if (Screen >= CurrentSession.ScreenCount){
                    s = Session;
                    s = ++s % sessions.Count;
                    Session = s; // sets Screen to 0;
                }
            }
        }
        // navigate to previous screen if possible
        public bool Back() {
            bool ok;
            if (View != View.student){
                if(ok = Screen > 0)
                    Screen--;
            }
            else { // student mode
                if (ok = Screen > 0)
                    Screen--;
                else if (ok = Session > 0)
                    Session--;
            }
            return ok;
        }
        [XmlIgnore]
        // get the directory name to use in creating form caption
        public string Name { // used to create form title
            get { return Path.GetFileNameWithoutExtension(directory); } 
        }
        // enum to indicate insert of session before or after in session view
        public enum InsertLocation {before,after }
        // insert session either before or after current session
        public void InsertSession(InsertLocation loc){
            // if session = 0 and after, insert at 1
            // if session = 0 and before, insert at 0
            if (loc == InsertLocation.after){
                Session++;
            }
            sessions.Insert(control.Session,
                new Session(newSessionName(),directory));
            Screen = 0;
        }
        // used only in Course class to insert session when moving a session up or down
        private void insertSession() {
            sessions.Insert(control.Session, 
                new Session(newSessionName(),directory));
        }
        // generate a new session name when inserting session
        // find a unique name beginning NewSession00, etc.
        private string newSessionName() {
            string name = "NewSession";
            int i = -1;
            bool ok = false;
            while (!ok){
                i++;
                ok = true;
                foreach (Session s in sessions)
                    ok = ok && s.Title != (name + i.ToString());
            }
            return name + i.ToString();
        }
        // used with SaveAs to copy images to new directory
        public void CopyImages(string newdir) {
            string oldd = directory + "/" + imagedir + "/";
            string newd = newdir + "/" + imagedir + "/";
            DirectoryInfo di = new DirectoryInfo(newd);
            di.Create();
            di = new DirectoryInfo(directory + "/" + undodir);
            di.Create();
            foreach (Session sess in sessions)
                foreach (Screen scrn in sess.Screens){
                    if (scrn.ImageInstance.ImageFile != "")
                        (new FileInfo(oldd + scrn.ImageInstance.ImageFile)).CopyTo(newd + scrn.ImageInstance.ImageFile, true);
                }
        }
        // used with SaveAs to copy sounds to new directory
        public void CopySounds(string newdir)
        {
            string oldd = directory + "/" + sounddir + "/";
            string newd = newdir + "/" + sounddir + "/";
            DirectoryInfo di = new DirectoryInfo(newd);
            di.Create();
            foreach (Session sess in sessions)
                foreach (Screen scrn in sess.Screens){
                    if (scrn.soundfile != null && scrn.soundfile != "")
                        (new FileInfo(oldd + scrn.soundfile)).CopyTo(newd + scrn.soundfile, true);
                }
        }
        // returns the Screen instance of the current screen
        [XmlIgnore]
        public Screen CurrentScreen{ // gets the current screen
            get {return sessions[control.Session].Screens[control.Screen]; }
            set { sessions[control.Session].Screens[control.Screen] = value; }
        }
        // deletes the current screen
        public void DeleteScreen() {
            Session s = sessions[control.Session];
            if(s.Screens.Count > 1)
                s.Screens.RemoveAt(control.Screen);
            if (control.Screen > s.Screens.Count - 1){
                Screen--;
            }
        }
        // inserts Screen s at the current location
        public void InsertScreen(Screen s) {
            CurrentSession.Screens.Insert(control.Screen, s);
        }
        // copy the current screen to the clipboard and delete it
        public void CutScreen() {
            Clipboard.SetData(DataFormats.Serializable, CurrentScreen);
            DeleteScreen();
        }
        // copy the current screen to the clipboard
        public void CopyScreen() {
            Clipboard.SetData(DataFormats.Serializable, CurrentScreen);
        }
        // past the current screen from the clipboard
        // also copies image and sound files if from a different directory
        public bool PasteScreen() {
            if(Clipboard.ContainsData(DataFormats.Serializable)){
                object obj = Clipboard.GetData(DataFormats.Serializable);
                if (obj.GetType().Name == "Screen"){
                    CurrentScreen = (Screen)obj;
                    CurrentScreen.ImageInstance.GetFile(directory);
                    CurrentScreen.GetSoundFile(directory);
                    return true;
                }
            }
            return false;
        }
        // returns the Session instance of the current session
        [XmlIgnore]
        public Session CurrentSession {
            get { return sessions[control.Session]; }
            set { sessions[control.Session] = value; }
        }
        // deletes the current session unless it's the last one
        public void DeleteSession() {
            if (sessions.Count > 1){
                sessions.RemoveAt(Session);
                Screen = 0;
                if(Session == sessions.Count){
                    Session--;
                }
            }
        }
        // property to get and set the undo counter
        // undo counter is the number of the last undo files saved in the undo directory
        [XmlIgnore]
        public int UnDoCtr { 
            get { return undo.UnDoCtr; } 
            set { 
                undo.UnDoCtr = value;
                serializeUnDo();
            } 
        }
        // property to keep track of where we are in the undo backups when undoing
        [XmlIgnore]
        public int UnDoPos { 
            get { return undo.UnDoPos; } 
            set { 
                undo.UnDoPos = value;
                serializeUnDo();
            } 
        }
        // Property that persists the form location
        [XmlIgnore]
        public Point Location { 
            get { return control.Location; } 
            set { 
                control.Location = value;
                serializeControl();
            } 
        }
        // property that persists the form window state
        [XmlIgnore]
        public FormWindowState WindowState {
            get { return control.WindowState; }
            set{
                control.WindowState = value;
                serializeControl();
            }
        }
        // property that persists the current session pointer
        [XmlIgnore]
        public int Session { 
            get { return control.Session; } 
            set { 
                control.Session = value;
                control.Screen = 0;
                serializeControl();
            } 
        }
        // property that persists the current view
        [XmlIgnore]
        public View View { 
            get { return control.View; } 
            set { 
                control.View = value;
                serializeControl();
            } 
        }
        // property that persists the help state on/off
        [XmlIgnore]
        public bool Help {
            get { return control.Help == 1; }
            set { control.Help = (value ? 1 : 0); }
        }
        // property that gets and sets the directory, which is also the course name
        // the directory is propogated to sessions and screens so that
        //     when they are copied and pasted, the image and sound files can be
        //     copied from the old directory
        [XmlIgnore]
        public string Directory { 
            get { return directory; } 
            set { 
                directory = value;
                foreach (Session s in sessions)
                    s.Directory = directory;
            } 
        }
        // property that persists the pointer to the current screen
        [XmlIgnore]
        public int Screen { 
            get { return control.Screen; } 
            set { 
                control.Screen = value;
                serializeControl();
            } 
        }
        // enum to specify session move as up or down in list
        public enum MoveDirection {up,down }
        // move a session up or down in the list of sessions
        public void SessionMove(MoveDirection drn) {
            Session s = CurrentSession;
            sessions.RemoveAt(control.Session);
            Session += (drn == MoveDirection.down) ? 1 : -1;
            insertSession();
            CurrentSession = s;
        }
        // save the current course to a new directory
        // this copies images and sound clips so may take a long time
        public bool SaveAs(string dir) {
            string file = dir + "/" + filename;
            if (new FileInfo(file).Exists && !OKtoReplace()) 
                return false;
            CopyImages(dir); // do this before changing directory!
            CopySounds(dir);
            Directory = dir;
            UnDoCtr = -1;
            UnDoPos = -1;
            DirectoryInfo di = new DirectoryInfo(dir + "/" + undodir);
            di.Create();
            foreach (FileInfo fi in di.GetFiles())
                fi.Delete();
            Save();
            return true;
        }
        // dialog that asks the user if it's OK to replace the course in the target directory 
        private bool OKtoReplace() {
            return DialogResult.Yes == MessageBox.Show(
                "A course already exists in this directory.  Replace?", 
                "Save As", MessageBoxButtons.YesNo);
        }
        // some private properties that compose full path names of files
        private string coursefile { get { return directory + "/" + filename; } }
        private string controlfile { get { return directory + "/" + controlname; } }
        private string undofile { get { return directory + "/" + undoname; } }
        private string hashfile { get { return directory + "/" + hashname; } }
        // inserts a new screen
        public void InsertNewScreen() {
            InsertScreen(new Screen(directory));
        }
        // pastes a session from the clipboard
        // copies files from previous directory if pasted to a different directory
        public bool PasteSession() {
            if (Clipboard.ContainsData(DataFormats.Serializable)){
                object obj = Clipboard.GetData(DataFormats.Serializable);
                if (obj.GetType().Name == "Session"){
                    CurrentSession = (Session)obj;
                    foreach (Screen s in CurrentSession.Screens){
                        s.ImageInstance.GetFile(Directory);
                        s.GetSoundFile(Directory);
                    }
                    CurrentSession.Directory = directory;
                    return true;
                }
            }
            return false;
        }
        // plays the sound clip only if in student view
        public void Sound() {
            Sound(false);
        }
        // plays the sound clip if in student view or if force is true
        public void Sound(bool force)
        {
            if (force || View == View.student){
                string file = CurrentScreen.SoundFile;
                if (file != null)
                    (new SoundPlayer(file)).Play();
            }
        }
        // navigates to the previous session 
        public bool Up() {
            if (Session > 0){
                Session--;
                Screen = 0;
                return true;
            }
            return false;
        }
        // navigates to the next session
        public bool Down() {
            if (Session < SessionItems.Length - 1){            
                Session++;
                Screen = 0;
                return true;
            }
            return false;
        }
        // plays sound for good or bad answer
        public void GBSound(bool good){
            if (good)
                (new SoundPlayer("good.wav")).Play();
            else
                (new SoundPlayer("bad.wav")).Play();
        }
    }   
}
