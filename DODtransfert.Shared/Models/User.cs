namespace DODtransfert.Shared.Models;

public class User
{
    public string Id { get; set; } = string.Empty; // UUID
    public string Name { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public string? IpAddress { get; set; }
    public int? Port { get; set; }
    public DateTime LastSeen { get; set; }
}
