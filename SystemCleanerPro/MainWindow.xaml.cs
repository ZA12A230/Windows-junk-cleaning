using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SystemCleanerPro.Models;
using SystemCleanerPro.Services;
using SystemCleanerPro.ViewModels;

namespace SystemCleanerPro;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        FileListView.SelectionChanged += FileListView_SelectionChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.StatusText):
                StatusText.Text = ViewModel.StatusText;
                break;
            case nameof(MainViewModel.ProgressValue):
                ScanProgress.Value = ViewModel.ProgressValue;
                break;
            case nameof(MainViewModel.IsScanning):
                ScanProgress.Visibility = ViewModel.IsScanning ? Visibility.Visible : Visibility.Collapsed;
                break;
            case nameof(MainViewModel.TotalFiles):
                TotalFilesText.Text = $"{ViewModel.TotalFiles} 个文件";
                break;
            case nameof(MainViewModel.TotalSize):
                TotalSizeText.Text = $" ({FormatSize(ViewModel.TotalSize)})";
                break;
            case nameof(MainViewModel.SelectedFiles):
                SelectedFilesText.Text = $"{ViewModel.SelectedFiles} 个文件";
                break;
            case nameof(MainViewModel.SelectedSize):
                SelectedSizeText.Text = $" ({FormatSize(ViewModel.SelectedSize)})";
                break;
            case nameof(MainViewModel.CurrentItems):
                FileListView.ItemsSource = ViewModel.CurrentItems;
                UpdateFileCount();
                break;
        }
    }

    private void FileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        foreach (FileItem item in e.AddedItems)
        {
            item.IsSelected = true;
        }
        foreach (FileItem item in e.RemovedItems)
        {
            item.IsSelected = false;
        }
        ViewModel?.NotifySelectionChanged();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            MaximizeButton_Click(sender, e);
        }
        else
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tagStr && int.TryParse(tagStr, out int index))
        {
            ViewModel.SelectedNavIndex = index;

            bool showFileList = index < 4;
            FileListView.Visibility = showFileList ? Visibility.Visible : Visibility.Collapsed;
            SoftwareListView.Visibility = showFileList ? Visibility.Collapsed : Visibility.Visible;
            CleanButton.Visibility = showFileList ? Visibility.Visible : Visibility.Collapsed;
            UninstallButton.Visibility = showFileList ? Visibility.Collapsed : Visibility.Visible;

            CategoryTitle.Text = index switch
            {
                0 => "垃圾文件",
                1 => "不常用文件",
                2 => "大文件",
                3 => "不重要文件",
                4 => "软件卸载",
                _ => "文件列表"
            };

            if (index == 4)
            {
                SoftwareListView.ItemsSource = ViewModel.InstalledSoftware;
                if (!ViewModel.InstalledSoftware.Any())
                {
                    ViewModel.LoadSoftwareCommand.Execute(null);
                }
            }
            else
            {
                FileListView.ItemsSource = ViewModel.CurrentItems;
                UpdateFileCount();
            }
        }
    }

    private void UpdateFileCount()
    {
        FileCountText.Text = $" ({ViewModel.CurrentItems?.Count ?? 0} 个文件)";
    }

    private async void StartScan_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsScanning) return;
        
        await ViewModel.StartScanCommand.ExecuteAsync(null);
        FileListView.ItemsSource = ViewModel.CurrentItems;
        UpdateFileCount();
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectAllCommand.Execute(null);
    }

    private void DeselectAll_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.DeselectAllCommand.Execute(null);
    }

    private async void Clean_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsCleaning) return;

        int selectedMode = DeleteModeCombo.SelectedIndex;
        ViewModel.SelectedDeleteMode = selectedMode switch
        {
            0 => DeleteMode.RecycleBin,
            1 => DeleteMode.Permanent,
            2 => DeleteMode.Secure,
            _ => DeleteMode.RecycleBin
        };

        await ViewModel.CleanSelectedCommand.ExecuteAsync(null);
        UpdateFileCount();
    }

    private async void Uninstall_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsCleaning) return;

        var selected = ViewModel.InstalledSoftware.FirstOrDefault(s => s.IsSelected);
        if (selected == null)
        {
            MessageBox.Show("请先选择要卸载的软件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"确定要卸载 '{selected.DisplayName}' 吗？\n这将执行深度卸载，清除应用本体、残留文件和注册表项。",
            "确认卸载",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            await ViewModel.UninstallSelectedCommand.ExecuteAsync(null);
        }
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}
