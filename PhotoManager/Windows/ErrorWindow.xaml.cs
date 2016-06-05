using System;
using System.Windows;

namespace PhotoManager.Windows {
    /// <summary>
    /// вывод ошибки
    /// </summary>
    public partial class ErrorWindow : Window {
        public ErrorWindow() {
            InitializeComponent();
        }

        public ErrorWindow(string errorDetail) {
            InitializeComponent();

            textBoxErrorDetail.Text = errorDetail;
        }

        public ErrorWindow(Exception exception) {
            InitializeComponent();

            textBoxErrorDetail.Text = String.Format("{0}\n\n{1}", exception.Message, exception.StackTrace);
            if (exception.InnerException != null)
                textBoxErrorDetail.Text += String.Format("{0}\n\n{1}", exception.InnerException.Message, exception.InnerException.StackTrace);
        }

        private void ButtonClose(object sender, RoutedEventArgs e) {
            Close();
        }

        private void ButtonRestart(object sender, RoutedEventArgs e) {
            System.Windows.Forms.Application.Restart();
            Application.Current.Shutdown();
        }
    }
}
