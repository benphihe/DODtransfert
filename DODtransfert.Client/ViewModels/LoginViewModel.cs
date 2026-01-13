using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DODtransfert.Client.Services;
using DODtransfert.Shared.Models;

namespace DODtransfert.Client.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly UserService _userService;

    [ObservableProperty]
    private string userName = string.Empty;

    [ObservableProperty]
    private string serverIp = "127.0.0.1";

    [ObservableProperty]
    private bool isServerMode = true;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public User? CurrentUser { get; private set; }

    public event EventHandler<User>? UserLoggedIn;
    public event EventHandler<string>? ErrorOccurred;

    public LoginViewModel(UserService userService)
    {
        _userService = userService;
    }

    [RelayCommand]
    private void Login()
    {
        if (string.IsNullOrWhiteSpace(UserName))
        {
            StatusMessage = "Veuillez entrer un nom d'utilisateur";
            ErrorOccurred?.Invoke(this, StatusMessage);
            return;
        }

        var user = _userService.CreateUser(UserName);
        _userService.SetCurrentUser(user);
        CurrentUser = user;

        StatusMessage = $"Connecté en tant que {user.Name}";
        UserLoggedIn?.Invoke(this, user);
    }

    [RelayCommand]
    private void LoadExistingUser(string userId)
    {
        var user = _userService.GetUserById(userId);
        if (user != null)
        {
            _userService.SetCurrentUser(user);
            CurrentUser = user;
            UserLoggedIn?.Invoke(this, user);
        }
    }
}
