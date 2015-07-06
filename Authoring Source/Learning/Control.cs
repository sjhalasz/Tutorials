using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Drawing;
using System.Security;
using System.Security.AccessControl;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Windows;

// The Control class persists navigation information (current session, current screen)
// For authoring, it is serialized as an xml file named control.xml.
// For students, the information is saved in the registry.
// For simplicity, this class is the same for both versions
// and the file and registry operations are done redundantly.
// In fact, the serialization of this class can be removed completely
// if the view setting is saved in the registry.

namespace Learning
{
    [XmlRoot]
    public class Control
    {
        // instance of registry key, it's set at first use
        private RegistryKey registryKey = null;
        // guid keeps navigation information separate by course
        [XmlElement("guid")]
        public string Guid;

        public Control() {
        }

        // xml seralize / deserialize methods
        private XmlSerializer serializer = new XmlSerializer(typeof(Control));
        public void Serialize(string file){
            TextWriter writer = new StreamWriter(file);
            serializer.Serialize(writer, this);
            writer.Close();
        }
        public Control Deserialize(string file)
        {
            StreamReader reader = new StreamReader(file);
            Control control = (Control)serializer.Deserialize(reader);
            reader.Close();
            return control;
        }
        // the window state, private field and public property
        private FormWindowState windowState;
        [XmlElement("windowstate")]
        public FormWindowState WindowState {
            get{
                registry(); // retrieves fields from registry
                return windowState;
            }
            set{
                registry();
                windowState = value;
                // don't let the user persist the minimized state
                if (windowState == FormWindowState.Minimized)
                    windowState = FormWindowState.Normal; 
                registry("WindowState", (int)windowState); // sets the registry
            }
        }
        // private and public form location property
        private Point location;
        [XmlElement("location")]
        public Point Location { 
            get {
                registry();
                return location; 
            } 
            set {
                registry();
                if (value.X >= 0) { // gets bad value when maximized
                    location = value;
                    registry("LocationX", value.X);
                    registry("LocationY", value.Y);
                }
            } 
        }
        // pointer to the current session
        private int session = 0;
        [XmlElement("session")]
        public int Session { 
            get {
                registry();
                return session;
            } 
            set { 
                session = value;
                registry("Session", value);
            }
        }
        // pointer to the current screen
        private int screen = 0;
        [XmlElement("screen")]
        public int Screen { 
            get {
                registry();
                return screen; 
            } 
            set { 
                screen = value;
                registry("Screen", value);
            } 
        }
        // the current view, saved in control file only, not in the registry
        private View view;
        [XmlElement("view")]
        public View View { get { return view; } set { view = value; } }
        private int help;
        [XmlIgnore]
        public int Help {
            get{
                registry();
                return help;
            }
            set{
                help = value;
                registry("Help", value);
            }
        }
        // gets an integer item from the registry
        private int registryInt(string item){
            registry();
            object r = registryKey.GetValue(item);
            if (r != null) return (int)r;
            switch (item){
                case "LocationX": return 0;
                case "LocationY": return 0;
                case "Session": return 0;
                case "Screen": return 0;
                case "WindowState": return (int)FormWindowState.Normal;
                case "Help": return 0;
                default: throw new ApplicationException("Unknown registry item: " + item);
            }
        }
        // sets an item value in the registry
        private void registry(string item, object value){
            registry();
            registryKey.SetValue(item, value);
        }
        // creates the registry key instance if needed and gets fields from the registry
        private void registry() {
            if (registryKey == null){
                string s = @"Software\Cyryx College Maldives\" + Guid.ToString();
                registryKey = Registry.CurrentUser.OpenSubKey(s, true);
                if (registryKey == null){
                    registryKey = Registry.CurrentUser.CreateSubKey(s);
                    registryKey = Registry.CurrentUser.OpenSubKey(s, true);
                }
                location = new Point(registryInt("LocationX"), registryInt("LocationY"));
                windowState = (FormWindowState) registryInt("WindowState");
                session = registryInt("Session");
                screen = registryInt("Screen");
                help = registryInt("Help");
            }
        }
    }
}
