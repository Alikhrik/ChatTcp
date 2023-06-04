using System.Windows.Controls;
using ChatTcpLib.Model;

namespace ChatTcpClientV2.Items;

public partial class MyMessageItem : ListBoxItem
{
    private readonly Message _message;
    public string? Text => _message.Text;
    
    public MyMessageItem(Message message)
    {
        _message = message;
        InitializeComponent();
    }
}