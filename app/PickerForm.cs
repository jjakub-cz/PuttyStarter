using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PuttyStarter;

public class PickerForm : Form
{
    private readonly AppConfig _config;
    private readonly Logger _logger;
    private readonly ListBox _list;
    private readonly Dictionary<string, string> _sessions;

    // Paleta pro aktuální theme
    private Color _bg, _fg, _selBg, _selFg, _border;

    private void SetItemMetrics()
    {
        // Změříme výšku textu pro realistický řádek a přidáme padding
        var sample = "Ag"; // stačí krátký vzorek
        var sz = TextRenderer.MeasureText(sample, _list.Font, new Size(int.MaxValue, int.MaxValue),
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        var line = Math.Max(sz.Height, _list.Font.Height);
        _list.ItemHeight = line + 6; // 3px nahoře + 3px dole
        _list.Invalidate();
    }

    private void List_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;

        bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var bg = selected ? _selBg : _bg;
        var fg = selected ? _selFg : _fg;

        // pozadí položky
        using (var b = new SolidBrush(bg))
            e.Graphics.FillRectangle(b, e.Bounds);

        string text = _list.Items[e.Index].ToString()!;
        var textRect = new Rectangle(e.Bounds.X + 8, e.Bounds.Y + 2,
                                     e.Bounds.Width - 16, e.Bounds.Height - 4);

        TextRenderer.DrawText(
            e.Graphics, text, e.Font!, textRect, fg,
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter
        );

        // oddělovací linka jen když nejsme na posledním pixlu mimo bounds
        if (e.Index < _list.Items.Count - 1)
        {
            using var pen = new Pen(_border);
            e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        e.DrawFocusRectangle();
    }

    public PickerForm(AppConfig config, Logger logger)
    {
        _config = config;
        _logger = logger;
        _sessions = config.Sessions;

        Text = "PuttyStarter";
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        StartPosition = FormStartPosition.Manual;
        ClientSize = new Size(460, 340);
        TopMost = _config.PickerTopmost;
        ShowInTaskbar = false;
        KeyPreview = true;

        // ListBox – owner draw pro custom barvy
        _list = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,            // <- důležité
            BorderStyle = BorderStyle.FixedSingle,
            DrawMode = DrawMode.OwnerDrawFixed
        };
        // dynamicky dopočítáme výšku řádku
        SetItemMetrics();

        _list.DrawItem += List_DrawItem;
        _list.SelectionMode = SelectionMode.One;
        _list.MouseDoubleClick += OnListMouseDoubleClick;

        // reaguj na změnu DPI/fontu
        FontChanged += (_, __) => SetItemMetrics();
        DpiChanged += (_, __) => SetItemMetrics();


        Controls.Add(_list);

        // Naplnění položek
        foreach (var kv in _sessions)
            _list.Items.Add($"{kv.Key}  —  {kv.Value}");

        // Až teď aplikujeme theme (už máme _list)
        ApplyTheme();

        KeyDown += PickerForm_KeyDown;

        Shown += (s, e) =>
        {
            if (_list.Items.Count > 0)
                _list.SelectedIndex = 0;
            _list.Focus();
        };
    }
    private void OnListMouseDoubleClick(object? sender, MouseEventArgs e)
    {
        // Zjistíme, na kterou položku bylo kliknuto
        int idx = _list.IndexFromPoint(e.Location);
        if (idx >= 0)
        {
            _list.SelectedIndex = idx; // pro jistotu vyber kliknutou položku
            RunSelected();             // stejná akce jako Enter
        }
    }

    private void ApplyTheme()
    {
        var theme = _config.ResolveTheme();
        if (theme == AppTheme.Dark)
        {
            _bg = Color.FromArgb(24, 24, 24);
            _fg = Color.Gainsboro;
            _selBg = Color.FromArgb(54, 93, 171);   // decentní „highlight“ na tmavém
            _selFg = Color.White;
            _border = Color.FromArgb(64, 64, 64);
        }
        else
        {
            _bg = SystemColors.Window;
            _fg = SystemColors.WindowText;
            _selBg = SystemColors.Highlight;
            _selFg = SystemColors.HighlightText;
            _border = SystemColors.ActiveBorder;
        }

        BackColor = _bg;
        ForeColor = _fg;

        _list.BackColor = _bg;
        _list.ForeColor = _fg;
        _list.Invalidate(); // překreslit
    }


    public void ShowOnCurrentMonitor(string monitorMode)
    {
        var targetScreen = monitorMode.Equals("primary", StringComparison.OrdinalIgnoreCase)
            ? Screen.PrimaryScreen
            : Screen.FromPoint(Cursor.Position);

        var area = targetScreen.WorkingArea;
        Left = area.Left + (area.Width - Width) / 2;
        Top = area.Top + (area.Height - Height) / 2;

        Show();
        Activate();
    }

    private void PickerForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Close();
            return;
        }

        if (e.KeyCode == Keys.Enter)
        {
            RunSelected();
            return;
        }
        // Šipky obslouží ListBox
    }

    private void RunSelected()
    {
        if (_list.SelectedIndex < 0) return;

        var itemText = _list.SelectedItem!.ToString()!;
        // "id  —  username@host[:port]"
        var id = itemText.Split('—')[0].Trim();
        if (!_sessions.TryGetValue(id, out var spec)) return;

        try
        {
            PuTTYLauncher.Launch(spec, _config, _logger);
        }
        catch (Exception ex)
        {
            _logger.Error("Launch error: " + ex.Message);
            MessageBox.Show("Failed to start PuTTY.\n\n" + ex.Message, "PuttyStarter",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        Close();
    }
}
