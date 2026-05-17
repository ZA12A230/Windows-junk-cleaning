using System.Diagnostics;

namespace DiskCleaner.Helpers;

public static class PrivilegeHelper
{
    public static void EnableAllPrivileges()
    {
        EnablePrivilege("SeDebugPrivilege");
        EnablePrivilege("SeBackupPrivilege");
        EnablePrivilege("SeRestorePrivilege");
    }
    
    public static bool EnablePrivilege(string privilegeName)
    {
        try
        {
            var tokenHandle = nint.Zero;
            
            if (!NativeMethods.OpenProcessToken(
                NativeMethods.GetCurrentProcess(),
                NativeMethods.TOKEN_ADJUST_PRIVILEGES | NativeMethods.TOKEN_QUERY,
                out tokenHandle))
            {
                Debug.WriteLine($"Failed to open process token for {privilegeName}");
                return false;
            }
            
            try
            {
                if (!NativeMethods.LookupPrivilegeValue(null, privilegeName, out var luid))
                {
                    Debug.WriteLine($"Failed to lookup privilege value for {privilegeName}");
                    return false;
                }
                
                var tp = new NativeMethods.TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Privileges = new NativeMethods.LUID_AND_ATTRIBUTES[1]
                };
                
                tp.Privileges[0].Luid = luid;
                tp.Privileges[0].Attributes = NativeMethods.SE_PRIVILEGE_ENABLED;
                
                if (!NativeMethods.AdjustTokenPrivileges(
                    tokenHandle,
                    false,
                    ref tp,
                    System.Runtime.InteropServices.Marshal.SizeOf(tp),
                    nint.Zero,
                    nint.Zero))
                {
                    Debug.WriteLine($"Failed to adjust token privileges for {privilegeName}");
                    return false;
                }
                
                Debug.WriteLine($"Enabled privilege: {privilegeName}");
                return true;
            }
            finally
            {
                if (tokenHandle != nint.Zero)
                    NativeMethods.CloseHandle(tokenHandle);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error enabling privilege {privilegeName}: {ex.Message}");
            return false;
        }
    }
}
