using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Media;

// The Screen class represents one slide.
// The Session class maintains a list of Screen instances.
// The Screen class is serialized as xml as part of the course.xml file
// and is also serialized as binary for copy and paste via the clipboard.
// The image and sound files are not serialized, only the file names.

namespace Learning
{
    [Serializable]
    public class Screen : ISerializable
    {
        // The directory is copied to the Screen object so that
        // image and sound files can be copied from the source to
        // target directory after copy and paste via the clipboard.
        private string directory;
        // The ImageInstance is an instance of the Image class that handles the image for the slide. 
        [XmlElement("image")]
        public Image ImageInstance = new Image();
        // This is the name of the .wav file that contains the sound clip for this slide.
        [XmlElement("soundfile")]
        public string soundfile = null;
        // These constants are redundantly specified here and in the Course class
        private const string sounddir = "sounds";
        private const string soundname = "sound";
        // The hash of the sound file is checked when the sound clip is played.
        // It helps to prevent tampering / vandalism.
        [XmlElement("soundhash")]
        public byte[] soundhash;
        // The caption field is no longer used.
        private string caption = "...";
        // The main text field, saved in RTF.
        private string text = ""; 
        // The question field is true if the question panel is displayed, otherwise false.
        private bool question = false;
        // These are the texts for the answers in the question panel.
        private string answer1 = "...";
        private string answer2 = "...";
        private string answer3 = "...";
        private string answer4 = "...";
        // The number of the correct answer (1, 2, 3, 4)
        private int correct = 0;  // correct answer
        // The hash of the text of the correct answer.
        // This isn't used.
        private int correctHash;  // encrypted correct answer

