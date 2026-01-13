using System.IO;
using System.Text.Json;
using DODtransfert.Shared;
using DODtransfert.Shared.Models;

namespace DODtransfert.Client.Services;

public class UserService
{
    private readonly string _dataDirectory;
    private readonly string _usersFilePath;
    private User? _currentUser;
    private List<User> _knownUsers = new();

    public UserService()
    {
        _dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.DataDirectory);
        _usersFilePath = Path.Combine(_dataDirectory, Constants.UsersDataFile);
        
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }
        
        LoadUsers();
    }

    public User? CurrentUser => _currentUser;

    public List<User> KnownUsers => _knownUsers;

    public User CreateUser(string name)
    {
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            IsConnected = false,
            LastSeen = DateTime.Now
        };

        _knownUsers.Add(user);
        SaveUsers();
        return user;
    }

    public User? GetUserById(string userId)
    {
        return _knownUsers.FirstOrDefault(u => u.Id == userId);
    }

    public User? GetOrCreateUser(string userId, string name)
    {
        var user = GetUserById(userId);
        if (user == null)
        {
            user = new User
            {
                Id = userId,
                Name = name,
                IsConnected = false,
                LastSeen = DateTime.Now
            };
            _knownUsers.Add(user);
            SaveUsers();
        }
        else
        {
            user.Name = name;
            user.LastSeen = DateTime.Now;
            SaveUsers();
        }
        return user;
    }

    public void SetCurrentUser(User user)
    {
        _currentUser = user;
        if (!_knownUsers.Any(u => u.Id == user.Id))
        {
            _knownUsers.Add(user);
            SaveUsers();
        }
    }

    public void UpdateUserConnectionStatus(string userId, bool isConnected, string? ipAddress = null, int? port = null)
    {
        var user = GetUserById(userId);
        if (user != null)
        {
            user.IsConnected = isConnected;
            user.IpAddress = ipAddress;
            user.Port = port;
            user.LastSeen = DateTime.Now;
            SaveUsers();
        }
    }

    public void AddOrUpdateKnownUser(User user)
    {
        var existing = GetUserById(user.Id);
        if (existing != null)
        {
            existing.Name = user.Name;
            existing.IsConnected = user.IsConnected;
            existing.IpAddress = user.IpAddress;
            existing.Port = user.Port;
            existing.LastSeen = user.LastSeen;
        }
        else
        {
            _knownUsers.Add(user);
        }
        SaveUsers();
    }

    private void LoadUsers()
    {
        if (File.Exists(_usersFilePath))
        {
            try
            {
                var json = File.ReadAllText(_usersFilePath);
                _knownUsers = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
            }
            catch
            {
                _knownUsers = new List<User>();
            }
        }
    }

    private void SaveUsers()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_knownUsers, options);
            File.WriteAllText(_usersFilePath, json);
        }
        catch
        {
            // Log error in production
        }
    }
}
