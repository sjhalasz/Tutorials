using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.Drawing;
using System.IO;

// The Image class encapsulates image handling for a screen.
// One instance of this class is created in the Screen class.
// It supports both xml serialization for saving the course.xml file
// and binary serialization for copy and past via the clipboard.
// Only the file name is serialized, not the contents of the image file.

namespace Learning
{
    [Serializable]
    public class Image : ISerializable
    {
        // the name of the image file
        [XmlElement("imagefile")]
        public string ImageFile = "";
        // the course directory, used for copying the image file from the
        // source to the target directory when copying and pasting via the clipboard
        [XmlElement("directory")]
        public string Directory;
        // constants that need to be conformed in other classes if changed
        private const string defaultname = "noimage.bmp";
        // image height and width need to be conformed with Slide class
        // They are used to resize the image when pasting with the default "fit" option
        private const int imageHeight = 435;
        private const int imageWidth = 370;
        private const string imagedir = "images";
        private const string imagename = "image";
        // The image hash is used to prevent tampering / vandalism.
        private byte[] imagehash;
        [XmlElement("imagehash")]
        public byte[] ImageHash { get { return imagehash; } set { imagehash = value; } }

        public Image() { }
        // Binary serialization functions.
        public Image(SerializationInfo info, StreamingContext ctxt){
            ImageFile = (string)info.GetValue("imagefile", typeof(string));
            imagehash = (byte[])info.GetValue("imagehash", typeof(byte[]));
            Directory = (string)info.GetValue("directory", typeof(string));
        }
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt){
            info.AddValue("imagefile", ImageFile);
            info.AddValue("imagehash", imagehash);
            info.AddValue("directory", Directory);
        }
        // Property to get and set the image bitmap.
        // Hashes are not checked in authoring version, only in student version.
        [XmlIgnore]
        public Bitmap BitMap{
            get{
                if (ImageFile.Length > 0){
                    string file = Directory + "/" + imagedir + "/" + ImageFile;
                    FileStream fs = new FileStream(file, FileMode.Open,FileAccess.Read);
                    Bitmap bm = new Bitmap(fs);
                    fs.Close();
                    return bm;
                }
                else
                    return new Bitmap(defaultname);
            }
            set{
                if (value != null)
                    saveImage(resizeImage(value, imageWidth, imageHeight));
                else
                    ImageFile = "";
            }
        }
        // Method to set the image bitmap depending on placement option 
        public void SetImage(Bitmap image, Course.ImagePlacement place) {
            switch (place) {
                case Course.ImagePlacement.fit:
                    saveImage(resizeImage(image,imageWidth,imageHeight));
                    break;
                default:
                    saveImage(clipImage(image,imageWidth,imageHeight,place));
                    break;
            }
        }
        // Method to resize image while retaining aspect ratio.
        // Used to fit image in picture control area.
        private Bitmap resizeImage(Bitmap bm, int w, int h)
        {
            if (((double)w / (double)h) < (double)bm.Width / (double)bm.Height)
                h = (int)(bm.Height * ((double)w / (double)bm.Width));
            else
                w = (int)(bm.Width * ((double)h / (double)bm.Height));
            return (Bitmap)bm.GetThumbnailImage(w, h, null, IntPtr.Zero);
        }
        // Method to clip the image without resizing, depending on which corner it is aligned to.
        private Bitmap clipImage(Bitmap bm, int w, int h, Course.ImagePlacement place){
            Bitmap result = new Bitmap(w, h);
            // first rotate / flip depending on ultimate placement
            switch (place) {
                case Course.ImagePlacement.lowerLeft:
                    bm.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    break;
                case Course.ImagePlacement.lowerRight:
                    bm.RotateFlip(RotateFlipType.RotateNoneFlipXY);
                    break;
                case Course.ImagePlacement.upperRight:
                    bm.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    break;
            }
            // unscaled clip, always to upper left corner
            using (Graphics g = Graphics.FromImage((System.Drawing.Image)result))
                g.DrawImageUnscaled(bm,0, 0, w, h);
            bm.Dispose();
            // do the inverse of the rotate / flip
            switch (place){
                case Course.ImagePlacement.lowerLeft:
                    result.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    break;
                case Course.ImagePlacement.lowerRight:
                    result.RotateFlip(RotateFlipType.RotateNoneFlipXY);
                    break;
                case Course.ImagePlacement.upperRight:
                    result.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    break;
            }
            return result;
        }
        // save an image to file in the images directory
        private void saveImage(Bitmap image){
            string dir = Directory + @"\" + imagedir;
            ImageFile = nextName(dir);
            image.Save(dir + @"\" + ImageFile);
            // hash the file to prevent tampering / vandalism
            imagehash = HashStat.ReadHash2(dir + @"\" + ImageFile);
        }
        // Method to find next available serial number file name
        private string nextName(string dir) {
            DirectoryInfo di = new DirectoryInfo(dir);
            di.Create();
            FileInfo[] files = di.GetFiles("*.bmp");
            int max = 0;
            foreach (FileInfo file in files)
                max = Math.Max(max, int.Parse(
                    Path.GetFileName(file.Name).Substring(
                    imagename.Length, 5)));
            max++;
            return imagename + max.ToString("00000") + ".bmp";
        }
        // Method used when pasting binary serialization from clipboard.
        // Copies the file from the source directory when pasting from a different directory
        public void GetFile(string dir) {
            if (dir != Directory){
                string srcdir = Directory + @"\" + imagedir;
                string tgtdir = dir + @"\" + imagedir;
                string newfile = nextName(tgtdir);
                (new FileInfo( srcdir + @"\" + ImageFile)).CopyTo(
                     tgtdir + @"\" + newfile);
                ImageFile = newfile;
                Directory = dir;
            }
        }
    }
}
