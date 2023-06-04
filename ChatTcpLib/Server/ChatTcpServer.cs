using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using ChatTcpLib.DbAccess;
using ChatTcpLib.Model;

namespace ChatTcpLib.Server;

public class ChatTcpServer
{
    private readonly string _sqlSeverName;
    private readonly TcpListener _tcpListener;
    private readonly CancellationTokenSource _animTokenSource;
    private readonly CancellationTokenSource _clientTokenSource;

    public ChatTcpServer(string sqlSeverName)
    {
        _tcpListener = new TcpListener(IPAddress.Any, 13542);
        _sqlSeverName = sqlSeverName;
        _animTokenSource = new CancellationTokenSource();
        _clientTokenSource = new CancellationTokenSource();
    }
    
    [Obsolete("Obsolete")]
    public async Task StartAsync()
    {
        var waitAnimation = LoadConsoleAnimationAsync("Server starting", 150,
            _animTokenSource.Token);
        var handlers = new List<Task>();
        try
        {
            await Task.Run(() =>
            {
                var overclockServer = new ChatContext(_sqlSeverName);
                overclockServer.Dispose();
            }); // this is a task for "overclock" the server
            _tcpListener.Start();
            _animTokenSource.Cancel();
            await waitAnimation;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Server is running");
            while (true)
            {
                ListenerConsoleLog("Server waiting for a connection...");
                var client = await _tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
                handlers.Add(HandlerAsync(      // the first connect can be slow
                    client,
                    _sqlSeverName,
                    _clientTokenSource.Token));
                ListenerConsoleLog($"Server caught client: {client.Client.RemoteEndPoint}");
            }
        }
        catch (Exception ex)
        {
            _animTokenSource.Cancel();
            await waitAnimation;
            ListenerConsoleLogError(ex.Message);
        }
        finally
        {
            _tcpListener.Stop();
            ListenerConsoleLog("Server is waiting for the handlers to complete...");
            await Task.WhenAll(handlers);
            foreach (var handler in handlers)
            {
                handler.Dispose();
            }
            ListenerConsoleLog("handlers disposed");
            _animTokenSource.Dispose();
        }
    }

