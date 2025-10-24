using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace PuttyStarter;

public enum AppTheme { Auto, Light, Dark }

public sealed class AppConfig
{
    public string Version { get; set; } = "1.1.0";
    public string Hotkey { get; set; } = "Ctrl+Alt+P";
    public AppTheme Theme { get; set; } = AppTheme.Auto;
    public bool SingleInstance { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;

    public string PuttyPath { get; set; } = "";
    public string FullscreenMode { get; set; } = "maximize";
    public int PuttyStartTimeoutMs { get; set; } = 4000;

    public bool PickerTopmost { get; set; } = true;
    public string PickerMonitor { get; set; } = "cursor"; // "cursor" | "primary"
    public string DpiAwareness { get; set; } = "per_monitor_v2";
    public bool RememberLastSelection { get; set; } = true;

    public bool RunAtStartup { get; set; } = false;

    public bool LogEnabled { get; set; } = true;
    public string LogLevel { get; set; } = "info";
    public int LogMaxFiles { get; set; } = 3;
    public int LogMaxSizeKb { get; set; } = 256;

    public Dictionary<string, string> Sessions { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static AppConfig LoadOrCreate(string path, Logger logger)
    {
        if (!File.Exists(path))
        {
            var cfg = new AppConfig();
            File.WriteAllText(path, ConfigParser.GenerateDefaultTemplate(cfg));
            logger.Info("Created default config.");
            return cfg;
        }

        try
        {
            var text = File.ReadAllText(path);
            return ConfigParser.Parse(text);
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to parse config: {ex.Message}. Using defaults.");
            return new AppConfig();
        }
    }

    public AppTheme ResolveTheme()
    {
        if (Theme == AppTheme.Auto)
        {
            try
            {
                // Windows setting: 1 = light, 0 = dark
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var val = key?.GetValue("AppsUseLightTheme");
                if (val is int i) return i == 1 ? AppTheme.Light : AppTheme.Dark;
            }
            catch { /* ignore */ }
            return AppTheme.Light;
        }
        return Theme;
    }
}
