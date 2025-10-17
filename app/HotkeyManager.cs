using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PuttyStarter;

[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008
}

public sealed class HotkeyManager : NativeWindow, IDisposable
{
    public event EventHandler? HotkeyPressed;

    private readonly Logger _logger;
    private bool _registered;
    private const int WM_HOTKEY = 0x0312;

    public HotkeyManager(Logger logger)
    {
        _logger = logger;
        CreateHandle(new CreateParams());
    }

    public bool TryRegisterGlobalHotkey(string hotkeyText)
    {
        ParseHotkey(hotkeyText, out var mods, out var vk);
        _registered = RegisterHotKey(Handle, 1, (uint)mods, (uint)vk);
        return _registered;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            return;
        }
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (_registered)
        {
            UnregisterHotKey(Handle, 1);
            _registered = false;
        }
        DestroyHandle();
    }

    private static void ParseHotkey(string text, out HotkeyModifiers mods, out Keys key)
    {
        mods = HotkeyModifiers.None;
        key = Keys.P; // default fallback

        var parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var p in parts)
        {
            switch (p.ToLowerInvariant())
            {
                case "ctrl":
                case "control": mods |= HotkeyModifiers.Control; break;
                case "alt": mods |= HotkeyModifiers.Alt; break;
                case "shift": mods |= HotkeyModifiers.Shift; break;
                case "win":
                case "windows": mods |= HotkeyModifiers.Win; break;
                default:
                    if (Enum.TryParse<Keys>(p, ignoreCase: true, out var k))
                        key = k;
                    break;
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
