using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleaner.Core.Cleaning;
using DiskCleaner.Core.Scanning;
using DiskCleaner.Models;

namespace DiskCleaner.UI.ViewModels;

public partial class CleanViewModel : ObservableObject
{
    private readonly IScannerEngine _scannerEngine;
    private readonly ICleanerEngine _cleanerEngine;
    
    [ObservableProperty]
    private bool _isScanning;
    
    [ObservableProperty]
    private bool _isCleaning;
    
    [ObservableProperty]
    private string _scanProgressText = "";
    
    [ObservableProperty]
    private double _scanProgressValue;
    
    [ObservableProperty]
    private string _cleanProgressText = "";
    
    [ObservableProperty]
    private double _cleanProgressValue;
    
    [ObservableProperty]
    private ObservableCollection<FileEntry> _files = new();
    
    [ObservableProperty]
    private FileEntry? _selectedFile;
    
    [ObservableProperty]
    private long _totalSize;
    
    [ObservableProperty]
    private int _totalCount;
    
    [ObservableProperty]
    private long _selectedSize;
    
    [ObservableProperty]
    private int _selectedCount;
    
    public CleanViewModel(IScannerEngine scannerEngine, ICleanerEngine cleanerEngine)
    {
        _scannerEngine = scannerEngine;
        _cleanerEngine = cleanerEngine;
    }
    
    [RelayCommand]
    private async Task StartScanAsync()
    {
        if (IsScanning)
            return;
        
        IsScanning = true;
        Files.Clear();
        ScanProgressText = "正在扫描...";
        ScanProgressValue = 0;
        
        var options = new ScanOptions();
        var progress = new Progress<ScanProgress>(p =>
        {
            ScanProgressText = string.IsNullOrEmpty(p.CurrentDirectory) 
                ? $"已发现 {p.FilesFound} 个文件" 
                : $"扫描中：{p.CurrentDirectory}";
            ScanProgressValue = p.ProgressPercentage;
            TotalSize = p.TotalSizeScanned;
        });
        
        try
        {
            var result = await _scannerEngine.ScanAllDrivesAsync(
                options, 
                progress, 
                CancellationToken.None);
            
            foreach (var file in result.AllFiles)
            {
                Files.Add(file);
            }
            
            TotalCount = result.TotalCount;
            TotalSize = result.TotalSize;
            ScanProgressText = $"扫描完成 - 发现 {TotalCount} 个文件，总计 {FormatSize(TotalSize)}";
        }
        catch (OperationCanceledException)
        {
            ScanProgressText = "扫描已取消";
        }
        catch (Exception ex)
        {
            ScanProgressText = $"扫描失败：{ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }
    
    [RelayCommand]
    private void CancelScan()
    {
        if (IsScanning)
        {
            _scannerEngine.Cancel();
        }
    }
    
    [RelayCommand(CanExecute = nameof(CanClean))]
    private async Task CleanSelectedAsync()
    {
        var filesToClean = Files.Where(f => f == SelectedFile).ToList();
        await CleanFilesAsync(filesToClean);
    }
    
    [RelayCommand(CanExecute = nameof(CanClean))]
    private async Task CleanAllAsync()
    {
        await CleanFilesAsync(Files.ToList());
    }
    
    private bool CanClean() => Files.Count > 0 && !IsScanning && !IsCleaning;
    
    private async Task CleanFilesAsync(List<FileEntry> files)
    {
        IsCleaning = true;
        CleanProgressText = "正在清理...";
        CleanProgressValue = 0;
        
        var progress = new Progress<CleanProgress>(p =>
        {
            CleanProgressText = $"正在删除：{p.CurrentFile} ({p.ProcessedCount}/{p.TotalCount})";
            CleanProgressValue = p.ProgressPercentage;
        });
        
        try
        {
            var result = await _cleanerEngine.CleanAsync(files, true, progress, CancellationToken.None);
            
            foreach (var deletedFile in result.DeletedFiles)
            {
                Files.Remove(deletedFile);
            }
            
            TotalCount = Files.Count;
            TotalSize = Files.Sum(f => f.FileSize);
            
            if (result.HasFailures)
            {
                CleanProgressText = $"清理完成 - {result.DeletedFiles.Count} 成功，{result.FailedCount} 失败";
            }
            else
            {
                CleanProgressText = $"清理完成 - 释放 {FormatSize(result.TotalFreedSpace)}";
            }
        }
        catch (Exception ex)
        {
            CleanProgressText = $"清理失败：{ex.Message}";
        }
        finally
        {
            IsCleaning = false;
        }
    }
    
    private static string FormatSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
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
