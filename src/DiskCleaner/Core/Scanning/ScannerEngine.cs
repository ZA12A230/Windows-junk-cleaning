using System.IO;
using DiskCleaner.Models;

namespace DiskCleaner.Core.Scanning;

public interface IScannerEngine
{
    Task<ScanResult> ScanAllDrivesAsync(
        ScanOptions options,
        IProgress<ScanProgress> progress,
        CancellationToken cancellationToken);
    
    void Cancel();
}

public class ScannerEngine : IScannerEngine
{
    private readonly FileClassifier _classifier;
    private CancellationTokenSource? _cancellationTokenSource;
    
    private static readonly string[] ProtectedDirectories =
    [
        @"C:\Windows\System32",
        @"C:\Windows\WinSxS",
        @"C:\Program Files\WindowsApps"
    ];

    public ScannerEngine(FileClassifier classifier)
    {
        _classifier = classifier;
    }
    
    public async Task<ScanResult> ScanAllDrivesAsync(
        ScanOptions options,
        IProgress<ScanProgress> progress,
        CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _cancellationTokenSource.Token;
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var drives = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
            .ToList();
        
        var junkFiles = new System.Collections.Concurrent.ConcurrentBag<FileEntry>();
        var unusedFiles = new System.Collections.Concurrent.ConcurrentBag<FileEntry>();
        var largeFiles = new System.Collections.Concurrent.ConcurrentBag<FileEntry>();
        var unimportantFiles = new System.Collections.Concurrent.ConcurrentBag<FileEntry>();
        
        var progressReporter = new ProgressReporter(progress);
        
        try
        {
            await Parallel.ForEachAsync(drives, new ParallelOptions
            {
                MaxDegreeOfParallelism = 4,
                CancellationToken = token
            }, async (drive, ct) =>
            {
                await ScanDriveAsync(drive, options, progressReporter, ct);
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        
        stopwatch.Stop();
        
        return new ScanResult
        {
            JunkFiles = junkFiles.ToList().AsReadOnly(),
            UnusedFiles = unusedFiles.ToList().AsReadOnly(),
            LargeFiles = largeFiles.ToList().AsReadOnly(),
            UnimportantFiles = unimportantFiles.ToList().AsReadOnly(),
            ScanTime = DateTime.Now,
            Duration = stopwatch.Elapsed
        };
    }
    
    private async Task ScanDriveAsync(
        DriveInfo drive,
        ScanOptions options,
        ProgressReporter progress,
        CancellationToken token)
    {
        if (!drive.IsReady) return;
        await ScanDirectoryAsync(drive.RootDirectory.FullName, options, progress, token);
    }
    
    private async Task ScanDirectoryAsync(
        string path,
        ScanOptions options,
        ProgressReporter progress,
        CancellationToken token)
    {
        if (IsProtectedPath(path)) return;
        
        progress.ReportCurrentDirectory(path);
        
        try
        {
            var files = Directory.EnumerateFiles(path);
            foreach (var filePath in files)
            {
                if (token.IsCancellationRequested) return;
                
                try
                {
                    var entry = await ProcessFileAsync(filePath, options, token);
                    if (entry != null) progress.AddFile(entry);
                }
                catch { }
            }
        }
        catch { }
        
        try
        {
            var directories = Directory.EnumerateDirectories(path);
            foreach (var dir in directories)
            {
                if (token.IsCancellationRequested) return;
                if (IsProtectedPath(dir)) continue;
                
                try
                {
                    await ScanDirectoryAsync(dir, options, progress, token);
                }
                catch { }
            }
        }
        catch { }
    }
    
    private async Task<FileEntry?> ProcessFileAsync(
        string filePath,
        ScanOptions options,
        CancellationToken token)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists) return null;
            
            var fileType = _classifier.Classify(fileInfo, options);
            if (fileType == FileType.Unknown) return null;
            
            return new FileEntry
            {
                FullPath = fileInfo.FullName,
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
                FileType = fileType,
                LastAccessTime = fileInfo.LastAccessTime,
                LastWriteTime = fileInfo.LastWriteTime,
                Extension = fileInfo.Extension,
                Directory = fileInfo.DirectoryName
            };
        }
        catch { return null; }
    }
    
    private static bool IsProtectedPath(string path)
    {
        var normalized = Path.GetFullPath(path).ToLowerInvariant();
        return ProtectedDirectories.Any(d => normalized.StartsWith(d.ToLowerInvariant()));
    }
    
    public void Cancel() => _cancellationTokenSource?.Cancel();
}

public class ProgressReporter
{
    private readonly IProgress<ScanProgress> _progress;
    private int _filesFound;
    private long _totalSizeScanned;
    
    public ProgressReporter(IProgress<ScanProgress> progress)
    {
        _progress = progress;
    }
    
    public void ReportCurrentDirectory(string directory)
    {
        _progress?.Report(new ScanProgress
        {
            CurrentDirectory = directory,
            FilesFound = _filesFound,
            TotalSizeScanned = _totalSizeScanned,
            ProgressPercentage = 0
        });
    }
    
    public void AddFile(FileEntry entry)
    {
        Interlocked.Increment(ref _filesFound);
        Interlocked.Add(ref _totalSizeScanned, entry.FileSize);
        ReportCurrentDirectory("");
    }
}
