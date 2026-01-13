using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using DODtransfert.Shared;
using DODtransfert.Shared.Models;
using Newtonsoft.Json;

namespace DODtransfert.Client.Services;

public class NetworkService
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private bool _isConnected;
    private readonly EncryptionService _encryptionService;
    private string _sharedKey = string.Empty;
    private readonly Dictionary<string, List<byte[]>> _fileChunks = new();
    private readonly Dictionary<string, FileMetadata> _fileMetadata = new();

    public event EventHandler<List<User>>? UserListReceived;
    public event EventHandler<TransferRequest>? TransferReceived;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler? Disconnected;

    public bool IsConnected => _isConnected;

    public NetworkService(EncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public async Task<bool> ConnectAsync(string serverIp, int port, User user)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(serverIp, port);
            _stream = _client.GetStream();
            _isConnected = true;

            // Generate shared key
            _sharedKey = _encryptionService.GenerateSharedKey();

            // Authenticate
            var authData = new AuthenticationData
            {
                UserId = user.Id,
                UserName = user.Name
            };

            var authMessage = new NetworkMessage
            {
                Type = MessageType.Authentication,
                Data = JsonConvert.SerializeObject(authData),
                SenderId = user.Id,
                Timestamp = DateTime.Now
            };

            await SendMessageAsync(authMessage);

            // Start listening for messages
            _ = Task.Run(ListenForMessagesAsync);

            return true;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Erreur de connexion: {ex.Message}");
            _isConnected = false;
            return false;
        }
    }

    public void Disconnect()
    {
        _isConnected = false;
        _stream?.Close();
        _client?.Close();
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    public async Task SendTransferRequestAsync(TransferRequest request)
    {
        if (!_isConnected || _stream == null) return;

        try
        {
            var transferData = new TransferRequestData
            {
                RecipientId = request.RecipientId,
                Files = request.Files.Select(f => new FileMetadata
                {
                    FileName = f.FileName,
                    FileSize = f.FileSize,
                    FileType = f.FileType,
                    IsImage = f.IsImage,
                    IsPdf = f.IsPdf
                }).ToList(),
                IsProductTransfer = request.IsProductTransfer,
                ProductData = request.ProductData != null ? new ProductData
                {
                    BrandName = request.ProductData.BrandName,
                    ProductName = request.ProductData.ProductName
                } : null
            };

            var message = new NetworkMessage
            {
                Type = MessageType.TransferRequest,
                Data = JsonConvert.SerializeObject(transferData),
                Timestamp = DateTime.Now
            };

            await SendMessageAsync(message);

            // Send files
            foreach (var file in request.Files)
            {
                await SendFileAsync(file.FilePath);
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Erreur lors de l'envoi: {ex.Message}");
        }
    }

    private async Task SendFileAsync(string filePath)
    {
        if (!_isConnected || _stream == null || !File.Exists(filePath)) return;

        var fileInfo = new FileInfo(filePath);
        var fileSize = fileInfo.Length;
        var chunkSize = Constants.BufferSize;
        var totalChunks = (int)Math.Ceiling((double)fileSize / chunkSize);

        var fileBytes = await File.ReadAllBytesAsync(filePath);
        var encryptedBytes = _encryptionService.Encrypt(fileBytes, _sharedKey);

        for (int i = 0; i < totalChunks; i++)
        {
            var offset = i * chunkSize;
            var length = Math.Min(chunkSize, encryptedBytes.Length - offset);
            var chunk = new byte[length];
            Array.Copy(encryptedBytes, offset, chunk, 0, length);

            var chunkData = new FileChunkData
            {
                FileName = Path.GetFileName(filePath),
                ChunkIndex = i,
                TotalChunks = totalChunks,
                Data = chunk
            };

            var message = new NetworkMessage
            {
                Type = MessageType.FileChunk,
                Data = JsonConvert.SerializeObject(chunkData),
                Timestamp = DateTime.Now
            };

            await SendMessageAsync(message);
        }

        // Send completion message
        var completeMessage = new NetworkMessage
        {
            Type = MessageType.FileComplete,
            Data = JsonConvert.SerializeObject(new { fileName = Path.GetFileName(filePath) }),
            Timestamp = DateTime.Now
        };
        await SendMessageAsync(completeMessage);
    }

    private async Task SendMessageAsync(NetworkMessage message)
    {
        if (_stream == null) return;

        var json = JsonConvert.SerializeObject(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        var lengthBytes = BitConverter.GetBytes(bytes.Length);

        await _stream.WriteAsync(lengthBytes, 0, lengthBytes.Length);
        await _stream.WriteAsync(bytes, 0, bytes.Length);
        await _stream.FlushAsync();
    }

    private async Task ListenForMessagesAsync()
    {
        var buffer = new byte[Constants.BufferSize];
        
        while (_isConnected && _stream != null)
        {
            try
            {
                var message = await ReadMessageAsync(buffer);
                if (message == null)
                {
                    Disconnect();
                    break;
                }

                await HandleMessageAsync(message);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Erreur de réception: {ex.Message}");
                Disconnect();
                break;
            }
        }
    }

    private async Task<NetworkMessage?> ReadMessageAsync(byte[] buffer)
    {
        if (_stream == null) return null;

        var lengthBytes = new byte[4];
        var bytesRead = await _stream.ReadAsync(lengthBytes, 0, 4);
        if (bytesRead != 4) return null;

        var length = BitConverter.ToInt32(lengthBytes, 0);
        if (length <= 0 || length > 10 * 1024 * 1024) return null;

        var messageBytes = new byte[length];
        var totalRead = 0;
        while (totalRead < length)
        {
            var read = await _stream.ReadAsync(messageBytes, totalRead, length - totalRead);
            if (read == 0) return null;
            totalRead += read;
        }

        var json = Encoding.UTF8.GetString(messageBytes);
        return JsonConvert.DeserializeObject<NetworkMessage>(json);
    }

    private async Task HandleMessageAsync(NetworkMessage message)
    {
        switch (message.Type)
        {
            case MessageType.UserList:
                var userListJson = message.Data?.ToString() ?? "[]";
                var userList = JsonConvert.DeserializeObject<List<dynamic>>(userListJson) ?? new List<dynamic>();
                var users = userList.Select(u => new User
                {
                    Id = u.Id.ToString(),
                    Name = u.Name.ToString(),
                    IsConnected = u.IsConnected
                }).ToList();
                UserListReceived?.Invoke(this, users);
                break;

            case MessageType.TransferRequest:
                await HandleTransferRequestAsync(message);
                break;

            case MessageType.FileChunk:
                await HandleFileChunkAsync(message);
                break;

            case MessageType.FileComplete:
                await HandleFileCompleteAsync(message);
                break;

            case MessageType.Error:
                ErrorOccurred?.Invoke(this, message.Data?.ToString() ?? "Erreur inconnue");
                break;
        }
    }

    private async Task HandleTransferRequestAsync(NetworkMessage message)
    {
        try
        {
            var transferData = JsonConvert.DeserializeObject<TransferRequestData>(message.Data?.ToString() ?? "{}");
            if (transferData == null) return;

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

            // Store metadata for file reception
            foreach (var file in request.Files)
            {
                _fileMetadata[file.FileName] = new FileMetadata
                {
                    FileName = file.FileName,
                    FileSize = file.FileSize,
                    FileType = file.FileType,
                    IsImage = file.IsImage,
                    IsPdf = file.IsPdf
                };
                _fileChunks[file.FileName] = new List<byte[]>();
            }

            TransferReceived?.Invoke(this, request);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Erreur lors de la réception du transfert: {ex.Message}");
        }
    }

    private async Task HandleFileChunkAsync(NetworkMessage message)
    {
        try
        {
            var chunkData = JsonConvert.DeserializeObject<FileChunkData>(message.Data?.ToString() ?? "{}");
            if (chunkData == null) return;

            if (!_fileChunks.ContainsKey(chunkData.FileName))
            {
                _fileChunks[chunkData.FileName] = new List<byte[]>();
            }

            _fileChunks[chunkData.FileName].Add(chunkData.Data);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Erreur lors de la réception du chunk: {ex.Message}");
        }
    }

    private async Task HandleFileCompleteAsync(NetworkMessage message)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<dynamic>(message.Data?.ToString() ?? "{}");
            if (data == null) return;

            string fileName = data.fileName?.ToString() ?? "";
            if (string.IsNullOrEmpty(fileName) || !_fileChunks.ContainsKey(fileName))
                return;

            // Combine all chunks
            var allChunks = _fileChunks[fileName];
            var totalSize = allChunks.Sum(chunk => chunk.Length);
            var fileBytes = new byte[totalSize];
            var offset = 0;

            foreach (var chunk in allChunks)
            {
                Array.Copy(chunk, 0, fileBytes, offset, chunk.Length);
                offset += chunk.Length;
            }

            // Decrypt file
            var decryptedBytes = _encryptionService.Decrypt(fileBytes, _sharedKey);

            // Save file
            var downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "DODtransfert");
            if (!Directory.Exists(downloadsPath))
            {
                Directory.CreateDirectory(downloadsPath);
            }

            var filePath = Path.Combine(downloadsPath, fileName);
            await File.WriteAllBytesAsync(filePath, decryptedBytes);

            // Cleanup
            _fileChunks.Remove(fileName);
            _fileMetadata.Remove(fileName);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Erreur lors de la sauvegarde du fichier: {ex.Message}");
        }
    }
}
