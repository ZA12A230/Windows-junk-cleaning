using Microsoft.Win32;

namespace DiskCleaner.Services;

public interface IRegistryService
{
    Task<IReadOnlyList<InstalledSoftware>> GetInstalledSoftwareAsync();
    Task<IReadOnlyList<string>> ScanResidualRegistryAsync(string appName, string? publisher);
}

public class RegistryService : IRegistryService
{
    private static readonly string[] UninstallPaths =
    [
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    ];

    public Task<IReadOnlyList<InstalledSoftware>> GetInstalledSoftwareAsync()
    {
        return Task.Run(() =>
        {
            var software = new List<InstalledSoftware>();
            
            foreach (var path in UninstallPaths)
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(path);
                    if (key == null) continue;
                    
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        try
                        {
                            using var subKey = key.OpenSubKey(subKeyName);
                            if (subKey == null) continue;
                            
                            var displayName = subKey.GetValue("DisplayName") as string;
                            if (string.IsNullOrEmpty(displayName)) continue;
                            
                            software.Add(new InstalledSoftware
                            {
                                DisplayName = displayName,
                                Publisher = subKey.GetValue("Publisher") as string,
                                DisplayVersion = subKey.GetValue("DisplayVersion") as string,
                                InstallDate = subKey.GetValue("InstallDate") as string,
                                EstimatedSize = subKey.GetValue("EstimatedSize") as long?,
                                UninstallString = subKey.GetValue("UninstallString") as string,
                                InstallLocation = subKey.GetValue("InstallLocation") as string,
                                DisplayIcon = subKey.GetValue("DisplayIcon") as string,
                                RegistryKeyPath = $@"HKEY_LOCAL_MACHINE\{path}\{subKeyName}",
                                RegistryHive = RegistryHive.LocalMachine,
                                Is64Bit = path.Contains("WOW6432")
                            });
                        }
                        catch { }
                    }
                }
                catch { }
            }
            
            try
            {
                using var userKey = Registry.CurrentUser.OpenSubKey(UninstallPaths[0]);
                if (userKey != null)
                {
                    foreach (var subKeyName in userKey.GetSubKeyNames())
                    {
                        try
                        {
                            using var subKey = userKey.OpenSubKey(subKeyName);
                            if (subKey == null) continue;
                            
                            var displayName = subKey.GetValue("DisplayName") as string;
                            if (string.IsNullOrEmpty(displayName)) continue;
                            
                            software.Add(new InstalledSoftware
                            {
                                DisplayName = displayName,
                                Publisher = subKey.GetValue("Publisher") as string,
                                DisplayVersion = subKey.GetValue("DisplayVersion") as string,
                                InstallDate = subKey.GetValue("InstallDate") as string,
                                EstimatedSize = subKey.GetValue("EstimatedSize") as long?,
                                UninstallString = subKey.GetValue("UninstallString") as string,
                                InstallLocation = subKey.GetValue("InstallLocation") as string,
                                RegistryKeyPath = $@"HKEY_CURRENT_USER\{UninstallPaths[0]}\{subKeyName}",
                                RegistryHive = RegistryHive.CurrentUser,
                                Is64Bit = false
                            });
                        }
                        catch { }
                    }
                }
            }
            catch { }
            
            return (IReadOnlyList<InstalledSoftware>)software.OrderBy(s => s.DisplayName).ToList();
        });
    }
    
    public Task<IReadOnlyList<string>> ScanResidualRegistryAsync(string appName, string? publisher)
    {
        return Task.Run(() =>
        {
            var results = new List<string>();
            // 简化实现，仅返回空列表
            return (IReadOnlyList<string>)results;
        });
    }
}

public interface IConfigService
{
    Task<ScanOptions> LoadOptionsAsync();
    Task SaveOptionsAsync(ScanOptions options);
}

public class ConfigService : IConfigService
{
    public Task<ScanOptions> LoadOptionsAsync() => Task.FromResult(new ScanOptions());
    public Task SaveOptionsAsync(ScanOptions options) => Task.CompletedTask;
}
