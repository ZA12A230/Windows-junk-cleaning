using Microsoft.Win32;

namespace DiskCleaner.Services;

public interface IRegistryService
{
    Task<IReadOnlyList<InstalledSoftware>> GetInstalledSoftwareAsync();
    Task<string> ExportRegistryKeyAsync(RegistryHive hive, string subKey);
    Task<bool> DeleteKeyAsync(RegistryHive hive, string subKey);
    Task<IReadOnlyList<string>> ScanResidualRegistryAsync(string appName, string? publisher);
}

public class RegistryService : IRegistryService
{
    private readonly ILogger<RegistryService> _logger;
    
    private static readonly string[] UninstallPaths =
    [
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    ];

    public RegistryService(ILogger<RegistryService> logger)
    {
        _logger = logger;
    }
    
    public Task<IReadOnlyList<InstalledSoftware>> GetInstalledSoftwareAsync()
    {
        return Task.Run(() =>
        {
            var software = new List<InstalledSoftware>();
            
            foreach (var path in UninstallPaths)
            {
                try
                {
                    var is64Bit = path.Contains("WOW6432");
                    
                    using var key = Registry.LocalMachine.OpenSubKey(path);
                    if (key == null)
                        continue;
                    
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        try
                        {
                            using var subKey = key.OpenSubKey(subKeyName);
                            if (subKey == null)
                                continue;
                            
                            var displayName = subKey.GetValue("DisplayName") as string;
                            if (string.IsNullOrEmpty(displayName))
                                continue;
                            
                            var softwareEntry = new InstalledSoftware
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
                                Is64Bit = is64Bit
                            };
                            
                            software.Add(softwareEntry);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Error reading software subkey {SubKey}", subKeyName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error reading uninstall path {Path}", path);
                }
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
                            if (subKey == null)
                                continue;
                            
                            var displayName = subKey.GetValue("DisplayName") as string;
                            if (string.IsNullOrEmpty(displayName))
                                continue;
                            
                            var softwareEntry = new InstalledSoftware
                            {
                                DisplayName = displayName,
                                Publisher = subKey.GetValue("Publisher") as string,
                                DisplayVersion = subKey.GetValue("DisplayVersion") as string,
                                InstallDate = subKey.GetValue("InstallDate") as string,
                                EstimatedSize = subKey.GetValue("EstimatedSize") as long?,
                                UninstallString = subKey.GetValue("UninstallString") as string,
                                InstallLocation = subKey.GetValue("InstallLocation") as string,
                                DisplayIcon = subKey.GetValue("DisplayIcon") as string,
                                RegistryKeyPath = $@"HKEY_CURRENT_USER\{UninstallPaths[0]}\{subKeyName}",
                                RegistryHive = RegistryHive.CurrentUser,
                                Is64Bit = false
                            };
                            
                            software.Add(softwareEntry);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Error reading user software subkey {SubKey}", subKeyName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error reading user uninstall keys");
            }
            
            return (IReadOnlyList<InstalledSoftware>)software
                .OrderBy(s => s.DisplayName)
                .ToList();
        });
    }
    
    public async Task<string> ExportRegistryKeyAsync(RegistryHive hive, string subKey)
    {
        return await Task.Run(() =>
        {
            var backupPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DiskCleaner",
                "backup",
                $"Registry_{DateTime.Now:yyyyMMdd_HHmmss}.reg");
            
            Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
            
            try
            {
                var startKey = hive == RegistryHive.LocalMachine 
                    ? "HKEY_LOCAL_MACHINE" 
                    : "HKEY_CURRENT_USER";
                
                var fullKey = $@"{startKey}\{subKey}";
                
                using var writer = new StreamWriter(backupPath);
                writer.WriteLine("Windows Registry Editor Version 5.00");
                writer.WriteLine();
                writer.WriteLine($"[{fullKey}]");
                
                _logger.LogInformation("Exported registry key to {BackupPath}", backupPath);
                return backupPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export registry key {SubKey}", subKey);
                throw;
            }
        });
    }
    
    public Task<bool> DeleteKeyAsync(RegistryHive hive, string subKey)
    {
        return Task.Run(() =>
        {
            try
            {
                var baseKey = hive == RegistryHive.LocalMachine 
                    ? Registry.LocalMachine 
                    : Registry.CurrentUser;
                
                var parentPath = Path.GetDirectoryName(subKey);
                var keyName = Path.GetFileName(subKey);
                
                if (parentPath == null)
                    return false;
                
                using var parentKey = baseKey.OpenSubKey(parentPath, true);
                if (parentKey == null)
                    return false;
                
                parentKey.DeleteSubKeyTree(keyName, false);
                _logger.LogInformation("Deleted registry key: {SubKey}", subKey);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete registry key {SubKey}", subKey);
                return false;
            }
        });
    }
    
    public async Task<IReadOnlyList<string>> ScanResidualRegistryAsync(string appName, string? publisher)
    {
        return await Task.Run(() =>
        {
            var residualKeys = new List<string>();
            var searchTerms = new List<string> { appName };
            
            if (!string.IsNullOrEmpty(publisher))
                searchTerms.Add(publisher);
            
            var paths = new[]
            {
                @"SOFTWARE",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
            };
            
            foreach (var basePath in paths)
            {
                foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
                {
                    try
                    {
                        var baseKey = hive == RegistryHive.LocalMachine 
                            ? Registry.LocalMachine 
                            : Registry.CurrentUser;
                        
                        using var baseSubKey = baseKey.OpenSubKey(basePath);
                        if (baseSubKey == null)
                            continue;
                        
                        ScanKeyForResiduals(baseSubKey, searchTerms, basePath, hive, residualKeys);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error scanning {BasePath} for residuals", basePath);
                    }
                }
            }
            
            return residualKeys.Distinct().ToList().AsReadOnly();
        });
    }
    
    private static void ScanKeyForResiduals(
        RegistryKey key,
        List<string> searchTerms,
        string basePath,
        RegistryHive hive,
        List<string> results)
    {
        foreach (var subKeyName in key.GetSubKeyNames())
        {
            try
            {
                using var subKey = key.OpenSubKey(subKeyName);
                if (subKey == null)
                    continue;
                
                var fullName = $@"{(hive == RegistryHive.LocalMachine ? "HKLM" : "HKCU")}\{basePath}\{subKeyName}";
                
                foreach (var term in searchTerms)
                {
                    if (subKeyName.Contains(term, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(fullName);
                        break;
                    }
                }
                
                ScanKeyForResiduals(subKey, searchTerms, $@"{basePath}\{subKeyName}", hive, results);
            }
            catch
            {
            }
        }
    }
}
