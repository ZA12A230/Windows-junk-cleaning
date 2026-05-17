using System.Runtime.InteropServices;

namespace DiskCleaner.Helpers;

internal static class NativeMethods
{
    [DllImport("ntdll.dll")]
    public static extern int NtQuerySystemInformation(
        int SystemInformationClass,
        nint SystemInformation,
        int SystemInformationLength,
        out int ReturnLength);
    
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MoveFileEx(
        string lpExistingFileName,
        string? lpNewFileName,
        uint dwFlags);
    
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteFile(string lpFileName);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(nint hObject);
    
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool OpenProcessToken(
        nint ProcessHandle,
        uint DesiredAccess,
        out nint TokenHandle);
    
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool LookupPrivilegeValue(
        string? lpSystemName,
        string lpName,
        out LUID lpLuid);
    
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AdjustTokenPrivileges(
        nint TokenHandle,
        [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,
        ref TOKEN_PRIVILEGES NewState,
        int BufferLength,
        nint PreviousState,
        nint ReturnLength);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint GetCurrentProcess();
    
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern nint CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        nint lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        nint hTemplateFile);
    
    [DllImport("ntdll.dll")]
    public static extern int NtClose(nint Handle);
    
    public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    public const uint TOKEN_QUERY = 0x0008;
    public const uint SE_PRIVILEGE_ENABLED = 0x00000002;
    public const uint MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004;
    
    public const uint GENERIC_READ = 0x80000000;
    public const uint FILE_SHARE_READ = 0x00000001;
    public const uint FILE_SHARE_WRITE = 0x00000002;
    public const uint FILE_SHARE_DELETE = 0x00000004;
    public const uint OPEN_EXISTING = 3;
    
    public const int SystemHandleInformation = 16;
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct LUID
    {
        public int LowPart;
        public int HighPart;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct LUID_AND_ATTRIBUTES
    {
        public LUID Luid;
        public uint Attributes;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TOKEN_PRIVILEGES
    {
        public int PrivilegeCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public LUID_AND_ATTRIBUTES[] Privileges;
    }
}
