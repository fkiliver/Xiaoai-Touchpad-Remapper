using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using Microsoft.Win32;

internal static class ClientPathRemapCommon
{
    private const string ClientSubKey = @"SOFTWARE\Timi Personal Computing\Update\Clients\XiaoaiAgent";
    private const string InstallPathValueName = "InstallPath";
    private const string OriginalInstallPathValueName = "RemapperOriginalInstallPath";
    private const string ProxyFileName = "XiaoaiAgent.exe";
    private const string HelperFileName = "PressureToSnip.exe";

    internal static int Install()
    {
        try
        {
            if (!IsAdministrator())
            {
                return RelaunchElevatedSelf();
            }

            string helperPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, HelperFileName);
            if (!File.Exists(helperPath))
            {
                throw new FileNotFoundException("PressureToSnip.exe not found next to the installer.", helperPath);
            }

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(ClientSubKey, true))
            {
                if (key == null)
                {
                    throw new Win32Exception("Failed to open Xiaomi update client registry key.");
                }

                string installPath = GetStringValue(key, InstallPathValueName);
                if (string.IsNullOrEmpty(installPath) || !Directory.Exists(installPath))
                {
                    throw new DirectoryNotFoundException("Current XiaoaiAgent install path is invalid.");
                }

                string proxyDirectory = GetProxyDirectory();
                Directory.CreateDirectory(proxyDirectory);
                File.Copy(helperPath, Path.Combine(proxyDirectory, ProxyFileName), true);

                key.SetValue(OriginalInstallPathValueName, installPath, RegistryValueKind.String);
                key.SetValue(InstallPathValueName, proxyDirectory, RegistryValueKind.String);
            }

            Console.WriteLine("Installed Xiaomi client-path override for touchpad heavy press.");
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
                return RelaunchElevatedSelf();
            }

            string proxyDirectory = GetProxyDirectory();

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(ClientSubKey, true))
            {
                if (key == null)
                {
                    throw new Win32Exception("Failed to open Xiaomi update client registry key.");
                }

                string originalInstallPath = GetStringValue(key, OriginalInstallPathValueName);
                if (!string.IsNullOrEmpty(originalInstallPath))
                {
                    key.SetValue(InstallPathValueName, originalInstallPath, RegistryValueKind.String);
                    key.DeleteValue(OriginalInstallPathValueName, false);
                }
            }

            if (Directory.Exists(proxyDirectory))
            {
                Directory.Delete(proxyDirectory, true);
            }

            Console.WriteLine("Removed Xiaomi client-path override for touchpad heavy press.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }

    private static string GetProxyDirectory()
    {
        string commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        return Path.Combine(commonAppData, "MI", "XiaomiTouchpadRemapper");
    }

    private static string GetStringValue(RegistryKey key, string valueName)
    {
        object value = key.GetValue(valueName);
        return value == null ? string.Empty : value.ToString();
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

    private static int RelaunchElevatedSelf()
    {
        Process currentProcess = Process.GetCurrentProcess();
        string exePath = currentProcess.MainModule.FileName;
        currentProcess.Dispose();

        if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
        {
            throw new FileNotFoundException("Current executable path could not be resolved.", exePath);
        }

        Process process = Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = true,
            Verb = "runas"
        });

        if (process == null)
        {
            throw new Win32Exception("Failed to relaunch the tool as administrator.");
        }

        process.WaitForExit();
        return process.ExitCode;
    }
}
