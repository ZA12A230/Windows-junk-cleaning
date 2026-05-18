using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SystemCleanerPro.Models;

namespace SystemCleanerPro.Services;

public class ScanService
{
    private readonly ScanConfiguration _config;
    
    public event Action<string>? OnStatusUpdate;
    public event Action<int>? OnProgressChanged;
    public event Action<FileItem>? OnFileFound;
    
    public ScanService(ScanConfiguration config)
    {
        _config = config;
    }
    
    public async Task<List<FileItem>> ScanAllDrivesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<FileItem>();
        
        var drives = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
            .ToList();
        
        int totalDrives = drives.Count;
        int currentDrive = 0;
        
        foreach (var drive in drives)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            currentDrive++;
            OnStatusUpdate?.Invoke($"正在扫描 {drive.Name}...");
            
            if (_config.ScanJunkFiles)
            {
                await ScanJunkFilesAsync(drive.Name, results, cancellationToken);
            }
            
            if (_config.ScanLargeFiles)
            {
                await ScanLargeFilesAsync(drive.Name, results, cancellationToken);
            }
            
            if (_config.ScanUnusedFiles)
            {
                await ScanUnusedFilesAsync(drive.Name, results, cancellationToken);
            }
            
            if (_config.ScanUnimportantFiles)
            {
                await ScanUnimportantFilesAsync(drive.Name, results, cancellationToken);
            }
            
