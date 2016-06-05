using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PhotoManager.Static;

namespace PhotoManager.Models {
    public class FolderModel : INotifyPropertyChanged {
        public FolderStatus Status { get; set; }
        public ObservableCollection<Attribute> Folders { get; set; }
        public Folder Folder { get; set; }
        public int? CountVideo { get; set; }
        public int? CountPhoto { get; set; }
        public long? Size { get; set; }
        public ObservableCollection<ImageModel> Images { get; set; }
        public DateTime Date {
            get {
                DateTime dateTime;
                if (DateTime.TryParse(Folder.Name.Substring(0, 10), out dateTime)) {
                    return dateTime;
                } else {
                    return DateTime.MinValue;
                }
            }
            set {
                try {
                    var newFullName = value.ToString("yyyy-MM-dd") + " " + Name;
                    Directory.Move(
                        Path.Combine(ConfigurationManager.AppSettings["RootFolder"], Folder.Name),
                        Path.Combine(ConfigurationManager.AppSettings["RootFolder"], newFullName));
                    Folder.Name = newFullName;
                } catch (Exception exception) {
                    MessageBox.Show(exception.Message);
                }
            }
        }

        public string Name {
            get { return Folder.Name.Substring(11); }
            set {
                try {
                    var newFullName = Date.ToString("yyyy-MM-dd") + " " + value;
                    Directory.Move(
                        Path.Combine(ConfigurationManager.AppSettings["RootFolder"], Folder.Name),
                        Path.Combine(ConfigurationManager.AppSettings["RootFolder"], newFullName));
                    Folder.Name = newFullName;
                } catch (Exception exception) {
                    MessageBox.Show(exception.Message);
                }

            }
        }

        public void Load() {
            var path = Path.Combine(ConfigurationManager.AppSettings["RootFolder"], Folder.Name);
            if (Directory.Exists(path) == true) {
                Size = CountPhoto = CountVideo = 0;
                var files = GetFiles(path);
                foreach (var file in files) {
                    var fileInfo = new FileInfo(file);
                    Size += fileInfo.Length;
                    if (Settings.PhotoExtensions.Contains(fileInfo.Extension.ToLower())) {
                        CountPhoto++;
                    }
                    if (Settings.VideoExtensions.Contains(fileInfo.Extension.ToLower())) {
                        CountVideo++;
                    }
                }
                OnPropertyChanged("CountVideo");
                OnPropertyChanged("CountPhoto");
                OnPropertyChanged("Size");
            } else {
                Status = FolderStatus.Deleted;
                OnPropertyChanged("Status");
            }
        }

        private static IEnumerable<string> GetFiles(string path) {
            var files = Directory.GetFiles(path).ToList();
            foreach (var directory in Directory.GetDirectories(path)) {
                files.AddRange(GetFiles(directory));
            }
            return files;
        }

        public void ScanFolder(Dispatcher dispatcher, PhotoModel photoModel) {
            StaticClass.CounterReset();
            var newFullName = Date.ToString("yyyy-MM-dd") + " " + Name;
            var fullPath = Path.Combine(ConfigurationManager.AppSettings["RootFolder"], newFullName);
            var files = Directory.GetFiles(fullPath);
            var imagesData = photoModel.GetImagesData(Folder.FolderId);
            for (int i = 0; i < files.Count(); i++) {
                var file = files[i].ToLower();
                if (Images.Any(img => img.Path == file)) {
                    continue;
                }
                Thread.Sleep(1);
                var extension = Path.GetExtension(file).ToLower();
                //if (_extensions.Contains(extension) == false) {
                //    continue;
                //}
                if (extension == ".jpg") {
                    var bitmapFrame = BitmapFrame.Create(new Uri(file), BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                    DateTime dt = DateTime.Now;
                    if (((BitmapMetadata)bitmapFrame.Metadata).DateTaken != null) {
                        dt = DateTime.Parse(((BitmapMetadata)bitmapFrame.Metadata).DateTaken);
                    }
                    var camera = ((BitmapMetadata)bitmapFrame.Metadata).CameraManufacturer;
                    if (camera != null) {
                        camera = camera.Split(' ').First();
                    }
                    var fi = new FileInfo(file);
                    var imageModel = new ImageModel {
                        Path = file,
                        FileName = Path.GetFileName(file),
                        Extension = Path.GetExtension(file),
                        DateTimeShot = dt,
                        Size = fi.Length,
                        Width = bitmapFrame.PixelWidth,
                        Height = bitmapFrame.PixelHeight,
                        Camera = camera
                    };

                    var imageData = imagesData.FirstOrDefault(d => d.FileName == Path.GetFileName(file) && d.Size == fi.Length);
                    imagesData.Remove(imageData);

                    StaticClass.CounterLock();

                    dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, (ThreadStart)delegate {
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        var k = Math.Min((float)imageModel.Width / Settings.ImgWidth, (float)imageModel.Height / Settings.ImgHeight);
                        bitmapImage.DecodePixelWidth = Convert.ToInt32(imageModel.Width / k);
                        bitmapImage.DecodePixelHeight = Convert.ToInt32(imageModel.Height / k);
                        if (imageData != null) {
                            bitmapImage.StreamSource = new MemoryStream(imageData.Data);
                            bitmapImage.EndInit();
                        } else {
                            bitmapImage.UriSource = new Uri(file, UriKind.Absolute);
                            bitmapImage.EndInit();
                            photoModel.SaveImage(file, fi.Length, bitmapImage, Folder.FolderId);
                        }

                        imageModel.Width = bitmapImage.PixelWidth;
                        imageModel.Height = bitmapImage.PixelHeight;
                        imageModel.BitmapImage = bitmapImage;
                        Images.Add(imageModel);
                        imageModel.OnPropertyChanged("BitmapImage");

                        StaticClass.CounterRelease();
                    });
                }
            }
            StaticClass.CounterWait();
            photoModel.DeleteImageData(imagesData);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name) {
            if (PropertyChanged != null) {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public enum FolderStatus {
        Normal, Deleted, Created
    }
}
