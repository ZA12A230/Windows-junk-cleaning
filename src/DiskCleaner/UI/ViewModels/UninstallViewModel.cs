using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DiskCleaner.Core.Cleaning;
using DiskCleaner.Core.Scanning;
using DiskCleaner.Models;

namespace DiskCleaner.UI.ViewModels;

public class CleanViewModel : INotifyPropertyChanged
{
    private readonly IScannerEngine _scannerEngine;
    private readonly ICleanerEngine _cleanerEngine;
    
    private bool _isScanning;
    private string _scanProgressText = "";
    private double _scanProgressValue;
    private ObservableCollection<FileEntry> _files = new();
    private FileEntry? _selectedFile;
    private long _totalSize;
    private int _totalCount;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public CleanViewModel(IScannerEngine scannerEngine, ICleanerEngine cleanerEngine)
    {
        _scannerEngine = scannerEngine;
        _cleanerEngine = cleanerEngine;
    }
    
    public bool IsScanning { get => _isScanning; set { _isScanning = value; OnPropertyChanged(); } }
    public string ScanProgressText { get => _scanProgressText; set { _scanProgressText = value; OnPropertyChanged(); } }
    public double ScanProgressValue { get => _scanProgressValue; set { _scanProgressValue = value; OnPropertyChanged(); } }
    public ObservableCollection<FileEntry> Files { get => _files; set { _files = value; OnPropertyChanged(); } }
    public FileEntry? SelectedFile { get => _selectedFile; set { _selectedFile = value; OnPropertyChanged(); } }
    public long TotalSize { get => _totalSize; set { _totalSize = value; OnPropertyChanged(); } }
    public int TotalCount { get => _totalCount; set { _totalCount = value; OnPropertyChanged(); } }
    
    public ICommand StartScanCommand => new RelayCommand(async _ => await StartScanAsync());
    public ICommand CancelScanCommand => new RelayCommand(_ => CancelScan());
    public ICommand CleanSelectedCommand => new RelayCommand(async _ => await CleanSelectedAsync(), _ => CanClean());
    public ICommand CleanAllCommand => new RelayCommand(async _ => await CleanAllAsync(), _ => CanClean());
    
    private async Task StartScanAsync()
    {
        if (IsScanning) return;
        
        IsScanning = true;
        Files.Clear();
        ScanProgressText = "正在扫描...";
        
        var options = new ScanOptions();
        var progress = new Progress<ScanProgress>(p =>
        {
            ScanProgressText = $"已发现 {p.FilesFound} 个文件";
            ScanProgressValue = p.ProgressPercentage;
            TotalSize = p.TotalSizeScanned;
        });
        
        try
        {
            var result = await _scannerEngine.ScanAllDrivesAsync(options, progress, CancellationToken.None);
            foreach (var file in result.AllFiles) Files.Add(file);
            TotalCount = result.TotalCount;
            TotalSize = result.TotalSize;
            ScanProgressText = $"扫描完成 - 发现 {TotalCount} 个文件";
        }
        catch (OperationCanceledException) { ScanProgressText = "扫描已取消"; }
        catch (Exception ex) { ScanProgressText = $"扫描失败：{ex.Message}"; }
        finally { IsScanning = false; }
    }
    
    private void CancelScan() { if (IsScanning) _scannerEngine.Cancel(); }
    private bool CanClean() => Files.Count > 0 && !IsScanning;
    
    private async Task CleanSelectedAsync()
    {
        if (SelectedFile == null) return;
        await CleanFilesAsync(new[] { SelectedFile }.ToList());
    }
    
    private async Task CleanAllAsync() => await CleanFilesAsync(Files.ToList());
    
