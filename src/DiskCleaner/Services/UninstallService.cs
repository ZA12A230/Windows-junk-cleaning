using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Win32;
using DiskCleaner.Models;

namespace DiskCleaner.Services
{
    public class UninstallService
    {
        public List<SoftwareInfo> GetInstalledSoftware()
        {
            var softwareList = new List<SoftwareInfo>();
            
            softwareList.AddRange(GetSoftwareFromRegistry(
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")));
            
            softwareList.AddRange(GetSoftwareFromRegistry(
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall")));
            
            softwareList.AddRange(GetSoftwareFromRegistry(
                Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")));

            return softwareList.OrderBy(s => s.Name).ToList();
        }

        private List<SoftwareInfo> GetSoftwareFromRegistry(RegistryKey? registryKey)
        {
            var softwareList = new List<SoftwareInfo>();
            
            if (registryKey == null) return softwareList;

            try
            {
                foreach (var subKeyName in registryKey.GetSubKeyNames())
                {
                    try
                    {
                        using var subKey = registryKey.OpenSubKey(subKeyName);
                        if (subKey == null) continue;

                        var displayName = subKey.GetValue("DisplayName") as string;
                        if (string.IsNullOrWhiteSpace(displayName)) continue;

                        var version = subKey.GetValue("DisplayVersion") as string ?? "";
                        var publisher = subKey.GetValue("Publisher") as string ?? "";
                        var installLocation = subKey.GetValue("InstallLocation") as string ?? "";
                        var uninstallString = subKey.GetValue("UninstallString") as string ?? "";

                        softwareList.Add(new SoftwareInfo
                        {
                            Name = displayName,
                            Version = version,
                            Publisher = publisher,
                            InstallPath = installLocation,
                            UninstallString = uninstallString,
                            IsSelected = false
                        });
                    }
                    catch { }
                }
            }
            catch { }

            return softwareList;
        }

        public bool UninstallSoftware(SoftwareInfo software)
        {
            if (string.IsNullOrWhiteSpace(software.UninstallString))
                return false;

            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = software.UninstallString,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(processInfo);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
