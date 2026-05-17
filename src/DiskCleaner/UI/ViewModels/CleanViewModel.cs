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
    private bool _isCleaning;
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
    public bool IsCleaning { get => _isCleaning; set { _isCleaning = value; OnPropertyChanged(); } }
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
            var result = await _scannerEngine.ScanAllDrivesAsync(options, progress, CancellationToken.None);
            
            foreach (var file in result.AllFiles)
                Files.Add(file);
            
            TotalCount = result.TotalCount;
            TotalSize = result.TotalSize;
            ScanProgressText = $"扫描完成 - 发现 {TotalCount} 个文件";
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
    
    private void CancelScan()
    {
        if (IsScanning)
            _scannerEngine.Cancel();
    }
    
    private bool CanClean() => Files.Count > 0 && !IsScanning && !IsCleaning;
    
    private async Task CleanSelectedAsync()
    {
        if (SelectedFile == null) return;
        await CleanFilesAsync(new[] { SelectedFile }.ToList());
    }
    
    private async Task CleanAllAsync()
    {
        await CleanFilesAsync(Files.ToList());
    }
    
    private async Task CleanFilesAsync(List<FileEntry> files)
    {
        IsCleaning = true;
        
        var progress = new Progress<CleanProgress>(p =>
        {
            ScanProgressText = $"正在删除：{p.CurrentFile} ({p.ProcessedCount}/{p.TotalCount})";
        });
        
        try
        {
            var result = await _cleanerEngine.CleanAsync(files, true, progress, CancellationToken.None);
            
            foreach (var deletedFile in result.DeletedFiles)
                Files.Remove(deletedFile);
            
            TotalCount = Files.Count;
            TotalSize = Files.Sum(f => f.FileSize);
            ScanProgressText = $"清理完成 - 释放 {result.TotalFreedSpace} bytes";
        }
        catch (Exception ex)
        {
            ScanProgressText = $"清理失败：{ex.Message}";
        }
        finally
        {
            IsCleaning = false;
        }
    }
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
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
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
    
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    
    public async void Execute(object? parameter)
    {
        if (_executeAsync != null)
            await _executeAsync(parameter);
    }
}