    private async Task CleanFilesAsync(List<FileEntry> files)
    {
        IsScanning = true;
        try
        {
            var result = await _cleanerEngine.CleanAsync(files, true, null, CancellationToken.None);
            foreach (var f in result.DeletedFiles) Files.Remove(f);
            TotalCount = Files.Count;
            TotalSize = Files.Sum(f => f.FileSize);
            ScanProgressText = $"清理完成";
        }
        catch (Exception ex) { ScanProgressText = $"清理失败：{ex.Message}"; }
        finally { IsScanning = false; }
    }
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class UninstallViewModel : INotifyPropertyChanged
{
    private readonly IRegistryService _registryService;
    private ObservableCollection<InstalledSoftware> _softwareList = new();
    private InstalledSoftware? _selectedSoftware;
    private string _statusText = "";
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public UninstallViewModel(IRegistryService registryService) { _registryService = registryService; }
    
    public ObservableCollection<InstalledSoftware> SoftwareList { get => _softwareList; set { _softwareList = value; OnPropertyChanged(); } }
    public InstalledSoftware? SelectedSoftware { get => _selectedSoftware; set { _selectedSoftware = value; OnPropertyChanged(); } }
    public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }
    
    public ICommand LoadSoftwareListCommand => new RelayCommand(async _ => await LoadSoftwareListAsync());
    public ICommand UninstallSelectedCommand => new RelayCommand(async _ => await UninstallAsync(), _ => SelectedSoftware != null);
    public ICommand ExportToCsvCommand => new RelayCommand(_ => { });
    
    private async Task LoadSoftwareListAsync()
    {
        StatusText = "正在加载...";
        try
        {
            var software = await _registryService.GetInstalledSoftwareAsync();
            SoftwareList.Clear();
            foreach (var s in software) SoftwareList.Add(s);
            StatusText = $"已加载 {SoftwareList.Count} 个软件";
        }
        catch (Exception ex) { StatusText = $"加载失败：{ex.Message}"; }
    }
    
    private async Task UninstallAsync()
    {
        if (SelectedSoftware == null) return;
        StatusText = $"正在卸载 {SelectedSoftware.DisplayName}...";
        if (!string.IsNullOrEmpty(SelectedSoftware.UninstallString))
        {
            try
            {
                var parts = SelectedSoftware.UninstallString.Trim('"').Split(' ', 2);
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = parts[0].Trim('"'),
                    Arguments = parts.Length > 1 ? parts[1] : "",
                    UseShellExecute = true
                };
                using var p = System.Diagnostics.Process.Start(psi);
                p?.WaitForExit();
                StatusText = "卸载完成";
            }
            catch (Exception ex) { StatusText = $"卸载失败：{ex.Message}"; }
        }
    }
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
    
    public SettingsViewModel(IConfigService configService) { _configService = configService; }
    
    public bool ScanJunkFiles { get => _scanJunkFiles; set { _scanJunkFiles = value; OnPropertyChanged(); } }
    public bool ScanUnusedFiles { get => _scanUnusedFiles; set { _scanUnusedFiles = value; OnPropertyChanged(); } }
    public bool ScanLargeFiles { get => _scanLargeFiles; set { _scanLargeFiles = value; OnPropertyChanged(); } }
    public bool ScanUnimportantFiles { get => _scanUnimportantFiles; set { _scanUnimportantFiles = value; OnPropertyChanged(); } }
    public int UnusedDaysThreshold { get => _unusedDaysThreshold; set { _unusedDaysThreshold = value; OnPropertyChanged(); } }
    public int LargeFileSizeThreshold { get => _largeFileSizeThreshold; set { _largeFileSizeThreshold = value; OnPropertyChanged(); } }
    
    public ICommand SaveSettingsCommand => new RelayCommand(async _ => await SaveSettingsAsync());
    
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
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class RelayCommand : ICommand
{
    private readonly Func<object?, Task>? _executeAsync;
    private readonly Func<object?, bool>? _canExecute;
    
    public RelayCommand(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
    {
        _executeAsync = executeAsync;
        _canExecute = canExecute;
    }
    
    public event EventHandler? CanExecuteChanged
    {
        add => System.Windows.Input.CommandManager.RequerySuggested += value;
        remove => System.Windows.Input.CommandManager.RequerySuggested -= value;
    }
    
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public async void Execute(object? parameter) { if (_executeAsync != null) await _executeAsync(parameter); }
}
