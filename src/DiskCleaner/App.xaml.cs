using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace DiskCleaner;

public partial class App : Application
{
    private IHost? _host;
    
    public static T GetService<T>() where T : class
    {
        var app = (App)Current;
        return app._host?.Services.GetRequiredService<T>() 
            ?? throw new InvalidOperationException($"Service {typeof(T).Name} not registered");
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            Helpers.PrivilegeHelper.EnableAllPrivileges();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to enable privileges: {ex.Message}");
        }
        
        _host = Host.CreateDefaultBuilder()
            .ConfigureLogging(builder =>
            {
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<IConfigService, ConfigService>();
                services.AddSingleton<IRegistryService, RegistryService>();
                services.AddSingleton<FileClassifier>();
                services.AddSingleton<IScannerEngine, ScannerEngine>();
                services.AddSingleton<ICleanerEngine, CleanerEngine>();
                services.AddSingleton<CleanViewModel>();
                services.AddSingleton<MainWindow>();
                services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            })
            .Build();
        
        try
        {
            var logger = _host.Services.GetRequiredService<ILogger<App>>();
            logger.LogInformation("Application started");
        }
        catch { }
        
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Activate();
    }
}

public class Logger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) => null;
    
    public bool IsEnabled(LogLevel logLevel) => true;
    
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        System.Diagnostics.Debug.WriteLine($"[{logLevel}] {typeof(T).Name}: {formatter(state, exception)}");
    }
}
