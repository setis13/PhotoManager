using System;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace PhotoManager.Models
{

    public class ImageModel : INotifyPropertyChanged
    {
        /// <summary>
        /// A name for the image, not the file name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A description for the image.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Full path such as c:\path\to\image.png
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The image file name such as image.png
        /// </summary>
        public string FileName { get; set; }

        public string FileRename { get; set; }

        /// <summary>
        /// The file name extension: bmp, gif, jpg, png, tiff, etc...
        /// </summary>
        public string Extension { get; set; }

        /// <summary>
        /// The image height
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// The image width.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// The file size of the image.
        /// </summary>
        public long Size { get; set; }

        public DateTime DateTimeShot { get; set; }

        public BitmapImage BitmapImage { get; set; }

        public string Camera { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name) {
            if (PropertyChanged != null) {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
