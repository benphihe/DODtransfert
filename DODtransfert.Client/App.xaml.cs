using System.Windows;
using DODtransfert.Client.Services;
using DODtransfert.Client.ViewModels;
using DODtransfert.Client.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DODtransfert.Client;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<EncryptionService>();
        services.AddSingleton<UserService>();
        services.AddSingleton<NetworkService>(sp =>
        {
            var encryptionService = sp.GetRequiredService<EncryptionService>();
            return new NetworkService(encryptionService);
        });

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<TransferViewModel>();
        services.AddTransient<ProductTransferViewModel>();

        // Views
        services.AddTransient<MainWindow>(sp =>
        {
            var mainViewModel = sp.GetRequiredService<MainViewModel>();
            return new MainWindow(mainViewModel);
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
