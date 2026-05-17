using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleaner.Models;
using DiskCleaner.Services;

namespace DiskCleaner.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigService _configService;
    
    [ObservableProperty]
    private bool _scanJunkFiles = true;
    
    [ObservableProperty]
    private bool _scanUnusedFiles = true;
    
    [ObservableProperty]
    private bool _scanLargeFiles = true;
    
    [ObservableProperty]
    private bool _scanUnimportantFiles = true;
    
    [ObservableProperty]
    private int _unusedDaysThreshold = 90;
    
    [ObservableProperty]
    private int _largeFileSizeThreshold = 500;
    
    [ObservableProperty]
    private string _selectedLanguage = "zh-CN";
    
    [ObservableProperty]
    private string _selectedTheme = "Auto";
    
    public SettingsViewModel(IConfigService configService)
    {
        _configService = configService;
        LoadSettings();
    }
    
    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        var options = new ScanOptions
        {
            ScanJunkFiles = ScanJunkFiles,
            ScanUnusedFiles = ScanUnusedFiles,
            ScanLargeFiles = ScanLargeFiles,
            ScanUnimportantFiles = ScanUnimportantFiles,
            UnusedDaysThreshold = UnusedDaysThreshold,
            LargeFileSizeThreshold = LargeFileSizeThreshold * 1024 * 1024
        };
        
        await _configService.SaveOptionsAsync(options);
    }
    
    private async void LoadSettings()
    {
        var options = await _configService.LoadOptionsAsync();
        
        ScanJunkFiles = options.ScanJunkFiles;
        ScanUnusedFiles = options.ScanUnusedFiles;
        ScanLargeFiles = options.ScanLargeFiles;
        ScanUnimportantFiles = options.ScanUnimportantFiles;
        UnusedDaysThreshold = options.UnusedDaysThreshold;
        LargeFileSizeThreshold = (int)(options.LargeFileSizeThreshold / 1024 / 1024);
    }
}