        public Screen() { }
        public Screen(string d) { Directory = d; }
        // Binary serialization methods
        public Screen(SerializationInfo info, StreamingContext ctxt) {
            directory = (string)info.GetValue("directory", typeof(string));
            ImageInstance = (Image)info.GetValue("image", typeof(Image));
            caption = (string)info.GetValue("caption", typeof(string));
            text = (string)info.GetValue("text", typeof(string));
            question = (bool)info.GetValue("question", typeof(bool));
            answer1 = (string)info.GetValue("answer1", typeof(string));
            answer2 = (string)info.GetValue("answer2", typeof(string));
            answer3 = (string)info.GetValue("answer3", typeof(string));
            answer4 = (string)info.GetValue("answer4", typeof(string));
            correct = (int)info.GetValue("correct", typeof(int));
            correctHash = (int)info.GetValue("correctHash", typeof(int));
        }
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt) {
            info.AddValue("directory", directory);
            info.AddValue("image", ImageInstance);
            info.AddValue("caption", caption);
            info.AddValue("text", text);
            info.AddValue("question", question);
            info.AddValue("answer1", answer1);
            info.AddValue("answer2", answer2);
            info.AddValue("answer3", answer3);
            info.AddValue("answer4", answer4);
            info.AddValue("correct", correct);
            info.AddValue("correctHash", correctHash);
        }
        // Property to get and set the image bitmap of the Image instance.
        [XmlIgnore]
        public Bitmap Image{ get{ return ImageInstance.BitMap;} set{ ImageInstance.BitMap = value; } }
        // Property to get and set the directory.
        // It's propagated to the Image instance for copy and past via the clipboard.
        [XmlIgnore]
        public string Directory { 
            get { return directory; } 
            set { 
                directory = value;
                ImageInstance.Directory = value;
            } 
        }
        // This isn't used.
        [XmlElement("caption")]
        public string Caption { get {return caption ;} set {caption = value; } }
        // Property to get and set the main text.
        [XmlElement("text")]
        public string Text { get { return text;} set {text = value; } }
        // Property to get and set the toggle of the question panel on and off.
        [XmlElement("question")]
        public bool Question { get { return question;} set {question = value; } }
        // Properties to get and set the answer texts.
        [XmlElement("answer1")]
        public string Answer1 { get { return answer1; } set { answer1 = value; } }
        [XmlElement("answer2")]
        public string Answer2 { get { return answer2; } set { answer2 = value; } }
        [XmlElement("answer3")]
        public string Answer3 { get { return answer3; } set { answer3 = value; } }
        [XmlElement("answer4")]
        public string Answer4 { get { return answer4; } set { answer4 = value; } }
        // This method gets the answer text by number.
        public string Answer(int n){
            switch (n) {
                case 1: return answer1;
                case 2: return answer2;
                case 3: return answer3;
                case 4: return answer4;
                default: return "";
            }
        }
        // This method sets the answer text by number and updates the hash of the correct answer.
        public void Answer(int n, string s){
            switch (n){
                case 1: answer1 = s; break;
                case 2: answer2 = s; break;
                case 3: answer3 = s; break;
                case 4: answer4 = s; break;
            }
            if(n == correct)
                switch (n) { 
                    case 1: correctHash = answer1.GetHashCode(); break;
                    case 2: correctHash = answer2.GetHashCode(); break;
                    case 3: correctHash = answer3.GetHashCode(); break;
                    case 4: correctHash = answer4.GetHashCode(); break;
                }
        }
        // Property to get and set the hash of the correct answer text.
        // It also conforms the number field for the correct answer.
        [XmlElement("correcthash")]
        public int CorrectHash { 
            get { return correctHash; } 
            set { 
                correctHash = value;
                if(correctHash == answer1.GetHashCode())
                        correct = 1;
                else if (correctHash == answer2.GetHashCode())
                        correct = 2;
                else if (correctHash == answer3.GetHashCode())
                        correct = 3;
                else if(correctHash == answer4.GetHashCode())
                        correct = 4;
            } 
        }
        // This property gets and sets the correct answer number and conforms the hash of the
        // correct answer text.
        [XmlIgnore]
        public int Correct { 
            get { return correct; } 
            set { 
                correct = value;
                switch (correct) { 
                    case 1: correctHash = answer1.GetHashCode(); break;
                    case 2: correctHash = answer2.GetHashCode(); break;
                    case 3: correctHash = answer3.GetHashCode(); break;
                    case 4: correctHash = answer4.GetHashCode(); break;
                }
            } 
        }
        [XmlIgnore]
        public string SoundFile {
            get {
                if (soundfile == null) return null;
                string file = directory + @"\" + sounddir + @"\" + soundfile;
                return directory + @"\" + sounddir + @"\" + soundfile; 
            }
            set { saveSound(value); }
        }
        [XmlIgnore]
        public Stream SoundAudio {
            set { saveSound(value); }
        }
        // Method to save a sound file.
        // The file is copied to the sounds directory with a serial name
        // and that name is saved with the Screen instance.
        private void saveSound(string file){
            if (file.Length > 0){
                string dir = directory + @"\" + sounddir;
                soundfile = nextName(dir);
                (new FileInfo(file)).CopyTo(dir + @"\" + soundfile);
                // The hash of the sound file prevents tampering / vandalism.
                soundhash = HashStat.ReadHash2(dir + @"\" + soundfile);
            }
            else {
                soundfile = null;
            }
        }
        // Method to save an audio stream
        private void saveSound(Stream audio) {
                string dir = directory + @"\" + sounddir;
                soundfile = nextName(dir);
                FileStream fs = new FileStream(dir + @"\" + soundfile,FileMode.CreateNew);
                readWriteStream(audio, fs);
                fs.Close();
                // The hash of the sound file prevents tampering / vandalism.
                soundhash = HashStat.ReadHash2(dir + @"\" + soundfile);
            }
        // Method to get the next available serial number sound file name
        private string nextName(string dir) {
            DirectoryInfo di = new DirectoryInfo(dir);
            di.Create();
            FileInfo[] files = di.GetFiles("*.wav");
            int max = 0;
            foreach (FileInfo f in files)
                max = Math.Max(max, int.Parse(
                    Path.GetFileName(f.Name).Substring(
                    soundname.Length, 5)));
            max++;
            return soundname + max.ToString("00000") + ".wav";
        }
        // When pasting a Screen instance from the clipboard,
        // this method is called to copy the sound file from the source directory.
        public void GetSoundFile(string dir){
            if (dir != Directory){
                string srcdir = Directory + @"\" + sounddir;
                string tgtdir = dir + @"\" + sounddir;
                string newfile = nextName(tgtdir);
                (new FileInfo(srcdir + @"\" + soundfile)).CopyTo(
                    tgtdir + @"\" + newfile);
                soundfile = newfile;
                Directory = dir;
            }
        }
        // utility to read from one stream and write to another
        // readStream is the stream you need to read
        // writeStream is the stream you want to write to
        private void readWriteStream(Stream readStream, Stream writeStream)
        {
            int Length = 256;
            Byte[] buffer = new Byte[Length];
            int bytesRead = readStream.Read(buffer, 0, Length);
            // write the required bytes
            while (bytesRead > 0)
            {
                writeStream.Write(buffer, 0, bytesRead);
                bytesRead = readStream.Read(buffer, 0, Length);
            }
            readStream.Close();
            writeStream.Close();
        }
    }
}
