using System;
using System.Windows;
using System.Windows.Media;

namespace PdfDrive.Services;

/// <summary>
/// Переключение тёмной / светлой темы в рантайме.
/// Мутирует SolidColorBrush в Application.Resources — палитра единая с GardenPlanner.
/// </summary>
public static class ThemeService
{
    public const string Dark  = "dark";
    public const string Light = "light";

    public static string Current { get; private set; } = Dark;
    public static event Action? ThemeChanged;

    private static readonly (string Key, Color DarkColor, Color LightColor)[] _brushes =
    [
        ("BgDarkBrush",        Color.FromRgb(0x1A, 0x1A, 0x2E), Color.FromRgb(0xD0, 0xE8, 0xD0)),
        ("BgCardBrush",        Color.FromRgb(0x16, 0x21, 0x3E), Color.FromRgb(0xE4, 0xF2, 0xE4)),
        ("BgInputBrush",       Color.FromRgb(0x0F, 0x34, 0x60), Color.FromRgb(0xFF, 0xFF, 0xFF)),
        ("TextPrimaryBrush",   Color.FromRgb(0xEA, 0xEA, 0xEA), Color.FromRgb(0x2B, 0x2B, 0x2B)),
        ("TextSecondaryBrush", Color.FromRgb(0xA0, 0xA0, 0xB8), Color.FromRgb(0x4F, 0x5A, 0x41)),
        ("AccentBrush",        Color.FromRgb(0x6C, 0x63, 0xFF), Color.FromRgb(0x2C, 0x5F, 0x2D)),
        ("AccentHoverBrush",   Color.FromRgb(0x5A, 0x52, 0xD5), Color.FromRgb(0x23, 0x4C, 0x24)),
        ("DangerBrush",        Color.FromRgb(0xE7, 0x4C, 0x3C), Color.FromRgb(0xC6, 0x28, 0x28)),
        ("SeparatorBrush",     Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), Color.FromRgb(0xC8, 0xD8, 0xC8)),
        ("CardBorderBrush",    Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), Color.FromRgb(0xB8, 0xCC, 0xB8)),
        ("HeaderBgBrush",      Color.FromRgb(0x1A, 0x1A, 0x2E), Color.FromRgb(0x2C, 0x5F, 0x2D)),
        ("HeaderTextBrush",    Color.FromRgb(0xEA, 0xEA, 0xEA), Color.FromRgb(0xFF, 0xFF, 0xFF)),
        ("HeaderSubBrush",     Color.FromRgb(0xA0, 0xA0, 0xB8), Color.FromRgb(0xBC, 0xE1, 0xAF)),
    ];

    public static void SetTheme(string theme)
    {
        Current = theme == Light ? Light : Dark;
        bool isLight = Current == Light;
        var res = Application.Current.Resources;

        foreach (var (key, dark, light) in _brushes)
            res[key] = new SolidColorBrush(isLight ? light : dark);

        ThemeChanged?.Invoke();
    }

    public static string Toggle()
    {
        SetTheme(Current == Dark ? Light : Dark);
        return Current;
    }
}
