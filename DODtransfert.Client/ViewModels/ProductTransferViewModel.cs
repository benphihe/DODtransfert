using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DODtransfert.Client.Services;
using DODtransfert.Shared.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace DODtransfert.Client.ViewModels;

public partial class ProductTransferViewModel : ObservableObject
{
    private readonly NetworkService _networkService;
    private readonly UserService _userService;
    private readonly FileTransferService _fileTransferService;

    [ObservableProperty]
    private ObservableCollection<User> availableUsers = new();

    [ObservableProperty]
    private User? selectedUser;

    [ObservableProperty]
    private ObservableCollection<FileItem> selectedPhotos = new();

    [ObservableProperty]
    private ObservableCollection<FileItem> selectedPdfs = new();

    [ObservableProperty]
    private string brandName = string.Empty;

    [ObservableProperty]
    private string productName = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isSending;

    [ObservableProperty]
    private string photoValidationMessage = string.Empty;

    [ObservableProperty]
    private string pdfValidationMessage = string.Empty;

    [ObservableProperty]
    private string brandValidationMessage = string.Empty;

    [ObservableProperty]
    private string productValidationMessage = string.Empty;

    public bool CanSend => 
        SelectedPhotos.Count > 0 && 
        SelectedPdfs.Count > 0 && 
        !string.IsNullOrWhiteSpace(BrandName) && 
        !string.IsNullOrWhiteSpace(ProductName) &&
        !IsSending;

    public ProductTransferViewModel(NetworkService networkService, UserService userService)
    {
        _networkService = networkService;
        _userService = userService;
        _fileTransferService = new FileTransferService();

        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SelectedPhotos) ||
                e.PropertyName == nameof(SelectedPdfs) ||
                e.PropertyName == nameof(BrandName) ||
                e.PropertyName == nameof(ProductName) ||
                e.PropertyName == nameof(IsSending))
            {
                OnPropertyChanged(nameof(CanSend));
                UpdateValidationMessages();
            }
        };
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

    private void UpdateValidationMessages()
    {
        PhotoValidationMessage = SelectedPhotos.Count == 0 ? "Au moins une photo est requise" : "";
        PdfValidationMessage = SelectedPdfs.Count == 0 ? "Au moins un PDF est requis" : "";
        BrandValidationMessage = string.IsNullOrWhiteSpace(BrandName) ? "Le nom de la marque est requis" : "";
        ProductValidationMessage = string.IsNullOrWhiteSpace(ProductName) ? "Le nom du produit est requis" : "";
    }

    [RelayCommand]
    private void SelectPhotos()
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
        };

        if (dialog.ShowDialog() == true)
        {
            var files = _fileTransferService.GetFilesFromPaths(dialog.FileNames.ToList())
                .Where(f => f.IsImage)
                .ToList();

            foreach (var file in files)
            {
                if (!SelectedPhotos.Any(f => f.FilePath == file.FilePath))
                {
                    SelectedPhotos.Add(file);
                }
            }
            StatusMessage = $"{SelectedPhotos.Count} photo(s) sélectionnée(s)";
        }
    }

    [RelayCommand]
    private void SelectPdfs()
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "PDF|*.pdf"
        };

        if (dialog.ShowDialog() == true)
        {
            var files = _fileTransferService.GetFilesFromPaths(dialog.FileNames.ToList())
                .Where(f => f.IsPdf)
                .ToList();

            foreach (var file in files)
            {
                if (!SelectedPdfs.Any(f => f.FilePath == file.FilePath))
                {
                    SelectedPdfs.Add(file);
                }
            }
            StatusMessage = $"{SelectedPdfs.Count} PDF(s) sélectionné(s)";
        }
    }

    [RelayCommand]
    private void RemovePhoto(FileItem file)
    {
        SelectedPhotos.Remove(file);
        StatusMessage = $"{SelectedPhotos.Count} photo(s) sélectionnée(s)";
    }

    [RelayCommand]
    private void RemovePdf(FileItem file)
    {
        SelectedPdfs.Remove(file);
        StatusMessage = $"{SelectedPdfs.Count} PDF(s) sélectionné(s)";
    }

    [RelayCommand]
    private async Task SendProductTransferAsync()
    {
        if (!CanSend)
        {
            StatusMessage = "Veuillez remplir tous les champs requis";
            return;
        }

        if (SelectedUser == null)
        {
            StatusMessage = "Veuillez sélectionner un destinataire";
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
            var allFiles = SelectedPhotos.Concat(SelectedPdfs).ToList();
            var productData = new ProductTransfer
            {
                BrandName = BrandName,
                ProductName = ProductName,
                Photos = SelectedPhotos.ToList(),
                Pdfs = SelectedPdfs.ToList()
            };

            var request = new TransferRequest
            {
                RecipientId = SelectedUser.Id,
                RecipientName = SelectedUser.Name,
                Files = allFiles,
                IsProductTransfer = true,
                ProductData = productData
            };

            await _networkService.SendTransferRequestAsync(request);
            StatusMessage = "Transfert de produit envoyé avec succès";
            
            // Reset form
            SelectedPhotos.Clear();
            SelectedPdfs.Clear();
            BrandName = string.Empty;
            ProductName = string.Empty;
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
