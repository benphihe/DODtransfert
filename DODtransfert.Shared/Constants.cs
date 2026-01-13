namespace DODtransfert.Shared;

public static class Constants
{
    public const int DefaultPort = 8888;
    public const int MaxFileSize = 100 * 1024 * 1024; // 100 MB
    public const int BufferSize = 8192;
    
    public static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
    public static readonly string[] AllowedPdfExtensions = { ".pdf" };
    
    public const string UsersDataFile = "users.json";
    public const string DataDirectory = "Data";
}
