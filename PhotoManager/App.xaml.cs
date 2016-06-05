using System.Windows;
using System.Windows.Threading;
using PhotoManager.Contexts;
using PhotoManager.Windows;

namespace PhotoManager {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        private void ApplicationDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
            LoggerContext.Instance.Screenshot();
            LoggerContext.Instance.AddError(e.Exception);
            var window = new ErrorWindow(e.Exception);
            window.Show();
            e.Handled = true;
        }
    }
}
