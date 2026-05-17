namespace DiskCleaner.Models;
public enum FileType { Unknown, JunkFile, UnusedFile, LargeFile, UnimportantFile }
public record FileEntry {
    public string FullPath { get; init; } = "";
    public string FileName { get; init; } = "";
    public long FileSize { get; init; }
    public FileType FileType { get; init; }
    public DateTime LastAccessTime { get; init; }
    public DateTime LastWriteTime { get; init; }
    public string? Extension { get; init; }
    public string? Directory { get; init; }
    public string FormattedSize => FileSize > 1024*1024 ? $"{FileSize/1024/1024.0:F1} MB" :
        FileSize > 1024 ? $"{FileSize/1024.0:F1} KB" : $"{FileSize} B";
}
