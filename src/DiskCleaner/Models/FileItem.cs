using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DiskCleaner.Models
{
    public partial class FileItem : ObservableObject
    {
        [ObservableProperty]
        private string _path = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private long _size;

        public string SizeDisplay => Size >= 1073741824 ? $"{Size / 1073741824.0:F2} GB" :
                                     Size >= 1048576 ? $"{Size / 1048576.0:F2} MB" :
                                     Size >= 1024 ? $"{Size / 1024.0:F2} KB" : $"{Size} B";

        [ObservableProperty]
        private string _type = string.Empty;

        [ObservableProperty]
        private bool _isSelected = true;
    }
}
