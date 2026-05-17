namespace DiskCleaner.Models;

public enum FileType
{
    Unknown = 0,
    JunkFile = 1,
    UnusedFile = 2,
    LargeFile = 3,
    UnimportantFile = 4
}

public record FileEntry
{
    public string FullPath { get; init; } = "";
    public string FileName { get; init; } = "";
    public long FileSize { get; init; }
    public FileType FileType { get; init; }
    public DateTime LastAccessTime { get; init; }
    public DateTime LastWriteTime { get; init; }
    public bool IsInUse { get; init; }
    public string? Extension { get; init; }
    public string? Directory { get; init; }
    
    public string FormattedSize => FormatFileSize(FileSize);
    
    private static string FormatFileSize(long bytes)
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
