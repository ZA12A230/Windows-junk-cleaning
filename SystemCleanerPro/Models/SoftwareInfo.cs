namespace SystemCleanerPro.Models;

public class SoftwareInfo
{
    public string DisplayName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string InstallLocation { get; set; } = string.Empty;
    public string UninstallString { get; set; } = string.Empty;
    public DateTime? InstallDate { get; set; }
    public long EstimatedSize { get; set; }
    public string RegistryKey { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    
    public string FormattedSize => EstimatedSize > 0 
        ? $"{EstimatedSize / 1024.0 / 1024.0:0.##} MB" 
        : "未知";
}
