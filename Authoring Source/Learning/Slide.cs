using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.Windows;
using System.IO;
using System.Threading;
using System.Media;

/* Cyryx Self Study Authoring Program
 * 
 * Steve Halasz
 * sjhalasz@gmail.com, sjh@sjhalasz.com
 * January, 2007
 * 
 * This program is used to create Cyryx Self Study courses to be used as a supplement to Cyryx courses.
 * The "Student" MS Visual C# project is used to create the program that runs the courses 
 * created by this authoring program.
 * 
 * Slide class - the only form.  All interaction with the form is in this class.
 * Course class - the class used by the Slide class to manage the authoring functions.
 * Session class - a course consists of one or more sessions each made up of a series of screens.
 * Screen class - one interaction with the student, consisting of a text, an image, a sound clip 
 *     and possibly a question.
 * Image class - encapsulation of image handling.
 * Hash class - a hash of the course.xml file to prevent tampering.
 * HashStat class - a static class to generate and check hashes of the course.xml file, image files and sound files.
 * Control class - handles the navigation state which persists between uses of the program.
 * UnDo class - encapsulates unlimited undo and redo of authoring edits.
 * 
 * The program creates and manages the following files:
 * course.xml - contains all authoring information for the course except images and sound.
 * control.xml - persists navigation information for authoring only; not used by Student version.
 * undo.xml - persists undo/redo state for authoring only; not used by Student version.
 * hash.xml - persists the hash of course.xml to prevent tampering.
 * images/image0000.jpg etc. - image files, one per Screen.
 * sounds/sound0000.wav etc. - sound clips, one per Screen.
 * undo/course0000.xml etc. - backup of course.xml for undo/redo.
 * undo/hash0000.xml etc. - backup of hash of course.xml for undo/redo.
 * undo/control0000.xml etc. - backup of control.xml for undo/redo.
 * 
 * There is only one form, Slide, that is reconfigured for 3 different views, as follows:
 *     Slide view - shows the screens for one session and allows edits.
 *     Student view - shows what the course will look like to the student, without editing.
 *     Session view - allows adding, deleting, moving, renaming sessions.
 * 
 * Hashes are created in this program but not checked when loading files.  The student version
 * of the program checks the hashes though before loading files.
 * 
 */
 
namespace Learning
{

    /* View enum - 
     *     sessions - add, rename, delete, move etc. sessions
     *     slide - edit slides in a session
     *     student - what it looks like to student, no editing allowed.
     */

    public enum View { sessions, slide, student, none }

    // Slide class - the one and only form, reconfigured for different views.
    
    public partial class Slide : Form {
        // appName - "Cyryx Self Study"
        // it's encrypted to prevent tampering
        private string appName;
        // a and b are exclusive-or'ed to give the appName as c
        private byte[] a = { 072, 143, 147, 208, 132, 026, 097, 119, 174, 137, 083, 157, 185, 055, 204, 129 };
        private byte[] b = { 011, 246, 225, 169, 252, 058, 050, 018, 194, 239, 115, 206, 205, 066, 168, 248 };
        private byte[] c = new byte[16]; // conform this length with above
        // course - the Course class instance
        private Course course;
        // choice - the answer to the question
        //          it's saved here and referenced by the updateScreen() method
        private int choice;
        // session, screen and courseDirectory variables are assigned here when they change
        //        and referenced by updateScreen() to recognize when the screen has changed
        private int session = -1; // to track change of screen
        private int screen = -1;
        private string courseDirectory = "";
        private View view = View.none;

        public Slide(){
            InitializeComponent(); // standard MSVC# form initialization
        }

        // Form1_Load method
        // initialization of the form
        private void Form1_Load(object sender, EventArgs e){
            // backgroundWorker2 saves changes to the Text and Answer fields every 15 seconds
            backgroundWorker2.RunWorkerAsync();
            sessions.BringToFront();  // when editing in design view it may have been left in the back
            // dir is the directory where the course will be found
            // it's saved in the configuration settings for this program
            string dir = (new Properties.Settings()).Course;
            // decode the appName ("Cyryx Self Study")
            for (int i = 0; i < a.Length; i++)
                c[i] = (byte)(a[i] ^ b[i]);
            ASCIIEncoding enc = new ASCIIEncoding();
            appName = enc.GetString(c);
            // open the course and assign the instance variable for it
            course = (new Course()).Open(dir);
            // get the saved window location and state and apply them to the form
            Location = course.Location;
            WindowState = course.WindowState;
            help.LoadFile("help.rtf");
            helpShow(course.Help);
            // standardized update routine that writes the form depending on the current state
            updateScreen();
        }

