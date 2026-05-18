using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskCleaner.Models;
using DiskCleaner.Services;

namespace DiskCleaner.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ScanService _scanService;
        private readonly CleanService _cleanService;
        private readonly UninstallService _uninstallService;

        [ObservableProperty]
        private ObservableCollection<FileItem> _files = new ObservableCollection<FileItem>();

        [ObservableProperty]
        private ObservableCollection<SoftwareInfo> _software = new ObservableCollection<SoftwareInfo>();

        [ObservableProperty]
        private string _statusMessage = "准备就绪";

        [ObservableProperty]
        private bool _isScanning = false;

        [ObservableProperty]
        private bool _isUninstalling = false;

        [ObservableProperty]
        private bool _scanTempFiles = true;

        [ObservableProperty]
        private bool _scanRecycleBin = true;

        [ObservableProperty]
        private bool _scanBrowserCache = true;

        [ObservableProperty]
        private bool _scanDownloads = false;

        [ObservableProperty]
        private bool _scanLargeFiles = false;

        [ObservableProperty]
        private long _largeFileSizeThreshold = 100 * 1024 * 1024;

        [ObservableProperty]
        private ObservableCollection<string> _availableDrives = new ObservableCollection<string>();

        [ObservableProperty]
        private ObservableCollection<string> _selectedDrives = new ObservableCollection<string>();

        public MainViewModel()
        {
            _scanService = new ScanService();
            _cleanService = new CleanService();
            _uninstallService = new UninstallService();
            LoadAvailableDrives();
        }

        private void LoadAvailableDrives()
        {
            try
            {
                var drives = System.IO.DriveInfo.GetDrives()
                    .Where(d => d.IsReady)
                    .Select(d => d.Name)
                    .ToList();

                foreach (var drive in drives)
                {
                    AvailableDrives.Add(drive);
                    SelectedDrives.Add(drive);
                }
            }
            catch { }
        }

        [RelayCommand]
        private void StartScan()
        {
            IsScanning = true;
            StatusMessage = "正在扫描...";
            Files.Clear();

            try
            {
                if (ScanTempFiles)
                {
                    StatusMessage = "正在扫描临时文件...";
                    var tempFiles = _scanService.ScanTempFiles();
                    foreach (var file in tempFiles)
                    {
                        Files.Add(file);
                    }
                }

                if (ScanRecycleBin)
                {
                    StatusMessage = "正在扫描回收站...";
                    var recycleFiles = _scanService.ScanRecycleBin();
                    foreach (var file in recycleFiles)
                    {
                        Files.Add(file);
                    }
                }

                if (ScanBrowserCache)
                {
                    StatusMessage = "正在扫描浏览器缓存...";
                    var cacheFiles = _scanService.ScanBrowserCache();
                    foreach (var file in cacheFiles)
                    {
                        Files.Add(file);
                    }
                }

                if (ScanDownloads)
                {
                    StatusMessage = "正在扫描下载文件夹...";
                    var downloadFiles = _scanService.ScanDownloads();
                    foreach (var file in downloadFiles)
                    {
                        Files.Add(file);
                    }
                }

                if (ScanLargeFiles)
                {
                    StatusMessage = "正在扫描大文件（这可能需要一些时间）...";
                    var drives = SelectedDrives.ToList();
                    var largeFiles = _scanService.ScanLargeFiles(LargeFileSizeThreshold, drives);
                    foreach (var file in largeFiles)
                    {
                        Files.Add(file);
                    }
                }

                var totalSize = Files.Sum(f => f.Size);
                var selectedSize = Files.Where(f => f.IsSelected).Sum(f => f.Size);
                StatusMessage = $"扫描完成！找到 {Files.Count} 个文件，共 {FormatSize(totalSize)}，已选中 {FormatSize(selectedSize)}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"扫描出错: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
            }
        }

        [RelayCommand]
        private void CleanFiles()
        {
            var selectedFiles = Files.Where(f => f.IsSelected).ToList();
            if (!selectedFiles.Any())
            {
                StatusMessage = "请先选择要清理的文件";
                return;
            }

            var result = MessageBox.Show(
                $"确定要删除选中的 {selectedFiles.Count} 个文件吗？\n这将释放约 {FormatSize(_cleanService.GetTotalSize(selectedFiles))} 磁盘空间。",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                StatusMessage = "正在清理...";
                var deletedCount = _cleanService.DeleteFiles(selectedFiles);
                
                foreach (var file in selectedFiles.ToList())
                {
                    Files.Remove(file);
                }
                
                StatusMessage = $"清理完成！成功删除了 {deletedCount} 个文件";
            }
            catch (Exception ex)
            {
                StatusMessage = $"清理出错: {ex.Message}";
            }
        }

        [RelayCommand]
        private void SelectAll()
        {
            foreach (var file in Files)
            {
                file.IsSelected = true;
            }
            var totalSize = Files.Where(f => f.IsSelected).Sum(f => f.Size);
            StatusMessage = $"已全选，共 {FormatSize(totalSize)}";
        }

        [RelayCommand]
        private void DeselectAll()
        {
            foreach (var file in Files)
            {
                file.IsSelected = false;
            }
            StatusMessage = "已取消全选";
        }

        [RelayCommand]
        private void LoadSoftware()
        {
            try
            {
                StatusMessage = "正在加载已安装软件列表...";
                Software.Clear();
                
                var softwareList = _uninstallService.GetInstalledSoftware();
                foreach (var item in softwareList)
                {
                    Software.Add(item);
                }
                
                StatusMessage = $"已加载 {Software.Count} 个已安装软件";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载软件列表出错: {ex.Message}";
            }
        }

        [RelayCommand]
        private void UninstallSelectedSoftware()
        {
            var selectedSoftware = Software.Where(s => s.IsSelected).ToList();
            if (!selectedSoftware.Any())
            {
                StatusMessage = "请先选择要卸载的软件";
                return;
            }

            foreach (var software in selectedSoftware)
            {
                var result = MessageBox.Show(
                    $"确定要卸载 {software.Name} 吗？",
                    "确认卸载",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    continue;

                try
                {
                    StatusMessage = $"正在卸载 {software.Name}...";
                    if (_uninstallService.UninstallSoftware(software))
                    {
                        StatusMessage = $"已启动 {software.Name} 的卸载程序";
                    }
                    else
                    {
                        StatusMessage = $"无法卸载 {software.Name}，请手动卸载";
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"卸载出错: {ex.Message}";
                }
            }
        }

        private string FormatSize(long bytes)
        {
            if (bytes >= 1073741824)
                return $"{bytes / 1073741824.0:F2} GB";
            if (bytes >= 1048576)
                return $"{bytes / 1048576.0:F2} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F2} KB";
            return $"{bytes} B";
        }
    }
}
