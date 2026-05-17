using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SystemCleanerPro.Services;

public enum DeleteMode
{
    RecycleBin,
    Permanent,
    Secure
}

public class CleanResult
{
    public bool Success { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

public class ProcessInfo
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}

public class CleanService
{
    public event Action<string>? OnStatusUpdate;
    public event Action<int>? OnProgressChanged;
    
    public async Task<List<CleanResult>> CleanFilesAsync(
        IEnumerable<string> filePaths,
        DeleteMode mode,
        CancellationToken cancellationToken = default)
    {
        var results = new List<CleanResult>();
        var files = filePaths.ToList();
        int total = files.Count;
        int current = 0;
        
        foreach (var filePath in files)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            current++;
            OnStatusUpdate?.Invoke($"正在清理: {Path.GetFileName(filePath)}");
            
            var result = mode switch
            {
                DeleteMode.RecycleBin => await DeleteToRecycleBinAsync(filePath),
                DeleteMode.Permanent => await DeletePermanentlyAsync(filePath),
                DeleteMode.Secure => await DeleteSecurelyAsync(filePath),
                _ => await DeleteToRecycleBinAsync(filePath)
            };
            
            results.Add(result);
            OnProgressChanged?.Invoke((current * 100) / total);
        }
        
        return results;
    }
    
    private Task<CleanResult> DeleteToRecycleBinAsync(string filePath)
    {
        return Task.Run(() =>
        {
            try
            {
                if (Directory.Exists(filePath))
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(
                        filePath,
                        Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                        Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                }
                else if (File.Exists(filePath))
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                        filePath,
                        Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                        Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                }
                
                return new CleanResult { Success = true, FilePath = filePath };
            }
            catch (Exception ex)
            {
                return new CleanResult 
                { 
                    Success = false, 
                    FilePath = filePath, 
                    ErrorMessage = ex.Message 
                };
            }
        });
    }
    
    private Task<CleanResult> DeletePermanentlyAsync(string filePath)
    {
        return Task.Run(() =>
        {
            try
            {
                if (Directory.Exists(filePath))
                {
                    Directory.Delete(filePath, true);
                }
                else if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                
                return new CleanResult { Success = true, FilePath = filePath };
            }
            catch (Exception ex)
            {
                return new CleanResult 
                { 
                    Success = false, 
                    FilePath = filePath, 
                    ErrorMessage = ex.Message 
                };
            }
        });
    }
    
    private Task<CleanResult> DeleteSecurelyAsync(string filePath)
    {
        return Task.Run(() =>
        {
            try
            {
                if (!File.Exists(filePath) && !Directory.Exists(filePath))
                {
                    return new CleanResult { Success = true, FilePath = filePath };
                }
                
                if (Directory.Exists(filePath))
                {
                    var files = Directory.GetFiles(filePath, "*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        SecureOverwriteFile(file);
                    }
                    Directory.Delete(filePath, true);
                }
                else if (File.Exists(filePath))
                {
                    SecureOverwriteFile(filePath);
                    File.Delete(filePath);
                }
                
                return new CleanResult { Success = true, FilePath = filePath };
            }
            catch (Exception ex)
            {
                return new CleanResult 
                { 
                    Success = false, 
                    FilePath = filePath, 
                    ErrorMessage = ex.Message 
                };
            }
        });
    }
    
    private void SecureOverwriteFile(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        long fileLength = fileInfo.Length;
        
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Write);
        
        var random = new Random();
        var buffer = new byte[4096];
        
        for (int pass = 0; pass < 3; pass++)
        {
            stream.Position = 0;
            long remaining = fileLength;
            
            while (remaining > 0)
            {
                int toWrite = (int)Math.Min(buffer.Length, remaining);
                random.NextBytes(buffer);
                stream.Write(buffer, 0, toWrite);
                remaining -= toWrite;
            }
            
            stream.Flush();
        }
    }
    
    public List<ProcessInfo> GetProcessesLockingFile(string filePath)
    {
        var processes = new List<ProcessInfo>();
        
        try
        {
            var lockedFiles = GetLockedFiles();
            var fileName = Path.GetFileName(filePath);
            
            foreach (var locked in lockedFiles)
            {
                if (locked.FilePath.Contains(fileName, StringComparison.OrdinalIgnoreCase) ||
                    filePath.Contains(locked.FilePath, StringComparison.OrdinalIgnoreCase))
                {
                    processes.Add(locked);
                }
            }
        }
        catch { }
        
        return processes;
    }
    
    private List<ProcessInfo> GetLockedFiles()
    {
        var lockedFiles = new List<ProcessInfo>();
        
        try
        {
            foreach (var process in System.Diagnostics.Process.GetProcesses())
            {
                try
                {
                    if (string.IsNullOrEmpty(process.MainWindowTitle)) continue;
                    
                    var files = GetProcessFiles(process.Id);
                    foreach (var file in files)
                    {
                        lockedFiles.Add(new ProcessInfo
                        {
                            ProcessId = process.Id,
                            ProcessName = process.ProcessName,
                            FilePath = file
                        });
                    }
                }
                catch { }
            }
        }
        catch { }
        
        return lockedFiles;
    }
    
    private IEnumerable<string> GetProcessFiles(int processId)
    {
        var files = new List<string>();
        
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                $"SELECT * FROM Win32_Process WHERE ProcessId = {processId}");
            
            foreach (var obj in searcher.Get())
            {
                var commandLine = obj["CommandLine"]?.ToString();
                if (!string.IsNullOrEmpty(commandLine))
                {
                    var matches = Regex.Matches(commandLine, @"[\""].+?[\""]|[^\s]+");
                    foreach (Match match in matches)
                    {
                        var path = match.Value.Trim('"');
                        if (File.Exists(path))
                        {
                            files.Add(path);
                        }
                    }
                }
            }
        }
        catch { }
        
        return files;
    }
    
    public bool TerminateProcess(int processId)
    {
        try
        {
            var process = System.Diagnostics.Process.GetProcessById(processId);
            process.Kill();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
