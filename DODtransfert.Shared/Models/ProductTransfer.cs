namespace DODtransfert.Shared.Models;

public class ProductTransfer
{
    public string BrandName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public List<FileItem> Photos { get; set; } = new();
    public List<FileItem> Pdfs { get; set; } = new();
    
    public bool IsValid => 
        Photos.Count > 0 && 
        Pdfs.Count > 0 && 
        !string.IsNullOrWhiteSpace(BrandName) && 
        !string.IsNullOrWhiteSpace(ProductName);
}
