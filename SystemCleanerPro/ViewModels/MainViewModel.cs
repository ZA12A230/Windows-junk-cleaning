using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SystemCleanerPro.Models;
using SystemCleanerPro.Services;

namespace SystemCleanerPro.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ScanService _scanService;
    private readonly CleanService _cleanService;
    private readonly UninstallService _uninstallService;
    private readonly ScanConfiguration _config;
    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    private int _selectedNavIndex;

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private int _progressValue;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isCleaning;

    [ObservableProperty]
    private long _totalSize;

    [ObservableProperty]
    private int _totalFiles;

    [ObservableProperty]
    private int _selectedFiles;

    [ObservableProperty]
    private long _selectedSize;

    public ObservableCollection<FileItem> JunkFiles { get; } = new();
    public ObservableCollection<FileItem> UnusedFiles { get; } = new();
    public ObservableCollection<FileItem> LargeFiles { get; } = new();
    public ObservableCollection<FileItem> UnimportantFiles { get; } = new();
    public ObservableCollection<SoftwareInfo> InstalledSoftware { get; } = new();

    [ObservableProperty]
    private ObservableCollection<FileItem> _currentItems = new();

    [ObservableProperty]
    private DeleteMode _selectedDeleteMode = DeleteMode.RecycleBin;

    public MainViewModel()
    {
        _config = new ScanConfiguration();
        _scanService = new ScanService(_config);
        _cleanService = new CleanService();
        _uninstallService = new UninstallService();

        _scanService.OnStatusUpdate += status => StatusText = status;
        _scanService.OnProgressChanged += progress => ProgressValue = progress;
        _cleanService.OnStatusUpdate += status => StatusText = status;
        _cleanService.OnProgressChanged += progress => ProgressValue = progress;
        _uninstallService.OnStatusUpdate += status => StatusText = status;
    }

    partial void OnSelectedNavIndexChanged(int value)
    {
        CurrentItems = value switch
        {
            0 => JunkFiles,
            1 => UnusedFiles,
            2 => LargeFiles,
            3 => UnimportantFiles,
            4 => new ObservableCollection<FileItem>(),
            _ => JunkFiles
        };
        UpdateSelectionStats();
    }

    [RelayCommand]
    private async Task StartScan()
    {
        if (IsScanning) return;

        IsScanning = true;
        _cancellationTokenSource = new CancellationTokenSource();
        
        JunkFiles.Clear();
        UnusedFiles.Clear();
        LargeFiles.Clear();
        UnimportantFiles.Clear();
        TotalSize = 0;
        TotalFiles = 0;

        try
        {
            var results = await _scanService.ScanAllDrivesAsync(_cancellationTokenSource.Token);

            foreach (var item in results)
            {
                switch (item.Category)
                {
                    case FileCategory.Junk:
                        JunkFiles.Add(item);
                        break;
                    case FileCategory.Unused:
                        UnusedFiles.Add(item);
                        break;
                    case FileCategory.Large:
                        LargeFiles.Add(item);
                        break;
                    case FileCategory.Unimportant:
                    case FileCategory.Duplicate:
                        UnimportantFiles.Add(item);
                        break;
                }
            }

            TotalFiles = results.Count;
            TotalSize = results.Sum(r => r.Size);
            StatusText = $"扫描完成: 发现 {TotalFiles} 个文件 ({FormatSize(TotalSize)})";
        }
        catch (OperationCanceledException)
        {
            StatusText = "扫描已取消";
        }
        catch (Exception ex)
        {
            StatusText = $"扫描错误: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            UpdateSelectionStats();
        }
    }

    [RelayCommand]
    private void CancelScan()
    {
        _cancellationTokenSource?.Cancel();
    }

    [RelayCommand]
    private async Task CleanSelected()
    {
        if (IsCleaning) return;

        var selectedItems = CurrentItems.Where(i => i.IsSelected).ToList();
        if (!selectedItems.Any())
        {
            StatusText = "请先选择要清理的文件";
            return;
        }

        IsCleaning = true;

        try
        {
            var filePaths = selectedItems.Select(i => i.FullPath);
            var results = await _cleanService.CleanFilesAsync(filePaths, SelectedDeleteMode);

            var successCount = results.Count(r => r.Success);
            var failCount = results.Count(r => !r.Success);

            foreach (var item in selectedItems.Where(i => results.Any(r => r.Success && r.FilePath == i.FullPath)))
            {
                CurrentItems.Remove(item);
            }

            StatusText = $"清理完成: 成功 {successCount}, 失败 {failCount}";
            UpdateSelectionStats();
        }
        catch (Exception ex)
        {
            StatusText = $"清理错误: {ex.Message}";
        }
        finally
        {
            IsCleaning = false;
        }
    }

    [RelayCommand]
    private async Task LoadSoftware()
    {
        if (InstalledSoftware.Any()) return;

        StatusText = "正在加载已安装软件...";

        try
        {
            var software = await _uninstallService.GetInstalledSoftwareAsync();
            
            foreach (var item in software)
            {
                InstalledSoftware.Add(item);
            }

            StatusText = $"已加载 {software.Count} 个软件";
        }
        catch (Exception ex)
        {
            StatusText = $"加载错误: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task UninstallSelected()
    {
        var selected = InstalledSoftware.FirstOrDefault(s => s.IsSelected);
        if (selected == null)
        {
            StatusText = "请先选择要卸载的软件";
            return;
        }

        IsCleaning = true;

        try
        {
            var (success, message) = await _uninstallService.UninstallSoftwareAsync(selected);
            
            if (success)
            {
                InstalledSoftware.Remove(selected);
                StatusText = message;
            }
            else
            {
                StatusText = message;
            }
        }
        catch (Exception ex)
        {
            StatusText = $"卸载错误: {ex.Message}";
        }
        finally
        {
            IsCleaning = false;
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var item in CurrentItems)
        {
            item.IsSelected = true;
        }
        UpdateSelectionStats();
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var item in CurrentItems)
        {
            item.IsSelected = false;
        }
        UpdateSelectionStats();
    }

    public void UpdateSelectionStats()
    {
        SelectedFiles = CurrentItems.Count(i => i.IsSelected);
        SelectedSize = CurrentItems.Where(i => i.IsSelected).Sum(i => i.Size);
    }

    public void NotifySelectionChanged()
    {
        UpdateSelectionStats();
        OnPropertyChanged(nameof(CurrentItems));
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}
