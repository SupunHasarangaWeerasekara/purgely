using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Purgely.Core.Uninstall;

/// <summary>
/// Runs a command line and waits until it AND every process it started have exited.
/// Many uninstallers (NSIS, Inno Setup) copy themselves to %TEMP%, start the copy and exit
/// immediately, so waiting for the first process alone reports "done" far too early.
/// The process is created suspended, put into a Job Object (children inherit the job),
/// then resumed; we wait until the job has no active processes.
/// </summary>
public static class JobProcessRunner
{
    private const int ErrorBadExeFormat = 193;

    /// <returns>Exit code of the first process, or null if it could not be read.</returns>
    public static async Task<int?> RunAsync(string commandLine, CancellationToken ct = default)
    {
        try
        {
            return await RunInJobAsync(commandLine, ct);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorBadExeFormat)
        {
            // Not a PE file (e.g. a .bat/.cmd uninstaller): let cmd.exe interpret it.
            var cmd = Path.Combine(Environment.SystemDirectory, "cmd.exe");
            return await RunInJobAsync($"\"{cmd}\" /d /s /c \"{commandLine}\"", ct);
        }
    }

    private static async Task<int?> RunInJobAsync(string commandLine, CancellationToken ct)
    {
        var job = Native.CreateJobObjectW(IntPtr.Zero, null);
        if (job == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());

        try
        {
            var si = new Native.STARTUPINFO { cb = Marshal.SizeOf<Native.STARTUPINFO>() };
            // lpCommandLine must be a writable buffer.
            var cmdBuffer = new StringBuilder(commandLine, commandLine.Length + 1);
            if (!Native.CreateProcessW(null, cmdBuffer, IntPtr.Zero, IntPtr.Zero, false,
                    Native.CREATE_SUSPENDED, IntPtr.Zero, null, ref si, out var pi))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            try
            {
                var inJob = Native.AssignProcessToJobObject(job, pi.hProcess);
                Native.ResumeThread(pi.hThread);

                while (inJob ? ActiveProcesses(job) > 0 : !HasExited(pi.hProcess))
                {
                    await Task.Delay(400, ct);
                }

                return Native.GetExitCodeProcess(pi.hProcess, out var code) ? unchecked((int)code) : null;
            }
            finally
            {
                Native.CloseHandle(pi.hThread);
                Native.CloseHandle(pi.hProcess);
            }
        }
        finally
        {
            // No KILL_ON_JOB_CLOSE: if the user stops waiting, the uninstaller keeps running.
            Native.CloseHandle(job);
        }
    }

    private static uint ActiveProcesses(IntPtr job)
    {
        if (!Native.QueryInformationJobObject(job, Native.JobObjectBasicAccountingInformation,
                out var info, (uint)Marshal.SizeOf<Native.JOBOBJECT_BASIC_ACCOUNTING_INFORMATION>(), IntPtr.Zero))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return info.ActiveProcesses;
    }

    private static bool HasExited(IntPtr process) => Native.WaitForSingleObject(process, 0) == 0;

    private static class Native
    {
        public const uint CREATE_SUSPENDED = 0x00000004;
        public const int JobObjectBasicAccountingInformation = 1;

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO
        {
            public int cb;
            public IntPtr lpReserved, lpDesktop, lpTitle;
            public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
            public short wShowWindow, cbReserved2;
            public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess, hThread;
            public int dwProcessId, dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_BASIC_ACCOUNTING_INFORMATION
        {
            public long TotalUserTime, TotalKernelTime, ThisPeriodTotalUserTime, ThisPeriodTotalKernelTime;
            public uint TotalPageFaultCount, TotalProcesses, ActiveProcesses, TotalTerminatedProcesses;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CreateProcessW(string? lpApplicationName, StringBuilder lpCommandLine,
            IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags,
            IntPtr lpEnvironment, string? lpCurrentDirectory, ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateJobObjectW(IntPtr lpJobAttributes, string? lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool QueryInformationJobObject(IntPtr hJob, int infoClass,
            out JOBOBJECT_BASIC_ACCOUNTING_INFORMATION info, uint infoLength, IntPtr returnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetExitCodeProcess(IntPtr hProcess, out uint exitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);
    }
}
