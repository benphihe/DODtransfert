namespace DODtransfert.Shared.Models;

public class TransferRequest
{
    public string RecipientId { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public List<FileItem> Files { get; set; } = new();
    public bool IsProductTransfer { get; set; }
    public ProductTransfer? ProductData { get; set; }
}
