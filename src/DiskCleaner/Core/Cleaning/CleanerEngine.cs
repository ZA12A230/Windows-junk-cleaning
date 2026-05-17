using System.IO;
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
    public async Task<CleanResult> CleanAsync(
        IEnumerable<FileEntry> files,
        bool forceDelete,
        IProgress<CleanProgress> progress,
        CancellationToken cancellationToken)
    {
        var filesList = files.ToList();
        var deletedFiles = new List<FileEntry>();
        var failedFiles = new List<FailedFileEntry>();
        var freedSpace = 0L;
        
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
                failedFiles.Add(new FailedFileEntry { File = file, Reason = result.ErrorMessage ?? "Unknown" });
            }
            
            progress?.Report(new CleanProgress
            {
                ProcessedCount = i + 1,
                TotalCount = filesList.Count,
                CurrentFile = file.FileName,
                ProgressPercentage = (i + 1) * 100.0 / filesList.Count
            });
        }
        
        return new CleanResult
        {
            DeletedFiles = deletedFiles.AsReadOnly(),
            FailedFiles = failedFiles.AsReadOnly(),
            TotalFreedSpace = freedSpace
        };
    }
    
    private static async Task<DeleteResult> DeleteFileAsync(FileEntry file, bool forceDelete)
    {
        try
        {
            if (!File.Exists(file.FullPath)) return DeleteResult.Success();
            
            await Task.Run(() =>
            {
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        File.Delete(file.FullPath);
                        return;
                    }
                    catch (IOException) when (i < 2)
                    {
                        Thread.Sleep(100 * (i + 1));
                    }
                }
            });
            
            return DeleteResult.Success();
        }
        catch (Exception ex)
        {
            return DeleteResult.Failure($"Error: {ex.Message}");
        }
    }
}

public record CleanResult
{
    public IReadOnlyList<FileEntry> DeletedFiles { get; init; } = new List<FileEntry>();
    public IReadOnlyList<FailedFileEntry> FailedFiles { get; init; } = new List<FailedFileEntry>();
    public long TotalFreedSpace { get; init; }
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
