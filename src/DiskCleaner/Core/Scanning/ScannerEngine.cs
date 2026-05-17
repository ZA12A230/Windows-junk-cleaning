using System.Collections.Concurrent;
using System.IO.Enumeration;
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
    private readonly ILogger<ScannerEngine> _logger;
    private CancellationTokenSource? _cancellationTokenSource;
    
    private static readonly string[] ProtectedDirectories =
    [
        @"C:\Windows\System32",
        @"C:\Windows\WinSxS",
        @"C:\Program Files\WindowsApps",
        @"C:\ProgramData\Microsoft\Windows",
        @"C:\Windows\Microsoft.NET"
    ];

    public ScannerEngine(FileClassifier classifier, ILogger<ScannerEngine> logger)
    {
        _classifier = classifier;
        _logger = logger;
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
        
        _logger.LogInformation("Starting scan on {DriveCount} drives", drives.Count);
        
        var junkFiles = new ConcurrentBag<FileEntry>();
        var unusedFiles = new ConcurrentBag<FileEntry>();
        var largeFiles = new ConcurrentBag<FileEntry>();
        var unimportantFiles = new ConcurrentBag<FileEntry>();
        
        var progressReporter = new ProgressReporter(progress, options);
        
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
            _logger.LogInformation("Scan cancelled by user");
            throw;
        }
        
        stopwatch.Stop();
        
        var result = new ScanResult
        {
            JunkFiles = junkFiles.ToList().AsReadOnly(),
            UnusedFiles = unusedFiles.ToList().AsReadOnly(),
            LargeFiles = largeFiles.ToList().AsReadOnly(),
            UnimportantFiles = unimportantFiles.ToList().AsReadOnly(),
            ScanTime = DateTime.Now,
            Duration = stopwatch.Elapsed
        };
        
        _logger.LogInformation("Scan completed in {Duration} - Found {Count} files ({Size})", 
            result.Duration, result.TotalCount, result.TotalSize);
        
        return result;
    }
    
    private async Task ScanDriveAsync(
        DriveInfo drive,
        ScanOptions options,
        ProgressReporter progress,
        CancellationToken token)
    {
        try
        {
            _logger.LogDebug("Scanning drive {DriveName}", drive.Name);
            
            if (!drive.IsReady)
                return;
            
            await ScanDirectoryAsync(drive.RootDirectory.FullName, options, progress, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning drive {DriveName}", drive.Name);
        }
    }
    
    private async Task ScanDirectoryAsync(
        string path,
        ScanOptions options,
        ProgressReporter progress,
        CancellationToken token)
    {
        if (IsProtectedPath(path))
            return;
        
        progress.ReportCurrentDirectory(path);
        
        try
        {
            var files = Directory.EnumerateFiles(path);
            await Parallel.ForEachAsync(files, new ParallelOptions
            {
                MaxDegreeOfParallelism = 8,
                CancellationToken = token
            }, async (filePath, ct) =>
            {
                var entry = await ProcessFileAsync(filePath, options, ct);
                if (entry != null)
                {
                    progress.AddFile(entry);
                }
            });
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Error enumerating files in {Path}", path);
        }
        
        var directories = Directory.EnumerateDirectories(path);
        foreach (var dir in directories)
        {
            if (token.IsCancellationRequested)
                token.ThrowIfCancellationRequested();
            
            if (IsProtectedPath(dir))
                continue;
            
            try
            {
                await ScanDirectoryAsync(dir, options, progress, token);
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogDebug(ex, "Error scanning directory {Dir}", dir);
            }
        }
    }
    
    private async Task<FileEntry?> ProcessFileAsync(
        string filePath,
        ScanOptions options,
        CancellationToken token)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            
            if (!fileInfo.Exists)
                return null;
            
            var fileType = _classifier.Classify(fileInfo, options);
            
            if (fileType == FileType.Unknown)
                return null;
            
            return new FileEntry
            {
                FullPath = fileInfo.FullName,
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
                FileType = fileType,
                LastAccessTime = fileInfo.LastAccessTime,
                LastWriteTime = fileInfo.LastWriteTime,
                Extension = fileInfo.Extension,
                Directory = fileInfo.DirectoryName,
                IsInUse = await IsFileInUseAsync(fileInfo, token)
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error processing file {FilePath}", filePath);
            return null;
        }
    }
    
    private static Task<bool> IsFileInUseAsync(FileInfo fileInfo, CancellationToken token)
    {
        return Task.Run(() =>
        {
            try
            {
                using var stream = fileInfo.Open(
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None);
                return false;
            }
            catch
            {
                return true;
            }
        }, token);
    }
    
    private static bool IsProtectedPath(string path)
    {
        var normalized = Path.GetFullPath(path).ToLowerInvariant();
        return ProtectedDirectories.Any(d => normalized.StartsWith(d.ToLowerInvariant()));
    }
    
    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }
}

public class ProgressReporter
{
    private readonly IProgress<ScanProgress> _progress;
    private readonly ScanOptions _options;
    private int _filesFound;
    private long _totalSizeScanned;
    
    public ProgressReporter(IProgress<ScanProgress> progress, ScanOptions options)
    {
        _progress = progress;
        _options = options;
    }
    
    public void ReportCurrentDirectory(string directory)
    {
        var progressObj = new ScanProgress
        {
            CurrentDirectory = directory,
            FilesFound = _filesFound,
            TotalSizeScanned = _totalSizeScanned,
            ProgressPercentage = 0
        };
        
        _progress?.Report(progressObj);
    }
    
    public void AddFile(FileEntry entry)
    {
        Interlocked.Increment(ref _filesFound);
        Interlocked.Add(ref _totalSizeScanned, entry.FileSize);
        
        ReportCurrentDirectory("");
    }
}
