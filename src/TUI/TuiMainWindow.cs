using ScreenshotPlugin.App;
using System.Collections.ObjectModel;
using TG = Terminal.Gui;

namespace ScreenshotPlugin.TUI;

public enum TuiMode { Panel, Terminal }
public enum TuiAction { None, Capture, Exit, Minimize }

public class TuiMainWindow
{
    private readonly AppConfig _config;
    private readonly ObservableCollection<string> _logItems = new();
    private TuiMode _mode = TuiMode.Panel;
    private bool _switchMode;
    private TuiAction _pendingAction = TuiAction.None;

    // Tokyo Night color palette
    private static readonly TG.Color TnBg        = new(0x1a, 0x1b, 0x26);
    private static readonly TG.Color TnBgDark     = new(0x16, 0x16, 0x1e);
    private static readonly TG.Color TnBgLight    = new(0x24, 0x28, 0x3b);
    private static readonly TG.Color TnFg         = new(0xc0, 0xca, 0xf5);
    private static readonly TG.Color TnComment    = new(0x56, 0x5f, 0x89);
    private static readonly TG.Color TnBlue       = new(0x7a, 0xa2, 0xf7);
    private static readonly TG.Color TnCyan       = new(0x7d, 0xcf, 0xff);
    private static readonly TG.Color TnGreen      = new(0x9e, 0xce, 0x6a);
    private static readonly TG.Color TnMagenta    = new(0xbb, 0x9a, 0xf7);
    private static readonly TG.Color TnRed        = new(0xf7, 0x76, 0x8e);
    private static readonly TG.Color TnYellow     = new(0xe0, 0xaf, 0x68);
    private static readonly TG.Color TnOrange     = new(0xff, 0x9e, 0x64);

    public TuiMainWindow(AppConfig config)
    {
        _config = config;
    }

    public TuiAction Run()
    {
        if (_logItems.Count == 0)
        {
            _logItems.Add("[init] SnapForge started");
            _logItems.Add("[init] Hotkey: " + _config.Hotkey);
            _logItems.Add("[info] Type 'help' for commands");
        }

        do
        {
            _switchMode = false;
            _pendingAction = TuiAction.None;
            if (_mode == TuiMode.Panel) RunPanel();
            else RunTerminal();
        } while (_switchMode);

        return _pendingAction;
    }

    private void ScrollLogToEnd(TG.ListView lv)
    {
        if (_logItems.Count > 0)
        {
            try { lv.SelectedItem = _logItems.Count - 1; }
            catch { /* ignore if layout not ready */ }
        }
    }

    private static TG.ColorScheme MakeScheme() => new()
    {
        Normal = new TG.Attribute(TnFg, TnBg),
        Focus = new TG.Attribute(TnCyan, TnBgLight),
        HotNormal = new TG.Attribute(TnMagenta, TnBg),
        HotFocus = new TG.Attribute(TnMagenta, TnBgLight),
    };

    private static TG.ColorScheme MakeFrameScheme() => new()
    {
        Normal = new TG.Attribute(TnBlue, TnBgDark),
        Focus = new TG.Attribute(TnCyan, TnBgLight),
        HotNormal = new TG.Attribute(TnYellow, TnBgDark),
        HotFocus = new TG.Attribute(TnYellow, TnBgLight),
    };

    private static TG.ColorScheme MakeBannerScheme() => new()
    {
        Normal = new TG.Attribute(TnMagenta, TnBg),
        Focus = new TG.Attribute(TnMagenta, TnBg),
        HotNormal = new TG.Attribute(TnMagenta, TnBg),
        HotFocus = new TG.Attribute(TnMagenta, TnBg),
    };

    private static TG.ColorScheme MakeButtonScheme() => new()
    {
        Normal = new TG.Attribute(TnBg, TnBlue),
        Focus = new TG.Attribute(TnBg, TnCyan),
        HotNormal = new TG.Attribute(TnBgDark, TnMagenta),
        HotFocus = new TG.Attribute(TnBgDark, TnCyan),
    };

    private static TG.ColorScheme MakeLogScheme() => new()
    {
        Normal = new TG.Attribute(TnGreen, TnBgDark),
        Focus = new TG.Attribute(TnCyan, TnBgLight),
        HotNormal = new TG.Attribute(TnGreen, TnBgDark),
        HotFocus = new TG.Attribute(TnCyan, TnBgLight),
    };

