namespace DiskCleaner.Models;
public record ScanResult {
    public IReadOnlyList<FileEntry> JunkFiles { get; init; } = new List<FileEntry>();
    public DateTime ScanTime { get; init; } = DateTime.Now;
    public TimeSpan Duration { get; init; }
    public IReadOnlyList<FileEntry> AllFiles => JunkFiles;
    public long TotalSize => JunkFiles.Sum(f => f.FileSize);
    public int TotalCount => JunkFiles.Count;
}
public record ScanProgress { public string CurrentDirectory { get; init; } = ""; public int FilesFound { get; init; } public long TotalSizeScanned { get; init; } public double ProgressPercentage { get; init; } }
public record ScanOptions {
    public bool ScanJunkFiles { get; init; } = true;
    public bool ScanUnusedFiles { get; init; } = true;
    public bool ScanLargeFiles { get; init; } = true;
    public bool ScanUnimportantFiles { get; init; } = true;
    public int UnusedDaysThreshold { get; init; } = 90;
    public long LargeFileSizeThreshold { get; init; } = 500*1024*1024;
}
public record CleanResult {
    public IReadOnlyList<FileEntry> DeletedFiles { get; init; } = new List<FileEntry>();
    public long TotalFreedSpace { get; init; }
}
public record CleanProgress { public int ProcessedCount { get; init; } public int TotalCount { get; init; } public string CurrentFile { get; init; } = ""; public double ProgressPercentage { get; init; } }
