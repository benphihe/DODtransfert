using Newtonsoft.Json;

namespace DODtransfert.Shared;

public enum MessageType
{
    Authentication,
    AuthenticationResponse,
    UserList,
    TransferRequest,
    TransferResponse,
    FileChunk,
    FileComplete,
    ProductTransfer,
    Error
}

public class NetworkMessage
{
    [JsonProperty("type")]
    public MessageType Type { get; set; }
    
    [JsonProperty("data")]
    public object? Data { get; set; }
    
    [JsonProperty("senderId")]
    public string? SenderId { get; set; }
    
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }
}

public class AuthenticationData
{
    [JsonProperty("userId")]
    public string UserId { get; set; } = string.Empty;
    
    [JsonProperty("userName")]
    public string UserName { get; set; } = string.Empty;
}

public class TransferRequestData
{
    [JsonProperty("recipientId")]
    public string RecipientId { get; set; } = string.Empty;
    
    [JsonProperty("files")]
    public List<FileMetadata> Files { get; set; } = new();
    
    [JsonProperty("isProductTransfer")]
    public bool IsProductTransfer { get; set; }
    
    [JsonProperty("productData")]
    public ProductData? ProductData { get; set; }
}

public class FileMetadata
{
    [JsonProperty("fileName")]
    public string FileName { get; set; } = string.Empty;
    
    [JsonProperty("fileSize")]
    public long FileSize { get; set; }
    
    [JsonProperty("fileType")]
    public string FileType { get; set; } = string.Empty;
    
    [JsonProperty("isImage")]
    public bool IsImage { get; set; }
    
    [JsonProperty("isPdf")]
    public bool IsPdf { get; set; }
}

public class ProductData
{
    [JsonProperty("brandName")]
    public string BrandName { get; set; } = string.Empty;
    
    [JsonProperty("productName")]
    public string ProductName { get; set; } = string.Empty;
}

public class FileChunkData
{
    [JsonProperty("fileName")]
    public string FileName { get; set; } = string.Empty;
    
    [JsonProperty("chunkIndex")]
    public int ChunkIndex { get; set; }
    
    [JsonProperty("totalChunks")]
    public int TotalChunks { get; set; }
    
    [JsonProperty("data")]
    public byte[] Data { get; set; } = Array.Empty<byte>();
}