    private static TG.ColorScheme MakeInputScheme() => new()
    {
        Normal = new TG.Attribute(TnFg, TnBgLight),
        Focus = new TG.Attribute(TnCyan, TnBgLight),
        HotNormal = new TG.Attribute(TnFg, TnBgLight),
        HotFocus = new TG.Attribute(TnCyan, TnBgLight),
    };

    // ─── Panel Mode (dashboard) ───────────────────────────────
    private void RunPanel()
    {
        TG.Application.Init();
        var top = new TG.Toplevel();
        var win = new TG.Window
        {
            Title = " SnapForge v1.0 [Panel] ",
            X = 0, Y = 0,
            Width = TG.Dim.Fill(), Height = TG.Dim.Fill(),
            ColorScheme = MakeScheme(),
        };

        // Banner
        win.Add(new TG.Label
        {
            Text = BannerCompact, X = 1, Y = 0,
            Width = TG.Dim.Fill(), Height = 4,
            ColorScheme = MakeBannerScheme(),
        });

        // Status
        var sf = new TG.FrameView
        {
            Title = " Status ", X = 0, Y = 4, Width = TG.Dim.Percent(50), Height = 4,
            ColorScheme = MakeFrameScheme(),
        };
        sf.Add(new TG.Label { Text = GetStatusText(), X = 1, Y = 0, Width = TG.Dim.Fill(), Height = TG.Dim.Fill() });
        win.Add(sf);

        // Config
        var cf = new TG.FrameView
        {
            Title = " Config ", X = TG.Pos.Percent(50), Y = 4, Width = TG.Dim.Fill(), Height = 4,
            ColorScheme = MakeFrameScheme(),
        };
        cf.Add(new TG.Label { Text = GetConfigText(), X = 1, Y = 0, Width = TG.Dim.Fill(), Height = TG.Dim.Fill() });
        win.Add(cf);

        // Log
        var logFrame = new TG.FrameView
        {
            Title = " Log ", X = 0, Y = 8, Width = TG.Dim.Fill(), Height = TG.Dim.Fill(5),
            ColorScheme = MakeFrameScheme(),
        };
        var logView = new TG.ListView
        {
            X = 0, Y = 0, Width = TG.Dim.Fill(), Height = TG.Dim.Fill(),
            ColorScheme = MakeLogScheme(),
        };
        logView.SetSource(_logItems);
        logFrame.Add(logView);
        win.Add(logFrame);

        // Command input
        var cmdFrame = new TG.FrameView
        {
            Title = " Command ", X = 0, Y = TG.Pos.AnchorEnd(4), Width = TG.Dim.Fill(), Height = 3,
            ColorScheme = MakeFrameScheme(),
        };
        var cmdInput = new TG.TextField
        {
            X = 0, Y = 0, Width = TG.Dim.Fill(), Text = "",
            ColorScheme = MakeInputScheme(),
        };
        cmdInput.KeyDown += (sender, e) =>
        {
            if (e.KeyCode == TG.KeyCode.Enter)
            {
                var cmd = cmdInput.Text?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(cmd))
                {
                    _logItems.Add($"> {cmd}");
                    ProcessCommand(cmd.Trim());
                    ScrollLogToEnd(logView);
                    cmdInput.Text = "";
                }
                e.Handled = true;
            }
        };
        cmdFrame.Add(cmdInput);
        win.Add(cmdFrame);

        // Buttons row
        var btnCapture = new TG.Button { Text = "Capture(F1)", X = 1, Y = TG.Pos.AnchorEnd(1), ColorScheme = MakeButtonScheme() };
        btnCapture.Accepting += (s, e) => { _pendingAction = TuiAction.Capture; TG.Application.RequestStop(); };
        win.Add(btnCapture);

        var btnHelp = new TG.Button { Text = "Help(F2)", X = 18, Y = TG.Pos.AnchorEnd(1), ColorScheme = MakeButtonScheme() };
        btnHelp.Accepting += (s, e) =>
        {
            _logItems.Add("[help] Available commands:");
            _logItems.Add("  capture | mode | config | set | get");
            _logItems.Add("  hotkey | autostart | about | clear | exit");
            ScrollLogToEnd(logView);
        };
        win.Add(btnHelp);

