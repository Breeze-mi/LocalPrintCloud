using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PrintClient.Services;
using PrintClient.ViewModels;

namespace PrintClient;

public partial class App : Application
{
    public static ServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        var mainWindow = new MainWindow
        {
            DataContext = ServiceProvider.GetRequiredService<MainViewModel>()
        };
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IApiClient, ApiClient>();
        services.AddTransient<MainViewModel>();
    }
}
