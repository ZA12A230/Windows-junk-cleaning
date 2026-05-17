using System.Windows;
using System.Windows.Controls;
using DiskCleaner.Services;
using DiskCleaner.Models;

namespace DiskCleaner.UI.Views;

public partial class UninstallView : Page
{
    private readonly IRegistryService _registryService;
    
    public UninstallView()
    {
        InitializeComponent();
        _registryService = App.GetService<IRegistryService>();
        
        RefreshBtn.Click += async (s, e) => await LoadSoftwareListAsync();
        UninstallBtn.Click += async (s, e) => await UninstallSelectedAsync();
    }
    
    private async Task LoadSoftwareListAsync()
    {
        StatusText.Text = "正在加载...";
        try
        {
            var software = await _registryService.GetInstalledSoftwareAsync();
            SoftwareGrid.ItemsSource = software;
            StatusText.Text = $"已加载 {software.Count} 个软件";
        }
        catch (Exception ex) { StatusText.Text = $"加载失败：{ex.Message}"; }
    }
    
    private async Task UninstallSelectedAsync()
    {
        var software = SoftwareGrid.SelectedItem as InstalledSoftware;
        if (software == null) return;
        
        StatusText.Text = $"正在卸载 {software.DisplayName}...";
        if (!string.IsNullOrEmpty(software.UninstallString))
        {
            try
            {
                var parts = software.UninstallString.Trim('"').Split(' ', 2);
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = parts[0].Trim('"'),
                    Arguments = parts.Length > 1 ? parts[1] : "",
                    UseShellExecute = true
                };
                using var p = System.Diagnostics.Process.Start(psi);
                p?.WaitForExit();
                StatusText.Text = "卸载完成";
            }
            catch (Exception ex) { StatusText.Text = $"卸载失败：{ex.Message}"; }
        }
    }
}
