using System;
using System.IO;
using System.Text.Json;

namespace PdfDrive.Services;

/// <summary>
/// Настройки приложения (язык + тема). Хранятся в %LOCALAPPDATA%\PdfDrive\settings.json,
/// как и у остальных приложений SeniorHub.
/// </summary>
public class AppSettings
{
    public string Lang  { get; set; } = Loc.FallbackLang;
    public string Theme { get; set; } = ThemeService.Dark;
    public bool OpenAfterSave { get; set; } = true;

    private static string DataDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PdfDrive");

    private static string FilePath => Path.Combine(DataDir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var s = JsonSerializer.Deserialize<AppSettings>(json);
                if (s != null) return s;
            }
        }
        catch { /* повреждённый файл — берём значения по умолчанию */ }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch { /* не критично — настройки не сохранятся, но работа продолжится */ }
    }
}
