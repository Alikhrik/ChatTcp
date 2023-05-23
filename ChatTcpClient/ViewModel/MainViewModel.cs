using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Client.Command;
using System.Windows.Input;
using System.Threading;
using ChatTcpClient.ViewModel;
using ChatTcpLib.Model;

namespace Client.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly SynchronizationContext _synContext;
        public ObservableCollection<Message> Messages { get; set; }
        public ObservableCollection<User> Users { get; set; }

        string serverIPAddress;
        public string ServerIPAddress
        {
            get
            {
                return serverIPAddress;
            }
            set
            {
                serverIPAddress = value;
                OnPropertyChanged(nameof(serverIPAddress));
            }
        }
        string _clientMessage;
        public string ClientMessage
        {
            get => _clientMessage;
            set
            {
                _clientMessage = value;
                OnPropertyChanged(nameof(_clientMessage));
            }
        }

        string _clientName;

        public string ClientName 
        {
            get => _clientName;
            set
            {
                _clientName = value;
                OnPropertyChanged(nameof(_clientName));
            }
        }

        User selecipient;
        public User SelectedRecipient 
        {
            get => selecipient;
            set
            {
                selecipient = value;
                if (selecipient != null)
                    RefreshMessages(listenner.GetStream(), listenner.ReceiveBufferSize, SelectedRecipient.Name);
            }
        }

        private TcpClient listenner;
        public MainViewModel()
        {
            listenner = new();
            Users = new();
            Messages = new();
            ServerIPAddress = "192.168.0.103";
            ClientMessage = "";
            ClientName = "";
            _synContext = SynchronizationContext.Current!;
        }

        DelegateCommand DelegateConnectCommand;

        DelegateCommand DelegateRefreshCommand;

        DelegateCommand DelegateNewMessageCommand;

        public event EventHandler<EventArgs> ErrorEvent;

        public ICommand ConnectCommand => DelegateConnectCommand ??
                    (DelegateConnectCommand = new(exec => Connect(), can_exec => CanConnect()));
        bool CanConnect() => listenner == null || ServerIPAddress.Trim().Length > 0 && ClientName.Trim().Length > 0 && !listenner.Connected;

        public ICommand RefreshCommand => DelegateRefreshCommand ??
                    (DelegateRefreshCommand = new(exec => Refresh(), cen_exec => CanRefresh()));

        bool CanRefresh() => listenner != null && listenner.Connected;

        public ICommand NewMessageCommand => DelegateNewMessageCommand ??
            (DelegateNewMessageCommand = new(exec => NewMessage(), can_exec => CanNewMessege()));

        bool CanNewMessege() => ClientMessage.Trim().Length != 0 && SelectedRecipient != null;

        async void Connect()
        {
            await Task.Run(async () => {
                try
                {
                    await listenner.ConnectAsync(IPAddress.Parse(ServerIPAddress), 13542);
                    RefreshUsers(listenner.GetStream(), listenner.ReceiveBufferSize, ClientName);
                }
                catch (IOException ex)
                {
                    ErrorEvent.Invoke(ex, EventArgs.Empty);
                    listenner.Close();
                }
                catch (Exception ex)
                {
                    ErrorEvent.Invoke(ex, EventArgs.Empty);
                }
            });
        }

        async void Refresh()
        {
            await Task.Run(() =>
            {
                try
                {
                    RefreshUsers(listenner.GetStream(), listenner.ReceiveBufferSize, ClientName);
                    if (SelectedRecipient != null)
                    {
                        RefreshMessages(listenner.GetStream(), listenner.ReceiveBufferSize, SelectedRecipient.Name);
                    }
                }
                catch (IOException ex)
                {
                    ErrorEvent.Invoke(ex, EventArgs.Empty);
                    _synContext.Send(m => Messages.Clear(), null);
                    _synContext.Send(m => Users.Clear(), null);
                    listenner.Close();
                }
                catch (Exception ex)
                {
                    ErrorEvent.Invoke(ex, EventArgs.Empty);
                }
            });
        }
        async void NewMessage()
        {
            await Task.Run(() => {
                try
                {
                    newMessage(listenner.GetStream(), SelectedRecipient.Name, ClientMessage);
                }
                catch (IOException ex)
                {
                    ErrorEvent.Invoke(ex, EventArgs.Empty);
                    _synContext.Send(m => Messages.Clear(), null);
                    _synContext.Send(m => Users.Clear(), null);
                    listenner.Close();
                }
                catch (Exception ex)
                {
                    ErrorEvent.Invoke(ex, EventArgs.Empty);
                }
            });
        }

        void newMessage(NetworkStream netStream, string RecipientName, string Message)
        {
            byte[] buffer_Write = Encoding.Default.GetBytes("newmesg" + " " + RecipientName + " " + Message);
            netStream.Write(buffer_Write, 0, buffer_Write.Length);
            Message msg = new();
            msg.Recipient.Name = RecipientName;
            msg.Sender.Name = ClientName;
            msg.Text = Message;
            _synContext.Send(m => Messages.Add(msg), null);
        }
        void RefreshUsers(NetworkStream netStream, int BufferSize, string ClientName)
        {
            byte[] buffer_Read = new byte[BufferSize];
            var name = Encoding.Default.GetBytes("refuser" + " " + ClientName + " ");
            netStream.Write(name, 0, name.Length);
            int bytesRead;
            bytesRead = netStream.Read(buffer_Read);
            if (bytesRead == 0)
            {
                netStream.Close();
                throw new IOException("Server disconnected");
            }
            using var ms = new MemoryStream(buffer_Read);
            BinaryFormatter bf = new();
#pragma warning disable SYSLIB0011
            List<User> list_users = bf.Deserialize(ms) as List<User>;
#pragma warning restore SYSLIB0011
            bool flag;
            foreach (var list_user in list_users)
            {
                flag = true;
                if (Users.Count == 0)
                {
                    _synContext.Send(p => Users.Add(list_user), null);
                    continue;
                }
                foreach (var user in Users)
                {
                    if (user.Name == list_user.Name)
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                    _synContext.Send(p => Users.Add(list_user), null);
            }
        }

        async void RefreshMessages(NetworkStream netStream, int BufferSize, string RecipientName)
        {
            await Task.Run(() => {
                byte[] buffer_Read = new byte[BufferSize];
                int bytesRead;
                byte[] buffer_Write = Encoding.Default.GetBytes("refmesg" + " " + RecipientName + " ");
                netStream.Write(buffer_Write, 0, buffer_Write.Length);
                bytesRead = netStream.Read(buffer_Read);
                if (bytesRead == 0)
                {
                    netStream.Close();
                    throw new IOException("Server disconnected");
                }
                using var ms = new MemoryStream(buffer_Read);
                BinaryFormatter bf = new();
#pragma warning disable SYSLIB0011
                List<Message> messages = bf.Deserialize(ms) as List<Message>;
#pragma warning restore SYSLIB0011
                _synContext.Send(m => Messages.Clear(), null);
                foreach (var message in messages)
                {
                    _synContext.Send(m => Messages.Add(message), null);
                }
            });
        }
    }
}
