namespace ChatTcpLib.Model;

public static class CreateTcpMessage
{
    //--------- Initial Request ----------//
    #region InitialRequest
    public static TcpMessage InitialRequest(User sender)
    {
        return new TcpMessage(TcpMessageType.Request)
        {
            Code = TcpMessageCode.InitialRequest,
            Body = sender
        };
    }
    #endregion
    
    //------------- Get User --------------//
    #region GetUser
    public static TcpMessage GetUsersRequest()
    {
        return new TcpMessage(TcpMessageType.Request)
        {
            Code = TcpMessageCode.GetUsers
        };
    }
    public static TcpMessage GetUsersResponse(List<User> users)
    {
        return new TcpMessage(TcpMessageType.Response)
        {
            Code = TcpMessageCode.GetUsers,
            Body = users
        };
    }
    #endregion

    //----------- Get Messages -----------//
    #region GetMessages
    public static TcpMessage GetMessagesRequest(User recipient)
    {
        return new TcpMessage(TcpMessageType.Request)
        {
            Code = TcpMessageCode.GetMessages,
            Body = recipient
        };
    }
    public static TcpMessage GetMessagesResponse(List<Message> messages)
    {
        return new TcpMessage(TcpMessageType.Response)
        {
            Code = TcpMessageCode.GetMessages,
            Body = messages
        };
    }
    #endregion
    
    //------------ New Message ------------//
    #region NewMessageRequest
    public static TcpMessage NewMessageRequest(Message message)
    {
        return new TcpMessage(TcpMessageType.Request)
        {
            Code = TcpMessageCode.NewMessage,
            Body = message
        };
    }
    
    public static TcpMessage NewMessageRequest(User recipient, string text)
    {
        return new TcpMessage(TcpMessageType.Request)
        {
            Code = TcpMessageCode.NewMessage,
            Body = Message.NewMessage(recipient, text)
        };
    }
    #endregion
    
    //----------- Error Message -----------//
    #region ErrorMessage
    public static TcpMessage ErrorResponse(string message)
    {
        return new TcpMessage(TcpMessageType.Response)
        {
            Code = TcpMessageCode.Error,
            Body = message
        };
    }
    #endregion
}