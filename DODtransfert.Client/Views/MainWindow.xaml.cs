using System.Windows;
using DODtransfert.Client.ViewModels;

namespace DODtransfert.Client.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Set initial view to login
        if (viewModel.CurrentView == null)
        {
            viewModel.CurrentView = viewModel.LoginViewModel;
        }
    }
}
