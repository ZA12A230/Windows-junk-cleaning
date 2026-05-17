using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SystemCleanerPro.Models;
using Microsoft.Win32;

namespace SystemCleanerPro.Services;

public class UninstallService
{
    private const string UninstallKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
    private const string WowUninstallKeyPath = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
    
    public event Action<string>? OnStatusUpdate;
    public event Action<int>? OnProgressChanged;
    
    public async Task<List<SoftwareInfo>> GetInstalledSoftwareAsync()
    {
        return await Task.Run(() =>
        {
            var software = new List<SoftwareInfo>();
            
            var locations = new[]
            {
                (Registry.LocalMachine, UninstallKeyPath),
                (Registry.LocalMachine, WowUninstallKeyPath),
                (Registry.CurrentUser, UninstallKeyPath)
            };
            
            foreach (var (root, path) in locations)
            {
                try
                {
                    using var key = root.OpenSubKey(path);
                    if (key == null) continue;
                    
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        try
                        {
                            using var subKey = key.OpenSubKey(subKeyName);
                            if (subKey == null) continue;
                            
                            var displayName = subKey.GetValue("DisplayName")?.ToString();
                            if (string.IsNullOrEmpty(displayName)) continue;
                            
                            var info = new SoftwareInfo
                            {
                                DisplayName = displayName,
                                Version = subKey.GetValue("DisplayVersion")?.ToString() ?? string.Empty,
                                Publisher = subKey.GetValue("Publisher")?.ToString() ?? string.Empty,
                                InstallLocation = subKey.GetValue("InstallLocation")?.ToString() ?? string.Empty,
                                UninstallString = subKey.GetValue("UninstallString")?.ToString() ?? string.Empty,
                                EstimatedSize = Convert.ToInt64(subKey.GetValue("EstimatedSize") ?? 0) * 1024,
                                RegistryKey = $"{root.Name}\\{path}\\{subKeyName}"
                            };
                            
                            var installDateStr = subKey.GetValue("InstallDate")?.ToString();
                            if (!string.IsNullOrEmpty(installDateStr) && DateTime.TryParseExact(installDateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var installDate))
                            {
                                info.InstallDate = installDate;
                            }
                            
                            if (!software.Any(s => s.DisplayName.Equals(info.DisplayName, StringComparison.OrdinalIgnoreCase)))
                            {
                                software.Add(info);
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
            
            return software.OrderBy(s => s.DisplayName).ToList();
        });
    }
    
    public async Task<(bool Success, string Message)> UninstallSoftwareAsync(SoftwareInfo software)
    {
        return await Task.Run(() =>
        {
            try
            {
                OnStatusUpdate?.Invoke($"正在卸载: {software.DisplayName}");
                
                if (!string.IsNullOrEmpty(software.UninstallString))
                {
                    var uninstallString = software.UninstallString;
                    
                    if (uninstallString.StartsWith("MsiExec.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(uninstallString, @"/[IX]\{[^}]+\}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            var msiPath = match.Value.Substring(3);
                            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "msiexec.exe",
                                Arguments = $"/x {msiPath} /qn",
                                UseShellExecute = false,
                                CreateNoWindow = true
                            });
                            process?.WaitForExit(120000);
                        }
                    }
                    else
                    {
                        uninstallString = uninstallString.Trim('"');
                        var parts = uninstallString.Split(' ', 2);
                        var exePath = parts[0];
                        var args = parts.Length > 1 ? parts[1] : "";
                        
                        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = exePath,
                            Arguments = args,
                            UseShellExecute = true,
                            CreateNoWindow = true
                        });
                        process?.WaitForExit(180000);
                    }
                }
                
                OnStatusUpdate?.Invoke("正在清理残留文件...");
                var cleanedFiles = CleanResidualFiles(software);
                
                OnStatusUpdate?.Invoke("正在清理注册表...");
                var cleanedRegistry = CleanResidualRegistry(software);
                
                var message = $"清理完成: {cleanedFiles} 个文件/文件夹, {cleanedRegistry} 个注册表项";
                return (true, message);
            }
            catch (Exception ex)
            {
                return (false, $"卸载失败: {ex.Message}");
            }
        });
    }
    
    private int CleanResidualFiles(SoftwareInfo software)
    {
        int cleaned = 0;
        var locations = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            software.InstallLocation
        };
        
        foreach (var location in locations)
        {
            if (string.IsNullOrEmpty(location) || !Directory.Exists(location)) continue;
            
            try
            {
                var appFolders = Directory.GetDirectories(location, $"*{software.DisplayName}*", SearchOption.TopDirectoryOnly);
                foreach (var folder in appFolders)
                {
                    try
                    {
                        Directory.Delete(folder, true);
                        cleaned++;
                    }
                    catch { }
                }
                
                var companyFolders = new DirectoryInfo(location).GetDirectories();
                foreach (var folder in companyFolders)
                {
                    if (folder.Name.Contains(software.Publisher ?? "", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var subFolders = folder.GetDirectories($"*{software.DisplayName}*");
                            foreach (var sub in subFolders)
                            {
                                try
                                {
                                    sub.Delete(true);
                                    cleaned++;
                                }
                                catch { }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }
        
        return cleaned;
    }
    
    private int CleanResidualRegistry(SoftwareInfo software)
    {
        int cleaned = 0;
        
        var keysToClean = new List<string>();
        
        try
        {
            using var uninstallKey = Registry.LocalMachine.OpenSubKey(UninstallKeyPath);
            if (uninstallKey != null)
            {
                foreach (var subKeyName in uninstallKey.GetSubKeyNames())
                {
                    if (subKeyName.Contains(software.DisplayName, StringComparison.OrdinalIgnoreCase))
                    {
                        keysToClean.Add($"{UninstallKeyPath}\\{subKeyName}");
                    }
                }
            }
        }
        catch { }
        
        try
        {
            using var uninstallKey = Registry.CurrentUser.OpenSubKey(UninstallKeyPath);
            if (uninstallKey != null)
            {
                foreach (var subKeyName in uninstallKey.GetSubKeyNames())
                {
                    if (subKeyName.Contains(software.DisplayName, StringComparison.OrdinalIgnoreCase))
                    {
                        keysToClean.Add($"{UninstallKeyPath}\\{subKeyName}");
                    }
                }
            }
        }
        catch { }
        
        try
        {
            using var uninstallKey = Registry.LocalMachine.OpenSubKey(WowUninstallKeyPath);
            if (uninstallKey != null)
            {
                foreach (var subKeyName in uninstallKey.GetSubKeyNames())
                {
                    if (subKeyName.Contains(software.DisplayName, StringComparison.OrdinalIgnoreCase))
                    {
                        keysToClean.Add($"{WowUninstallKeyPath}\\{subKeyName}");
                    }
                }
            }
        }
        catch { }
        
        foreach (var keyPath in keysToClean)
        {
            try
            {
                Registry.LocalMachine.DeleteSubKeyTree(keyPath, false);
                cleaned++;
            }
            catch { }
            
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(keyPath, false);
                cleaned++;
            }
            catch { }
        }
        
        return cleaned;
    }
    
    public async Task<List<string>> GetResidualItemsAsync(SoftwareInfo software)
    {
        return await Task.Run(() =>
        {
            var items = new List<string>();
            
            var locations = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                software.InstallLocation
            };
            
            foreach (var location in locations)
            {
                if (string.IsNullOrEmpty(location) || !Directory.Exists(location)) continue;
                
                try
                {
                    var appFolders = Directory.GetDirectories(location, $"*{software.DisplayName}*", SearchOption.TopDirectoryOnly);
                    items.AddRange(appFolders);
                }
                catch { }
            }
            
            return items;
        });
    }
}
