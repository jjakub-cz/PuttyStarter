using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PuttyStarter;

public static class ConfigParser
{
    public static AppConfig Parse(string text)
    {
        var cfg = new AppConfig();
        var lines = text.Replace("\r", "").Split('\n');
        var inSessions = false;

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                inSessions = string.Equals(line, "[sessions]", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            var eq = line.IndexOf('=');
            if (eq < 0) continue;

            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();

            // strip quotes if any
            string Unquote(string s)
            {
                if (s.Length >= 2 && ((s.StartsWith("\"") && s.EndsWith("\"")) || (s.StartsWith("'") && s.EndsWith("'"))))
                    return s[1..^1];
                return s;
            }

            if (inSessions)
            {
                var sid = key;
                var sval = Unquote(value);
                if (!cfg.Sessions.ContainsKey(sid))
                    cfg.Sessions[sid] = sval;
                continue;
            }

            // top-level keys
            switch (key)
            {
                case "hotkey": cfg.Hotkey = Unquote(value); break;
                case "theme":
                    cfg.Theme = Unquote(value).ToLowerInvariant() switch
                    {
                        "dark" => AppTheme.Dark,
                        "light" => AppTheme.Light,
                        _ => AppTheme.Auto
                    };
                    break;
                case "single_instance": cfg.SingleInstance = ParseBool(value); break;
                case "show_notifications": cfg.ShowNotifications = ParseBool(value); break;

                case "putty_path": cfg.PuttyPath = Unquote(value); break;
                case "fullscreen_mode": cfg.FullscreenMode = Unquote(value); break;
                case "putty_start_timeout_ms": cfg.PuttyStartTimeoutMs = ParseInt(value, 4000); break;

                case "picker_topmost": cfg.PickerTopmost = ParseBool(value); break;
                case "picker_monitor": cfg.PickerMonitor = Unquote(value); break;
                case "dpi_awareness": cfg.DpiAwareness = Unquote(value); break;
                case "remember_last_selection": cfg.RememberLastSelection = ParseBool(value); break;

                case "run_at_startup": cfg.RunAtStartup = ParseBool(value); break;

                case "log_enabled": cfg.LogEnabled = ParseBool(value); break;
                case "log_level": cfg.LogLevel = Unquote(value); break;
                case "log_max_files": cfg.LogMaxFiles = ParseInt(value, 3); break;
                case "log_max_size_kb": cfg.LogMaxSizeKb = ParseInt(value, 256); break;
            }
        }

        return cfg;
    }

    private static bool ParseBool(string v)
        => v.Trim().Trim('"', '\'').Equals("true", StringComparison.OrdinalIgnoreCase);

    private static int ParseInt(string v, int def)
        => int.TryParse(v.Trim().Trim('"', '\''), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : def;

    public static string GenerateDefaultTemplate(AppConfig c)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# PuttyStarter.conf — configuration");
        sb.AppendLine();
        sb.AppendLine("# — Application —");
        sb.AppendLine($"hotkey = \"{c.Hotkey}\"");
        sb.AppendLine($"theme = \"auto\"  # auto | light | dark");
        sb.AppendLine($"single_instance = true");
        sb.AppendLine($"show_notifications = true");
        sb.AppendLine();
        sb.AppendLine("# — PuTTY —");
        sb.AppendLine($"putty_path = \"C:\\Program Files\\PuTTY\\putty.exe\"");
        sb.AppendLine($"fullscreen_mode = \"maximize\"");
        sb.AppendLine($"putty_start_timeout_ms = 4000");
        sb.AppendLine();
        sb.AppendLine("# — Picker window —");
        sb.AppendLine($"picker_topmost = true");
        sb.AppendLine($"picker_monitor = \"cursor\"  # cursor | primary");
        sb.AppendLine($"dpi_awareness = \"per_monitor_v2\"");
        sb.AppendLine($"remember_last_selection = true");
        sb.AppendLine();
        sb.AppendLine("# — Autostart —");
        sb.AppendLine($"run_at_startup = false");
        sb.AppendLine();
        sb.AppendLine("# — Logging —");
        sb.AppendLine($"log_enabled = true");
        sb.AppendLine($"log_level = \"info\"");
        sb.AppendLine($"log_max_files = 3");
        sb.AppendLine($"log_max_size_kb = 256");
        sb.AppendLine();
        sb.AppendLine("# — Sessions —");
        sb.AppendLine("[sessions]");
        sb.AppendLine("example = \"admin@10.0.0.15\"");
        return sb.ToString();
    }
}
