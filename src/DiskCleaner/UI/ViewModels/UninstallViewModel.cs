using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DiskCleaner.Models;
using DiskCleaner.Services;

namespace DiskCleaner.UI.ViewModels;

public class UninstallViewModel : INotifyPropertyChanged
{
    private readonly IRegistryService _registryService;
    private ObservableCollection<InstalledSoftware> _softwareList = new();
    private InstalledSoftware? _selectedSoftware;
    private bool _isLoading;
    private string _statusText = "";
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public UninstallViewModel(IRegistryService registryService)
    {
        _registryService = registryService;
    }
    
    public ObservableCollection<InstalledSoftware> SoftwareList { get => _softwareList; set { _softwareList = value; OnPropertyChanged(); } }
    public InstalledSoftware? SelectedSoftware { get => _selectedSoftware; set { _selectedSoftware = value; OnPropertyChanged(); } }
    public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }
    public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }
    
    public ICommand LoadSoftwareListCommand => new RelayCommand(async _ => await LoadSoftwareListAsync());
    public ICommand UninstallSelectedCommand => new RelayCommand(async _ => await UninstallSelectedAsync(), _ => SelectedSoftware != null);
    public ICommand ExportToCsvCommand => new RelayCommand(async _ => await ExportToCsvAsync());
    
    private async Task LoadSoftwareListAsync()
    {
        IsLoading = true;
        StatusText = "正在加载已安装软件列表...";
        
        try
        {
            var software = await _registryService.GetInstalledSoftwareAsync();
            SoftwareList.Clear();
            foreach (var item in software)
                SoftwareList.Add(item);
            StatusText = $"已加载 {SoftwareList.Count} 个软件";
        }
        catch (Exception ex)
        {
            StatusText = $"加载失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private async Task UninstallSelectedAsync()
    {
        if (SelectedSoftware == null) return;
        
        StatusText = $"正在卸载 {SelectedSoftware.DisplayName}...";
        
        try
        {
            if (!string.IsNullOrEmpty(SelectedSoftware.UninstallString))
            {
                await RunUninstallerAsync(SelectedSoftware.UninstallString);
            }
            
            var residualRegistry = await _registryService.ScanResidualRegistryAsync(
                SelectedSoftware.DisplayName, SelectedSoftware.Publisher);
            
            StatusText = $"扫描到 {residualRegistry.Count} 个残留注册表项";
        }
        catch (Exception ex)
        {
            StatusText = $"卸载失败：{ex.Message}";
        }
    }
    
    private static async Task RunUninstallerAsync(string uninstallString)
    {
        await Task.Run(() =>
        {
            try
            {
                var parts = uninstallString.Trim('"').Split(' ', 2);
                var exePath = parts[0].Trim('"');
                var args = parts.Length > 1 ? parts[1] : "";
                
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = args,
                    UseShellExecute = true
                };
                
                using var process = System.Diagnostics.Process.Start(startInfo);
                process?.WaitForExit();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to run uninstaller: {ex.Message}", ex);
            }
        });
    }
    
    private Task ExportToCsvAsync()
    {
        // TODO: 实现 CSV 导出
        return Task.CompletedTask;
    }
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly IConfigService _configService;
    private bool _scanJunkFiles = true;
    private bool _scanUnusedFiles = true;
    private bool _scanLargeFiles = true;
    private bool _scanUnimportantFiles = true;
    private int _unusedDaysThreshold = 90;
    private int _largeFileSizeThreshold = 500;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public SettingsViewModel(IConfigService configService)
    {
        _configService = configService;
        LoadSettings();
    }
    
    public bool ScanJunkFiles { get => _scanJunkFiles; set { _scanJunkFiles = value; OnPropertyChanged(); } }
    public bool ScanUnusedFiles { get => _scanUnusedFiles; set { _scanUnusedFiles = value; OnPropertyChanged(); } }
    public bool ScanLargeFiles { get => _scanLargeFiles; set { _scanLargeFiles = value; OnPropertyChanged(); } }
    public bool ScanUnimportantFiles { get => _scanUnimportantFiles; set { _scanUnimportantFiles = value; OnPropertyChanged(); } }
    public int UnusedDaysThreshold { get => _unusedDaysThreshold; set { _unusedDaysThreshold = value; OnPropertyChanged(); } }
    public int LargeFileSizeThreshold { get => _largeFileSizeThreshold; set { _largeFileSizeThreshold = value; OnPropertyChanged(); } }
    
    public ICommand SaveSettingsCommand => new RelayCommand(async _ => await SaveSettingsAsync());
    
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
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
