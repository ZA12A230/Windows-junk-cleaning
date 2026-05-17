using System.IO;
using DiskCleaner.Models;

namespace DiskCleaner.Core.Scanning;

public class FileClassifier
{
    private readonly ILogger<FileClassifier> _logger;
    
    private static readonly string[] JunkExtensions =
    [
        ".log", ".etl", ".dmp", ".tmp", ".temp", ".old", ".bak",
        ".thumbs", ".db~", "thumbs.db"
    ];
    
    private static readonly string[] JunkPaths =
    [
        "temp", "tmp", "cache", "prefetch", "softwaredistribution"
    ];
    
    private static readonly string[] ExecutableExtensions =
    [
        ".exe", ".dll", ".sys", ".msi", ".msp", ".com", ".scr"
    ];
    
    private static readonly string[] UnimportantExtensions =
    [
        ".ds_store", "desktop.ini", "thumbs.db"
    ];

    public FileClassifier(ILogger<FileClassifier> logger)
    {
        _logger = logger;
    }
    
    public FileType Classify(FileInfo fileInfo, ScanOptions options)
    {
        try
        {
            if (options.ScanJunkFiles && IsJunkFile(fileInfo, options))
                return FileType.JunkFile;
            
            if (options.ScanUnusedFiles && IsUnusedFile(fileInfo, options))
                return FileType.UnusedFile;
            
            if (options.ScanLargeFiles && IsLargeFile(fileInfo, options))
                return FileType.LargeFile;
            
            if (options.ScanUnimportantFiles && IsUnimportantFile(fileInfo))
                return FileType.UnimportantFile;
            
            return FileType.Unknown;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error classifying file {FilePath}", fileInfo.FullName);
            return FileType.Unknown;
        }
    }
    
    public bool IsJunkFile(FileInfo fileInfo, ScanOptions options)
    {
        if (IsInJunkDirectory(fileInfo))
            return true;
        
        if (HasJunkExtension(fileInfo))
            return true;
        
        if (IsBrowserCache(fileInfo, options))
            return true;
        
        if (IsWindowsUpdateCache(fileInfo))
            return true;
        
        if (IsTempFile(fileInfo))
            return true;
        
        return false;
    }
    
    public bool IsUnusedFile(FileInfo fileInfo, ScanOptions options)
    {
        if (IsExecutableFile(fileInfo))
            return false;
        
        var daysSinceAccess = (DateTime.Now - fileInfo.LastAccessTime).TotalDays;
        return daysSinceAccess >= options.UnusedDaysThreshold;
    }
    
    public bool IsLargeFile(FileInfo fileInfo, ScanOptions options)
    {
        return fileInfo.Length >= options.LargeFileSizeThreshold;
    }
    
    public bool IsUnimportantFile(FileInfo fileInfo)
    {
        var fileName = fileInfo.Name.ToLowerInvariant();
        
        if (UnimportantExtensions.Any(ext => fileName.EndsWith(ext)))
            return true;
        
        if (fileName.StartsWith("~$"))
            return true;
        
        if (fileInfo.Length == 0 && fileInfo.Extension != "")
            return true;
        
        return false;
    }
    
    private static bool IsInJunkDirectory(FileInfo fileInfo)
    {
        var fullPath = fileInfo.FullName.ToLowerInvariant();
        return JunkPaths.Any(p => fullPath.Contains($@"{p}\"));
    }
    
    private static bool HasJunkExtension(FileInfo fileInfo)
    {
        var ext = fileInfo.Extension.ToLowerInvariant();
        return JunkExtensions.Any(e => ext.EndsWith(e));
    }
    
    private static bool IsBrowserCache(FileInfo fileInfo, ScanOptions options)
    {
        if (!options.JunkFileRules.ScanBrowserCache)
            return false;
        
        var path = fileInfo.FullName.ToLowerInvariant();
        return path.Contains(@"google\chrome") && path.Contains(@"cache") ||
               path.Contains(@"microsoft\edge") && path.Contains(@"cache") ||
               path.Contains(@"mozilla\firefox") && path.Contains(@"cache");
    }
    
    private static bool IsWindowsUpdateCache(FileInfo fileInfo)
    {
        var path = fileInfo.FullName.ToLowerInvariant();
        return path.Contains(@"softwaredistribution\download");
    }
    
    private static bool IsTempFile(FileInfo fileInfo)
    {
        var tempPath = Path.GetTempPath().ToLowerInvariant();
        return fileInfo.FullName.ToLowerInvariant().StartsWith(tempPath);
    }
    
    private static bool IsExecutableFile(FileInfo fileInfo)
    {
        var ext = fileInfo.Extension.ToLowerInvariant();
        return ExecutableExtensions.Contains(ext);
    }
}