    [Obsolete("Obsolete")]
    private static async Task HandlerAsync(TcpClient client, string sqlSeverName, CancellationToken token)
    {
        ChatContext? chatContext = null;
        var endPoint = client.Client.RemoteEndPoint;
        try
        {
            var netStream = client.GetStream();
            var clientUser = new User(string.Empty);
            while (true)
            {
                var buffer = new byte[client.ReceiveBufferSize];
                var bytesRead = await netStream.ReadAsync(buffer, token).ConfigureAwait(false);
                chatContext ??= new ChatContext(sqlSeverName);
                Task? saveChangesTask = null;
                if (bytesRead == 0)
                {
                    netStream.Close();
                    client.Close();
                    HandlerConsoleLog($"{clientUser?.Name} disconnected", endPoint);
                    return;
                }

                using MemoryStream ms = new(buffer);
                BinaryFormatter bf = new();
                if (bf.Deserialize(ms) is not TcpMessage request) continue;
                TcpMessage? response = null;
                switch (request)
                {
                    case
                    {
                        Code: TcpMessageCode.InitialRequest,
                        Body: User
                    }:
                        var bodyUser = (User)request.Body;
                        var queryUser = chatContext.GetUserByName(bodyUser.Name);
                        if (queryUser == null)
                        {
                            clientUser = bodyUser;
                            await chatContext.Users.AddAsync(bodyUser, token).ConfigureAwait(false);
                            saveChangesTask = chatContext.SaveChangesAsync(token);
                        }
                        else
                        {
                            clientUser = queryUser;
                        }
                        HandlerConsoleLog(clientUser.Name, null);
                        break;

                    case { Code: TcpMessageCode.GetUsers }:
                        if (clientUser == null)
                        {
                            HandlerConsoleLogError("has not the Initial Message", endPoint);
                            break;
                        }

                        var users = chatContext.GetUsers(clientUser.Name);
                        response = CreateTcpMessage.GetUsersResponse(users);
                        break;

                    case
                    {
                        Code: TcpMessageCode.GetMessages,
                        Body: User
                    }:
                        if (clientUser == null)
                        {
                            HandlerConsoleLogError("has not the Initial Message", endPoint);
                            break;
                        }

                        var recipient = (User)request.Body;
                        var messages = chatContext
                            .GetDialogueMessages(clientUser.Name, recipient.Name);
                        response = CreateTcpMessage.GetMessagesResponse(messages);
                        break;

                    case
                    {
                        Code: TcpMessageCode.NewMessage,
                        Body: Message
                    }:
                        if (clientUser == null)
                        {
                            HandlerConsoleLogError("has not the Initial Message", endPoint);
                            break;
                        }

                        var message = (Message)request.Body;
                        message.Sender = clientUser;
                        message.Recipient = chatContext.GetUserByName(message.Recipient.Name)!;
                        if (message.Text == null)
                        {
                            HandlerConsoleLogError("Invalid message", endPoint);
                            break;
                        }

                        await chatContext.Messages.AddAsync(message, token).ConfigureAwait(false);
                        saveChangesTask = chatContext.SaveChangesAsync(token);
                        break;
                    default:
                        HandlerConsoleLogError("Undefined request", endPoint);
                        break;
                }

                if (response != null)
                    await SendResponseAsync(response, netStream).ConfigureAwait(false);

                if (saveChangesTask != null) await saveChangesTask;
            }
        }
        catch (IOException)
        {
            HandlerConsoleLog( "disconnected", endPoint);
        }
        catch (Exception e)
        {
            HandlerConsoleLogError(e.Message, endPoint);
        }
        finally
        {
            if (chatContext != null)
            {
                var disposeAsync = chatContext.DisposeAsync();
                client.Dispose();
                await disposeAsync;
            }
        }
    }
    [Obsolete("Obsolete")]
    private static async Task SendResponseAsync(TcpMessage request, Stream netStream)
    {
        using MemoryStream ms = new();
        BinaryFormatter bf = new();
        bf.Serialize(ms, request);
        var bytes = ms.ToArray();
        await netStream.WriteAsync(bytes);
    }
    
    private static void HandlerConsoleLogError(string message, EndPoint? endPoint)
    {
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.Write("Handler | Client ");
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write(endPoint);Console.Write('\n');
        Console.ForegroundColor = ConsoleColor.Gray;
        if (message.Length < 75)
        {
            Console.Write(": " + message);
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine(message);
        }
        if (message.Length > 160) return;
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.Write(": Client | Handler");
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write(endPoint); Console.Write(' ');
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine("| Handler");
    }
    private static void HandlerConsoleLog(string message, EndPoint? endPoint)
    {
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.Write("Handler | Client ");
        if (endPoint != null)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(endPoint);Console.Write(' ');
        }
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"{message}");
    }
    private static void ListenerConsoleLogError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("Listener | ");
        Console.ForegroundColor = ConsoleColor.Gray;
        if (message.Length < 75)
        {
            Console.Write(": " + message);
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine(message);
        }
        if (message.Length > 160) return;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(": | Listener");
    }
    private static void ListenerConsoleLog(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("Listener | ");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine(message);
    }
    
    private static async Task LoadConsoleAnimationAsync(string message, int millisecondsTimeout, CancellationToken token)
    {
        await Task.Run(() =>
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write(message);
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            while (true)
            {
                Console.Write('.');
                Thread.Sleep(millisecondsTimeout);
                Console.Write('.');
                Thread.Sleep(millisecondsTimeout);
                Console.Write('.');
                Thread.Sleep(millisecondsTimeout);
                Console.Write('.');
                Thread.Sleep(millisecondsTimeout);
                Console.Write("\b\b\b\b    \b\b\b\b");
                if (token.IsCancellationRequested)
                {
                    Console.WriteLine();
                    return;
                }
                Thread.Sleep(millisecondsTimeout);
            }
        }, token);
    }
}