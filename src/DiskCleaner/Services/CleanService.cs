using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiskCleaner.Models;

namespace DiskCleaner.Services
{
    public class CleanService
    {
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
                catch { }
            }
            return deletedCount;
        }
    }
}
