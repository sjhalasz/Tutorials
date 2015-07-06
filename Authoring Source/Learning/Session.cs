using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Drawing;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

// The Session class handles a series of screens on one topic.
// The Course class maintains a list of Session instances.
// The Session class is serializable both to xml and to binary.
// It is saved as xml as part of the course.xml file.
// Binary serialization is for copy and paste.

namespace Learning
{
    [Serializable]
    public class Session : ISerializable
    {
        // The Session title appears at the top left of a slide.
        private string title = "NewSession0";
        // The directory is copied here and passed to Screen instances so that when pasting a session,
        // the image and sound files can be copied from the source to target directories
        // if necessary.
        private string directory;
        // A session consists of a list of Screen instances.
        private List<Screen> screens = new List<Screen>();
        // Constructors
        public Session() {}
        public Session(string t, string d) {
            title = t;
            Directory = d;
            screens.Add(new Screen(directory));
        }
        // Binary serialization methods
        public Session(SerializationInfo info, StreamingContext ctxt){
            title = (string)info.GetValue("title", typeof(string));
            screens = (List<Screen>)info.GetValue("screens", typeof(List<Screen>));
        }
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt){
            info.AddValue("title", title);
            info.AddValue("screens", screens);
        }
        // Public properties for xml serialization, etc.
        [XmlElement("title")]
        public string Title { get { return title; } set { title = value; } }
        [XmlElement("screen")]
        public List<Screen> Screens { get { return screens; } set { screens = value; } }
        // When the directory is set, it is propagated to the Screen instances
        // so that, when copied to the clipboard as binary serializations, 
        // the image and sound files, which are not part of the serialization,
        // can be copied from the source to target directories if necessary.
        [XmlIgnore]
        public string Directory { 
            get { return directory; }
            set {
                directory = value;
                foreach (Screen s in screens)
                    s.Directory = directory;
            }
        }
        // Property that returns the number of screens.
        [XmlIgnore]
        public int ScreenCount { get { return screens.Count; } }
    }
}
