using System;
using System.Collections.Generic;

namespace DiskCleaner.Models
{
    public class ScanConfiguration
    {
        public bool ScanTempFiles { get; set; } = true;
        public bool ScanRecycleBin { get; set; } = true;
        public bool ScanBrowserCache { get; set; } = true;
        public bool ScanDownloads { get; set; } = false;
        public bool ScanLargeFiles { get; set; } = false;
        public long LargeFileSizeThreshold { get; set; } = 100 * 1024 * 1024; // 100MB
        public List<string> TargetDrives { get; set; } = new List<string>();
    }
}
