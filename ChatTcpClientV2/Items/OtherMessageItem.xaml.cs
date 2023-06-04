using System.Windows.Controls;
using ChatTcpLib.Model;

namespace ChatTcpClientV2.Items;

public partial class OtherMessageItem : ListBoxItem
{
    private Message _message;
    public int Id => _message.Id;
    public User Sender => _message.Sender;
    public string? Text => _message.Text;
    public OtherMessageItem(Message message)
    {
        _message = message;
        InitializeComponent();
    }
}