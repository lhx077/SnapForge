using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ScreenshotPlugin.App;
using ScreenshotPlugin.GUI;
using ScreenshotPlugin.TUI;

namespace ScreenshotPlugin;

static class Program
{
    private static AppConfig _config = null!;
    private static TrayIcon? _tray;
    private static volatile bool _running = true;
    private static volatile bool _tuiActive;
    private static readonly Mutex _mutex = new(true, "SnapForge_SingleInstance");

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandler? handler, bool add);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private delegate bool ConsoleCtrlHandler(int ctrlType);
    private static ConsoleCtrlHandler? _consoleCtrlHandler;
    private const int SW_HIDE = 0;

    [STAThread]
    static void Main(string[] args)
    {
        if (!_mutex.WaitOne(TimeSpan.Zero, true))
        {
            MessageBox.Show("SnapForge is already running.", "Info",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        _config = AppConfig.Load();

        bool background = args.Contains("--background");

        var trayThread = new Thread(() => RunTray(background)) { IsBackground = true };
        trayThread.SetApartmentState(ApartmentState.STA);
        trayThread.Start();

        if (!background)
        {
            RunTuiLoop();
        }

        while (_running)
        {
            Thread.Sleep(100);
        }

        _tray?.Dispose();
        _mutex.ReleaseMutex();
    }

    private static void RunTray(bool showBalloon)
    {
        _tray = new TrayIcon();
        _tray.OnCaptureClicked += () => RunOnNewThread(() => { StartCapture(); RunTuiLoop(); });
        _tray.OnOpenTuiClicked += () => RunOnNewThread(RunTuiLoop);
        _tray.OnSettingsClicked += () => RunOnNewThread(RunTuiLoop);
        _tray.OnExitClicked += ExitApp;

        if (_tray.RegisterGlobalHotkey(_config))
        {
            if (showBalloon)
                _tray.ShowBalloon("SnapForge",
                    $"Running in background. Hotkey: {_config.Hotkey}");
        }
        else if (showBalloon)
        {
            _tray.ShowBalloon("SnapForge",
                $"Running in background. (Hotkey '{_config.Hotkey}' registration failed)");
        }

        System.Windows.Forms.Application.Run();
    }

    private static void ExitApp()
    {
        _running = false;
        System.Windows.Forms.Application.ExitThread();
    }

    private static void RunOnNewThread(Action action)
    {
        var t = new Thread(() => action()) { IsBackground = true };
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
    }

    private static void RunTuiLoop()
    {
        if (_tuiActive) return; // prevent multiple TUI instances
        _tuiActive = true;

        try
        {
            // Ensure console exists for Terminal.Gui
            if (GetConsoleWindow() == IntPtr.Zero)
                AllocConsole();

            // Show console in case it was hidden
            ShowConsole();

            // Intercept console close button (X) to hide instead of kill
            _consoleCtrlHandler = ctrlType =>
            {
                if (ctrlType == 2) // CTRL_CLOSE_EVENT
                {
                    HideConsole();
                    return true; // prevent process termination
                }
                return false;
            };
            SetConsoleCtrlHandler(_consoleCtrlHandler, true);

            var tui = new TuiMainWindow(_config);
            while (true)
            {
                var action = tui.Run();
                switch (action)
                {
                    case TuiAction.Capture:
                        StartCapture();
                        continue;
                    case TuiAction.Exit:
                        ExitApp();
                        return;
                    case TuiAction.Minimize:
                        HideConsole();
                        _tray?.ShowBalloon("SnapForge", "Minimized to tray. Use tray icon to reopen.");
                        return;
                    default:
                        return;
                }
            }
        }
        finally
        {
            _tuiActive = false;
        }
    }

    private static void HideConsole()
    {
        var hwnd = GetConsoleWindow();
        if (hwnd != IntPtr.Zero)
            ShowWindow(hwnd, SW_HIDE);
    }

    private static void ShowConsole()
    {
        var hwnd = GetConsoleWindow();
        if (hwnd != IntPtr.Zero)
            ShowWindow(hwnd, 9); // SW_RESTORE
    }

    private static void StartCapture()
    {
        Thread.Sleep(200);

        Bitmap? captured = null;

        var overlayThread = new Thread(() =>
        {
            using var overlay = new ScreenshotOverlay();
            System.Windows.Forms.Application.Run(overlay);
            if (overlay.CaptureResult == DialogResult.OK && overlay.CapturedImage != null)
                captured = overlay.CapturedImage;
        });
        overlayThread.SetApartmentState(ApartmentState.STA);
        overlayThread.Start();
        overlayThread.Join();

        if (captured == null) return;

        AnnotationEditor.EditorResult result = AnnotationEditor.EditorResult.Cancel;
        Bitmap? finalImage = null;

        var editorThread = new Thread(() =>
        {
            using var editor = new AnnotationEditor(captured, _config);
            System.Windows.Forms.Application.Run(editor);
            result = editor.Result;
            if (result == AnnotationEditor.EditorResult.CopyToClipboard)
                finalImage = editor.GetFinalImage();
        });
        editorThread.SetApartmentState(ApartmentState.STA);
        editorThread.Start();
        editorThread.Join();

        try
        {
            if (result == AnnotationEditor.EditorResult.CopyToClipboard && finalImage != null)
            {
                CopyToClipboard(finalImage);
                _tray?.ShowBalloon("SnapForge", "Screenshot copied to clipboard!");
            }
            else if (result == AnnotationEditor.EditorResult.Save)
            {
                _tray?.ShowBalloon("SnapForge", "Screenshot saved!");
            }
        }
        finally
        {
            captured.Dispose();
            finalImage?.Dispose();
        }
    }

    private static void CopyToClipboard(Bitmap image)
    {
        var t = new Thread(() => Clipboard.SetImage(image));
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();
    }
}
