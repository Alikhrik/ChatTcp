using System.Windows;
using ChatTcpClient.View;
using Client.ViewModel;

namespace ChatTcpClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Start(object sender, StartupEventArgs e)
        {
            MainViewModel mainViewModel = new MainViewModel();
            MainWindow mainWindow = new MainWindow(mainViewModel);
            mainWindow.Show();
        }
    }
}