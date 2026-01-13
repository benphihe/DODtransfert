using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DODtransfert.Client.Services;
using DODtransfert.Shared.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace DODtransfert.Client.ViewModels;

public partial class TransferViewModel : ObservableObject
{
    private readonly NetworkService _networkService;
    private readonly UserService _userService;
    private readonly FileTransferService _fileTransferService;

    [ObservableProperty]
    private ObservableCollection<User> availableUsers = new();

    [ObservableProperty]
    private User? selectedUser;

    [ObservableProperty]
    private ObservableCollection<FileItem> selectedFiles = new();

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isSending;

    public TransferViewModel(NetworkService networkService, UserService userService)
    {
        _networkService = networkService;
        _userService = userService;
        _fileTransferService = new FileTransferService();
    }

    public void UpdateAvailableUsers(List<User> users)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            AvailableUsers.Clear();
            foreach (var user in users)
            {
                AvailableUsers.Add(user);
            }
        });
    }

    [RelayCommand]
    private void SelectFiles()
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.gif|PDF|*.pdf|Tous les fichiers|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            var files = _fileTransferService.GetFilesFromPaths(dialog.FileNames.ToList());
            foreach (var file in files)
            {
                if (!SelectedFiles.Any(f => f.FilePath == file.FilePath))
                {
                    SelectedFiles.Add(file);
                }
            }
            StatusMessage = $"{SelectedFiles.Count} fichier(s) sélectionné(s)";
        }
    }

    [RelayCommand]
    private void RemoveFile(FileItem file)
    {
        SelectedFiles.Remove(file);
        StatusMessage = $"{SelectedFiles.Count} fichier(s) sélectionné(s)";
    }

    [RelayCommand]
    private async Task SendTransferAsync()
    {
        if (SelectedUser == null)
        {
            StatusMessage = "Veuillez sélectionner un destinataire";
            return;
        }

        if (SelectedFiles.Count == 0)
        {
            StatusMessage = "Veuillez sélectionner au moins un fichier";
            return;
        }

        if (!_networkService.IsConnected)
        {
            StatusMessage = "Non connecté au réseau";
            return;
        }

        IsSending = true;
        StatusMessage = "Envoi en cours...";

        try
        {
            var request = new TransferRequest
            {
                RecipientId = SelectedUser.Id,
                RecipientName = SelectedUser.Name,
                Files = SelectedFiles.ToList(),
                IsProductTransfer = false
            };

            await _networkService.SendTransferRequestAsync(request);
            StatusMessage = "Transfert envoyé avec succès";
            SelectedFiles.Clear();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erreur: {ex.Message}";
        }
        finally
        {
            IsSending = false;
        }
    }
}
