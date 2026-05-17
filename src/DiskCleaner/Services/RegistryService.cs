using Microsoft.Win32;
using DiskCleaner.Models;
namespace DiskCleaner.Services;
public interface IRegistryService {
    Task<IReadOnlyList<InstalledSoftware>> GetInstalledSoftwareAsync();
}
public class RegistryService : IRegistryService {
    public Task<IReadOnlyList<InstalledSoftware>> GetInstalledSoftwareAsync() {
        return Task.Run(() => {
            var list = new List<InstalledSoftware>();
            try {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                if (key != null) {
                    foreach (var name in key.GetSubKeyNames()) {
                        using var sub = key.OpenSubKey(name);
                        var displayName = sub?.GetValue("DisplayName") as string;
                        if (!string.IsNullOrEmpty(displayName)) {
                            list.Add(new InstalledSoftware {
                                DisplayName = displayName,
                                Publisher = sub.GetValue("Publisher") as string,
                                DisplayVersion = sub.GetValue("DisplayVersion") as string,
                                InstallDate = sub.GetValue("InstallDate") as string,
                                EstimatedSize = sub.GetValue("EstimatedSize") as long?,
                                UninstallString = sub.GetValue("UninstallString") as string,
                                InstallLocation = sub.GetValue("InstallLocation") as string
                            });
                        }
                    }
                }
            } catch { }
            return (IReadOnlyList<InstalledSoftware>)list.OrderBy(s => s.DisplayName).ToList();
        });
    }
}
public interface IConfigService {
    Task<ScanOptions> LoadOptionsAsync();
    Task SaveOptionsAsync(ScanOptions options);
}
public class ConfigService : IConfigService {
    public Task<ScanOptions> LoadOptionsAsync() => Task.FromResult(new ScanOptions());
    public Task SaveOptionsAsync(ScanOptions options) => Task.CompletedTask;
}
