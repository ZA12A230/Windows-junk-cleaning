using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleaner.Models;
using DiskCleaner.Services;

namespace DiskCleaner.UI.ViewModels;

public partial class UninstallViewModel : ObservableObject
{
    private readonly IRegistryService _registryService;
    
    [ObservableProperty]
    private ObservableCollection<InstalledSoftware> _softwareList = new();
    
    [ObservableProperty]
    private InstalledSoftware? _selectedSoftware;
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private string _statusText = "";
    
    [ObservableProperty]
    private ObservableCollection<string> _residualItems = new();
    
    [ObservableProperty]
    private bool _includeResidualFiles = true;
    
    [ObservableProperty]
    private bool _includeResidualRegistry = true;
    
    [ObservableProperty]
    private bool _createRestorePoint = false;
    
    [ObservableProperty]
    private bool _backupRegistry = true;

    public UninstallViewModel(IRegistryService registryService)
    {
        _registryService = registryService;
    }
    
    [RelayCommand]
    private async Task LoadSoftwareListAsync()
    {
        IsLoading = true;
        StatusText = "正在加载已安装软件列表...";
        
        try
        {
            var software = await _registryService.GetInstalledSoftwareAsync();
            
            SoftwareList.Clear();
            foreach (var item in software)
            {
                SoftwareList.Add(item);
            }
            
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
    
    [RelayCommand]
    private async Task UninstallSelectedAsync()
    {
        if (SelectedSoftware == null)
            return;
        
        await UninstallAsync(SelectedSoftware);
    }
    
    [RelayCommand]
    private async Task ExportToCsvAsync()
    {
        // TODO: Implement CSV export
        await Task.CompletedTask;
    }
    
    private async Task UninstallAsync(InstalledSoftware software)
    {
        StatusText = $"正在卸载 {software.DisplayName}...";
        
        try
        {
            // 1. 运行官方卸载程序
            if (!string.IsNullOrEmpty(software.UninstallString))
            {
                await RunUninstallerAsync(software.UninstallString);
            }
            
            // 2. 扫描残留
            var residualRegistry = await _registryService.ScanResidualRegistryAsync(
                software.DisplayName, 
                software.Publisher);
            
            ResidualItems.Clear();
            foreach (var item in residualRegistry)
            {
                ResidualItems.Add(item);
            }
            
            StatusText = $"扫描到 {ResidualItems.Count} 个残留注册表项";
            
            // TODO: 显示确认对话框并清理
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
                    UseShellExecute = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal
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
}
