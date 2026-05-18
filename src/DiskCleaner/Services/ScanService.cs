using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiskCleaner.Models;

namespace DiskCleaner.Services
{
    public class ScanService
    {
        public List<FileItem> ScanTempFiles()
        {
            var files = new List<FileItem>();
            try
            {
                var tempPath = Path.GetTempPath();
                if (Directory.Exists(tempPath))
                {
                    files.AddRange(GetFilesFromDirectory(tempPath, "临时文件"));
                }
            }
            catch { }
            return files;
        }

        public List<FileItem> ScanRecycleBin()
        {
            var files = new List<FileItem>();
            try
            {
                var drives = DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => d.Name);
                foreach (var drive in drives)
                {
                    var recyclePath = Path.Combine(drive, "$Recycle.Bin");
                    if (Directory.Exists(recyclePath))
                    {
                        files.AddRange(GetFilesFromDirectory(recyclePath, "回收站"));
                    }
                }
            }
            catch { }
            return files;
        }

        public List<FileItem> ScanBrowserCache()
        {
            var files = new List<FileItem>();
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                
                var chromeCache = Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Cache");
                if (Directory.Exists(chromeCache))
                {
                    files.AddRange(GetFilesFromDirectory(chromeCache, "Chrome缓存"));
                }

                var edgeCache = Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default", "Cache");
                if (Directory.Exists(edgeCache))
                {
                    files.AddRange(GetFilesFromDirectory(edgeCache, "Edge缓存"));
                }

                var firefoxPath = Path.Combine(localAppData, "Mozilla", "Firefox", "Profiles");
                if (Directory.Exists(firefoxPath))
                {
                    foreach (var profile in Directory.GetDirectories(firefoxPath))
                    {
                        var cachePath = Path.Combine(profile, "cache2");
                        if (Directory.Exists(cachePath))
                        {
                            files.AddRange(GetFilesFromDirectory(cachePath, "Firefox缓存"));
                        }
                    }
                }
            }
            catch { }
            return files;
        }

        public List<FileItem> ScanDownloads()
        {
            var files = new List<FileItem>();
            try
            {
                var downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var downloadsFolder = Path.Combine(downloadsPath, "Downloads");
                
                if (Directory.Exists(downloadsFolder))
                {
                    files.AddRange(GetFilesFromDirectory(downloadsFolder, "下载文件夹"));
                }
            }
            catch { }
            return files;
        }

        public List<FileItem> ScanLargeFiles(long sizeThreshold, List<string> drives)
        {
            var files = new List<FileItem>();
            try
            {
                foreach (var drive in drives)
                {
                    if (!DriveExists(drive)) continue;
                    
                    try
                    {
                        ScanLargeFilesInDirectory(drive, sizeThreshold, files, 0, 3);
                    }
                    catch { }
                }
            }
            catch { }
            return files;
        }

        private void ScanLargeFilesInDirectory(string path, long sizeThreshold, List<FileItem> files, int depth, int maxDepth)
        {
            if (depth > maxDepth) return;
            
            try
            {
                var directoryInfo = new DirectoryInfo(path);
                
                foreach (var file in directoryInfo.GetFiles("*", SearchOption.TopDirectoryOnly))
                {
                    if (file.Length >= sizeThreshold)
                    {
                        files.Add(new FileItem
                        {
                            Path = file.FullName,
                            Name = file.Name,
                            Size = file.Length,
                            Type = "大文件",
                            IsSelected = false
                        });
                    }
                }

                foreach (var dir in directoryInfo.GetDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        ScanLargeFilesInDirectory(dir.FullName, sizeThreshold, files, depth + 1, maxDepth);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private List<FileItem> GetFilesFromDirectory(string path, string type)
        {
            var files = new List<FileItem>();
            try
            {
                var directoryInfo = new DirectoryInfo(path);
                foreach (var file in directoryInfo.GetFiles("*", SearchOption.AllDirectories).Take(100))
                {
                    files.Add(new FileItem
                    {
                        Path = file.FullName,
                        Name = file.Name,
                        Size = file.Length,
                        Type = type,
                        IsSelected = true
                    });
                }
            }
            catch { }
            return files;
        }

        private bool DriveExists(string drivePath)
        {
            try
            {
                var driveInfo = new DriveInfo(drivePath);
                return driveInfo.IsReady;
            }
            catch
            {
                return false;
            }
        }
    }
}
