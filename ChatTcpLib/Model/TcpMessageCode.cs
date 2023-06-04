namespace ChatTcpLib.Model;

public enum TcpMessageCode
{
    InitialRequest = 0,
    GetUsers = 1,
    GetMessages = 2,
    NewMessage = 3,
    Error = -1
}