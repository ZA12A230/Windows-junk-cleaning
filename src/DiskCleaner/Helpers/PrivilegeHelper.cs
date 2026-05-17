using System.Runtime.InteropServices;

namespace DiskCleaner.Helpers;

public class PrivilegeHelper
{
    private static readonly ILogger<PrivilegeHelper> _logger = 
        App.GetService<ILogger<PrivilegeHelper>>();
    
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
                _logger.LogWarning("Failed to open process token for {Privilege}", privilegeName);
                return false;
            }
            
            try
            {
                if (!NativeMethods.LookupPrivilegeValue(null, privilegeName, out var luid))
                {
                    _logger.LogWarning("Failed to lookup privilege value for {Privilege}", privilegeName);
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
                    Marshal.SizeOf(tp),
                    nint.Zero,
                    nint.Zero))
                {
                    _logger.LogWarning("Failed to adjust token privileges for {Privilege}", privilegeName);
                    return false;
                }
                
                _logger.LogInformation("Enabled privilege: {Privilege}", privilegeName);
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
            _logger.LogError(ex, "Error enabling privilege {Privilege}", privilegeName);
            return false;
        }
    }
}
