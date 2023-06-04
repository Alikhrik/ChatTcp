using System.Runtime.Serialization;

namespace ChatTcpLib.Model;

[Serializable]
public class TcpMessage
{
    [DataMember]
    
    public TcpMessageType Type { get; internal set; }
    [DataMember]
    public TcpMessageCode Code { get; internal set; }
    [DataMember]
    public object? Body { get; internal set; }

    internal TcpMessage(TcpMessageType type)
    {
        Type = type;
    }
}