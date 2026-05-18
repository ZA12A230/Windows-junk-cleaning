using System;

namespace DiskCleaner.Models
{
    public class SoftwareInfo
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Publisher { get; set; }
        public string InstallPath { get; set; }
        public string UninstallString { get; set; }
        public bool IsSelected { get; set; }
    }
}
