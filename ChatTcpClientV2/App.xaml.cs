using System;
using System.Windows;
using ChatTcpClientV2.View;
using ChatTcpClientV2.ViewModel;

namespace ChatTcpClientV2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [Obsolete("Obsolete")]
        private void Start(object sender, StartupEventArgs e)
        {
            var mainViewModel = new MainViewModel();
            var mainWindow = new MainWindow(mainViewModel);
            mainWindow.Show();
        }
    }
}