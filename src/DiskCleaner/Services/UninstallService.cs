using System;
using System.Collections.Generic;
using DiskCleaner.Models;

namespace DiskCleaner.Services
{
    public class UninstallService
    {
        public List<SoftwareInfo> GetInstalledSoftware()
        {
            var softwareList = new List<SoftwareInfo>();
            try
            {
                softwareList.Add(new SoftwareInfo
                {
                    Name = "示例软件 1",
                    Version = "1.0.0",
                    Publisher = "示例公司",
                    InstallPath = "C:\\Program Files\\Example1",
                    IsSelected = false
                });
                softwareList.Add(new SoftwareInfo
                {
                    Name = "示例软件 2",
                    Version = "2.5.3",
                    Publisher = "测试开发",
                    InstallPath = "C:\\Program Files\\Example2",
                    IsSelected = false
                });
            }
            catch { }
            return softwareList;
        }
    }
}
