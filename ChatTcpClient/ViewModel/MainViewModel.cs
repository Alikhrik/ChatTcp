using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ChatTcpClient.Command;
using ChatTcpLib.Model;

namespace ChatTcpClient.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly SynchronizationContext _synContext;
        public ObservableCollection<Message> Messages { get; set; }
        public ObservableCollection<User> Users { get; set; }

        string _serverIpAddress;
        public string ServerIpAddress
        {
            get => _serverIpAddress;
            set
            {
                _serverIpAddress = value;
                OnPropertyChanged(nameof(_serverIpAddress));
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

        private User _selectedRecipient;
        public User SelectedRecipient 
        {
            get => _selectedRecipient;
            set
            {
                _selectedRecipient = value;
                if (_selectedRecipient != null)
                {
                    RefreshMessages(_listener.GetStream(), _listener.ReceiveBufferSize, SelectedRecipient.Name);
                }
            }
        }

        private readonly TcpClient _listener;
        public MainViewModel()
        {
            _listener = new();
            Users = new();
            Messages = new();
            ServerIpAddress = "127.0.0.103";
            ClientMessage = "";
            ClientName = "";
            _synContext = SynchronizationContext.Current!;
        }

        DelegateCommand _delegateConnectCommand;

        DelegateCommand _delegateRefreshCommand;

        DelegateCommand _delegateNewMessageCommand;

        public event EventHandler<EventArgs> ErrorEvent;

        public ICommand ConnectCommand => _delegateConnectCommand ??
                    (_delegateConnectCommand = new(exec => Connect(), can_exec => CanConnect()));
        bool CanConnect() => ServerIpAddress.Trim().Length > 0 && ClientName.Trim().Length > 0 && !_listener.Connected;

        public ICommand RefreshCommand => _delegateRefreshCommand ??
                    (_delegateRefreshCommand = new(exec => RefreshAsync(), cen_exec => CanRefresh()));

        bool CanRefresh() => _listener.Connected;

        public ICommand NewMessageCommand => _delegateNewMessageCommand ??
            (_delegateNewMessageCommand = new(exec => NewMessageAsync(), can_exec => CanNewMessage()));

        bool CanNewMessage() => ClientMessage.Trim().Length != 0 && SelectedRecipient != null;

        async void Connect()
        {
            await Task.Run(async () => {
                try
                {
                    await _listener.ConnectAsync(IPAddress.Parse(ServerIpAddress), 13542);
                    RefreshUsers(_listener.GetStream(), _listener.ReceiveBufferSize, ClientName);
                }
                catch (IOException ex)
                {
                    ErrorEvent.Invoke(ex, EventArgs.Empty);
                    _listener.Close();
                }
                catch (Exception ex)
                {
                    ErrorEvent.Invoke(ex, EventArgs.Empty);
                }
            });
        }

        private async void RefreshAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    RefreshUsers(_listener.GetStream(), _listener.ReceiveBufferSize, ClientName);
                    if (SelectedRecipient != null)
                    {
                        RefreshMessages(_listener.GetStream(), _listener.ReceiveBufferSize, SelectedRecipient.Name);
                    }
                }
                catch (IOException ex)
                {
                    ErrorEvent.Invoke(ex, EventArgs.Empty);
                    _synContext.Send(m => Messages.Clear(), null);
                    _synContext.Send(m => Users.Clear(), null);
                    _listener.Close();
                }
                catch (Exception ex)
                {
                    ErrorEvent.Invoke(ex, EventArgs.Empty);
                }
            });
        }

        private async void NewMessageAsync()
        {
            await Task.Run(() => {
                try
                {
                    NewMessage(_listener.GetStream(), SelectedRecipient.Name, ClientMessage);
                }
                catch (IOException ex)
                {
                    ErrorEvent.Invoke(ex, EventArgs.Empty);
                    _synContext.Send(m => Messages.Clear(), null);
                    _synContext.Send(m => Users.Clear(), null);
                    _listener.Close();
                }
                catch (Exception ex)
                {
                    ErrorEvent.Invoke(ex, EventArgs.Empty);
                }
            });
        }

        void NewMessage(NetworkStream netStream, string recipientName, string message)
        {
            byte[] bufferWrite = Encoding.Default.GetBytes("newmesg" + " " + recipientName + " " + message);
            netStream.Write(bufferWrite, 0, bufferWrite.Length);
            Message msg = new();
            msg.Recipient.Name = recipientName;
            msg.Sender.Name = ClientName;
            msg.Text = message;
            _synContext.Send(m => Messages.Add(msg), null);
        }
        void RefreshUsers(NetworkStream netStream, int bufferSize, string clientName)
        {
            byte[] bufferRead = new byte[bufferSize];
            var name = Encoding.Default.GetBytes("refuser" + " " + clientName + " ");
            netStream.Write(name, 0, name.Length);
            int bytesRead;
            bytesRead = netStream.Read(bufferRead);
            if (bytesRead == 0)
            {
                netStream.Close();
                throw new IOException("Server disconnected");
            }
            using var ms = new MemoryStream(bufferRead);
            BinaryFormatter bf = new();
#pragma warning disable SYSLIB0011
            List<User> listUsers = bf.Deserialize(ms) as List<User>;
#pragma warning restore SYSLIB0011
            bool flag;
            if (listUsers != null)
                foreach (var listUser in listUsers)
                {
                    flag = true;
                    if (Users.Count == 0)
                    {
                        _synContext.Send(p => Users.Add(listUser), null);
                        continue;
                    }

                    foreach (var user in Users)
                    {
                        if (user.Name == listUser.Name)
                        {
                            flag = false;
                            break;
                        }
                    }

                    if (flag)
                        _synContext.Send(p => Users.Add(listUser), null);
                }
        }

        async void RefreshMessages(NetworkStream netStream, int bufferSize, string recipientName)
        {
            await Task.Run(() => {
                byte[] bufferRead = new byte[bufferSize];
                int bytesRead;
                byte[] bufferWrite = Encoding.Default.GetBytes("refmesg" + " " + recipientName + " ");
                netStream.Write(bufferWrite, 0, bufferWrite.Length);
                bytesRead = netStream.Read(bufferRead);
                if (bytesRead == 0)
                {
                    netStream.Close();
                    throw new IOException("Server disconnected");
                }
                using var ms = new MemoryStream(bufferRead);
                BinaryFormatter bf = new();
#pragma warning disable SYSLIB0011
                List<Message> messages = bf.Deserialize(ms) as List<Message>;
#pragma warning restore SYSLIB0011
                _synContext.Send(m => Messages.Clear(), null);
                if (messages != null)
                    foreach (var message in messages)
                    {
                        _synContext.Send(m => Messages.Add(message), null);
                    }
            });
        }
    }
}
