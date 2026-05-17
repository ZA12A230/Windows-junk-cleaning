namespace DiskCleaner.Models;

public record ScanResult
{
    public IReadOnlyList<FileEntry> JunkFiles { get; init; } = new List<FileEntry>();
    public IReadOnlyList<FileEntry> UnusedFiles { get; init; } = new List<FileEntry>();
    public IReadOnlyList<FileEntry> LargeFiles { get; init; } = new List<FileEntry>();
    public IReadOnlyList<FileEntry> UnimportantFiles { get; init; } = new List<FileEntry>();
    public DateTime ScanTime { get; init; }
    public TimeSpan Duration { get; init; }
    
    public IReadOnlyList<FileEntry> AllFiles => 
        JunkFiles.Concat(UnusedFiles).Concat(LargeFiles).Concat(UnimportantFiles).ToList();
    
    public long TotalSize => AllFiles.Sum(f => f.FileSize);
    public int TotalCount => AllFiles.Count;
}

public record ScanProgress
{
    public string CurrentDirectory { get; init; } = "";
    public int FilesFound { get; init; }
    public long TotalSizeScanned { get; init; }
    public double ProgressPercentage { get; init; }
}
