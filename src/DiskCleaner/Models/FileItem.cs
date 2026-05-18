using System;

namespace DiskCleaner.Models
{
    public class FileItem
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public string SizeDisplay => Size >= 1073741824 ? $"{Size / 1073741824.0:F2} GB" :
                                     Size >= 1048576 ? $"{Size / 1048576.0:F2} MB" :
                                     Size >= 1024 ? $"{Size / 1024.0:F2} KB" : $"{Size} B";
        public string Type { get; set; }
        public bool IsSelected { get; set; }
    }
}
