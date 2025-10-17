using Microsoft.Win32;
using System;

namespace PuttyStarter;

public static class RunAtStartup
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "PuttyStarter";

    public static bool Get()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            var val = key?.GetValue(AppName) as string;
            return !string.IsNullOrEmpty(val);
        }
        catch { return false; }
    }

    public static bool Set(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key == null) return false;

            if (enable)
                key.SetValue(AppName, $"\"{System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName}\"");
            else
                key.DeleteValue(AppName, throwOnMissingValue: false);

            return true;
        }
        catch { return false; }
    }
}
