using Microsoft.Win32;

namespace DiskCleaner.Models;

public record InstalledSoftware
{
    public string DisplayName { get; init; } = "";
    public string? Publisher { get; init; }
    public string? DisplayVersion { get; init; }
    public string? InstallDate { get; init; }
    public long? EstimatedSize { get; init; }
    public string? UninstallString { get; init; }
    public string? InstallLocation { get; init; }
    public string? DisplayIcon { get; init; }
    public string RegistryKeyPath { get; init; } = "";
    public RegistryHive RegistryHive { get; init; }
    public bool Is64Bit { get; init; }
    
    public string FormattedSize => EstimatedSize.HasValue 
        ? FormatSize(EstimatedSize.Value) 
        : "Unknown";
    
    private static string FormatSize(long sizeKB)
    {
        if (sizeKB < 1024)
            return $"{sizeKB} KB";
        if (sizeKB < 1024 * 1024)
            return $"{sizeKB / 1024.0:F1} MB";
        return $"{sizeKB / (1024.0 * 1024):F2} GB";
    }
}
