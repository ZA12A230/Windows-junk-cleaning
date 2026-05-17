using DiskCleaner.Models;
namespace DiskCleaner.Core.Cleaning;
public interface ICleanerEngine {
    Task<CleanResult> CleanAsync(IEnumerable<FileEntry> files, bool force, IProgress<CleanProgress> progress, CancellationToken ct);
}
public class CleanerEngine : ICleanerEngine {
    public async Task<CleanResult> CleanAsync(IEnumerable<FileEntry> files, bool force, IProgress<CleanProgress> progress, CancellationToken ct) {
        var deleted = new List<FileEntry>();
        long freed = 0;
        foreach (var f in files) {
            try { File.Delete(f.FullPath); deleted.Add(f); freed += f.FileSize; } catch { }
        }
        return new CleanResult { DeletedFiles = deleted.AsReadOnly(), TotalFreedSpace = freed };
    }
}
