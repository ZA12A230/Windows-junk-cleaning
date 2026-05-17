using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DiskCleaner.UI.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        Title = "DiskCleaner - Windows 系统清理工具";
        NavView.SelectedItem = NavView.MenuItems[0];
    }
    
    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var selectedItem = args.SelectedItem as NavigationViewItem;
        var tag = selectedItem?.Tag as string;
        
        switch (tag)
        {
            case "clean":
                ContentFrame.Navigate(typeof(CleanView));
                break;
            case "uninstall":
                ContentFrame.Navigate(typeof(UninstallView));
                break;
            case "settings":
                ContentFrame.Navigate(typeof(SettingsView));
                break;
        }
    }
}
