using System.Diagnostics;
using DiskCleaner.Models;

namespace DiskCleaner.Core.Cleaning;

public interface ICleanerEngine
{
    Task<CleanResult> CleanAsync(
        IEnumerable<FileEntry> files,
        bool forceDelete,
        IProgress<CleanProgress> progress,
        CancellationToken cancellationToken);
}

public class CleanerEngine : ICleanerEngine
{
    private readonly ILogger<CleanerEngine> _logger;
    
    public CleanerEngine(ILogger<CleanerEngine> logger)
    {
        _logger = logger;
    }
    
    public async Task<CleanResult> CleanAsync(
        IEnumerable<FileEntry> files,
        bool forceDelete,
        IProgress<CleanProgress> progress,
        CancellationToken cancellationToken)
    {
        var filesList = files.ToList();
        var totalFiles = filesList.Count;
        var totalSize = filesList.Sum(f => f.FileSize);
        
        var deletedFiles = new List<FileEntry>();
        var failedFiles = new List<FailedFileEntry>();
        var freedSpace = 0L;
        
        _logger.LogInformation("Starting cleanup of {Count} files ({Size})", totalFiles, totalSize);
        
        for (int i = 0; i < filesList.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var file = filesList[i];
            var result = await DeleteFileAsync(file, forceDelete);
            
            if (result.Success)
            {
                deletedFiles.Add(file);
                freedSpace += file.FileSize;
            }
            else
            {
                failedFiles.Add(new FailedFileEntry
                {
                    File = file,
                    Reason = result.ErrorMessage ?? "Unknown error"
                });
            }
            
            progress?.Report(new CleanProgress
            {
                ProcessedCount = i + 1,
                TotalCount = totalFiles,
                CurrentFile = file.FileName,
                ProgressPercentage = (i + 1) * 100.0 / totalFiles
            });
        }
        
        var cleanResult = new CleanResult
        {
            DeletedFiles = deletedFiles.AsReadOnly(),
            FailedFiles = failedFiles.AsReadOnly(),
            TotalFreedSpace = freedSpace,
            Duration = TimeSpan.Zero
        };
        
        _logger.LogInformation("Cleanup completed: {Deleted}/{Total} files deleted, {FreedSpace} freed",
            deletedFiles.Count, totalFiles, freedSpace);
        
        return cleanResult;
    }
    
    private static async Task<DeleteResult> DeleteFileAsync(FileEntry file, bool forceDelete)
    {
        try
        {
            if (!File.Exists(file.FullPath))
                return DeleteResult.Success();
            
            if (forceDelete)
            {
                return await TryForceDeleteAsync(file);
            }
            
            File.Delete(file.FullPath);
            return DeleteResult.Success();
        }
        catch (IOException ex) when (forceDelete)
        {
            return await TryForceDeleteAsync(file);
        }
        catch (UnauthorizedAccessException ex)
        {
            return DeleteResult.Failure($"Access denied: {ex.Message}");
        }
        catch (Exception ex)
        {
            return DeleteResult.Failure($"Error: {ex.Message}");
        }
    }
    
    private static async Task<DeleteResult> TryForceDeleteAsync(FileEntry file)
    {
        try
        {
            await Task.Run(() =>
            {
                if (DeleteFileWithRetry(file.FullPath))
                    return;
                
                if (MoveFileEx(file.FullPath, null, NativeMethods.MOVEFILE_DELAY_UNTIL_REBOOT))
                    return;
            });
            
            return DeleteResult.Success();
        }
        catch (Exception ex)
        {
            return DeleteResult.Failure($"Force delete failed: {ex.Message}");
        }
    }
    
    private static bool DeleteFileWithRetry(string path, int maxRetries = 3)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                File.Delete(path);
                return true;
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                Thread.Sleep(100 * (i + 1));
            }
        }
        return false;
    }
}

public record CleanResult
{
    public IReadOnlyList<FileEntry> DeletedFiles { get; init; } = new List<FileEntry>();
    public IReadOnlyList<FailedFileEntry> FailedFiles { get; init; } = new List<FailedFileEntry>();
    public long TotalFreedSpace { get; init; }
    public TimeSpan Duration { get; init; }
    
    public int TotalFiles => DeletedFiles.Count;
    public int FailedCount => FailedFiles.Count;
    public bool HasFailures => FailedFiles.Any();
}

public record FailedFileEntry
{
    public FileEntry File { get; init; } = new();
    public string Reason { get; init; } = "";
}

public record DeleteResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    
    public static DeleteResult Success() => new() { Success = true };
    public static DeleteResult Failure(string error) => new() { Success = false, ErrorMessage = error };
}

public record CleanProgress
{
    public int ProcessedCount { get; init; }
    public int TotalCount { get; init; }
    public string CurrentFile { get; init; } = "";
    public double ProgressPercentage { get; init; }
}
