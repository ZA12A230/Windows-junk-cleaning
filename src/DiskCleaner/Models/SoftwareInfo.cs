using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DiskCleaner.Models
{
    public partial class SoftwareInfo : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _version = string.Empty;

        [ObservableProperty]
        private string _publisher = string.Empty;

        [ObservableProperty]
        private string _installPath = string.Empty;

        [ObservableProperty]
        private string _uninstallString = string.Empty;

        [ObservableProperty]
        private bool _isSelected;
    }
}
