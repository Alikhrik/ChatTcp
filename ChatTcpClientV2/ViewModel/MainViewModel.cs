using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ChatTcpClientV2.Command;
using ChatTcpLib.Client;
using ChatTcpLib.Model;

namespace ChatTcpClientV2.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly SynchronizationContext _uiContext;
        public ObservableCollection<Message> Messages { get; set; }
        public ObservableCollection<User> Users { get; set; }

        private string? _serverIpAddress;
        public string? ServerIpAddress
        {
            get => _serverIpAddress;
            set
            {
                _serverIpAddress = value;
                OnPropertyChanged(nameof(ServerIpAddress));
            }
        }
        
        private string _newMessageText;
        public string NewMessageText
        {
            get => _newMessageText;
            set
            {
                _newMessageText = value;
                OnPropertyChanged(nameof(NewMessageText));
            }
        }

        private User? _currentClientUser;

        private User? CurrentClientUser
        {
            get => _currentClientUser;
            set
            {
                _currentClientUser = value;
                OnPropertyChanged(nameof(_currentClientName));
            }
        }
        
        private string? _currentClientName;
        public string? CurrentClientName
        {
            get => _currentClientName;
            set
            {
                _currentClientName = value;
                OnPropertyChanged(nameof(CurrentClientName));
            }
        }

        private User? _selectedRecipient;
        [Obsolete("Obsolete")]
        public User? SelectedRecipient
        {
            get => _selectedRecipient;
            set
            {
                _selectedRecipient = value;
                Messages.Clear();
            }
        }

        private Task? _dataSynchronizerTask;
        private CancellationTokenSource _tokenSource;
        
        private readonly ChatTcpClient _chatTcpClient;
        [Obsolete("Obsolete")]
        public MainViewModel()
        {
            _tokenSource = new CancellationTokenSource();
            
            _chatTcpClient = new ChatTcpClient(); 
            Users = new ObservableCollection<User>();
            Messages = new ObservableCollection<Message>();
            _serverIpAddress = "127.0.0.103";
            _currentClientName = "";
            _newMessageText = "";
            _uiContext = SynchronizationContext.Current!;
        }

        private DelegateCommand? _delegateDisconnectCommand;
        
        private DelegateCommand? _delegateConnectCommand;

        private DelegateCommand? _delegateNewMessageCommand;

        public event EventHandler<EventArgs>? ErrorEvent;
        
        public ICommand DisconnectCommand => _delegateDisconnectCommand ??=
            new DelegateCommand(
                _ => Disconnect(),
                _ => CanDisconnect()
            );
        [Obsolete("Obsolete")]
        public ICommand ConnectCommand => _delegateConnectCommand ??=
            new DelegateCommand(
                _ => ConnectAsync(),
                _ => CanConnect()
            );
        private bool CanConnect() => 
            CurrentClientName != null && ServerIpAddress != null &&
            ServerIpAddress.Trim().Length > 0 && IsValidClientName(CurrentClientName) &&
            !CanDisconnect();
        
        private bool CanDisconnect() =>
            _chatTcpClient.IsReady;
        
        private bool CanSynchronizeUsers() => _chatTcpClient.IsReady;
        [Obsolete("Obsolete")]
        private bool CanSynchronizeMessages() => _chatTcpClient.IsReady && SelectedRecipient != null;
        
        [Obsolete("Obsolete")]
        public ICommand NewMessageCommand => _delegateNewMessageCommand ??=
            new DelegateCommand(
                _ => SendNewMessageAsync(),
                _ => CanNewMessage()
            );

        [Obsolete("Obsolete")]
        private bool CanNewMessage() => IsValidClientMessage(NewMessageText) && SelectedRecipient != null;

        private static bool IsValidClientName(string name)
        {
            return name.Trim().Length > 3;
        }

        private static bool IsValidClientMessage(string message)
        {
            return message.Trim().Length > 0;
        }
        
        [Obsolete("Obsolete")]
        private async void ConnectAsync()
        {
            try
            {
                var endPoint = new IPEndPoint(IPAddress.Parse(ServerIpAddress!), 13542);
                CurrentClientUser = new User( CurrentClientName! );
                await _chatTcpClient.Start(endPoint, CurrentClientUser);
                _dataSynchronizerTask = DataSynchronizer(_tokenSource.Token);
            }
            catch (Exception ex)
            {
                ErrorEvent?.Invoke(ex, EventArgs.Empty);
            }
        }

        private async void Disconnect()
        {
            _chatTcpClient.Close();
            _tokenSource.Cancel();
            if (_dataSynchronizerTask == null) return;
            try
            {
                await _dataSynchronizerTask;
            }
            catch (OperationCanceledException){}
            _tokenSource = new CancellationTokenSource();
            _dataSynchronizerTask.Dispose();
            Users.Clear();
            Messages.Clear();
        }

        [Obsolete("Obsolete")]
        private async void SendNewMessageAsync()
        {
            try
            {
                var newMessage = Message.NewMessage(SelectedRecipient!, NewMessageText);
                await _chatTcpClient.SendNewMessage(newMessage);
            }
            catch (Exception ex)
            {
                _uiContext.Send(_ =>
                {
                    ErrorEvent?.Invoke(ex, EventArgs.Empty);
                    CurrentClientUser = null;
                    Users.Clear();
                    Messages.Clear();
                }, null);
            }
            finally
            {
                NewMessageText = "";
            }
        }

        [Obsolete("Obsolete")]
        private async Task DataSynchronizer(CancellationToken token)
        {
            await Task.Run(async () =>
            {
                try
                {
                    while (_chatTcpClient.IsReady)
                    {
                        await SynchronizeUsersAsync();
                        await SynchronizeMessagesAsync();
                        await Task.Delay(500, token);
                        if (token.IsCancellationRequested) return;
                    }
                }
                catch (IOException ex)
                {
                    _uiContext.Send(_ =>
                    {
                        ErrorEvent?.Invoke(ex, EventArgs.Empty);
                        CurrentClientUser = null;
                        Users.Clear();
                        Messages.Clear();
                    }, null);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _uiContext.Send(_ =>
                    {
                        ErrorEvent?.Invoke(ex, EventArgs.Empty);
                    }, null);
                }
            }, token);
        }

        [Obsolete("Obsolete")]
        private async Task SynchronizeMessagesAsync()
        {
            if (CanSynchronizeMessages())
            {
                var newMessages = await _chatTcpClient.GetMassages(SelectedRecipient!);
                
                if (newMessages == null) return;
                foreach (var newMessage in newMessages)
                {
                    _uiContext.Send(_ =>
                    {
                        if (Messages.All(oldMessage => oldMessage.Id != newMessage.Id ))
                        {
                            Messages.Add(newMessage);
                        }
                    }, null);
                }
            }
        }

        [Obsolete("Obsolete")]
        private async Task SynchronizeUsersAsync()
        {
            if (CanSynchronizeUsers())
            {
                var newUsers = await _chatTcpClient.GetUsers();
                if (newUsers == null) return;
                foreach (var newUser in newUsers)
                {
                    _uiContext.Send(_ =>
                    {
                        if (Users.All(oldUser => oldUser.Name != newUser.Name))
                        {
                            Users.Add(newUser);
                        }
                    }, this);
                }
            }
        }
    }
}
