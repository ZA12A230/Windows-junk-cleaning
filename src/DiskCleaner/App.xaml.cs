using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DiskCleaner.Core.Scanning;
using DiskCleaner.Core.Cleaning;
using DiskCleaner.Services;
using DiskCleaner.UI.ViewModels;
using DiskCleaner.UI.Views;

namespace DiskCleaner;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        try
        {
            Helpers.PrivilegeHelper.EnableAllPrivileges();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to enable privileges: {ex.Message}");
        }
        
        var host = Host.CreateDefaultBuilder()
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
                services.AddSingleton<MainWindow>();
                services.AddSingleton<CleanViewModel>();
                services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            })
            .Build();
        
        Services = host.Services;
        
        try
        {
            var logger = Services.GetRequiredService<ILogger<App>>();
            logger.LogInformation("Application started");
        }
        catch { }
    }
    
    public static T GetService<T>() where T : class
    {
        return Services.GetRequiredService<T>();
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
