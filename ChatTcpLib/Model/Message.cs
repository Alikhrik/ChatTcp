using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace ChatTcpLib.Model;

[Serializable]
public sealed class Message : INotifyPropertyChanged
{
    public int Id { get; set; }
    [DataMember]
    public User Sender { get; set; }

    [NotMapped] private string _text;

    [DataMember]
    public string Text
    { 
        get => _text;
        set
        {
            _text = value;
            OnPropertyChanged(nameof(_text));
        }
    }

    [DataMember]
    public User Recipient { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public override string ToString()
    {
        return Sender.Name + ":    " + Text;
    }
    public Message()
    {
        Sender = new User();
        Recipient = new User();
    }
}