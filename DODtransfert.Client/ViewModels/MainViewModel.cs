using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DODtransfert.Client.Services;
using DODtransfert.Server;
using DODtransfert.Shared.Models;

namespace DODtransfert.Client.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly UserService _userService;
    private readonly NetworkService _networkService;
    private readonly EncryptionService _encryptionService;
    private ServerService? _serverService;

    [ObservableProperty]
    private User? currentUser;

    [ObservableProperty]
    private bool isServerMode;

    [ObservableProperty]
    private string statusMessage = "Prêt";

    [ObservableProperty]
    private string serverIp = "127.0.0.1";

    [ObservableProperty]
    private string localIp = "Non disponible";

    [ObservableProperty]
    private object? currentView;

    public LoginViewModel LoginViewModel { get; }
    public TransferViewModel TransferViewModel { get; }
    public ProductTransferViewModel ProductTransferViewModel { get; }

    public MainViewModel(
        UserService userService,
        NetworkService networkService,
        EncryptionService encryptionService)
    {
        _userService = userService;
        _networkService = networkService;
        _encryptionService = encryptionService;

        LoginViewModel = new LoginViewModel(userService);
        TransferViewModel = new TransferViewModel(_networkService, _userService);
        ProductTransferViewModel = new ProductTransferViewModel(_networkService, _userService);

        LoginViewModel.UserLoggedIn += OnUserLoggedIn;
        _networkService.UserListReceived += OnUserListReceived;
        _networkService.ErrorOccurred += OnNetworkError;
        
        UpdateLocalIp();
    }

    private void UpdateLocalIp()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    LocalIp = ip.ToString();
                    break;
                }
            }
        }
        catch
        {
            LocalIp = "Non disponible";
        }
    }

    private void OnUserLoggedIn(object? sender, User user)
    {
        CurrentUser = user;
        CurrentView = TransferViewModel;
        StatusMessage = $"Connecté: {user.Name}";
        
        // Récupérer l'IP du serveur depuis LoginViewModel si disponible
        if (sender is LoginViewModel loginVm && !string.IsNullOrWhiteSpace(loginVm.ServerIp))
        {
            ServerIp = loginVm.ServerIp;
        }
    }

    private void OnUserListReceived(object? sender, List<User> users)
    {
        TransferViewModel.UpdateAvailableUsers(users);
        ProductTransferViewModel.UpdateAvailableUsers(users);
    }

    private void OnNetworkError(object? sender, string error)
    {
        StatusMessage = $"Erreur: {error}";
    }

    [RelayCommand]
    private async Task StartServerAsync()
    {
        if (_serverService != null) return;

        _serverService = new ServerService();
        _serverService.UserConnected += OnServerUserConnected;
        _serverService.TransferReceived += OnServerTransferReceived;

        await _serverService.StartAsync();
        IsServerMode = true;
        UpdateLocalIp();
        StatusMessage = $"Serveur démarré sur {LocalIp}:8888";
    }

    [RelayCommand]
    private void StopServer()
    {
        _serverService?.Stop();
        _serverService = null;
        IsServerMode = false;
        StatusMessage = "Serveur arrêté";
    }

    [RelayCommand]
    private async Task ConnectToServerAsync()
    {
        if (CurrentUser == null)
        {
            StatusMessage = "Veuillez d'abord vous connecter";
            return;
        }

        if (string.IsNullOrWhiteSpace(ServerIp))
        {
            StatusMessage = "Veuillez entrer l'adresse IP du serveur";
            return;
        }

        StatusMessage = $"Connexion à {ServerIp}...";
        var connected = await _networkService.ConnectAsync(ServerIp, 8888, CurrentUser);
        if (connected)
        {
            StatusMessage = $"Connecté au serveur {ServerIp}";
        }
        else
        {
            StatusMessage = $"Échec de la connexion à {ServerIp}";
        }
    }

    [RelayCommand]
    private void NavigateToTransfer()
    {
        CurrentView = TransferViewModel;
    }

    [RelayCommand]
    private void NavigateToProductTransfer()
    {
        CurrentView = ProductTransferViewModel;
    }

    private void OnServerUserConnected(object? sender, User user)
    {
        StatusMessage = $"Utilisateur connecté: {user.Name}";
    }

    private void OnServerTransferReceived(object? sender, (string userId, TransferRequest request) data)
    {
        StatusMessage = $"Transfert reçu de {data.userId}";
    }
}
