using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScreenshotPlugin.App;

public class AppConfig
{
    public string SaveDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
    public string ImageFormat { get; set; } = "png";
    public int JpegQuality { get; set; } = 90;
    public bool AutoStart { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool PlaySound { get; set; } = false;
    public bool CopyToClipboard { get; set; } = true;
    public string Hotkey { get; set; } = "PrintScreen";
    public int MosaicBlockSize { get; set; } = 12;
    public int DefaultPenWidth { get; set; } = 2;
    public string DefaultColor { get; set; } = "#FF0000";
    public int DefaultFontSize { get; set; } = 16;
    public string DefaultFontFamily { get; set; } = "Microsoft YaHei";

    // Legacy fields kept for deserialization compat, ignored on save
    [JsonIgnore] public string? HotkeyCapture { get; set; }
    [JsonIgnore] public string? HotkeyModifiers { get; set; }

    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SnapForge");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json, JsonOpts) ?? new AppConfig();
            }
        }
        catch { }

        var config = new AppConfig();
        config.Save();
        return config;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var json = JsonSerializer.Serialize(this, JsonOpts);
            File.WriteAllText(ConfigPath, json);
        }
        catch { }
    }

    public string GetConfigPath() => ConfigPath;

    /// <summary>
    /// Parse the Hotkey string like "Ctrl+Shift+X" or "PrintScreen" into
    /// Win32 modifier flags and virtual key code.
    /// </summary>
    public (uint modifiers, uint vk) ParseHotkey()
    {
        uint modifiers = 0;
        uint vk = 0;

        var parts = Hotkey.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var lower = part.ToLower();
            switch (lower)
            {
                case "ctrl" or "control": modifiers |= 0x0002; break;
                case "alt": modifiers |= 0x0001; break;
                case "shift": modifiers |= 0x0004; break;
                case "win" or "super": modifiers |= 0x0008; break;
                default: vk = KeyNameToVk(lower); break;
            }
        }

        return (modifiers, vk);
    }

    private static uint KeyNameToVk(string key)
    {
        return key switch
        {
            "printscreen" or "prtsc" or "snapshot" => 0x2C,
            "f1" => 0x70, "f2" => 0x71, "f3" => 0x72, "f4" => 0x73,
            "f5" => 0x74, "f6" => 0x75, "f7" => 0x76, "f8" => 0x77,
            "f9" => 0x78, "f10" => 0x79, "f11" => 0x7A, "f12" => 0x7B,
            "a" => 0x41, "b" => 0x42, "c" => 0x43, "d" => 0x44,
            "e" => 0x45, "f" => 0x46, "g" => 0x47, "h" => 0x48,
            "i" => 0x49, "j" => 0x4A, "k" => 0x4B, "l" => 0x4C,
            "m" => 0x4D, "n" => 0x4E, "o" => 0x4F, "p" => 0x50,
            "q" => 0x51, "r" => 0x52, "s" => 0x53, "t" => 0x54,
            "u" => 0x55, "v" => 0x56, "w" => 0x57, "x" => 0x58,
            "y" => 0x59, "z" => 0x5A,
            "0" => 0x30, "1" => 0x31, "2" => 0x32, "3" => 0x33,
            "4" => 0x34, "5" => 0x35, "6" => 0x36, "7" => 0x37,
            "8" => 0x38, "9" => 0x39,
            "space" => 0x20, "tab" => 0x09, "escape" or "esc" => 0x1B,
            "insert" or "ins" => 0x2D, "delete" or "del" => 0x2E,
            "home" => 0x24, "end" => 0x23,
            "pageup" or "pgup" => 0x21, "pagedown" or "pgdn" => 0x22,
            _ => 0x2C, // fallback to PrintScreen
        };
    }
}
