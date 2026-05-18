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
                    files.AddRange(GetFilesFromDirectory(chromeCache, "浏览器缓存"));
                }
            }
            catch { }
            return files;
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
    }
}
