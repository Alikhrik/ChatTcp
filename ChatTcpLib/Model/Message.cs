using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace ChatTcpLib.Model;

[Serializable]
public sealed class Message : INotifyPropertyChanged
{
    [NotMapped] private string? _text;
    
    [DataMember] public int Id { get; internal set; }
    [DataMember] public User Sender { get; internal set; } = null!;

    [DataMember] public string? Text
    { 
        get => _text;
        internal set
        {
            _text = value;
            OnPropertyChanged(nameof(_text));
        }
    }

    [DataMember] public User Recipient { get; internal set; } = null!;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public override string ToString()
    {
        return Sender.Name + ":    " + Text;
    }
    public Message(User sender, User recipient)
    {
        Sender = sender;
        Recipient = recipient;
    }

    public static Message NewMessage(User recipient, string text)
    {
        return new Message
        {
            Recipient = recipient,
            Text = text
        };
    }

    internal Message() // to DbContext
    {
        
    }
}