            OnProgressChanged?.Invoke((currentDrive * 100) / totalDrives);
        }
        
        OnStatusUpdate?.Invoke("扫描完成");
        return results;
    }
    
    private async Task ScanJunkFilesAsync(string rootPath, List<FileItem> results, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            var junkPatterns = _config.GetJunkPatterns();
            
            foreach (var (path, pattern) in junkPatterns)
            {
                if (cancellationToken.IsCancellationRequested) break;
                
                try
                {
                    if (!Directory.Exists(path)) continue;
                    
                    var files = Directory.EnumerateFiles(path, pattern, SearchOption.AllDirectories);
                    
                    foreach (var file in files)
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        
                        try
                        {
                            var info = new FileInfo(file);
                            if (info.Exists && info.Length > 0)
                            {
                                var item = CreateFileItem(file, FileCategory.Junk);
                                lock (results) { results.Add(item); }
                                OnFileFound?.Invoke(item);
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }, cancellationToken);
    }
    
    private async Task ScanLargeFilesAsync(string rootPath, List<FileItem> results, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            try
            {
                var files = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories);
                long threshold = _config.LargeFileThresholdBytes;
                
                foreach (var file in files)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    
                    try
                    {
                        var info = new FileInfo(file);
                        if (info.Exists && info.Length >= threshold)
                        {
                            var item = CreateFileItem(file, FileCategory.Large);
                            lock (results) { results.Add(item); }
                            OnFileFound?.Invoke(item);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }, cancellationToken);
    }
    
    private async Task ScanUnusedFilesAsync(string rootPath, List<FileItem> results, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            var systemExtensions = _config.GetSystemFileExtensions();
            var daysThreshold = _config.UnusedFileDaysThreshold;
            var thresholdDate = DateTime.Now.AddDays(-daysThreshold);
            
            try
            {
                var files = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories);
                
                foreach (var file in files)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    
                    try
                    {
                        var info = new FileInfo(file);
                        if (!info.Exists) continue;
                        
                        var ext = info.Extension.ToLowerInvariant();
                        if (systemExtensions.Contains(ext)) continue;
                        if (info.IsReadOnly) continue;
                        
                        if (info.LastAccessTime < thresholdDate && info.Length > 0)
                        {
                            var item = CreateFileItem(file, FileCategory.Unused);
                            lock (results) { results.Add(item); }
                            OnFileFound?.Invoke(item);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }, cancellationToken);
    }
    
    private async Task ScanUnimportantFilesAsync(string rootPath, List<FileItem> results, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            var unimportantPatterns = _config.GetUnimportantPatterns();
            
            foreach (var pattern in unimportantPatterns)
            {
                if (cancellationToken.IsCancellationRequested) break;
                
                try
                {
                    var files = Directory.EnumerateFiles(rootPath, pattern, SearchOption.AllDirectories);
                    
                    foreach (var file in files)
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        
                        try
                        {
                            var item = CreateFileItem(file, FileCategory.Unimportant);
                            lock (results) { results.Add(item); }
                            OnFileFound?.Invoke(item);
                        }
                        catch { }
                    }
                }
                catch { }
            }
            
            if (_config.ScanEmptyFolders)
            {
                ScanEmptyFoldersAsync(rootPath, results, cancellationToken);
            }
            
            if (_config.ScanDuplicateFiles)
            {
                ScanDuplicateFilesAsync(rootPath, results, cancellationToken);
            }
        }, cancellationToken);
    }
    
    private void ScanEmptyFoldersAsync(string rootPath, List<FileItem> results, CancellationToken cancellationToken)
    {
        try
        {
            var dirs = Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories);
            
            foreach (var dir in dirs)
            {
                if (cancellationToken.IsCancellationRequested) break;
                
                try
                {
                    var subDirs = Directory.GetDirectories(dir);
                    var files = Directory.GetFiles(dir);
                    
                    if (subDirs.Length == 0 && files.Length == 0)
                    {
                        var item = new FileItem
                        {
                            FullPath = dir,
                            FileName = Path.GetFileName(dir),
                            DirectoryPath = Path.GetDirectoryName(dir) ?? string.Empty,
                            Size = 0,
                            LastAccessTime = Directory.GetLastAccessTime(dir),
                            LastWriteTime = Directory.GetLastWriteTime(dir),
                            Category = FileCategory.Unimportant,
                            IsSelected = false,
                            IconKey = "folder_empty"
                        };
                        lock (results) { results.Add(item); }
                        OnFileFound?.Invoke(item);
                    }
                }
                catch { }
            }
        }
        catch { }
    }
    
    private void ScanDuplicateFilesAsync(string rootPath, List<FileItem> results, CancellationToken cancellationToken)
    {
        var sizeGroups = new Dictionary<long, List<string>>();
        
        try
        {
            var files = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => !f.Contains("$Recycle.Bin"))
                .ToList();
            
            foreach (var file in files)
            {
                try
                {
                    var info = new FileInfo(file);
                    if (!info.Exists || info.Length == 0) continue;
                    
                    if (!sizeGroups.ContainsKey(info.Length))
                        sizeGroups[info.Length] = new List<string>();
                    sizeGroups[info.Length].Add(file);
                }
                catch { }
            }
            
            foreach (var group in sizeGroups.Where(g => g.Value.Count > 1))
            {
                var hashGroups = new Dictionary<string, List<string>>();
                
                foreach (var file in group.Value)
                {
                    try
                    {
                        var hash = ComputeQuickHash(file);
                        if (!hashGroups.ContainsKey(hash))
                            hashGroups[hash] = new List<string>();
                        hashGroups[hash].Add(file);
                    }
                    catch { }
                }
                
                foreach (var dupGroup in hashGroups.Where(g => g.Value.Count > 1))
                {
                    foreach (var dup in dupGroup.Value.Skip(1))
                    {
                        var item = CreateFileItem(dup, FileCategory.Duplicate);
                        lock (results) { results.Add(item); }
                        OnFileFound?.Invoke(item);
                    }
                }
            }
        }
        catch { }
    }
    
    private string ComputeQuickHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var buffer = new byte[4096];
        stream.Read(buffer, 0, 4096);
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(buffer);
        return Convert.ToBase64String(hash);
    }
    
    private FileItem CreateFileItem(string path, FileCategory category)
    {
        var info = new FileInfo(path);
        return new FileItem
        {
            FullPath = path,
            FileName = info.Name,
            Extension = info.Extension,
            Size = info.Exists ? info.Length : 0,
            LastAccessTime = info.Exists ? info.LastAccessTime : DateTime.MinValue,
            LastWriteTime = info.Exists ? info.LastWriteTime : DateTime.MinValue,
            Category = category,
            IsSelected = false,
            IconKey = GetIconKey(info.Extension),
            DirectoryPath = info.DirectoryName ?? string.Empty
        };
    }
    
    private string GetIconKey(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".exe" => "exe",
            ".dll" => "dll",
            ".zip" or ".rar" or ".7z" => "archive",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "image",
            ".mp4" or ".avi" or ".mkv" or ".mov" => "video",
            ".mp3" or ".wav" or ".flac" => "audio",
            ".doc" or ".docx" or ".pdf" or ".txt" => "document",
            _ => "file"
        };
    }
}
