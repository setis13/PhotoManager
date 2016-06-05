using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using PhotoManager.Static;

namespace PhotoManager.Models {
    public class PhotoModel : INotifyPropertyChanged {
        private PhotoEntities _photoEntities;
        public ListCollectionView ItemsSourceView { get; set; }
        public List<FolderModel> ItemsSource;
        public ObservableCollection<Attribute> ItemsSourceAttribute { get; set; }
        public ImageSize ImageSize;

        public void Init() {
            _photoEntities = new PhotoEntities();
            if (_photoEntities.TestConnection() == false) {
                MessageBox.Show("ms sql server was not found or is not available");
                System.Windows.Forms.Application.Exit();
            }
            if (_photoEntities.DatabaseExists() == false) {
                _photoEntities.CreateDatabase();
            }
            ItemsSource = new List<FolderModel>();
            ItemsSourceView = new ListCollectionView(ItemsSource);
            ItemsSourceAttribute = _photoEntities.Attribute.ToObservableCollection();
            ImageSize = _photoEntities.ImageSize.FirstOrDefault(s => s.Width == (short)Settings.ImgWidth && s.Height == (short)Settings.ImgHeight);
            if (ImageSize == null) {
                ImageSize = new ImageSize { Width = (short)Settings.ImgWidth, Height = (short)Settings.ImgHeight };
                _photoEntities.ImageSize.AddObject(ImageSize);
                _photoEntities.SaveChanges();
            }
            OnPropertyChanged("ItemsSourceAttribute");
        }

        public void ScanRootFolder() {
            var directories1 = Directory.GetDirectories(ConfigurationManager.AppSettings["RootFolder"]);
            var directories = directories1.Where(d => {
                var dateTime = new DateTime();
                var directory = Path.GetFileName(d);
                if (directory.Length > 10) {
                    return DateTime.TryParse(directory.Substring(0, 10), out dateTime);
                } else {
                    return false;
                }
            }).ToList();
            var folders = _photoEntities.Folder.Include("Attribute").ToList();

            foreach (var folder in folders) {
                var folderModel = new FolderModel();
                folderModel.Folder = folder;
                folderModel.Folders = ItemsSourceAttribute;
                folderModel.Images = new ObservableCollection<ImageModel>();
                folderModel.Status = FolderStatus.Deleted;
                ItemsSource.Insert(0, folderModel);
            }

            var newFolder = false;
            foreach (var directory in directories) {
                var folderModel = ItemsSource.FirstOrDefault(f => f.Folder.Name == Path.GetFileName(directory));
                if (folderModel == null) {
                    newFolder = true;
                    var folder = new Folder();
                    folder.Name = directory.Replace(ConfigurationManager.AppSettings["RootFolder"] + "\\", "");
                    _photoEntities.Folder.AddObject(folder);
                    folderModel = new FolderModel();
                    folderModel.Folder = folder;
                    folderModel.Folders = ItemsSourceAttribute;
                    folderModel.Images = new ObservableCollection<ImageModel>();
                    folderModel.Status = FolderStatus.Created;
                    ItemsSource.Insert(0, folderModel);
                } else {
                    folderModel.Status = FolderStatus.Normal;
                }
            }

            if (newFolder) {
                _photoEntities.SaveChanges();
            }
            OnPropertyChanged("ItemsSourceView");
        }

        public void LoadFolderInfo() {
            foreach (FolderModel model in ItemsSource) {
                StaticClass.Wait();
                if (model.Status != FolderStatus.Deleted)
                    model.Load();
            }
        }

        public void Filter(IList selectedItems) {
            if (selectedItems.Count != 0) {
                var items = selectedItems.Cast<Attribute>().Select(a => a.AttrId);
                ItemsSourceView.Filter = new Predicate<object>(folderModel => ((FolderModel)folderModel).Folder.Attribute.Any(a => items.Contains(a.AttrId)));
            } else {
                ItemsSourceView.Filter = null;
            }
        }

        public List<ImageData> GetImagesData(short folderId) {
            return _photoEntities.ImageData.Where(d => d.FolderId == folderId && d.SizeId == ImageSize.SizeId).ToList();
        }

        public void DeleteImageData(List<ImageData> imagesData) {
            foreach (var imageData in imagesData) {
                _photoEntities.ImageData.DeleteObject(imageData);
            }
            _photoEntities.SaveChanges();
        }

        public void SaveImage(string file, long size, BitmapImage bitmapImage, short folderId) {
            byte[] data;
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
            using (var ms = new MemoryStream()) {
                encoder.Save(ms);
                data = ms.ToArray();
            }

            var imageData = new ImageData();
            imageData.FolderId = folderId;
            imageData.SizeId = ImageSize.SizeId;
            imageData.FileName = Path.GetFileName(file);
            imageData.Size = size;
            imageData.Data = data;
            _photoEntities.ImageData.AddObject(imageData);
        }

        public void AddAttribute(string name) {
            var attribute = new Attribute();
            attribute.Name = name;
            _photoEntities.Attribute.AddObject(attribute);
            _photoEntities.SaveChanges();
            ItemsSourceAttribute.Add(attribute);
        }

        public void Deleted(FolderModel folderModel) {
            _photoEntities.Folder.DeleteObject(folderModel.Folder);
            _photoEntities.SaveChanges();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name) {
            if (PropertyChanged != null) {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        public void SaveChanges() {
            _photoEntities.SaveChanges();
        }

        public long GetTotalSizeCache() {
            return Enumerable.Sum(_photoEntities.ImageData, imageData => imageData.Data.Length);
        }

        public void ClearCache() {
            _photoEntities.ExecuteStoreCommand("DELETE FROM ImageData");
            _photoEntities.ExecuteStoreCommand("DELETE FROM ImageSize");

            foreach (var imageData in _photoEntities.ImageData) {
                _photoEntities.ObjectStateManager.ChangeObjectState(imageData, EntityState.Detached);
            }
            foreach (var imageSize in _photoEntities.ImageSize) {
                _photoEntities.ObjectStateManager.ChangeObjectState(imageSize, EntityState.Detached);
            }
        }
    }
}