        //  updateScreen method
        //  Standardized update routine to write the form depending on the current state.
        //  It sets form controls and properties redundantly for the sake of simplicity.
        //  There's no noticeable performance penalty from doing so.
        private void updateScreen(){
            // set the form caption from the application name "Cyryx Self Study" and the course name
            // courseName is a private property of Slide
            Text = appName + " - " + course.Name;
            // make the session panel visible if in session view
            sessions.Visible = course.View == View.sessions;
            // session view
            if (sessions.Visible){
                // disable callbacks for session view so they're not triggered when set here
                rename.TextChanged -= rename_TextChanged;
                listBox.SelectedIndexChanged -= listBox1_SelectedIndexChanged;
                // set the text box for renaming a session
                rename.Text = course.CurrentSession.Title;
                // set the list box that lists sessions in session view
                listBox.Items.Clear();
                listBox.Items.AddRange(course.SessionItems);
                listBox.SelectedIndex = course.Session;
                // re-enable the calbacks
                rename.TextChanged += rename_TextChanged;
                listBox.SelectedIndexChanged += listBox1_SelectedIndexChanged;
            }
            else{ // slide or student view
                // set the title of the current session
                // session numbering is added automatically
                title.Text = ((1 + course.Session).ToString()) + ". " + course.CurrentSession.Title;
                // set the "Slide n of p" message
                slideNumber.Text = course.SlideNumber;
                // set the screen image
                picture.Image = course.CurrentScreen.Image;
                // set the screen text
                text.Rtf = course.CurrentScreen.Text;
                // change the text cursor to arrow when in student view
                text.Cursor = (course.View == View.student) ? Cursors.Arrow : Cursors.IBeam;
                // the question panel is visible only if this screen is a question
                // make text read only in student view
                text.ReadOnly = course.View == View.student;
                questions.Visible = course.CurrentScreen.Question;
                // set up the question's answer panel if this is a question screen
                // the question itself is given in the text field
                if (questions.Visible){
                    // set the answer text
                    answer1.Text = course.CurrentScreen.Answer(1);
                    answer2.Text = course.CurrentScreen.Answer(2);
                    answer3.Text = course.CurrentScreen.Answer(3);
                    answer4.Text = course.CurrentScreen.Answer(4);
                    // set the cursor to arrow for student view
                    answer1.Cursor = (course.View == View.student) ? Cursors.Arrow : Cursors.IBeam;
                    answer2.Cursor = (course.View == View.student) ? Cursors.Arrow : Cursors.IBeam;
                    answer3.Cursor = (course.View == View.student) ? Cursors.Arrow : Cursors.IBeam;
                    answer4.Cursor = (course.View == View.student) ? Cursors.Arrow : Cursors.IBeam;
                    // reset normal background color which changes for correct/incorrect answers
                    answer1.BackColor = text.BackColor;
                    answer2.BackColor = text.BackColor;
                    answer3.BackColor = text.BackColor;
                    answer4.BackColor = text.BackColor;
                    // disable radio button callbacks so they're not triggered when setting them here
                    radioButton1.CheckedChanged -= radioButton1_CheckedChanged;
                    radioButton2.CheckedChanged -= radioButton2_CheckedChanged;
                    radioButton3.CheckedChanged -= radioButton3_CheckedChanged;
                    radioButton4.CheckedChanged -= radioButton4_CheckedChanged;
                    // set all radio buttons to false initially
                    radioButton1.Checked = false;
                    radioButton2.Checked = false;
                    radioButton3.Checked = false;
                    radioButton4.Checked = false;
                    // set answer text to read only in student view
                    answer1.ReadOnly = course.View == View.student;
                    answer2.ReadOnly = course.View == View.student;
                    answer3.ReadOnly = course.View == View.student;
                    answer4.ReadOnly = course.View == View.student;
                    // set choice to correct choice if not in instructor view
                    if (course.View != View.student)
                        choice = course.CurrentScreen.Correct;
                    // set radio button and answer text background color depending on choice
                    RadioButton rb = null;
                    TextBox tb = null;
                    // get the radio button and answer control for the choice
                    switch (choice){
                        case 1: rb = radioButton1; tb = answer1; break;
                        case 2: rb = radioButton2; tb = answer2; break;
                        case 3: rb = radioButton3; tb = answer3; break;
                        case 4: rb = radioButton4; tb = answer4; break;
                    }
                    if (rb != null){ // if there is a valid choice
                        rb.Checked = true;  // set checked to true
                        // if in authoring (slide) view, green background indicates correct choice
                        if (course.View != View.student)
                            tb.BackColor = Color.LightGreen;
                        else // in student view, background and sound depend on whether selection is correct or not
                            if (course.CurrentScreen.Correct != choice){
                                tb.BackColor = Color.LightPink;
                                course.GBSound(false);
                            }
                            else{
                                tb.BackColor = Color.LightGreen;
                                course.GBSound(true);
                            }
                    }
                    // re-enable radio button callbacks
                    radioButton1.CheckedChanged += radioButton1_CheckedChanged;
                    radioButton2.CheckedChanged += radioButton2_CheckedChanged;
                    radioButton3.CheckedChanged += radioButton3_CheckedChanged;
                    radioButton4.CheckedChanged += radioButton4_CheckedChanged;
                }
                // enable context menus only in slide view
                contextMenuStrip1.Enabled = course.View == View.slide;
                contextMenuStrip2.Enabled = course.View == View.slide;
                // initialized text and answer modified states to false, to detect when they change
                text.Modified = false;
                answer1.Modified = false;
                answer2.Modified = false;
                answer3.Modified = false;
                answer4.Modified = false;
            }
            // make the question toggle button visible only in slide view
            questionsShow.Visible = course.View == View.slide;
            // disable callback for button to toggle question panel on and off
            questionsShow.CheckedChanged -= checkBox1_CheckedChanged;
            // set the toggle
            questionsShow.Checked = questions.Visible;
            // re-enable the callback
            questionsShow.CheckedChanged += checkBox1_CheckedChanged;
            // turn "Student View" label on or off
            StudentView.Visible = course.View == View.student;
            // set menu checked states for the 3 different views
            toolStripMenuItemSessions.CheckState =
                (course.View == View.sessions) ? CheckState.Checked : CheckState.Unchecked;
            toolStripMenuItemSlides.CheckState =
                (course.View == View.slide) ? CheckState.Checked : CheckState.Unchecked;
            toolStripMenuItemStudent.CheckState =
                (course.View == View.student) ? CheckState.Checked : CheckState.Unchecked;
            // enable or disable undo and redo depending on the undo state
            undoToolStripMenuItem.Enabled = course.UnDoCtr > -1;
            redoToolStripMenuItem.Enabled = course.UnDoPos <= course.UnDoCtr;
            // enable or disable session menu items depending on view
            cutToolStripMenuItem.Enabled = course.View == View.slide;
            copyToolStripMenuItem.Enabled = course.View == View.slide;
            pasteToolStripMenuItem.Enabled = course.View == View.slide;
            insertToolStripMenuItem.Enabled = course.View == View.slide;
            // "Sound" menu item is enabled if there is a sound available for this screen
            // clicking it plays the sound clip, which normally doesn't play automatically
            soundToolStripMenuItem.Enabled =
                course.CurrentScreen.SoundFile != ""
                && course.CurrentScreen.SoundFile != null;
            // make the panel at the bottom of the screen (with Back and Next buttons) invisible in session view
            panel1.Visible = course.View != View.sessions;
            // when the screen changes...
            if (course.Directory != courseDirectory
                || course.Session != session
                || course.Screen != screen
                || course.View != view){
                choice = -1; // set the question response to invalid
                course.Sound(); // play the sound clip for this screen
                // save the navigation state so we can detect screen change next time
                courseDirectory = course.Directory;
                session = course.Session;
                screen = course.Screen;
                view = course.View;
                // set initial focus to the Next button
                nextButton.Focus();
            }
        }

