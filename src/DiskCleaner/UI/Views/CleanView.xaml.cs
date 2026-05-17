using System.Windows;
using System.Windows.Controls;
using DiskCleaner.Core.Scanning;
using DiskCleaner.Core.Cleaning;
using DiskCleaner.Models;

namespace DiskCleaner.UI.Views;

public partial class CleanView : Page
{
    private readonly IScannerEngine _scannerEngine;
    private readonly ICleanerEngine _cleanerEngine;
    private bool _isScanning;
    
    public CleanView()
    {
        InitializeComponent();
        _scannerEngine = App.GetService<IScannerEngine>();
        _cleanerEngine = App.GetService<ICleanerEngine>();
        
        ScanBtn.Click += async (s, e) => await StartScanAsync();
        CancelBtn.Click += (s, e) => CancelScan();
        CleanBtn.Click += async (s, e) => await CleanSelectedAsync();
        CleanAllBtn.Click += async (s, e) => await CleanAllAsync();
    }
    
    private async Task StartScanAsync()
    {
        if (_isScanning) return;
        _isScanning = true;
        CancelBtn.IsEnabled = true;
        ScanBtn.IsEnabled = false;
        FilesGrid.ItemsSource = null;
        StatusText.Text = "正在扫描...";
        
        var options = new ScanOptions();
        var progress = new Progress<ScanProgress>(p =>
        {
            StatusText.Text = $"已发现 {p.FilesFound} 个文件";
        });
        
        try
        {
            var result = await _scannerEngine.ScanAllDrivesAsync(options, progress, CancellationToken.None);
            FilesGrid.ItemsSource = result.AllFiles;
            StatusText.Text = $"扫描完成 - 发现 {result.TotalCount} 个文件，总计 {result.TotalSize} bytes";
        }
        catch (OperationCanceledException) { StatusText.Text = "扫描已取消"; }
        catch (Exception ex) { StatusText.Text = $"扫描失败：{ex.Message}"; }
        finally
        {
            _isScanning = false;
            CancelBtn.IsEnabled = false;
            ScanBtn.IsEnabled = true;
        }
    }
    
    private void CancelScan() => _scannerEngine.Cancel();
    
    private async Task CleanSelectedAsync()
    {
        var file = FilesGrid.SelectedItem as FileEntry;
        if (file == null) return;
        await CleanFilesAsync(new[] { file }.ToList());
    }
    
    private async Task CleanAllAsync()
    {
        var files = FilesGrid.ItemsSource?.Cast<FileEntry>().ToList() ?? new List<FileEntry>();
        if (files.Count == 0) return;
        await CleanFilesAsync(files);
    }
    
    private async Task CleanFilesAsync(List<FileEntry> files)
    {
        StatusText.Text = "正在清理...";
        try
        {
            var result = await _cleanerEngine.CleanAsync(files, true, null, CancellationToken.None);
            var current = (FilesGrid.ItemsSource as List<FileEntry>) ?? new List<FileEntry>();
            foreach (var f in result.DeletedFiles) current.Remove(f);
            FilesGrid.ItemsSource = null;
            FilesGrid.ItemsSource = current;
            StatusText.Text = $"清理完成 - 释放 {result.TotalFreedSpace} bytes";
        }
        catch (Exception ex) { StatusText.Text = $"清理失败：{ex.Message}"; }
    }
}
