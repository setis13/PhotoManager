using System;
using System.Windows;

namespace PhotoManager.Windows {
    /// <summary>
    /// окно для ввода текста
    /// </summary>
    public partial class CustomTextWindow : Window {
        public string TextResult;
        public CustomTextWindow(string title, string content) {
            InitializeComponent();

            Title = title;
            textBlock1.Text = content;
            textBox1.Focus();
        }

        public CustomTextWindow(string title, string content, string text) {
            InitializeComponent();

            Title = title;
            textBlock1.Text = content;
            textBox1.Text = text;
            textBox1.Focus();
        }

        private void OkClick(object sender, RoutedEventArgs e) {
            if (String.IsNullOrEmpty(textBox1.Text) == false) {
                TextResult = textBox1.Text;
                DialogResult = true;
            } else {
                MessageBox.Show("Text is not valid");
            }
        }

        private void CancelClick(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }
    }
}