        // These methods relate to the panel at the bottom of the screen in slide/student mode

        // this check box toggles the question panel on and off
        private void checkBox1_CheckedChanged(object sender, EventArgs e){
            working(true);
            saveText();
            if (((CheckBox)sender).CheckState == CheckState.Checked){
                questions.Visible = true;
                answer1.Focus();
            }
            else
                questions.Visible = false;
            // save the question on/off state for this screen
            course.CurrentScreen.Question = questions.Visible;
            saveCourse();
            working(false);
        }

        // ***** UTILITIES *****

        // puts up the "Saving..." message when the course has been saved
        // it stays up for 5 seconds regardless of how long it takes to save
        // in fact the save takes only milliseconds
        // it's possible for it to be called multiple times within 5 seconds
        //     and you need to catch the error when it tries to run the background worker
        //     when it's already running
        private void saveMessage() {
            try{
                Saving.Visible = true;
                backgroundWorker1.RunWorkerAsync();
            }
            catch { }
        }

        // the following 2 methods save the course to the course.xml file
        // this one is the default behaviour, with update
        private void saveCourse() {
            saveCourse(true);
        }
        // saves the course with or without call to updateScreen method
        //      and triggers the "Saving..." message
        // the only reason for calling updateScreen here is to enable/disable the undo/redo menu items
        private void saveCourse(bool updatescreen) {
            saveMessage();
            course.Save();
            if(updatescreen)
                updateScreen(); // resets menu undo
        }

