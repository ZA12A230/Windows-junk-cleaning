using System.Windows;
using System.Windows.Controls;
using DiskCleaner.Services;
using DiskCleaner.Models;

namespace DiskCleaner.UI.Views;

public partial class SettingsView : Page
{
    private readonly IConfigService _configService;
    
    public SettingsView()
    {
        InitializeComponent();
        _configService = App.GetService<IConfigService>();
        SaveBtn.Click += async (s, e) => await SaveSettingsAsync();
    }
    
    private async Task SaveSettingsAsync()
    {
        var options = new ScanOptions
        {
            ScanJunkFiles = JunkFilesCheck.IsChecked ?? true,
            ScanUnusedFiles = UnusedFilesCheck.IsChecked ?? true,
            ScanLargeFiles = LargeFilesCheck.IsChecked ?? true,
            ScanUnimportantFiles = UnimportantFilesCheck.IsChecked ?? true,
            UnusedDaysThreshold = int.TryParse(UnusedDaysText.Text, out var d) ? d : 90,
            LargeFileSizeThreshold = (long)(int.TryParse(LargeSizeText.Text, out var s) ? s : 500) * 1024 * 1024
        };
        
        await _configService.SaveOptionsAsync(options);
        MessageBox.Show("设置已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
