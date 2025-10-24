using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

namespace PuttyStarter;

public class AppContextEx : ApplicationContext
{
    private AppConfig _config;
    private readonly string _confPath;
    private readonly Logger _logger;

    private readonly NotifyIcon _tray;
    private readonly HotkeyManager _hotkey;
    private PickerForm? _picker;

    private readonly ToolStripMenuItem _miRunAtStartup;

    public AppContextEx(AppConfig config, string confPath, Logger logger)
    {
        _config = config;
        _confPath = confPath;
        _logger = logger;

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        _tray = new NotifyIcon
        {
            Text = $"PuttyStarter {_config.Version}",
            Visible = true,
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        };

        // ---- Context menu (ContextMenuStrip + ToolStripMenuItem) ----
        var cms = new ContextMenuStrip();

        var miOpen = new ToolStripMenuItem("Open");
        miOpen.Click += (s, e) => ShowPicker();

        var miRefresh = new ToolStripMenuItem("Reload config");
        miRefresh.Click += (s, e) => RefreshConfig();

        var miOpenLocation = new ToolStripMenuItem("Open location");
        miOpenLocation.Click += (s, e) => OpenLocation();

        _miRunAtStartup = new ToolStripMenuItem("Run at startup")
        {
            Checked = RunAtStartup.Get(),
            CheckOnClick = false
        };
        _miRunAtStartup.Click += (s, e) =>
        {
            var newVal = !_miRunAtStartup.Checked;
            if (RunAtStartup.Set(newVal))
            {
                _miRunAtStartup.Checked = newVal;
                _logger.Info($"RunAtStartup set to {newVal}");
            }
            else
            {
                MessageBox.Show("Unable to update Run at startup.", "PuttyStarter",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        var miAbout = new ToolStripMenuItem("About...");
        miAbout.Click += (s, e) => ShowAbout();

        var miExit = new ToolStripMenuItem("Exit");
        miExit.Click += (s, e) => ExitThread();

        cms.Items.Add(miOpen);
        cms.Items.Add(miRefresh);
        cms.Items.Add(miOpenLocation);
        cms.Items.Add(_miRunAtStartup);
        cms.Items.Add(miAbout);
        cms.Items.Add(new ToolStripSeparator());
        cms.Items.Add(miExit);

        _tray.ContextMenuStrip = cms;
        _tray.DoubleClick += (s, e) => ShowPicker();
        // -------------------------------------------------------------

        // Hotkey
        _hotkey = new HotkeyManager(_logger);
        if (!_hotkey.TryRegisterGlobalHotkey(_config.Hotkey))
        {
            if (_config.ShowNotifications)
                _tray.ShowBalloonTip(3000, "PuttyStarter", "Hotkey is already in use.", ToolTipIcon.Warning);
            _logger.Warn("Hotkey registration failed.");
        }
        _hotkey.HotkeyPressed += (_, __) => ShowPicker();

        _logger.Info("App started.");
    }

    // ==== Helper methods that were missing ====
    private void RefreshConfig()
    {
        try
        {
            var oldHotkey = _config.Hotkey;
            var oldVersion = _config.Version;

            var newCfg = AppConfig.LoadOrCreate(_confPath, _logger);
            _config = newCfg;

            if (!string.Equals(oldHotkey, _config.Hotkey, StringComparison.OrdinalIgnoreCase))
            {
                if (!_hotkey.Rebind(_config.Hotkey))
                {
                    if (_config.ShowNotifications)
                        _tray.ShowBalloonTip(3000, "PuttyStarter", "Hotkey is already in use.", ToolTipIcon.Warning);
                    _logger.Warn("Hotkey rebind failed.");
                }
                else
                {
                    _logger.Info($"Hotkey rebind to '{_config.Hotkey}' succeeded.");
                }
            }

            if (!string.Equals(oldVersion, _config.Version, StringComparison.Ordinal))
                _tray.Text = $"PuttyStarter {_config.Version}";

            if (_picker is { IsDisposed: false })
            {
                try { _picker.Close(); } catch { /* ignore */ }
                _picker = null;
            }

            if (_config.ShowNotifications)
                _tray.ShowBalloonTip(2000, "PuttyStarter", "Configuration reloaded.", ToolTipIcon.Info);

            _logger.Info("Configuration reloaded.");
        }
        catch (Exception ex)
        {
            _logger.Error("Refresh failed: " + ex.Message);
            MessageBox.Show("Failed to reload configuration.\n\n" + ex.Message, "PuttyStarter",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenLocation()
    {
        try
        {
            var exeDir = System.IO.Path.GetDirectoryName(Application.ExecutablePath)!;
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{exeDir}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _logger.Warn("OpenLocation failed: " + ex.Message);
            MessageBox.Show("Unable to open location.\n\n" + ex.Message, "PuttyStarter",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowPicker()
    {
        if (_picker is { IsDisposed: false })
        {
            try
            {
                _picker.BringToFront();
                _picker.Activate();
                _picker.Focus();
                return;
            }
            catch { /* ignore */ }
        }

        _picker = new PickerForm(_config, _logger);
        _picker.FormClosed += (_, __) => _picker = null;
        _picker.ShowOnCurrentMonitor(_config.PickerMonitor);
    }
    private void ShowAbout()
    {
        using var dlg = new Form
        {
            Text = "About PuttyStarter",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            ClientSize = new Size(480, 340),
            MaximizeBox = false,
            MinimizeBox = false,
            TopMost = true,
            ShowInTaskbar = false
        };

        ApplyTheme(dlg);

        var theme = _config.ResolveTheme();
        var fg = theme == AppTheme.Dark ? Color.WhiteSmoke : SystemColors.WindowText;
        var bg = theme == AppTheme.Dark ? Color.FromArgb(32, 32, 32) : SystemColors.Window;
        var linkColor = theme == AppTheme.Dark ? Color.LightSkyBlue : Color.RoyalBlue;
        var linkActive = theme == AppTheme.Dark ? Color.DeepSkyBlue : Color.MediumBlue;
        var linkVisited = theme == AppTheme.Dark ? Color.SkyBlue : Color.Purple;

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = bg,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 1,
            AutoSize = false
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        Label L(string text, Font? font = null)
        {
            return new Label
            {
                AutoSize = true,
                Text = text,
                ForeColor = fg,
                BackColor = bg,
                Margin = new Padding(0, 0, 0, 6),
                Font = font ?? dlg.Font
            };
        }

        // Nadpis
        panel.Controls.Add(L($"PuttyStarter {_config.Version}", new Font(dlg.Font, FontStyle.Bold)));

        // Popis
        panel.Controls.Add(L($"Lightweight launcher for quick opening of PuTTY SSH sessions via a global hotkey ({_config.Hotkey})."));
        panel.Controls.Add(L("")); // pr�zdn� ��dek

        // Author
        panel.Controls.Add(L("Author: jjakub-cz"));

        // GitHub � SAMOSTATN� LinkLabel
        var url = "https://github.com/jjakub-cz/PuttyStarter";
        var llGit = new LinkLabel
        {
            AutoSize = true,
            Text = "GitHub: " + url,
            LinkColor = linkColor,
            ActiveLinkColor = linkActive,
            VisitedLinkColor = linkVisited,
            BackColor = bg,
            ForeColor = fg,
            Margin = new Padding(0, 0, 0, 0)
        };
        // ozna��me jen tu URL ��st jako link (p�esn� rozsah)
        int start = llGit.Text.IndexOf(url, StringComparison.Ordinal);
        if (start >= 0) llGit.Links.Add(start, url.Length, url);
        llGit.LinkClicked += (_, e) =>
        {
            try
            {
                var target = e.Link.LinkData?.ToString();
                if (!string.IsNullOrWhiteSpace(target))
                    Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open link.\n\n" + ex.Message,
                    "PuttyStarter", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        panel.Controls.Add(llGit);

        // License � klidn� taky link zvl᚝ (na LICENSE v repu)
        panel.Controls.Add(L("License: MIT"));

        // Build info
        panel.Controls.Add(L("Build: .NET 8, win-x64, self-contained"));

        // OK button
        var btn = new Button
        {
            Text = "OK",
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            DialogResult = DialogResult.OK,
            Width = 90,
            Height= 36
        };
        // spodn� panel pro button, a� hezky sed� vpravo
        var bottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            BackColor = bg
        };
        btn.Left = bottom.Width - btn.Width - 16;
        btn.Top = (bottom.Height - btn.Height) / 2;
        btn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        bottom.Controls.Add(btn);

        dlg.Controls.Add(panel);
        dlg.Controls.Add(bottom);
        dlg.AcceptButton = btn;
        dlg.CancelButton = btn;

        dlg.ShowDialog();
    }



    private void ApplyTheme(Form f)
    {
        var theme = _config.ResolveTheme();
        if (theme == AppTheme.Dark)
        {
            f.BackColor = Color.FromArgb(32, 32, 32);
            f.ForeColor = Color.WhiteSmoke;
        }
        else
        {
            f.BackColor = SystemColors.Window;
            f.ForeColor = SystemColors.WindowText;
        }
    }

    protected override void ExitThreadCore()
    {
        try
        {
            _hotkey.Dispose();
            _tray.Visible = false;
            _tray.Dispose();
        }
        catch { /* ignore */ }

        base.ExitThreadCore();
        _logger.Info("App exited.");
    }
}
