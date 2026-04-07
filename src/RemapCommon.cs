using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using Microsoft.Win32;

internal static class RemapCommon
{
    private const string TargetExeName = "XiaoaiAgent.exe";
    private const string IfeoSubKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\XiaoaiAgent.exe";

    internal static int Install()
    {
        try
        {
            if (!IsAdministrator())
            {
                return RelaunchElevated("InstallXiaoaiRemap.exe");
            }

            string launcherPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PressureToSnip.exe");
            if (!File.Exists(launcherPath))
            {
                throw new FileNotFoundException("PressureToSnip.exe not found next to InstallXiaoaiRemap.exe.", launcherPath);
            }

            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(IfeoSubKey))
            {
                if (key == null)
                {
                    throw new Win32Exception("Failed to create the IFEO registry key.");
                }

                key.SetValue("Debugger", launcherPath, RegistryValueKind.String);
            }

            StopXiaoaiAgentIfRunning();
            Console.WriteLine("Installed IFEO override for " + TargetExeName);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }

    internal static int Restore()
    {
        try
        {
            if (!IsAdministrator())
            {
                return RelaunchElevated("RestoreXiaoaiRemap.exe");
            }

            Registry.LocalMachine.DeleteSubKeyTree(IfeoSubKey, false);
            StopXiaoaiAgentIfRunning();
            Console.WriteLine("Removed IFEO override for " + TargetExeName);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }

    private static void StopXiaoaiAgentIfRunning()
    {
        Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(TargetExeName));
        for (int i = 0; i < processes.Length; i++)
        {
            try
            {
                processes[i].Kill();
            }
            catch
            {
            }
            finally
            {
                processes[i].Dispose();
            }
        }
    }

    private static bool IsAdministrator()
    {
        WindowsIdentity identity = WindowsIdentity.GetCurrent();
        if (identity == null)
        {
            return false;
        }

        WindowsPrincipal principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static int RelaunchElevated(string exeName)
    {
        string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, exeName);
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException(exeName + " not found next to the current executable.", exePath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = true,
            Verb = "runas"
        };

        Process process = Process.Start(startInfo);
        if (process == null)
        {
            throw new Win32Exception("Failed to relaunch the tool as administrator.");
        }

        process.WaitForExit();
        return process.ExitCode;
    }
}
