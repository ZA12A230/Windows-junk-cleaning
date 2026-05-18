using System.Windows;
using SystemCleanerPro.ViewModels;

namespace SystemCleanerPro;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var viewModel = new MainViewModel();
        var mainWindow = new MainWindow { DataContext = viewModel };
        mainWindow.Show();
    }
}
