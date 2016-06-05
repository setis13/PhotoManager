using System;
using System.IO;
using System.Reflection;
using System.Windows;
using PetCommon.Contexts;
using PhotoManager.Helpers;
using PhotoManager.Windows;

namespace PhotoManager.Contexts {
    /// <summary>
    /// контекст для записи в журнал. записывает в файл
    /// </summary>
    public class LoggerContext {
        #region Singleton

        private LoggerContext() {
        }

        public static LoggerContext Instance {
            get {
                if (_instance == null)
                    _instance = new LoggerContext();
                return _instance;
            }
        }

        private static LoggerContext _instance;

        #endregion Singleton

        /// <summary>
        /// добавить запись об ошибке
        /// </summary>
        /// <param name="exception">исключение, которое конвертируем в xml</param>
        public void AddError(Exception exception) {
            try {
                if (exception.InnerException != null)
                    exception = exception.InnerException;
                AddMessage(XmlSerializer.Instance.SerializeObject(exception).Value);
            } catch (Exception e) {
                AddMessage(e.Message);
            }
        }

        public void Screenshot() {
            foreach (Window window1 in Application.Current.Windows) {
                if (window1.GetType() == typeof(ErrorWindow) && window1.Visibility == Visibility.Visible)
                    return;
                try {
                    var path = Path.Combine(LoggerContext.RootPathLog, Environment.MachineName, "Screenshots");
                    if (Directory.Exists(path) == false)
                        Directory.CreateDirectory(path);

                    ScreenshotHelper.GetJpgImage(window1,
                        Path.Combine(path, (String.IsNullOrEmpty(window1.Name) ? window1.GetType().Name : window1.Name) +
                        String.Format("_{0:d-M-yyyy HH-mm-ss}", DateTime.Now) + ".jpg"));
                } catch (Exception) {
                }
            }
        }


        /// <summary>
        /// добавляем строку
        /// </summary>
        /// <param name="message">строка</param>
        public void AddMessage(string message) {
            using (var textWriter = GetStreamFileStream()) {
                textWriter.WriteLine(String.Format("********************** {0:HH:mm:ss} ***********************", DateTime.Now));
                textWriter.WriteLine(message);
            }
        }

        /// <summary>
        /// путь к файлу
        /// </summary>
        public static string RootPathLog = @"Z:\Оборудование\Базы\Log";
        /// <summary>
        /// имя файла
        /// </summary>
        private string FileName {
            get { return String.Format("{0} - {1:yyyy-MM-dd}.log", Assembly.GetEntryAssembly().GetName().Name, DateTime.Now); }
        }
        /// <summary>
        /// полное имя
        /// </summary>
        private string FullName {
            get { return Path.Combine(Path.Combine(RootPathLog, Environment.MachineName), FileName); }
        }
        /// <summary>
        /// возвращает поток
        /// </summary>
        /// <returns></returns>
        public TextWriter GetStreamFileStream() {
            if (Directory.Exists(Path.Combine(RootPathLog, Environment.MachineName)) == false)
                try {
                    Directory.CreateDirectory(Path.Combine(RootPathLog, Environment.MachineName));
                } catch (Exception) {
                    RootPathLog = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
                    Directory.CreateDirectory(Path.Combine(RootPathLog, Environment.MachineName));
                }
            if (File.Exists(FullName) == false)
                return File.CreateText(FullName);
            return new StreamWriter(FullName, true);
        }
    }
}
