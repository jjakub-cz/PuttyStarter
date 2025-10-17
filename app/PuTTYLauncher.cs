using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PuttyStarter;

public static class PuTTYLauncher
{
    public static void Launch(string sessionSpec, AppConfig cfg, Logger logger)
    {
        // sessionSpec: "user@host[:port]"
        string userHost = sessionSpec;
        int? port = null;

        var colon = sessionSpec.LastIndexOf(':');
        var at = sessionSpec.LastIndexOf('@');
        if (colon > at && colon > 0)
        {
            if (int.TryParse(sessionSpec[(colon + 1)..], out var p))
            {
                port = p;
                userHost = sessionSpec[..colon];
            }
        }

        var puttyExe = string.IsNullOrWhiteSpace(cfg.PuttyPath) ? "putty.exe" : cfg.PuttyPath;

        var psi = new ProcessStartInfo
        {
            FileName = puttyExe,
            UseShellExecute = false,
            CreateNoWindow = false
        };

        var args = new StringBuilder();
        args.Append("-ssh ");
        args.Append('"').Append(userHost).Append('"');
        if (port is int pp)
        {
            args.Append(' ').Append("-P ").Append(pp);
        }
        psi.Arguments = args.ToString();

        logger.Info($"Start: {psi.FileName} {psi.Arguments}");

        var proc = Process.Start(psi) ?? throw new InvalidOperationException("Cannot start PuTTY process.");

        // Počkej na hlavní okno
        IntPtr hWnd = IntPtr.Zero;
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < cfg.PuttyStartTimeoutMs)
        {
            proc.Refresh();
            if (proc.MainWindowHandle != IntPtr.Zero)
            {
                hWnd = proc.MainWindowHandle;
                break;
            }
            Thread.Sleep(50);
        }

        if (hWnd == IntPtr.Zero)
        {
            logger.Warn("PuTTY window not found in time.");
            return;
        }

        // 🎯 Zjistíme monitor, kde je kurzor
        var screen = Screen.FromPoint(Cursor.Position);
        var wa = screen.WorkingArea;

        // Přesuneme okno doprostřed toho monitoru a maximalizujeme
        SetWindowPos(hWnd, IntPtr.Zero,
            wa.Left + (wa.Width / 2) - 400, // odhadněme 800×600, jen startovní pozice
            wa.Top + (wa.Height / 2) - 300,
            800, 600,
            SWP_NOZORDER | SWP_SHOWWINDOW);

        ShowWindow(hWnd, SW_MAXIMIZE);
        SetForegroundWindow(hWnd);

    }

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private const int SW_MAXIMIZE = 3;

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_SHOWWINDOW = 0x0040;
}
