using System.Text.Json;
using DODtransfert.Shared;
using DODtransfert.Shared.Models;

namespace DODtransfert.Server;

public class UserRepository
{
    private readonly string _dataDirectory;
    private readonly string _usersFilePath;
    private readonly List<User> _connectedUsers = new();

    public UserRepository()
    {
        _dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.DataDirectory);
        _usersFilePath = Path.Combine(_dataDirectory, Constants.UsersDataFile);
        
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }
    }

    public void AddConnectedUser(User user)
    {
        var existing = _connectedUsers.FirstOrDefault(u => u.Id == user.Id);
        if (existing != null)
        {
            existing.Name = user.Name;
            existing.IsConnected = true;
            existing.IpAddress = user.IpAddress;
            existing.Port = user.Port;
            existing.LastSeen = DateTime.Now;
        }
        else
        {
            user.IsConnected = true;
            _connectedUsers.Add(user);
        }
    }

    public void RemoveConnectedUser(string userId)
    {
        var user = _connectedUsers.FirstOrDefault(u => u.Id == userId);
        if (user != null)
        {
            user.IsConnected = false;
            _connectedUsers.Remove(user);
        }
    }

    public List<User> GetConnectedUsers()
    {
        return _connectedUsers.Where(u => u.IsConnected).ToList();
    }

    public User? GetUserById(string userId)
    {
        return _connectedUsers.FirstOrDefault(u => u.Id == userId);
    }
}
