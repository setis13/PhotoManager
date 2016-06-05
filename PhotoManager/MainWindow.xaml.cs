using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PhotoManager.Helpers;
using PhotoManager.Models;
using PhotoManager.Providers;
using PhotoManager.Static;
using PhotoManager.Windows;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;
using ListBox = System.Windows.Controls.ListBox;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;

namespace PhotoManager {

    public partial class MainWindow : Window {
        public PhotoModel PhotoModel;
        private Thread _loadThread;

        public MainWindow() {
            InitializeComponent();

            RadGridView.KeyboardCommandProvider = new CustomKeyboardCommandProvider(RadGridView);
        }

        private void LoadFolderInfo() {
            _loadThread = new Thread(() => PhotoModel.LoadFolderInfo());
            _loadThread.Start();
        }

        private void WindowSourceInitialized(object sender, EventArgs e) {
            ControlSaveHelper.LoadValue(this, "WindowState");
            ControlSaveHelper.LoadValue(this, "Top");
            ControlSaveHelper.LoadValue(this, "Left");
            ControlSaveHelper.LoadValue(this, "Width");
            ControlSaveHelper.LoadValue(this, "Height");
            //ControlSaveHelper.LoadValue(columnDefinition1, "Width");

        }

        private void WindowClosed(object sender, EventArgs e) {
            PhotoModel.SaveChanges();
            if (WindowState != WindowState.Minimized) {
                ControlSaveHelper.SaveValue(this, "WindowState");
                ControlSaveHelper.SaveValue(this, "Width");
                ControlSaveHelper.SaveValue(this, "Height");
                ControlSaveHelper.SaveValue(this, "Top");
                ControlSaveHelper.SaveValue(this, "Left");
                //ControlSaveHelper.SaveValue(columnDefinition1, "Width");
            }
            _loadThread.Abort();
            if (_thread != null) {
                _thread.Abort();
            }
        }

        private void WindowLoaded(object sender, RoutedEventArgs e) {
            PhotoModel = new PhotoModel();
            PhotoModel.Init();
            PhotoModel.ScanRootFolder();
            DataContext = PhotoModel;

            LoadFolderInfo();
            //RadGridView.ScrollIntoView(PhotoModel.ItemsSource.Last());
        }

        private void AttributeSelectionChanged(object sender, SelectionChangedEventArgs e) {
            StaticClass.Lock();
            PhotoModel.Filter(((ListBox)sender).SelectedItems);
            StaticClass.Release();
        }

        private void AddAttributeClicked(object sender, RoutedEventArgs e) {
            var window = new CustomTextWindow("Photo Manager", "Enter name of new attribute");
            if (window.ShowDialog() == true) {
                PhotoModel.AddAttribute(window.TextResult);
            }
        }

        private void CreateBackupClicked(object sender, RoutedEventArgs e) {
            var dialog = new SaveFileDialog();
            dialog.Filter = "Backup files (*.bak)|*.bak";
            dialog.FileName = String.Format("{0:yyMMdd} Photo.bak", DateTime.Now);
            dialog.InitialDirectory = "E:\\Develop\\Backup";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                try {
                    var streamReader = new StreamReader("Scripts\\CreateBackupScript.sql");
                    var backupPath = Path.Combine(Settings.BackupLocation, "Photo.bak");
                    string script = String.Format(streamReader.ReadToEnd(), backupPath);
                    streamReader.Close();
                    var sqlFileTemp = Path.Combine(Path.GetTempPath(), "CreateBackupScript.sql");
                    if (File.Exists(sqlFileTemp))
                        File.Delete(sqlFileTemp);
                    var streamWriter = new StreamWriter(sqlFileTemp);
                    streamWriter.Write(script);
                    streamWriter.Close();
                    var process = Process.Start(Settings.SqlCmdLocation, String.Format("-E -S .\\SQLEXPRESS -d Photo -i \"{0}\"", sqlFileTemp));
                    process.WaitForExit();
                    if (File.Exists(backupPath)) {
                        if (File.Exists(dialog.FileName)) {
                            File.Delete(dialog.FileName);
                        }
                        File.Move(backupPath, dialog.FileName);

                    } else {
                        MessageBox.Show("File not created!");
                    }
                } catch (Exception exception) {
                    MessageBox.Show(exception.Message);
                }
            }
        }

