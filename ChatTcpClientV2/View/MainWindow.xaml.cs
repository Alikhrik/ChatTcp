using System;
using System.Windows;
using ChatTcpClientV2.ViewModel;

namespace ChatTcpClientV2.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.ErrorEvent += new EventHandler<EventArgs>(ErrorMessageFromHandler);
        }

        private void ErrorMessageFromHandler(object sender, EventArgs e)
        {
            MessageBox.Show((sender as Exception)?.Message);
        }

        private void listBoxUsers_Selected(object sender, RoutedEventArgs e)
        {

        }
    }
}
