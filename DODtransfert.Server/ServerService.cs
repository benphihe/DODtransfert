using System.Net;
using System.Net.Sockets;
using System.Text;
using DODtransfert.Shared;
using DODtransfert.Shared.Models;
using Newtonsoft.Json;

namespace DODtransfert.Server;

public class ServerService
{
    private TcpListener? _listener;
    private bool _isRunning;
    private readonly UserRepository _userRepository;
    private readonly int _port;
    private readonly Dictionary<string, TcpClient> _connectedClients = new();
    private readonly Dictionary<string, NetworkStream> _clientStreams = new();
    private readonly Dictionary<string, string> _pendingTransfers = new(); // senderId -> recipientId

    public event EventHandler<User>? UserConnected;
    public event EventHandler<string>? UserDisconnected;
    public event EventHandler<(string userId, TransferRequest request)>? TransferReceived;

    public ServerService(int port = Constants.DefaultPort)
    {
        _port = port;
        _userRepository = new UserRepository();
    }

    public async Task StartAsync()
    {
        if (_isRunning) return;

        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        _isRunning = true;

        _ = Task.Run(async () =>
        {
            while (_isRunning)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client));
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Log error
                    Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        });
    }

    public void Stop()
    {
        _isRunning = false;
        _listener?.Stop();
        
        foreach (var client in _connectedClients.Values)
        {
            client.Close();
        }
        _connectedClients.Clear();
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        string? userId = null;
        try
        {
            using var stream = client.GetStream();
            var buffer = new byte[Constants.BufferSize];
            
            while (client.Connected)
            {
                var message = await ReadMessageAsync(stream, buffer);
                if (message == null) break;

                switch (message.Type)
                {
                    case MessageType.Authentication:
                        userId = await HandleAuthenticationAsync(message, client, stream);
                        break;
                    case MessageType.TransferRequest:
                        await HandleTransferRequestAsync(message, stream, userId);
                        break;
                    case MessageType.FileChunk:
                        await HandleFileChunkAsync(message, userId);
                        break;
                    case MessageType.FileComplete:
                        await HandleFileCompleteAsync(message, userId);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client: {ex.Message}");
        }
        finally
        {
            if (userId != null)
            {
                _userRepository.RemoveConnectedUser(userId);
                _connectedClients.Remove(userId);
                _clientStreams.Remove(userId);
                _pendingTransfers.Remove(userId);
                UserDisconnected?.Invoke(this, userId);
                
                // Broadcast updated user list
                BroadcastUserList();
            }
            client.Close();
        }
    }

    private async Task<string?> HandleAuthenticationAsync(NetworkMessage message, TcpClient client, NetworkStream stream)
    {
        var authData = JsonConvert.DeserializeObject<AuthenticationData>(message.Data?.ToString() ?? "{}");
        if (authData == null) return null;

        var user = new User
        {
            Id = authData.UserId,
            Name = authData.UserName,
            IsConnected = true,
            IpAddress = ((IPEndPoint)client.Client.RemoteEndPoint!).Address.ToString(),
            Port = ((IPEndPoint)client.Client.RemoteEndPoint!).Port,
            LastSeen = DateTime.Now
        };

        _userRepository.AddConnectedUser(user);
        _connectedClients[user.Id] = client;
        _clientStreams[user.Id] = stream;
        UserConnected?.Invoke(this, user);
        
        // Broadcast updated user list to all connected clients
        BroadcastUserList();

        // Send authentication response
        var response = new NetworkMessage
        {
            Type = MessageType.AuthenticationResponse,
            Data = JsonConvert.SerializeObject(new { success = true }),
            Timestamp = DateTime.Now
        };
        await SendMessageAsync(stream, response);

        // Send user list
        var userList = _userRepository.GetConnectedUsers()
            .Select(u => new { u.Id, u.Name, u.IsConnected })
            .ToList();
        
        var userListMessage = new NetworkMessage
        {
            Type = MessageType.UserList,
            Data = JsonConvert.SerializeObject(userList),
            Timestamp = DateTime.Now
        };
        await SendMessageAsync(stream, userListMessage);

        return user.Id;
    }

    private async Task HandleTransferRequestAsync(NetworkMessage message, NetworkStream stream, string? senderId)
    {
        var transferData = JsonConvert.DeserializeObject<TransferRequestData>(message.Data?.ToString() ?? "{}");
        if (transferData == null || string.IsNullOrEmpty(senderId)) return;

        var request = new TransferRequest
        {
            RecipientId = transferData.RecipientId,
            Files = transferData.Files.Select(f => new FileItem
            {
                FileName = f.FileName,
                FileSize = f.FileSize,
                FileType = f.FileType,
                IsImage = f.IsImage,
                IsPdf = f.IsPdf
            }).ToList(),
            IsProductTransfer = transferData.IsProductTransfer,
            ProductData = transferData.ProductData != null ? new ProductTransfer
            {
                BrandName = transferData.ProductData.BrandName,
                ProductName = transferData.ProductData.ProductName
            } : null
        };

        // Store pending transfer
        _pendingTransfers[senderId] = transferData.RecipientId;

        // Forward transfer request to recipient
        if (_clientStreams.ContainsKey(transferData.RecipientId))
        {
            var recipientStream = _clientStreams[transferData.RecipientId];
            message.SenderId = senderId;
            await SendMessageAsync(recipientStream, message);
        }

        TransferReceived?.Invoke(this, (senderId, request));
    }

    private async Task HandleFileChunkAsync(NetworkMessage message, string? senderId)
    {
        if (string.IsNullOrEmpty(senderId) || !_pendingTransfers.ContainsKey(senderId))
            return;

        var recipientId = _pendingTransfers[senderId];
        if (_clientStreams.ContainsKey(recipientId))
        {
            var recipientStream = _clientStreams[recipientId];
            message.SenderId = senderId;
            await SendMessageAsync(recipientStream, message);
        }
    }

    private async Task HandleFileCompleteAsync(NetworkMessage message, string? senderId)
    {
        if (string.IsNullOrEmpty(senderId) || !_pendingTransfers.ContainsKey(senderId))
            return;

        var recipientId = _pendingTransfers[senderId];
        if (_clientStreams.ContainsKey(recipientId))
        {
            var recipientStream = _clientStreams[recipientId];
            message.SenderId = senderId;
            await SendMessageAsync(recipientStream, message);
        }

        // Clear pending transfer after completion
        _pendingTransfers.Remove(senderId);
    }

    private async Task<NetworkMessage?> ReadMessageAsync(NetworkStream stream, byte[] buffer)
    {
        var lengthBytes = new byte[4];
        var bytesRead = await stream.ReadAsync(lengthBytes, 0, 4);
        if (bytesRead != 4) return null;

        var length = BitConverter.ToInt32(lengthBytes, 0);
        if (length <= 0 || length > 10 * 1024 * 1024) return null; // Max 10MB message

        var messageBytes = new byte[length];
        var totalRead = 0;
        while (totalRead < length)
        {
            var read = await stream.ReadAsync(messageBytes, totalRead, length - totalRead);
            if (read == 0) return null;
            totalRead += read;
        }

        var json = Encoding.UTF8.GetString(messageBytes);
        return JsonConvert.DeserializeObject<NetworkMessage>(json);
    }

    private async Task SendMessageAsync(NetworkStream stream, NetworkMessage message)
    {
        var json = JsonConvert.SerializeObject(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        var lengthBytes = BitConverter.GetBytes(bytes.Length);

        await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length);
        await stream.WriteAsync(bytes, 0, bytes.Length);
        await stream.FlushAsync();
    }

    public List<User> GetConnectedUsers()
    {
        return _userRepository.GetConnectedUsers();
    }

    private void BroadcastUserList()
    {
        var userList = _userRepository.GetConnectedUsers()
            .Select(u => new { u.Id, u.Name, u.IsConnected })
            .ToList();

        var userListMessage = new NetworkMessage
        {
            Type = MessageType.UserList,
            Data = JsonConvert.SerializeObject(userList),
            Timestamp = DateTime.Now
        };

        // Send to all connected clients
        foreach (var kvp in _clientStreams.ToList())
        {
            try
            {
                SendMessageAsync(kvp.Value, userListMessage).Wait();
            }
            catch
            {
                // Ignore errors for disconnected clients
            }
        }
    }
}
