using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

internal static class Program
{
    private const string ClientSubKey = @"SOFTWARE\Timi Personal Computing\Update\Clients\XiaoaiAgent";
    private const string OriginalInstallPathValueName = "RemapperOriginalInstallPath";
    private const string TargetExeName = "XiaoaiAgent.exe";

    private static int Main(string[] args)
    {
        try
        {
            if (HasScreenCaptureArgument(args))
            {
                LaunchScreenClip();
                Log("Launched explorer.exe ms-screenclip:");
                return 0;
            }

            string originalPath = ReadOriginalPath();
            if (string.IsNullOrEmpty(originalPath) || !File.Exists(originalPath))
            {
                throw new FileNotFoundException("Configured XiaoaiAgent path was not found.", originalPath);
            }

            string forwardedArguments = BuildForwardedArguments(args, originalPath);
            Process.Start(new ProcessStartInfo
            {
                FileName = originalPath,
                Arguments = forwardedArguments,
                WorkingDirectory = Path.GetDirectoryName(originalPath),
                UseShellExecute = false
            });

            Log("Launched original XiaoaiAgent: " + originalPath + " " + forwardedArguments);
            return 0;
        }
        catch (Exception ex)
        {
            Log(ex.ToString());
            return 2;
        }
    }

    private static bool HasScreenCaptureArgument(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--sc", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void LaunchScreenClip()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = "ms-screenclip:",
            UseShellExecute = true
        });
    }

    private static string ReadOriginalPath()
    {
        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(ClientSubKey, false))
        {
            if (key == null)
            {
                return string.Empty;
            }

            object value = key.GetValue(OriginalInstallPathValueName);
            if (value == null)
            {
                return string.Empty;
            }

            string installPath = value.ToString();
            if (string.IsNullOrEmpty(installPath))
            {
                return string.Empty;
            }

            return Path.Combine(installPath, TargetExeName);
        }
    }

    private static string BuildForwardedArguments(string[] args, string backupPath)
    {
        int startIndex = 0;
        if (args.Length > 0)
        {
            string firstArgument = args[0].Trim().Trim('"');
            if (firstArgument.EndsWith("XiaoaiAgent.exe", StringComparison.OrdinalIgnoreCase)
                || string.Equals(firstArgument, backupPath, StringComparison.OrdinalIgnoreCase))
            {
                startIndex = 1;
            }
        }

        return JoinArguments(args, startIndex);
    }

    private static string JoinArguments(string[] args, int startIndex)
    {
        if (args == null || startIndex >= args.Length)
        {
            return string.Empty;
        }

        string[] escaped = new string[args.Length - startIndex];
        for (int i = startIndex; i < args.Length; i++)
        {
            escaped[i - startIndex] = QuoteArgument(args[i]);
        }

        return string.Join(" ", escaped);
    }

    private static string QuoteArgument(string argument)
    {
        if (argument == null)
        {
            return "\"\"";
        }

        if (argument.Length == 0)
        {
            return "\"\"";
        }

        if (argument.IndexOfAny(new[] { ' ', '\t', '"' }) < 0)
        {
            return argument;
        }

        return "\"" + argument.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    private static void Log(string message)
    {
        try
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PressureToSnip.log");
            File.AppendAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + message + Environment.NewLine);
        }
        catch
        {
        }
    }
}
