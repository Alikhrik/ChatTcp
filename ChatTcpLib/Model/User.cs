using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace ChatTcpLib.Model;

[Serializable]
public class User
{
    [NotMapped]
    private string _name;
    [DataMember]
    public int Id { get; set; }
    [DataMember]
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged(nameof(_name));
        }
    }
    public ICollection<Message>? MessageSender { get; set; }
    public ICollection<Message>? MessageRecipient { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public override string ToString()
    {
        return Name;
    }

    public User(string name)
    {
        _name = name;
        MessageSender = new HashSet<Message>();
        MessageRecipient = new HashSet<Message>();
    }
}