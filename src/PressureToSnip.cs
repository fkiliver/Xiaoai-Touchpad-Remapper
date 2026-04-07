using System;
using System.Diagnostics;
using System.IO;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "ms-screenclip:",
                UseShellExecute = true
            });

            Log("Launched explorer.exe ms-screenclip:");
            return 0;
        }
        catch (Exception ex)
        {
            Log(ex.ToString());
            return 2;
        }
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
