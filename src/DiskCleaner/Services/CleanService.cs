using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using DiskCleaner.Models;

namespace DiskCleaner.Services
{
    public class CleanService
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, uint dwFlags);

        private const uint MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004;

        public int DeleteFiles(List<FileItem> files)
        {
            int deletedCount = 0;
            foreach (var file in files.Where(f => f.IsSelected))
            {
                try
                {
                    if (File.Exists(file.Path))
                    {
                        File.Delete(file.Path);
                        deletedCount++;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    TryForceDelete(file.Path);
                    deletedCount++;
                }
                catch (IOException)
                {
                    TryForceDelete(file.Path);
                    deletedCount++;
                }
                catch { }
            }
            return deletedCount;
        }

        private bool TryForceDelete(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.SetAttributes(filePath, FileAttributes.Normal);
                    File.Delete(filePath);
                    return true;
                }
            }
            catch { }
            
            try
            {
                MoveFileEx(filePath, null, MOVEFILE_DELAY_UNTIL_REBOOT);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public long GetTotalSize(List<FileItem> files)
        {
            return files.Where(f => f.IsSelected).Sum(f => f.Size);
        }
    }
}
