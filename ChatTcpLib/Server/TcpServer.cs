using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using ChatTcpLib.DbAccess;
using ChatTcpLib.Model;

namespace ChatTcpLib.Server;

internal abstract class TcpServer
{
    public static async Task Listener()
    {
        try
        {
            Console.Write("Enter server sql name: ");
            var sqlServerName = Console.ReadLine();

            if (sqlServerName == "def")
                sqlServerName = @"DESKTOP-254REBR\MSSQLSERVER2";

            if (sqlServerName != null)
            {
                await using ChatContext chatTcpContext = new(sqlServerName);
                TcpListener listener = new(IPAddress.Any, 13542);
                listener.Start();
                Console.WriteLine("Server | server is running and waiting for a connection");
                while (true)
                {
                    var client = await listener.AcceptTcpClientAsync();
                    Console.WriteLine($"Server caught client: {client.Client.RemoteEndPoint}");
                    Handler(client, chatTcpContext);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Server | " + ex);
        }
    }

    static async Task Handler(TcpClient client, ChatContext context)
    {
        await Task.Run(async () =>
        {
            var clientName = string.Empty;
            try
            {
                var regex = new Regex(@" \S* ");
                var netStream = client.GetStream();
                var buffer = new byte[client.ReceiveBufferSize];
                Console.WriteLine("Server | Client: " + clientName + " connected");
                while (true)
                {
                    var bytesRead = await netStream.ReadAsync(buffer);
                    if (bytesRead == 0)
                    {
                        netStream.Close();
                        client.Close();
                        Console.WriteLine("Server | Client: " + clientName + " disconnected");
                        return;
                    }

                    var message = Encoding.Default.GetString(buffer, 0, bytesRead);
                    switch (message[..7])
                    {
                        case "refuser":
                        {
                            if (clientName == string.Empty)
                                clientName = regex.Match(message[7..]).Value.Trim();
                            RefreshUsers(context, clientName, netStream);
                            Console.WriteLine("Client: " + clientName + " received users ");
                            break;
                        }
                        case "refmesg" when regex.IsMatch(message[7..]):
                        {
                            string senderName = regex.Match(message[7..]).Value.Trim();
                            RefreshMessages(context, senderName, clientName, netStream);
                            Console.WriteLine("Client: " + clientName + " received messages " + senderName);
                            break;
                        }
                        case "newmesg":
                        {
                            string recipientNameNoTrim = regex.Match(message[7..]).Value;
                            string recipientNameTrim = recipientNameNoTrim.Trim();
                            int l = 7 + recipientNameNoTrim.Length;
                            string clientMessage = message[l..];
                            NewMessage(context, clientName, recipientNameTrim, clientMessage);
                            Console.WriteLine("Client: " + clientName + " sent a message " + recipientNameTrim);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client: " + clientName + " " + ex.Message);
            }
        });
    }

    private static void RefreshUsers(ChatContext context, string clientName, NetworkStream netStream)
    {
        var u = from users in context.Users
            where users.Name == clientName
            select users;
        if (!u.Any())
        {
            User newUser = new(clientName);
            context.Users.Add(newUser);
            context.SaveChanges();
        }

        using var ms = new MemoryStream();
        var q = from users in context.Users
            where users.Name != clientName
            select users;
        BinaryFormatter bf = new();
#pragma warning disable SYSLIB0011
        bf.Serialize(ms, q.ToList());
#pragma warning restore SYSLIB0011
        var usersArr = ms.ToArray();
        netStream.Write(usersArr, 0, usersArr.Length);
    }

    private static void RefreshMessages(ChatContext context, string senderName, string recipientName, Stream netstream)
    {
        Regex regex = new(@" \S* ");
        using MemoryStream ms = new();
        BinaryFormatter bf = new();
        var msgs = from msg in context.Messages
            where msg.Sender.Name == senderName && msg.Recipient.Name == recipientName
                  || msg.Sender.Name == recipientName && msg.Recipient.Name == senderName
            select msg;
#pragma warning disable SYSLIB0011
        bf.Serialize(ms, msgs.ToList());
#pragma warning restore SYSLIB0011
        var serverMessage = ms.ToArray();
        netstream.Write(serverMessage, 0, serverMessage.Length);
    }

    static void NewMessage(ChatContext context, string senderName, string recipientName, string message)
    {
        var sender = from user in context.Users
            where user.Name == senderName
            select user;
        var recipient = from user in context.Users
            where user.Name == recipientName
            select user;
        Message newMessage = new(sender.Single(), recipient.Single())
        {
            Text = message
        };
        context.Messages.Add(newMessage);
        context.SaveChanges();
    }
}