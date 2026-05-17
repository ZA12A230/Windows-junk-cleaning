namespace DiskCleaner.Models;

public record ScanOptions
{
    public bool ScanJunkFiles { get; init; } = true;
    public bool ScanUnusedFiles { get; init; } = true;
    public bool ScanLargeFiles { get; init; } = true;
    public bool ScanUnimportantFiles { get; init; } = true;
    public int UnusedDaysThreshold { get; init; } = 90;
    public long LargeFileSizeThreshold { get; init; } = 500 * 1024 * 1024; // 500MB
    public string[]? CustomPaths { get; init; }
    
    public JunkFileRules JunkFileRules { get; init; } = new();
    public SecurityOptions Security { get; init; } = new();
}

public record JunkFileRules
{
    public bool ScanTempFolders { get; init; } = true;
    public bool ScanBrowserCache { get; init; } = true;
    public bool ScanRecycleBin { get; init; } = true;
    public bool ScanLogFiles { get; init; } = true;
    public bool ScanUpdateCache { get; init; } = true;
}

public record SecurityOptions
{
    public string[] WhitelistPaths { get; init; } = Array.Empty<string>();
    public bool RequireConfirmForProtectedFiles { get; init; } = true;
}
