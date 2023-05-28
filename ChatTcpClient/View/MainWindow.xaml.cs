using System;
using System.Windows;
using ChatTcpClient.ViewModel;

namespace ChatTcpClient.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
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

        void ErrorMessageFromHandler(object sender, EventArgs e)
        {
            MessageBox.Show((sender as Exception).Message);
        }

        private void listBoxUsers_Selected(object sender, RoutedEventArgs e)
        {

        }
    }
}
