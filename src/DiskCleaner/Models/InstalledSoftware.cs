namespace DiskCleaner.Models;
public record InstalledSoftware {
    public string DisplayName { get; init; } = "";
    public string? Publisher { get; init; }
    public string? DisplayVersion { get; init; }
    public string? InstallDate { get; init; }
    public long? EstimatedSize { get; init; }
    public string? UninstallString { get; init; }
    public string? InstallLocation { get; init; }
    public string FormattedSize => EstimatedSize.HasValue ? $"{EstimatedSize/1024.0:F1} MB" : "Unknown";
}
