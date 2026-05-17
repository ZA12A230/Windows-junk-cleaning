namespace SystemCleanerPro.Models;

public enum FileCategory
{
    Junk,
    Unused,
    Large,
    Unimportant,
    Duplicate
}

public class FileItem
{
    public string FullPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime LastAccessTime { get; set; }
    public DateTime LastWriteTime { get; set; }
    public FileCategory Category { get; set; }
    public bool IsSelected { get; set; }
    public string IconKey { get; set; } = string.Empty;
    public string DirectoryPath { get; set; } = string.Empty;
    
    public string FormattedSize => FormatSize(Size);
    public string FormattedLastAccess => LastAccessTime.ToString("yyyy-MM-dd HH:mm");
    
    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
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