        // background worker that waits 1 second then turns off "Saving..." label
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(1000);
        }
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Saving.Visible = false;
        }

        // background worker to save text every 15 seconds
        // it's started at form load
        //     runs for 15 seconds
        //     saves text if needed
        //     re-runs itself again
        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(15000);
        }
        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            saveText();
            backgroundWorker2.RunWorkerAsync();
        }

        // turn the "Working..." message on and off
        // it uses timer1 to flash
        private void working(bool state)
        {
            if (state)
            {
                work.Visible = true;
                Refresh();
                timer1.Enabled = true;
            }
            else
            {
                timer1.Enabled = false;
                work.Visible = false;
            }
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            work.Visible = !work.Visible;
            Refresh();
        }

        // ***** FORM *****

        // persist the form location when it changes
        private void Slide_LocationChanged(object sender, EventArgs e)
        {
            if (course != null)
            {
                course.Location = ((Form)sender).Location;
            }
        }
        // save text when form closes
        private void Slide_FormClosing(object sender, FormClosingEventArgs e)
        {
            working(true);
            saveText();
            working(false);
        }
        // save window state when it changes; it persists between runs
        private void Slide_Resize(object sender, EventArgs e)
        {
            if (course != null)
                course.WindowState = ((Form)sender).WindowState;
        }

        // ***** MAIN MENUS *****

        // menu item clicked to choose "session" view
        private void toolStripMenuItem7_Click(object sender, EventArgs e){ // view sessions
            working(true);
            saveText();
            course.View = View.sessions;
            updateScreen();
            working(false);
        }

        // menu item clicked to choose "slide" view
        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        { // view slides
            working(true);
            course.View = View.slide;
            updateScreen();
            working(false);
        }

        // menu item clicked to choose "student" view
        private void toolStripMenuItem2_Click_1(object sender, EventArgs e)
        { // view student
            working(true);
            saveText();
            course.View = View.student;
            updateScreen();
            working(false);
        }

        // menu handler for file/new
        // asks the user to create a new folder, then switches to it
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        { // new course
            working(true);
            saveText();
            string desc = "Create a new folder for the new course.  The folder name will be the name of the course.";
            openCourse(desc, true);
            working(false);
        }

        // menu handler for file/open
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        { // open a course
            working(true);
            saveText();
            string desc = "Open a folder for an existing course.  The folder name is the name of the course.";
            openCourse(desc, false);
            working(false);
        }

        // method to open a course, either new or existing
        //    desc - text instructions for the user
        //    isnew - true if this should be a new course
        // returns true if course was successfully opened
        private bool openCourse(string desc, bool isnew)
        {
            string dir = openDialog(desc);
            bool r = false;
            if (dir != "")
            { // if not canceled in directory selection dialog...
                Course c = (new Course()).Open(dir, isnew);
                // returns null if unable to open a new course at this directory, i.e. one already exists there
                if (c != null)
                {
                    course = c;
                    saveDirectorySetting(dir);
                    updateScreen();
                    r = true;
                }
                else
                {
                    MessageBox.Show("A course already exists in this directory.  Please create a new directory.");
                }
            }
            return r;
        }

        // puts up the open course dialog
        // desc is the text message for the user
        // returns the directory selected or empty string for cancel
        private string openDialog(string desc)
        {
            folderBrowserDialog1.Description = desc;
            string dir = "";
            if (DialogResult.OK == folderBrowserDialog1.ShowDialog())
                dir = Path.GetFullPath(folderBrowserDialog1.SelectedPath);
            return dir;
        }

        // saves the currently selected course directory in the application configuration file
        private void saveDirectorySetting(string dir)
        {
            Properties.Settings settings = new Properties.Settings();
            settings.Course = dir;
            settings.Save();
        }

        // file/save as menu item handler
        // saves the current course to a new directory
        // this can take a long time to run because it needs to copy images and sound clips
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        { // save as
            saveText();
            string desc = "Create a new folder to save a copy of this course.  The folder name will be the name of the course.";
            string dir = openDialog(desc);
            if ("" != dir)
            {
                working(true);
                if (course.SaveAs(dir))
                {
                    saveDirectorySetting(dir);
                    updateScreen(); // to update the form caption
                }
                working(false);
            }
        }

        // file/exit menu item handler
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        // edit/cut menu item handler
        // copies a serialization of the current screen to the clipboard and deletes it
        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            working(true);
            saveText();
            course.CutScreen();
            saveCourse();
            updateScreen();
            working(false);
        }

        // edit/copy menu item handler
        // copies a serialization of the current screen to the clipboard
        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            working(true);
            saveText();
            course.CopyScreen();
            working(false);
        }

        // edit/paste menu item handler
        // copies the serialization of a screen from the clipboard to the current screen
        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            working(true);
            saveText();
            if (course.PasteScreen())
            {
                saveCourse();
                updateScreen();
            }
            working(false);
        }

        // edit/insert menu item handler
        // inserts a new empty screen into the session
        private void toolStripMenuItem1_Click_1(object sender, EventArgs e)
        {
            working(true);
            saveText();
            course.InsertNewScreen();
            updateScreen();
            working(false);
        }

        // edit/undo menu item handler
        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            working(true);
            saveText();
            course = course.UnDo();
            updateScreen();
            working(false);
        }

        // edit/redo menu item handler
        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            working(true);
            saveText();
            course = course.ReDo();
            updateScreen();
            working(false);
        }

        // handler for Sound menu item; play sound clip
        private void soundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            course.Sound(true);
        }

        // ***** SLIDE AND STUDENT VIEW *****
        
        // context menu for picture
        // paste menu item clicked; paste image from clipboard
        private void toolStripMenuItemPaste_Click(object sender, EventArgs e)
        {
            working(true);
            saveText();
            if(course.Paste()){
                saveCourse();
                updateScreen();
            }
            working(false);
        }

        // context menu for picture
        // cut menu item clicked; copy image to clipboard and remove it
        private void toolStripMenuItemCut_Click(object sender, EventArgs e)
        {
            working(true);
            saveText();
            course.CutImage();
            saveCourse();
            updateScreen();
            working(false);
        }

        // context menu for picture
        // copy menu item clicked; copy image to clipboard
        private void toolStripMenuItemCopy_Click(object sender, EventArgs e)
        {
            working(true);
            saveText();
            course.CopyImage();
            working(false);
        }

        // picture context menu
        // Paste Upper Left menu item clicked
        // paste image from clipboard without scaling
        // position in upper left corner of picture control
        private void pasteUpperLeftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pasteClip(Course.ImagePlacement.upperLeft);
        }

        // picture context menu
        // Paste Upper Right menu item clicked
        // paste image from clipboard without scaling
        // position in upper right corner of picture control
        private void pasteUpperRightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pasteClip(Course.ImagePlacement.upperRight);
        }

        // picture context menu
        // Paste Lower Left menu item clicked
        // paste image from clipboard without scaling
        // position in lower left corner of picture control
        private void pasteLowerLeftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pasteClip(Course.ImagePlacement.lowerLeft);
        }

        // picture context menu
        // Paste Lower Right menu item clicked
        // paste image from clipboard without scaling
        // position in lower right corner of picture control
        private void pasteLowerRightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pasteClip(Course.ImagePlacement.lowerRight);
        }

        // generalized method to paste clip without scaling in one of 4 corners of picture
        private void pasteClip(Course.ImagePlacement place)
        {
            working(true);
            saveText();
            course.Paste(place);
            saveCourse();
            updateScreen();
            working(false);
        }
        // context menu for main text
        // Copy menu item clicked; copy selected text to clipboard
        private void toolStripMenuItemCopy2_Click(object sender, EventArgs e)
        {
            working(true);
            saveText();
            text.Copy();
            working(false);
        }

        // context menu for main text
        // Cut menu item clicked; copy selected text to clipboard and delete text
        private void toolStripMenuItemCut2_Click(object sender, EventArgs e)
        {
            working(true);
            saveText();
            text.Cut();
            course.CurrentScreen.Text = text.Rtf;
            saveCourse();
            working(false);
        }

        // context menu for main text
        // Paste menu item clicked; past from clipboard if plain text
        // Note:  doesn't past rtf formatted text because there may be very 
        //        weird formatting that doesn't work well with this program
        private void pasteText()
        {
            working(true);
            saveText();
            if (Clipboard.ContainsText(TextDataFormat.Text))
                text.SelectedText = Clipboard.GetText(TextDataFormat.Text);
            course.CurrentScreen.Text = text.Rtf;
            saveCourse();
            working(false);
        }

        // main text context menu
        // Paste menu item clicked; paste plain text only at selection
        private void toolStripMenuItemPaste2_Click(object sender, EventArgs e)
        {
            working(true);
            saveText();
            pasteText();
            working(false);
        }

        // main text context menu
        // Bold menu item clicked; set selection to bold
        private void toolStripMenuItemBold_Click(object sender, EventArgs e)
        {
            fontStyle(FontStyle.Bold);
        }

        // generalized method for setting font styles to main text
        private void fontStyle(FontStyle fs)
        {
            working(true);
            saveText();
            text.SelectionFont = new Font(text.SelectionFont, fs);
            text.SelectionColor = text.ForeColor;
            course.CurrentScreen.Text = text.Rtf;
            saveCourse();
            working(false);
        }

        // main text context menu
        // Underline menu item clicked
        private void toolStripMenuItemUnderline_Click(object sender, EventArgs e)
        {
            fontStyle(FontStyle.Underline);
        }

        // main text context menu
        // Italic menu item clicked
        private void toolStripMenuItemItalic_Click(object sender, EventArgs e)
        {
            fontStyle(FontStyle.Italic);
        }

        // generalized method for toggling the bulleting of selected main text
        private void toggleBullet()
        {
            working(true);
            saveText();
            text.SelectionBullet = !text.SelectionBullet;
            course.CurrentScreen.Text = text.Rtf;
            saveCourse();
            working(false);
        }

        // main text context menu
        // Bullet menu item clicked; toggle bullet on/off
        private void toolStripMenuItemBullet_Click(object sender, EventArgs e)
        {
            toggleBullet();
        }

        // main text context menu
        // Normal menu item selected
        private void toolStripMenuItemNormal_Click(object sender, EventArgs e)
        {
            fontStyle(FontStyle.Regular);
        }

        // generalized method for saving text changes when focus may
        //    be shifting away from the text field
        // checks whether the any of the various texts have changed
        //    and saves if they have; only one of them will have changed
        // doesn't call updateScreen method when saving
        private void saveText()
        {
            if (text.Modified == true)
            {
                course.CurrentScreen.Text = text.Rtf;
                saveCourse(false);
                text.Modified = false;
            }
            if (answer1.Modified == true)
            {
                course.CurrentScreen.Answer1 = answer1.Text;
                saveCourse(false);
                answer1.Modified = false;
            }
            if (answer2.Modified == true)
            {
                course.CurrentScreen.Answer2 = answer2.Text;
                saveCourse(false);
                answer2.Modified = false;
            }
            if (answer3.Modified == true)
            {
                course.CurrentScreen.Answer3 = answer3.Text;
                saveCourse(false);
                answer3.Modified = false;
            }
            if (answer4.Modified == true)
            {
                course.CurrentScreen.Answer4 = answer4.Text;
                saveCourse(false);
                answer4.Modified = false;
            }
        }
 
        // handler for Back button clicked
        private void back_LinkClicked(object sender, EventArgs e)
        {
            working(true);
            saveText();
            if (course.Back())
            {
                updateScreen();
                backButton.Focus();
            }
            working(false);
        }

        // handler for Next button clicked
        private void button1_Click(object sender, EventArgs e)
        {
            working(true);
            saveText();
            // in student view, next depends on correct answer
            course.Next(choice);
            updateScreen();
            nextButton.Focus();
            working(false);
        }

        // handler for key presses on Next button
        private void nextButton_KeyDown(object sender, KeyEventArgs e)
        {
            working(true);
            navigation(e);
            nextButton.Focus();
            working(false);
        }

        // handler for key presses on Back button
        private void backButton_KeyDown(object sender, KeyEventArgs e)
        {
            working(true);
            navigation(e);
            backButton.Focus();
            working(false);
        }

        // generalized method for handling navigation depending on key press
        // alt-left goes back, alt-right goes forward
        // alt-up goes to previous session, alt-down goes to next session
        private bool navigation(KeyEventArgs e)
        {
            bool changed = false;
            if (e.Alt && e.KeyCode == Keys.Left){
                working(true);
                saveText();
                course.Back();
                changed = true;
            }
            if (e.Alt && e.KeyCode == Keys.Right){
                working(true);
                saveText();
                course.Next(choice);
                changed = true;
            }
            if (e.Alt && e.KeyCode == Keys.Up){
                working(true);
                saveText();
                if (course.Up())
                    changed = true;
            }
            if (e.Alt && e.KeyCode == Keys.Down){
                working(true);
                saveText();
                if (course.Down())
                    changed = true;
            }
            if (changed){
                updateScreen();
                working(false);
            }
            return changed;
        }

        // in student view, entering a text field bounces the focus
        //    out to the Next button
        private void textfieldEnter()
        {
            if (course.View == View.student)
                nextButton.Focus();
        }
        private void text_Enter(object sender, EventArgs e)
        {
            textfieldEnter();
        }
        private void answer1_Enter(object sender, EventArgs e)
        {
            textfieldEnter();
        }
        private void answer2_Enter(object sender, EventArgs e)
        {
            textfieldEnter();
        }
        private void answer3_Enter(object sender, EventArgs e)
        {
            textfieldEnter();
        }
        private void answer4_Enter(object sender, EventArgs e)
        {
            textfieldEnter();
        }

        // handles key press for style setting and navigation in text fields
        private void keyDown(KeyEventArgs e)
        {
            if (e.Control){
                working(true);
                saveText();
                switch (e.KeyValue){
                    case (int)Keys.U: fontStyle(FontStyle.Underline); e.Handled = true; e.SuppressKeyPress = true; break;
                    case (int)Keys.B: fontStyle(FontStyle.Bold); e.Handled = true; e.SuppressKeyPress = true; break;
                    case (int)Keys.I: fontStyle(FontStyle.Italic); e.Handled = true; e.SuppressKeyPress = true; break;
                    case (int)Keys.L: toggleBullet(); e.Handled = true; e.SuppressKeyPress = true; break;
                    case (int)Keys.N: fontStyle(FontStyle.Regular); e.Handled = true; e.SuppressKeyPress = true; break;
                    case (int)Keys.V: pasteText(); e.Handled = true; e.SuppressKeyPress = true; break;
                }
                working(false);
            }
            navigation(e);
        }
        private void text_KeyDown(object sender, KeyEventArgs e)
        {
            keyDown(e);
        }
        private void answer1_KeyDown(object sender, KeyEventArgs e)
        {
            keyDown(e);
        }
        private void answer2_KeyDown(object sender, KeyEventArgs e)
        {
            keyDown(e);
        }
        private void answer3_KeyDown(object sender, KeyEventArgs e)
        {
            keyDown(e);
        }
        private void answer4_KeyDown(object sender, KeyEventArgs e)
        {
            keyDown(e);
        }

        // when leaving answer text fields on question panel,
        //    save text if it has changed
        private void answer1_Leave(object sender, EventArgs e)
        {
            working(true);
            saveText();
            working(false);
        }
        private void answer2_Leave(object sender, EventArgs e)
        {
            working(true);
            saveText();
            working(false);
        }
        private void answer3_Leave(object sender, EventArgs e)
        {
            working(true);
            saveText();
            working(false);
        }
        private void answer4_Leave(object sender, EventArgs e)
        {
            working(true);
            saveText();
            working(false);
        }

        // common handler for when user selects an answer
        private void radioButton_checkedChanged(int n)
        {
            working(true);
            saveText();
            choice = n;
            if (course.View != View.student)
            { // in authoring (slide) view, save this as the correct answer
                course.CurrentScreen.Correct = n;
                saveCourse();
            }
            else
            { // in student view, treat it like a student choice
                updateScreen();
            }
            working(false);
        }

        // handlers for radio button checked on question panel
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            radioButton_checkedChanged(1);
        }
        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            radioButton_checkedChanged(2);
        }
        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            radioButton_checkedChanged(3);
        }
        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            radioButton_checkedChanged(4);
        }

        // always bounce focus to Next button when radio buttons clicked
        private void radioButton1_Click(object sender, EventArgs e)
        {
            nextButton.Focus();
        }
        private void radioButton2_Click(object sender, EventArgs e)
        {
            nextButton.Focus();
        }
        private void radioButton3_Click(object sender, EventArgs e)
        {
            nextButton.Focus();
        }
        private void radioButton4_Click(object sender, EventArgs e)
        {
            nextButton.Focus();
        }

        // in student view, clicking on answer text selects the answer
        private void answer_Click(int n)
        {
            if (course.View == View.student)
                radioButton_checkedChanged(n);
        }
        private void answer1_Click(object sender, EventArgs e)
        {
            answer_Click(1);
        }
        private void answer2_Click(object sender, EventArgs e)
        {
            answer_Click(2);
        }
        private void answer3_Click(object sender, EventArgs e)
        {
            answer_Click(3);
        }
        private void answer4_Click(object sender, EventArgs e)
        {
            answer_Click(4);
        }

        // ***** SESSION VIEW *****

        // when the user clicks on a different session in the session list,
        //     set the navigation state and update the screen 
        //     (sets new text in the rename session text box)
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            working(true);
            int i = (sender as ListBox).SelectedIndex;
            if (i >= 0)
            {
                course.Session = i;
                updateScreen();
            }
            working(false);
        }

        // in session view, when the user types in the text box to rename the session,
        //    updates the list box items and saves the new session title
        private void rename_TextChanged(object sender, EventArgs e)
        {
            working(true);
            if (listBox.Items.Count > 0)
            { // in case it's called before list box is populated
                string text = (sender as TextBox).Text;
                course.CurrentSession.Title = text;
                updateScreen();
            }
            working(false);
        }

        // Open button clicked in session view
        // switches to slide view for the currently selected session
        private void open_Click(object sender, EventArgs e)
        {
            working(true);
            course.View = View.slide;
            updateScreen();
            working(false);
        }

        // in session view, "Up" button is clicked to move current session up in list of sessions
        private void up_Click(object sender, EventArgs e)
        {
            working(true);
            if (listBox.SelectedIndex > 0)
            {
                // insert before previous item
                course.SessionMove(Course.MoveDirection.up);
                updateScreen(); // to update list box
                saveCourse();
            }
            working(false);
        }

        // in session view, "Down" button is clicked to move session down in list of sessions
        private void down_Click(object sender, EventArgs e)
        {
            working(true);
            if (listBox.SelectedIndex < listBox.Items.Count - 1)
            {
                course.SessionMove(Course.MoveDirection.down);
                updateScreen(); // to update list box
                saveCourse();
            }
            working(false);
        }

        // in session view, "Add" button is clicked to add an empty session after the current one
        private void add_Click(object sender, EventArgs e)
        { // add after
            working(true);
            course.InsertSession(Course.InsertLocation.after);
            updateScreen(); // to update list box
            saveCourse();
            working(false);
        }

        // in session view, "Delete" button is clicked to delete the current session
        private void delete_Click(object sender, EventArgs e)
        {
            working(true);
            course.DeleteSession();
            updateScreen();
            saveCourse();
            working(false);
        }
   
        // handler for Copy button in session view
        // serialization of session is copied to the clipboard
        private void copy_Click(object sender, EventArgs e)
        {
            working(true);
            Clipboard.SetData(DataFormats.Serializable, course.CurrentSession);
            working(false);
        }

        // handler for Cut button in session view
        // serialization of session is copied to the clipboard and session is deleted
        private void cut_Click(object sender, EventArgs e)
        {
            working(true);
            Clipboard.SetData(DataFormats.Serializable, course.CurrentSession);
            course.DeleteSession();
            updateScreen();
            saveCourse();
            working(false);
        }

        // handler for Paste button in session view
        // this replaces the current session with the serialization from the clipboard
        // it works when copying sessions from other courses but may take some time to
        //     copy images and sound clips
        private void paste_Click(object sender, EventArgs e)
        {
            working(true);
            if (course.PasteSession())
            {
                saveCourse();
                updateScreen();
            }
            working(false);
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            helpShow(!help.Visible);
        }
        private void helpShow(bool show){
            Size siz;
            Point loc;
            if (show) {
                if (WindowState == FormWindowState.Normal) {
                    loc = Location;
                    loc.X = 20;
                    this.Location = loc;
                    siz = Size;
                    siz.Width = 980;
                    Size = siz;
                }
                help.Visible = true;
                course.Help = true; // persist the state
            }
            else {
                help.Visible = false;
                course.Help = false; // persist the state
                if (WindowState == FormWindowState.Normal){
                    siz = Size;
                    siz.Width = 800;
                    Size = siz;
                }
            }
        }
    }
}