        var btnSettings = new TG.Button { Text = "Settings(F3)", X = 32, Y = TG.Pos.AnchorEnd(1), ColorScheme = MakeButtonScheme() };
        btnSettings.Accepting += (s, e) => ShowSettings();
        win.Add(btnSettings);

        var btnAbout = new TG.Button { Text = "About(F4)", X = 50, Y = TG.Pos.AnchorEnd(1), ColorScheme = MakeButtonScheme() };
        btnAbout.Accepting += (s, e) => ShowAbout();
        win.Add(btnAbout);

        var btnExit = new TG.Button { Text = "Exit(F5)", X = 65, Y = TG.Pos.AnchorEnd(1), ColorScheme = MakeButtonScheme() };
        btnExit.Accepting += (s, e) => { _pendingAction = TuiAction.Exit; TG.Application.RequestStop(); };
        win.Add(btnExit);

        // Key bindings - any key auto-focuses command input
        win.KeyDown += (sender, e) =>
        {
            if (e.KeyCode == TG.KeyCode.F1) { _pendingAction = TuiAction.Capture; TG.Application.RequestStop(); e.Handled = true; return; }
            if (e.KeyCode == TG.KeyCode.F2) { btnHelp.InvokeCommand(TG.Command.Accept); e.Handled = true; return; }
            if (e.KeyCode == TG.KeyCode.F3) { ShowSettings(); e.Handled = true; return; }
            if (e.KeyCode == TG.KeyCode.F4) { ShowAbout(); e.Handled = true; return; }
            if (e.KeyCode == TG.KeyCode.F5) { _pendingAction = TuiAction.Exit; TG.Application.RequestStop(); e.Handled = true; return; }
            if (e.KeyCode == TG.KeyCode.Esc || e.KeyCode == (TG.KeyCode.Q | TG.KeyCode.CtrlMask))
            {
                ShowCloseConfirmation();
                e.Handled = true;
                return;
            }

            // Auto-focus command input on any printable key
            if (!cmdInput.HasFocus && !e.Handled
                && e.KeyCode != TG.KeyCode.Tab
                && e.KeyCode != TG.KeyCode.Esc
                && e.KeyCode != TG.KeyCode.Enter
                && (e.KeyCode & TG.KeyCode.CharMask) != 0
                && (e.KeyCode & TG.KeyCode.SpecialMask) == 0)
            {
                cmdInput.SetFocus();
                // Let the key propagate to the now-focused cmdInput
            }
        };

        // Scroll log to end after layout
        win.Initialized += (s, e) => ScrollLogToEnd(logView);