        private void RestoryBackupClicked(object sender, RoutedEventArgs e) {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Backup files (*.bak)|*.bak";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                try {
                    var streamReader = new StreamReader("Scripts\\RestoryBackupScript.sql");
                    string script = String.Format(streamReader.ReadToEnd(), dialog.FileName);
                    streamReader.Close();
                    var sqlFileTemp = Path.Combine(Path.GetTempPath(), "RestoryBackupScript.sql");
                    if (File.Exists(sqlFileTemp))
                        File.Delete(sqlFileTemp);
                    var streamWriter = new StreamWriter(sqlFileTemp);
                    streamWriter.Write(script);
                    streamWriter.Close();
                    var process = Process.Start(Settings.SqlCmdLocation, String.Format("-E -S .\\SQLEXPRESS -d Photo -i \"{0}\"", sqlFileTemp));
                    process.WaitForExit();
                    PhotoModel.Init();
                    PhotoModel.ScanRootFolder();
                    PhotoModel.LoadFolderInfo();
                } catch (Exception exception) {
                    MessageBox.Show(exception.Message);
                }
            }
        }

        private void FolderAttributeListBoxLoaded(object sender, RoutedEventArgs e) {
            foreach (var attribute in ((FolderModel)((ListBox)sender).DataContext).Folder.Attribute) {
                ((ListBox)sender).SelectedItems.Add(attribute);
            }
            ((ListBox)sender).SelectionChanged += FolderAttributeSelectionChanged;
        }

        private void FolderAttributeSelectionChanged(object sender, SelectionChangedEventArgs e) {
            foreach (Attribute attribute in e.AddedItems) {
                ((FolderModel)((ListBox)sender).DataContext).Folder.Attribute.Add(attribute);
            }
            foreach (Attribute attribute in e.RemovedItems) {
                ((FolderModel)((ListBox)sender).DataContext).Folder.Attribute.Remove(attribute);
            }
            PhotoModel.SaveChanges();
        }

        private void RadGridViewCellEditEnded(object sender, GridViewCellEditEndedEventArgs e) {
            PhotoModel.SaveChanges();
        }

        private void FolderAttributeListBoxMouseWheel(object sender, MouseWheelEventArgs e) {
            if (!e.Handled) {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                RadGridView.RaiseEvent(eventArg);
            }
        }

        private void OpenButtonClick(object sender, RoutedEventArgs e) {
            Process.Start(Path.Combine(ConfigurationManager.AppSettings["RootFolder"], ((FolderModel)((FrameworkElement)sender).DataContext).Folder.Name));
        }

        private void ListBoxItemMouseDoubleClick(object sender, MouseButtonEventArgs e) {
            Process.Start(((ImageModel)((ContentControl)sender).Content).Path);
        }

		private void RadGridView_Deleted(object sender, GridViewDeletedEventArgs e) {
            foreach (FolderModel folderModel in e.Items) {
                PhotoModel.Deleted(folderModel);
            }
        }

        private void ScrollViewerLoaded(object sender, RoutedEventArgs e) {
            scrollViewer.AddHandler(MouseWheelEvent, new RoutedEventHandler(MyMouseWheelH), true);
        }

        private void MyMouseWheelH(object sender, RoutedEventArgs e) {
            var eargs = (MouseWheelEventArgs)e;
            double x = eargs.Delta;
            double y = scrollViewer.VerticalOffset;
            scrollViewer.ScrollToVerticalOffset(y - x);
        }

        private Thread _thread;
        private void RadGridViewSelectedCellsChanged(object sender, GridViewSelectedCellsChangedEventArgs e) {
            if (_thread != null) {
                _thread.Abort();
                PhotoModel.SaveChanges();
            }
            _thread = new Thread(ThreadInvoke);
            _thread.Start(((DataControl)sender).SelectedItem);
            RadGridView.ScrollIntoView(RadGridView.SelectedItem);
        }

        private void ThreadInvoke(object sender) {
            if (sender != null) {
                ((FolderModel)sender).ScanFolder(Dispatcher, PhotoModel);
            } else {
                _thread.Abort();
            }
        }

        private void ClearCacheClicked(object sender, RoutedEventArgs e) {
            var totalSize = PhotoModel.GetTotalSizeCache();

            if (MessageBox.Show("You really want to delete the cache? " + ((IValueConverter)Resources["FileSizeConverter"]).
                Convert(totalSize, typeof(string), null, null), "Deleted", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                PhotoModel.ClearCache();
                MessageBox.Show("Clear completed!");
            }
        }
    }
}
