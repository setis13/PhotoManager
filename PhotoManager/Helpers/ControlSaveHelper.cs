using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PetCommon.Contexts;
using PhotoManager.Contexts;

namespace PhotoManager.Helpers {
    /// <summary>
    /// сохраняем поременные в реестре.
    /// все переменные привязываются к окну. 
    /// у окна в качестве Name берётся его тип
    /// </summary>
    public class ControlSaveHelper {
        /// <summary>
        /// каталог приложения в реестре
        /// </summary>
        public static string RegPath = string.Format(@"Software\{0}\", Assembly.GetEntryAssembly().GetName().Name);

        #region Window
        /// <summary>
        /// загрузить свойство из реестра. 
        /// </summary>
        /// <param name="window"></param>
        /// <param name="control"></param>
        /// <param name="propertyName">имя свойства. такое же как в control</param>
        public static void LoadValue(Control window, FrameworkElement control, string propertyName) {
            if (control.Name == null) throw new Exception("Name is null!");
            var key = Registry.CurrentUser.OpenSubKey(Path.Combine(RegPath, window.GetType().Name, control.Name));
            LoadValue(key, control, propertyName);
        }

        /// <summary>
        /// загрузить свойство из реестра. 
        /// </summary>
        /// <param name="window"></param>
        /// <param name="control"></param>
        /// <param name="propertyName">имя свойства. такое же как в control</param>
        public static void LoadValue(Control window, FrameworkContentElement control, string propertyName) {
            if (String.IsNullOrEmpty(control.Name))
                throw new Exception("Name is null!");
            var key = Registry.CurrentUser.OpenSubKey(Path.Combine(RegPath, window.GetType().Name, control.Name));
            LoadValue(key, control, propertyName);
        }

        /// <summary>
        /// загрузить свойство окна из реестра. 
        /// </summary>
        /// <param name="window"></param>
        /// <param name="propertyName">имя свойства. такое же как в window</param>
        public static void LoadValue(Control window, string propertyName) {
            if (String.IsNullOrEmpty(window.GetType().Name)) throw new Exception("Name is null!");
            var key = Registry.CurrentUser.OpenSubKey(Path.Combine(RegPath, window.GetType().Name));
            LoadValue(key, window, propertyName);
        }

        /// <summary>
        /// загрузить свойство окна из реестра. 
        /// </summary>
        /// <param name="column"></param>
        /// <param name="propertyName">имя свойства. такое же как в window</param>
        public static void LoadValue(ColumnDefinition column, string propertyName) {
            if (String.IsNullOrEmpty(column.GetType().Name)) throw new Exception("Name is null!");
            var key = Registry.CurrentUser.OpenSubKey(Path.Combine(RegPath, column.GetType().Name));
            LoadValue(key, column, propertyName);
        }

        /// <summary>
        /// сохранить значчение свойства в реестр
        /// </summary>
        public static void SaveValue(Control window, FrameworkElement control, string propertyName) {
            if (String.IsNullOrEmpty(window.GetType().Name) || String.IsNullOrEmpty(control.Name))
                throw new Exception("Name is null!");
            var path = Path.Combine(RegPath, window.GetType().Name, control.Name);
            var key = Registry.CurrentUser.CreateSubKey(path);
            SaveValue(key, control, propertyName);
        }

        /// <summary>
        /// сохранить значчение свойства в реестр
        /// </summary>
        public static void SaveValue(Control window, FrameworkContentElement control, string propertyName) {
            if (String.IsNullOrEmpty(window.GetType().Name) || String.IsNullOrEmpty(control.Name))
                throw new Exception("Name is null!");
            var path = Path.Combine(RegPath, window.GetType().Name, control.Name);
            var key = Registry.CurrentUser.CreateSubKey(path);
            SaveValue(key, control, propertyName);
        }

        /// <summary>
        /// сохранить значчение свойства в реестр
        /// </summary>
        public static void SaveValue(Control window, string propertyName) {
            if (String.IsNullOrEmpty(window.GetType().Name)) throw new Exception("Name is null!");
            var path = Path.Combine(RegPath, window.GetType().Name);
            var key = Registry.CurrentUser.CreateSubKey(path);
            SaveValue(key, window, propertyName);
        }

        /// <summary>
        /// сохранить значчение свойства в реестр
        /// </summary>
        public static void SaveValue(ColumnDefinition column, string propertyName) {
            if (String.IsNullOrEmpty(column.GetType().Name)) throw new Exception("Name is null!");
            var path = Path.Combine(RegPath, column.GetType().Name);
            var key = Registry.CurrentUser.CreateSubKey(path);
            SaveValue(key, column, propertyName);
        }

        #endregion Wordow

        #region Common

        private static void LoadValue(RegistryKey key, object control, string propertyName) {
            if (key != null) {
                var value = key.GetValue(propertyName);
                if (value != null) {
                    var value1 = GetValue1(control, propertyName);
                    if (value1 is GridLength) {
                        var serializer = new DataContractSerializer(((GridLength)value1).Value.GetType());
                        var stream = new MemoryStream(Encoding.UTF8.GetBytes((string)value));
                        SetValue1(control, propertyName, new GridLength((double)serializer.ReadObject(stream)));
                    } else {
                        var serializer = new DataContractSerializer(value1.GetType());
                        var stream = new MemoryStream(Encoding.UTF8.GetBytes((string)value));
                        SetValue1(control, propertyName, serializer.ReadObject(stream));
                    }
                }
            }
        }

        private static void SaveValue(RegistryKey key, object control, string propertyName) {
            if (key != null) {
                var stream = new MemoryStream();
                var value1 = GetValue1(control, propertyName);
                if (value1 is GridLength)
                    value1 = ((GridLength)value1).Value;
                var serializer = new DataContractSerializer(value1.GetType());
                serializer.WriteObject(stream, value1);
                try {
                    key.SetValue(propertyName, Encoding.UTF8.GetString(stream.ToArray()));
                } catch (Exception) {
                    LoggerContext.Instance.AddMessage("Не могу писать в реестр");
                }
            }
        }

        public static object LoadValue<T>(string path, string key) {
            var regKey = Registry.CurrentUser.OpenSubKey(Path.Combine(RegPath, path));
            if (regKey != null) {
                var value = regKey.GetValue(key);
                if (value != null) {
                    var serializer = new DataContractSerializer(typeof(T));
                    var stream = new MemoryStream(Encoding.UTF8.GetBytes((string)value));
                    return serializer.ReadObject(stream);
                } else {
                    return null;
                }
            }
            return null;
        }

        public static void SaveValue(string path, string key, object value) {
            var regPath = Path.Combine(RegPath, path);
            var regKey = Registry.CurrentUser.CreateSubKey(regPath);
            if (regKey != null) {
                var stream = new MemoryStream();
                var serializer = new DataContractSerializer(value.GetType());
                serializer.WriteObject(stream, value);
                try {
                    regKey.SetValue(key, Encoding.UTF8.GetString(stream.ToArray()));
                } catch (Exception) {
                    LoggerContext.Instance.AddMessage("Не могу писать в реестр");
                }
            }
        }

        private static object GetValue1(object obj, string pathValue) {
            foreach (var str in pathValue.Split('.')) {
                try {
                    obj = obj.GetType().GetProperty(str).GetValue(obj, null);
                    if (obj == null) return null;
                } catch (Exception) {
                    return null;
                }
            }
            return obj;
        }

        private static void SetValue1(object obj, string pathValue, object value) {
            var arrayStr = pathValue.Split('.');
            foreach (var str in arrayStr) {
                try {
                    if (arrayStr.Last() == str)
                        obj.GetType().GetProperty(str).SetValue(obj, value, null);
                    else
                        obj = obj.GetType().GetProperty(str).GetValue(obj, null);
                    if (obj == null) return;
                } catch (Exception) {
                    return;
                }
            }
        }

        #endregion Common
    }
}