        top.Add(win);
        TG.Application.Run(top);
        TG.Application.Shutdown();
    }

    // ─── Terminal Mode (bash/cmd-like fullscreen) ─────────────
    private void RunTerminal()
    {
        TG.Application.Init();
        var top = new TG.Toplevel
        {
            ColorScheme = new TG.ColorScheme
            {
                Normal = new TG.Attribute(TnGreen, TnBgDark),
                Focus = new TG.Attribute(TnCyan, TnBgDark),
                HotNormal = new TG.Attribute(TnGreen, TnBgDark),
                HotFocus = new TG.Attribute(TnCyan, TnBgDark),
            },
        };

        // Output area - takes up entire screen except last line
        var outputView = new TG.ListView
        {
            X = 0, Y = 0,
            Width = TG.Dim.Fill(), Height = TG.Dim.Fill(1),
            CanFocus = false,
        };
        outputView.SetSource(_logItems);
        top.Add(outputView);

        // Prompt on the last line
        var promptLabel = new TG.Label
        {
            Text = "snapforge> ", X = 0, Y = TG.Pos.AnchorEnd(1),
            CanFocus = false,
            ColorScheme = new TG.ColorScheme
            {
                Normal = new TG.Attribute(TnBlue, TnBgDark),
                Focus = new TG.Attribute(TnBlue, TnBgDark),
                HotNormal = new TG.Attribute(TnBlue, TnBgDark),
                HotFocus = new TG.Attribute(TnBlue, TnBgDark),
            },
        };
        top.Add(promptLabel);

        // Input field right after prompt
        var cmdInput = new TG.TextField
        {
            X = 12, Y = TG.Pos.AnchorEnd(1), Width = TG.Dim.Fill(),
            CanFocus = true,
            ColorScheme = new TG.ColorScheme
            {
                Normal = new TG.Attribute(TnFg, TnBgDark),
                Focus = new TG.Attribute(TnCyan, TnBgDark),
                HotNormal = new TG.Attribute(TnFg, TnBgDark),
                HotFocus = new TG.Attribute(TnCyan, TnBgDark),
            },
        };
        cmdInput.KeyDown += (sender, e) =>
        {
            if (e.KeyCode == TG.KeyCode.Enter)
            {
                var cmd = cmdInput.Text?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(cmd))
                {
                    _logItems.Add($"snapforge> {cmd}");
                    ProcessCommand(cmd.Trim());
                    ScrollLogToEnd(outputView);
                    cmdInput.Text = "";
                }
                e.Handled = true;
            }
        };
        top.Add(cmdInput);

        // Any keypress auto-focuses input
        top.KeyDown += (sender, e) =>
        {
            if (!cmdInput.HasFocus && !e.Handled
                && e.KeyCode != TG.KeyCode.Tab
                && e.KeyCode != TG.KeyCode.Esc
                && (e.KeyCode & TG.KeyCode.CharMask) != 0
                && (e.KeyCode & TG.KeyCode.SpecialMask) == 0)
            {
                cmdInput.SetFocus();
            }
        };

        // Scroll to end and focus input after layout
        top.Initialized += (s, e) =>
        {
            ScrollLogToEnd(outputView);
            cmdInput.SetFocus();
        };

        TG.Application.Run(top);
        TG.Application.Shutdown();
    }

    // ─── Command Processing ───────────────────────────────────
    private void ProcessCommand(string cmd)
    {
        var parts = cmd.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        var command = parts[0].ToLower();
        var args = parts.Skip(1).ToArray();

        switch (command)
        {
            case "help":
                _logItems.Add("[help] Available commands:");
                _logItems.Add("  capture       - Take a screenshot");
                _logItems.Add("  mode          - Switch between panel/terminal mode");
                _logItems.Add("  config        - Show current configuration");
                _logItems.Add("  set <k> <v>   - Set config value");
                _logItems.Add("  get <key>     - Get config value");
                _logItems.Add("  hotkey        - Record new hotkey");
                _logItems.Add("  autostart on|off - Enable/disable autostart");
                _logItems.Add("  about         - Show about info");
                _logItems.Add("  clear         - Clear log");
                _logItems.Add("  exit          - Exit application");
                break;

            case "capture":
                _logItems.Add("[info] Starting capture...");
                _pendingAction = TuiAction.Capture;
                TG.Application.RequestStop();
                break;

            case "mode":
                _mode = _mode == TuiMode.Panel ? TuiMode.Terminal : TuiMode.Panel;
                _switchMode = true;
                _logItems.Add($"[info] Switching to {_mode} mode...");
                TG.Application.RequestStop();
                break;

            case "config":
                _logItems.Add("[config] Current settings:");
                _logItems.Add($"  Hotkey: {_config.Hotkey}");
                _logItems.Add($"  SaveDirectory: {_config.SaveDirectory}");
                _logItems.Add($"  ImageFormat: {_config.ImageFormat}");
                _logItems.Add($"  DefaultColor: {_config.DefaultColor}");
                _logItems.Add($"  DefaultPenWidth: {_config.DefaultPenWidth}");
                _logItems.Add($"  DefaultFontSize: {_config.DefaultFontSize}");
                _logItems.Add($"  MosaicBlockSize: {_config.MosaicBlockSize}");
                break;

            case "set":
                if (args.Length < 2) { _logItems.Add("[error] Usage: set <key> <value>"); break; }
                SetConfigValue(args[0], string.Join(" ", args.Skip(1)));
                break;

            case "get":
                if (args.Length < 1) { _logItems.Add("[error] Usage: get <key>"); break; }
                GetConfigValue(args[0]);
                break;

            case "hotkey":
                _logItems.Add("[info] Opening hotkey recorder...");
                ShowHotkeyRecorder();
                break;

            case "autostart":
                if (args.Length < 1) { _logItems.Add("[error] Usage: autostart on|off"); break; }
                var enable = args[0].ToLower() == "on";
                AutoStartManager.SetEnabled(enable);
                _logItems.Add($"[info] Autostart {(enable ? "enabled" : "disabled")}");
                break;

            case "about":
                _logItems.Add("[about] SnapForge v1.0 by lhx077");
                _logItems.Add("  .NET 8.0 + Terminal.Gui + WinForms");
                _logItems.Add("  https://github.com/lhx077/SnapForge");
                break;

            case "clear":
                _logItems.Clear();
                _logItems.Add("[info] Log cleared");
                break;

            case "exit":
                ShowCloseConfirmation();
                break;

            default:
                _logItems.Add($"[error] Unknown command: {command}");
                _logItems.Add("[info] Type 'help' for available commands");
                break;
        }
    }

    private void SetConfigValue(string key, string value)
    {
        try
        {
            switch (key.ToLower())
            {
                case "hotkey":
                    _config.Hotkey = value;
                    _logItems.Add($"[config] Hotkey = {value}"); break;
                case "savedirectory": case "dir":
                    _config.SaveDirectory = value;
                    _logItems.Add($"[config] SaveDirectory = {value}"); break;
                case "imageformat": case "format":
                    _config.ImageFormat = value;
                    _logItems.Add($"[config] ImageFormat = {value}"); break;
                case "defaultcolor": case "color":
                    _config.DefaultColor = value;
                    _logItems.Add($"[config] DefaultColor = {value}"); break;
                case "defaultpenwidth": case "penwidth":
                    _config.DefaultPenWidth = int.Parse(value);
                    _logItems.Add($"[config] DefaultPenWidth = {value}"); break;
                case "defaultfontsize": case "fontsize":
                    _config.DefaultFontSize = int.Parse(value);
                    _logItems.Add($"[config] DefaultFontSize = {value}"); break;
                case "mosaicblocksize": case "mosaic":
                    _config.MosaicBlockSize = int.Parse(value);
                    _logItems.Add($"[config] MosaicBlockSize = {value}"); break;
                default:
                    _logItems.Add($"[error] Unknown config key: {key}"); return;
            }
            _config.Save();
        }
        catch (Exception ex)
        {
            _logItems.Add($"[error] {ex.Message}");
        }
    }

    private void GetConfigValue(string key)
    {
        switch (key.ToLower())
        {
            case "hotkey":
                _logItems.Add($"[config] Hotkey = {_config.Hotkey}"); break;
            case "savedirectory": case "dir":
                _logItems.Add($"[config] SaveDirectory = {_config.SaveDirectory}"); break;
            case "imageformat": case "format":
                _logItems.Add($"[config] ImageFormat = {_config.ImageFormat}"); break;
            case "defaultcolor": case "color":
                _logItems.Add($"[config] DefaultColor = {_config.DefaultColor}"); break;
            case "defaultpenwidth": case "penwidth":
                _logItems.Add($"[config] DefaultPenWidth = {_config.DefaultPenWidth}"); break;
            case "defaultfontsize": case "fontsize":
                _logItems.Add($"[config] DefaultFontSize = {_config.DefaultFontSize}"); break;
            case "mosaicblocksize": case "mosaic":
                _logItems.Add($"[config] MosaicBlockSize = {_config.MosaicBlockSize}"); break;
            default:
                _logItems.Add($"[error] Unknown config key: {key}"); break;
        }
    }

    // ─── Settings Dialog ──────────────────────────────────────
    private void ShowSettings()
    {
        var dlg = new TG.Dialog
        {
            Title = " Settings ", Width = 60, Height = 18,
            ColorScheme = MakeScheme(),
        };

        var valScheme = new TG.ColorScheme
        {
            Normal = new TG.Attribute(TnCyan, TnBg),
            Focus = new TG.Attribute(TnCyan, TnBg),
            HotNormal = new TG.Attribute(TnCyan, TnBg),
            HotFocus = new TG.Attribute(TnCyan, TnBg),
        };

        dlg.Add(new TG.Label { Text = "Hotkey:", X = 2, Y = 1 });
        var hotkeyLabel = new TG.Label { Text = _config.Hotkey, X = 15, Y = 1, Width = 25, ColorScheme = valScheme };
        dlg.Add(hotkeyLabel);
        var btnRec = new TG.Button { Text = "Record", X = 42, Y = 1, ColorScheme = MakeButtonScheme() };
        btnRec.Accepting += (s, e) =>
        {
            ShowHotkeyRecorder();
            hotkeyLabel.Text = _config.Hotkey;
        };
        dlg.Add(btnRec);

        dlg.Add(new TG.Label { Text = "Save Directory:", X = 2, Y = 3 });
        var dirField = new TG.TextField { Text = _config.SaveDirectory, X = 2, Y = 4, Width = TG.Dim.Fill(2) };
        dlg.Add(dirField);

        dlg.Add(new TG.Label { Text = "Image Format:", X = 2, Y = 6 });
        var fmtField = new TG.TextField { Text = _config.ImageFormat, X = 17, Y = 6, Width = 10 };
        dlg.Add(fmtField);

        dlg.Add(new TG.Label { Text = "Color:", X = 30, Y = 6 });
        var clrField = new TG.TextField { Text = _config.DefaultColor, X = 37, Y = 6, Width = 15 };
        dlg.Add(clrField);

        dlg.Add(new TG.Label { Text = "Pen Width:", X = 2, Y = 8 });
        var penField = new TG.TextField { Text = _config.DefaultPenWidth.ToString(), X = 13, Y = 8, Width = 5 };
        dlg.Add(penField);

        dlg.Add(new TG.Label { Text = "Font Size:", X = 20, Y = 8 });
        var fontField = new TG.TextField { Text = _config.DefaultFontSize.ToString(), X = 31, Y = 8, Width = 5 };
        dlg.Add(fontField);

        dlg.Add(new TG.Label { Text = "Mosaic:", X = 38, Y = 8 });
        var mosField = new TG.TextField { Text = _config.MosaicBlockSize.ToString(), X = 46, Y = 8, Width = 5 };
        dlg.Add(mosField);

        var btnSave = new TG.Button { Text = "Save", IsDefault = true, ColorScheme = MakeButtonScheme() };
        btnSave.Accepting += (s, e) =>
        {
            try
            {
                _config.SaveDirectory = dirField.Text?.ToString() ?? _config.SaveDirectory;
                _config.ImageFormat = fmtField.Text?.ToString() ?? _config.ImageFormat;
                _config.DefaultColor = clrField.Text?.ToString() ?? _config.DefaultColor;
                _config.DefaultPenWidth = int.TryParse(penField.Text?.ToString(), out var pw) ? pw : _config.DefaultPenWidth;
                _config.DefaultFontSize = int.TryParse(fontField.Text?.ToString(), out var fs) ? fs : _config.DefaultFontSize;
                _config.MosaicBlockSize = int.TryParse(mosField.Text?.ToString(), out var ms) ? ms : _config.MosaicBlockSize;
                _config.Save();
                _logItems.Add("[info] Settings saved");
                TG.Application.RequestStop();
            }
            catch (Exception ex)
            {
                _logItems.Add($"[error] {ex.Message}");
            }
        };
        dlg.AddButton(btnSave);

        var btnCancel = new TG.Button { Text = "Cancel", ColorScheme = MakeButtonScheme() };
        btnCancel.Accepting += (s, e) => TG.Application.RequestStop();
        dlg.AddButton(btnCancel);

        TG.Application.Run(dlg);
    }

    // ─── Hotkey Recorder ──────────────────────────────────────
    private void ShowHotkeyRecorder()
    {
        var recorded = "";
        var form = new System.Windows.Forms.Form
        {
            Text = "Record Hotkey",
            ClientSize = new System.Drawing.Size(380, 180),
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen,
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            TopMost = true, KeyPreview = true,
            BackColor = System.Drawing.Color.FromArgb(0x1a, 0x1b, 0x26),
            ForeColor = System.Drawing.Color.FromArgb(0xc0, 0xca, 0xf5),
        };

        var label = new System.Windows.Forms.Label
        {
            Text = "Press your desired key combination...\n\n(e.g., Ctrl+Shift+X)",
            Location = new System.Drawing.Point(20, 15),
            Size = new System.Drawing.Size(340, 60),
            Font = new System.Drawing.Font("Segoe UI", 10),
            ForeColor = System.Drawing.Color.FromArgb(0xc0, 0xca, 0xf5),
        };
        form.Controls.Add(label);

        var resultLabel = new System.Windows.Forms.Label
        {
            Text = "Waiting...",
            Location = new System.Drawing.Point(20, 80),
            Size = new System.Drawing.Size(340, 35),
            Font = new System.Drawing.Font("Consolas", 14, System.Drawing.FontStyle.Bold),
            ForeColor = System.Drawing.Color.FromArgb(0x7d, 0xcf, 0xff),
        };
        form.Controls.Add(resultLabel);

        var btnOk = new System.Windows.Forms.Button
        {
            Text = "OK", Enabled = false,
            Location = new System.Drawing.Point(100, 130),
            Size = new System.Drawing.Size(80, 35),
            FlatStyle = System.Windows.Forms.FlatStyle.Flat,
            BackColor = System.Drawing.Color.FromArgb(0x7a, 0xa2, 0xf7),
            ForeColor = System.Drawing.Color.FromArgb(0x1a, 0x1b, 0x26),
            Font = new System.Drawing.Font("Segoe UI", 10),
        };
        btnOk.Click += (_, _) => form.Close();
        form.Controls.Add(btnOk);

        var btnCancelForm = new System.Windows.Forms.Button
        {
            Text = "Cancel",
            Location = new System.Drawing.Point(200, 130),
            Size = new System.Drawing.Size(80, 35),
            FlatStyle = System.Windows.Forms.FlatStyle.Flat,
            BackColor = System.Drawing.Color.FromArgb(0xf7, 0x76, 0x8e),
            ForeColor = System.Drawing.Color.FromArgb(0x1a, 0x1b, 0x26),
            Font = new System.Drawing.Font("Segoe UI", 10),
        };
        btnCancelForm.Click += (_, _) => { recorded = ""; form.Close(); };
        form.Controls.Add(btnCancelForm);

        form.KeyDown += (_, e) =>
        {
            e.SuppressKeyPress = true;
            e.Handled = true;

            var keyParts = new List<string>();
            if (e.Control) keyParts.Add("Ctrl");
            if (e.Shift) keyParts.Add("Shift");
            if (e.Alt) keyParts.Add("Alt");

            var key = e.KeyCode;
            if (key != System.Windows.Forms.Keys.ControlKey &&
                key != System.Windows.Forms.Keys.ShiftKey &&
                key != System.Windows.Forms.Keys.Menu)
            {
                keyParts.Add(key.ToString());
            }

            if (keyParts.Count > 1)
            {
                recorded = string.Join("+", keyParts);
                resultLabel.Text = recorded;
                btnOk.Enabled = true;
            }
        };

        form.ShowDialog();

        if (!string.IsNullOrEmpty(recorded))
        {
            _config.Hotkey = recorded;
            _config.Save();
            _logItems.Add($"[info] Hotkey set to: {recorded}");
            _logItems.Add("[info] Restart to apply new hotkey");
        }
    }

    // ─── About Dialog ─────────────────────────────────────────
    private void ShowAbout()
    {
        var dlg = new TG.Dialog
        {
            Title = " About SnapForge ", Width = 58, Height = 18,
            ColorScheme = MakeScheme(),
        };

        var titleScheme = new TG.ColorScheme
        {
            Normal = new TG.Attribute(TnCyan, TnBg),
            Focus = new TG.Attribute(TnCyan, TnBg),
            HotNormal = new TG.Attribute(TnCyan, TnBg),
            HotFocus = new TG.Attribute(TnCyan, TnBg),
        };

        dlg.Add(new TG.Label { Text = "  SnapForge v1.0", X = 1, Y = 0, Width = TG.Dim.Fill(), ColorScheme = titleScheme });
        dlg.Add(new TG.Label { Text = "  Open-source screenshot & annotation tool", X = 1, Y = 1, Width = TG.Dim.Fill() });
        dlg.Add(new TG.Label { Text = "", X = 1, Y = 2 });
        dlg.Add(new TG.Label { Text = "  - Drag-to-select screen capture", X = 1, Y = 3, Width = TG.Dim.Fill() });
        dlg.Add(new TG.Label { Text = "  - Annotation (arrow, shapes, text, mosaic)", X = 1, Y = 4, Width = TG.Dim.Fill() });
        dlg.Add(new TG.Label { Text = "  - Global hotkey & system tray", X = 1, Y = 5, Width = TG.Dim.Fill() });
        dlg.Add(new TG.Label { Text = "  - TUI dashboard + terminal mode", X = 1, Y = 6, Width = TG.Dim.Fill() });
        dlg.Add(new TG.Label { Text = "", X = 1, Y = 7 });
        dlg.Add(new TG.Label { Text = "  Author: lhx077", X = 1, Y = 8, Width = TG.Dim.Fill() });
        dlg.Add(new TG.Label { Text = "  License: Apache-2.0", X = 1, Y = 9, Width = TG.Dim.Fill() });

        var repoUrl = "https://github.com/lhx077/SnapForge";
        var linkScheme = new TG.ColorScheme
        {
            Normal = new TG.Attribute(TnBlue, TnBg),
            Focus = new TG.Attribute(TnCyan, TnBgLight),
            HotNormal = new TG.Attribute(TnBlue, TnBg),
            HotFocus = new TG.Attribute(TnCyan, TnBgLight),
        };
        dlg.Add(new TG.Label { Text = $"  Repo: {repoUrl}", X = 1, Y = 10, Width = TG.Dim.Fill(), ColorScheme = linkScheme });

        var btnRepo = new TG.Button { Text = "Open Repo", X = 2, Y = 12, ColorScheme = MakeButtonScheme() };
        btnRepo.Accepting += (s, e) =>
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(repoUrl) { UseShellExecute = true }); }
            catch { }
        };
        dlg.Add(btnRepo);

        var btnClose = new TG.Button { Text = "Close", IsDefault = true, ColorScheme = MakeButtonScheme() };
        btnClose.Accepting += (s, e) => TG.Application.RequestStop();
        dlg.AddButton(btnClose);

        TG.Application.Run(dlg);
    }

    // ─── Helpers ──────────────────────────────────────────────
    private string GetStatusText()
    {
        return $"  Mode: {_mode}  |  Hotkey: {_config.Hotkey}";
    }

    private string GetConfigText()
    {
        return $"  Fmt: {_config.ImageFormat}  Dir: {_config.SaveDirectory}";
    }

    private static string BuildBanner()
    {
        const string line = "==========================================";
        return
            $" {line}\n" +
            $"   SnapForge v1.0\n" +
            $"   Capture  *  Annotate  *  Share\n" +
            $" {line}";
    }

    private static readonly string BannerCompact = BuildBanner();

    // ─── Close Confirmation ──────────────────────────────────
    private void ShowCloseConfirmation()
    {
        var dlg = new TG.Dialog
        {
            Title = " Close ", Width = 45, Height = 9,
            ColorScheme = MakeScheme(),
        };

        dlg.Add(new TG.Label
        {
            Text = "What would you like to do?", X = TG.Pos.Center(), Y = 1,
        });

        var btnMinimize = new TG.Button { Text = "Minimize to Tray", ColorScheme = MakeButtonScheme() };
        btnMinimize.Accepting += (s, e) =>
        {
            _pendingAction = TuiAction.Minimize;
            TG.Application.RequestStop(); // close dialog
            TG.Application.RequestStop(); // close TUI
        };
        dlg.AddButton(btnMinimize);

        var btnExit = new TG.Button { Text = "Exit", ColorScheme = MakeButtonScheme() };
        btnExit.Accepting += (s, e) =>
        {
            _pendingAction = TuiAction.Exit;
            TG.Application.RequestStop(); // close dialog
            TG.Application.RequestStop(); // close TUI
        };
        dlg.AddButton(btnExit);

        var btnCancel = new TG.Button { Text = "Cancel", IsDefault = true, ColorScheme = MakeButtonScheme() };
        btnCancel.Accepting += (s, e) => TG.Application.RequestStop();
        dlg.AddButton(btnCancel);

        TG.Application.Run(dlg);
    }
}
