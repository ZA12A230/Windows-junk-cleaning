using System.IO;
using DiskCleaner.Models;
namespace DiskCleaner.Core.Scanning;
public interface IScannerEngine {
    Task<ScanResult> ScanAllDrivesAsync(ScanOptions options, IProgress<ScanProgress> progress, CancellationToken ct);
    void Cancel();
}
public class ScannerEngine : IScannerEngine {
    CancellationTokenSource? _cts;
    static readonly string[] Protected = {@"C:\Windows\System32", @"C:\Program Files\WindowsApps"};
    public async Task<ScanResult> ScanAllDrivesAsync(ScanOptions options, IProgress<ScanProgress> progress, CancellationToken ct) {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var files = new System.Collections.Concurrent.ConcurrentBag<FileEntry>();
        var prog = new Progress<ScanProgress>(p => progress?.Report(p));
        try {
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady)) {
                await ScanDriveAsync(drive, options, prog, _cts.Token);
            }
        } catch { }
        sw.Stop();
        return new ScanResult { JunkFiles = files.ToList().AsReadOnly(), Duration = sw.Elapsed };
    }
    async Task ScanDriveAsync(DriveInfo drive, ScanOptions options, IProgress<ScanProgress> progress, CancellationToken ct) {
        if (!drive.IsReady) return;
        try {
            foreach (var file in Directory.EnumerateFiles(drive.RootDirectory.FullName, "*.*", SearchOption.AllDirectories)) {
                if (ct.IsCancellationRequested || IsProtected(file)) continue;
                try {
                    var info = new FileInfo(file);
                    if (info.Length > options.LargeFileSizeThreshold) {
                        files.Add(new FileEntry { FullPath = info.FullName, FileName = info.Name, FileSize = info.Length, FileType = FileType.LargeFile });
                    }
                } catch { }
            }
        } catch { }
    }
    static bool IsProtected(string path) => Protected.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    public void Cancel() => _cts?.Cancel();
}
static System.Collections.Concurrent.ConcurrentBag<FileEntry> files = new();
