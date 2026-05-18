using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
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
        private bool _scanTempFiles = true;

        [ObservableProperty]
        private bool _scanRecycleBin = true;

        [ObservableProperty]
        private bool _scanBrowserCache = true;

        public MainViewModel()
        {
            _scanService = new ScanService();
            _cleanService = new CleanService();
            _uninstallService = new UninstallService();
            LoadSoftware();
        }

        private void LoadSoftware()
        {
            var softwareList = _uninstallService.GetInstalledSoftware();
            foreach (var item in softwareList)
            {
                Software.Add(item);
            }
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
                    var tempFiles = _scanService.ScanTempFiles();
                    foreach (var file in tempFiles)
                    {
                        Files.Add(file);
                    }
                }

                if (ScanRecycleBin)
                {
                    var recycleFiles = _scanService.ScanRecycleBin();
                    foreach (var file in recycleFiles)
                    {
                        Files.Add(file);
                    }
                }

                if (ScanBrowserCache)
                {
                    var cacheFiles = _scanService.ScanBrowserCache();
                    foreach (var file in cacheFiles)
                    {
                        Files.Add(file);
                    }
                }

                var totalSize = Files.Sum(f => f.Size);
                StatusMessage = $"扫描完成！找到 {Files.Count} 个文件，共 {FormatSize(totalSize)}";
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

            try
            {
                StatusMessage = "正在清理...";
                var deletedCount = _cleanService.DeleteFiles(selectedFiles);
                foreach (var file in selectedFiles.ToList())
                {
                    Files.Remove(file);
                }
                StatusMessage = $"清理完成！删除了 {deletedCount} 个文件";
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
            StatusMessage = "已全选";
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
