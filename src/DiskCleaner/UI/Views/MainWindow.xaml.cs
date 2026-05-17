using System.Windows;

namespace DiskCleaner.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // 导航到初始页面
        CleanFrame.Navigate(new CleanView());
        UninstallFrame.Navigate(new UninstallView());
        SettingsFrame.Navigate(new SettingsView());
    }
}
