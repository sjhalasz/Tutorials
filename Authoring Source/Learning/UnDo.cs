using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

// The UnDo class serializes and deserializes the control information for the undo/redo feature.
// It's used by this authoring program but not by the student version.

namespace Learning
{
    [XmlRoot]
    public class UnDo{
        // xml serialization functions
        private XmlSerializer serializer = new XmlSerializer(typeof(UnDo));
        public void Serialize(string file){
            TextWriter writer = new StreamWriter(file);
            serializer.Serialize(writer, this);
            writer.Close();
        }
        public UnDo Deserialize(string file){
            StreamReader reader = new StreamReader(file);
            UnDo control = (UnDo)serializer.Deserialize(reader);
            reader.Close();
            return control;
        }
        // The undo counter is the serial file numbering of the last files written to the undo directory
        private int undoctr = -1;
        [XmlElement]
        public int UnDoCtr{
            get { return undoctr; }
            set { undoctr = value; }
        }
        // The undo position points to the set of files from the undo directory
        // that are currently visible after one or more undo operations.
        private int undopos = -1;
        [XmlElement]
        public int UnDoPos{
            get { return undopos; }
            set { undopos = value; }
        }
    }
}
