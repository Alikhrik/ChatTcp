using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using ChatTcpLib.Model;

namespace ChatTcpLib.Client;

public class ChatTcpClient
{
    private CancellationTokenSource? _tokenSource;
    public bool IsReady { get; private set; }

    private TcpClient? _tcpClient;

    [Obsolete("Obsolete")]
    public async Task Start(IPEndPoint endPoint, User sender)
    {
        if(_tokenSource is { IsCancellationRequested: true } &&
           _tcpClient is { Connected: true })
            throw new Exception("Client is started");
        _tokenSource = new CancellationTokenSource();
        _tcpClient = new TcpClient();
        var connect = _tcpClient.ConnectAsync(endPoint);
        var request = CreateTcpMessage.InitialRequest(sender);
        await connect;
        // Initial Request
        await SendRequestAsync(request);
        IsReady = true;
    }

    public void Close()
    {
        if (_tcpClient == null || _tokenSource == null)
            throw new Exception("Client is not started");
        IsReady = false;
        _tokenSource.Cancel();
        _tcpClient.Close();
        _tcpClient.Dispose();
        _tokenSource.Dispose();
    }
    
    [Obsolete("Obsolete")]
    public async Task<List<User>?> GetUsers()
    {
        var request = CreateTcpMessage.GetUsersRequest();
        await SendRequestAsync(request);
            
        var response = await GetResponseAsync();
        var result = response.Body as List<User>;
        return result;
    }

    [Obsolete("Obsolete")]
    public async Task<List<Message>?> GetMassages(User recipient)
    {
        var request = CreateTcpMessage.GetMessagesRequest(recipient);
        await SendRequestAsync(request);
            
        var response = await GetResponseAsync();
        var result = response.Body as List<Message>;
        return result;
    }

    [Obsolete("Obsolete")]
    public async Task SendNewMessage(Message message)
    {
        if (_tcpClient == null || _tokenSource == null)
            throw new Exception("Client is not started");
        var request = CreateTcpMessage.NewMessageRequest(message);
        await SendRequestAsync(request);
    }

    [Obsolete("Obsolete")]
    private async Task SendRequestAsync(TcpMessage request)
    {
        if (_tcpClient == null || _tokenSource == null)
            throw new Exception("Client is not started");
        var netStream = _tcpClient.GetStream();
        using var ms = new MemoryStream();
        BinaryFormatter bf = new();
        bf.Serialize(ms, request);
        try
        {
            await netStream.WriteAsync(ms.ToArray(), _tokenSource.Token);
        }
        catch (Exception)
        {
            Close();
            throw new IOException("Server disconnected");
        }
    }

    [Obsolete("Obsolete")]
    private async Task<TcpMessage> GetResponseAsync()
    {
        if (_tcpClient == null || _tokenSource == null)
            throw new Exception("Client is not started");
        var netStream = _tcpClient.GetStream();
        var bufferSize = _tcpClient.ReceiveBufferSize;
        using var ms = new MemoryStream();
        BinaryFormatter bf = new();
        var buffer = new byte[bufferSize];
        try
        {
            var unused = await netStream.ReadAsync(buffer, _tokenSource.Token);
        }
        catch(IOException)
        {
            Close();
        }

        await ms.WriteAsync(buffer);
        ms.Position = 0;
        if (bf.Deserialize(ms) is not TcpMessage request)
            throw new Exception("Not valid response");
        
        return request;
    }
    
}