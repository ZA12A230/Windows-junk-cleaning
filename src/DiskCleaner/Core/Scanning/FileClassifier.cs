using System.IO;
using DiskCleaner.Models;

namespace DiskCleaner.Core.Scanning;

public class FileClassifier
{
    private static readonly string[] JunkExtensions = [".log", ".etl", ".dmp", ".tmp", ".temp", ".old", ".bak"];
    private static readonly string[] ExecutableExtensions = [".exe", ".dll", ".sys", ".msi"];
    
    public FileType Classify(FileInfo fileInfo, ScanOptions options)
    {
        if (options.ScanJunkFiles && IsJunkFile(fileInfo)) return FileType.JunkFile;
        if (options.ScanUnusedFiles && IsUnusedFile(fileInfo, options)) return FileType.UnusedFile;
        if (options.ScanLargeFiles && IsLargeFile(fileInfo, options)) return FileType.LargeFile;
        if (options.ScanUnimportantFiles && IsUnimportantFile(fileInfo)) return FileType.UnimportantFile;
        return FileType.Unknown;
    }
    
    private bool IsJunkFile(FileInfo fileInfo)
    {
        var ext = fileInfo.Extension.ToLowerInvariant();
        if (JunkExtensions.Any(e => ext.EndsWith(e))) return true;
        
        var path = fileInfo.FullName.ToLowerInvariant();
        if (path.Contains(@"temp\") || path.Contains(@"tmp\") || path.Contains(@"cache\")) return true;
        if (path.Contains(@"softwaredistribution\download")) return true;
        
        return false;
    }
    
    private bool IsUnusedFile(FileInfo fileInfo, ScanOptions options)
    {
        if (IsExecutableFile(fileInfo)) return false;
        var daysSinceAccess = (DateTime.Now - fileInfo.LastAccessTime).TotalDays;
        return daysSinceAccess >= options.UnusedDaysThreshold;
    }
    
    private bool IsLargeFile(FileInfo fileInfo, ScanOptions options)
    {
        return fileInfo.Length >= options.LargeFileSizeThreshold;
    }
    
    private bool IsUnimportantFile(FileInfo fileInfo)
    {
        var fileName = fileInfo.Name.ToLowerInvariant();
        if (fileName.EndsWith(".ds_store") || fileName.EndsWith("desktop.ini") || fileName.EndsWith("thumbs.db")) return true;
        if (fileName.StartsWith("~$")) return true;
        if (fileInfo.Length == 0) return true;
        return false;
    }
    
    private static bool IsExecutableFile(FileInfo fileInfo)
    {
        var ext = fileInfo.Extension.ToLowerInvariant();
        return ExecutableExtensions.Contains(ext);
    }
}
