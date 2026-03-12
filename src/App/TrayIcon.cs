using System.Drawing;
using System.Windows.Forms;

namespace ScreenshotPlugin.App;

public class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private HotkeyWindow? _hotkeyWindow;

    public event Action? OnCaptureClicked;
    public event Action? OnOpenTuiClicked;
    public event Action? OnExitClicked;
    public event Action? OnSettingsClicked;

    public TrayIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("截图", null, (_, _) => OnCaptureClicked?.Invoke());
        menu.Items.Add("打开主面板", null, (_, _) => OnOpenTuiClicked?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("设置", null, (_, _) => OnSettingsClicked?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => OnExitClicked?.Invoke());

        _notifyIcon = new NotifyIcon
        {
            Text = "SnapForge",
            Icon = CreateDefaultIcon(),
            ContextMenuStrip = menu,
            Visible = true
        };

        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                OnOpenTuiClicked?.Invoke();
        };
    }

    public bool RegisterGlobalHotkey(AppConfig config)
    {
        _hotkeyWindow?.Dispose();
        _hotkeyWindow = new HotkeyWindow();
        _hotkeyWindow.HotkeyPressed += () => OnCaptureClicked?.Invoke();

        var (modifiers, vk) = config.ParseHotkey();
        return _hotkeyWindow.RegisterHotkey(modifiers, vk);
    }

    private static Icon CreateDefaultIcon()
    {
        var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(30, 30, 30));
        g.FillRectangle(Brushes.DodgerBlue, 4, 8, 24, 18);
        g.FillRectangle(Brushes.DodgerBlue, 10, 4, 12, 6);
        g.FillEllipse(Brushes.White, 10, 11, 12, 12);
        g.FillEllipse(Brushes.DodgerBlue, 13, 14, 6, 6);
        return Icon.FromHandle(bmp.GetHicon());
    }

    public void ShowBalloon(string title, string text, int timeout = 2000)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = text;
        _notifyIcon.ShowBalloonTip(timeout);
    }

    public void Dispose()
    {
        _hotkeyWindow?.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
