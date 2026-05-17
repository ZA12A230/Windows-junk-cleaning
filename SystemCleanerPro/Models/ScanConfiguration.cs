namespace SystemCleanerPro.Models;

public class ScanConfiguration
{
    public bool ScanSystemTemp { get; set; } = true;
    public bool ScanBrowserCache { get; set; } = true;
    public bool ScanRecycleBin { get; set; } = true;
    public bool ScanLogFiles { get; set; } = true;
    public bool ScanWindowsUpdateCache { get; set; } = true;
    public bool ScanThumbnailCache { get; set; } = true;
    public bool ScanUserTemp { get; set; } = true;
    
    public bool ScanUnusedFiles { get; set; } = true;
    public int UnusedFileDaysThreshold { get; set; } = 90;
    
    public bool ScanLargeFiles { get; set; } = true;
    public long LargeFileThresholdBytes { get; set; } = 500 * 1024 * 1024;
    
    public bool ScanJunkFiles { get; set; } = true;
    public bool ScanDuplicateFiles { get; set; } = true;
    public bool ScanEmptyFolders { get; set; } = true;
    public bool ScanUnimportantFiles { get; set; } = true;
    
    public List<string> GetTempPaths()
    {
        var paths = new List<string>();
        var temp = Environment.GetEnvironmentVariable("TEMP");
        var windir = Environment.GetEnvironmentVariable("WINDIR");
        var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
        
        if (!string.IsNullOrEmpty(temp)) paths.Add(temp);
        if (!string.IsNullOrEmpty(windir)) paths.Add(Path.Combine(windir, "Temp"));
        if (!string.IsNullOrEmpty(userProfile)) paths.Add(Path.Combine(userProfile, "AppData", "Local", "Temp"));
        
        return paths;
    }
    
    public List<(string Path, string Pattern)> GetJunkPatterns()
    {
        var patterns = new List<(string, string)>();
        var windir = Environment.GetEnvironmentVariable("WINDIR") ?? "C:\\Windows";
        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        var appData = Environment.GetEnvironmentVariable("APPDATA");
        var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
        
        if (ScanSystemTemp)
        {
            patterns.Add((Path.Combine(windir, "Prefetch"), "*.*"));
            patterns.Add((Path.Combine(windir, "SoftwareDistribution", "Download"), "*.*"));
        }
        
        if (ScanBrowserCache && !string.IsNullOrEmpty(localAppData))
        {
            patterns.Add((Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Cache"), "*.*"));
            patterns.Add((Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Code Cache"), "*.*"));
            patterns.Add((Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default", "Cache"), "*.*"));
            patterns.Add((Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default", "Code Cache"), "*.*"));
            patterns.Add((Path.Combine(appData ?? "", "Mozilla", "Firefox", "Profiles"), "cache2"));
        }
        
        if (ScanLogFiles)
        {
            patterns.Add((windir, "*.log"));
            patterns.Add((windir, "*.etl"));
            patterns.Add((windir, "*.dmp"));
        }
        
        if (ScanThumbnailCache && !string.IsNullOrEmpty(localAppData))
        {
            patterns.Add((Path.Combine(localAppData, "Microsoft", "Windows", "Explorer"), "thumbcache_*.db"));
        }
        
        return patterns;
    }
    
    public string[] GetUnimportantExtensions()
    {
        return new[] { ".db", ".thumbs.db", ".ds_store", ".desktop.ini", ".asd", "~$*" };
    }
    
    public List<string> GetUnimportantPatterns()
    {
        return new List<string>
        {
            "Thumbs.db",
            ".DS_Store",
            "desktop.ini",
            "*.asd",
            "~$*"
        };
    }
    
    public string[] GetSystemFileExtensions()
    {
        return new[] { ".exe", ".dll", ".sys", ".ini", ".cfg", ".xml", ".manifest" };
    }
}
