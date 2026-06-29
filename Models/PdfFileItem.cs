using System;
using System.ComponentModel;
using System.IO;
using PdfDrive.Services;

namespace PdfDrive.Models;

/// <summary>Тип исходного файла в списке.</summary>
public enum FileKind
{
    Pdf,
    Image,
    Text,
}

/// <summary>
/// Один файл в списке для сборки в общий PDF.
/// </summary>
public class PdfFileItem : INotifyPropertyChanged
{
    public string FullPath { get; }
    public string FileName => Path.GetFileName(FullPath);
    public FileKind Kind { get; }

    /// <summary>Значок типа файла для списка.</summary>
    public string Icon => Kind switch
    {
        FileKind.Image => "🖼",
        FileKind.Text  => "📝",
        _              => "📄",
    };

    private int? _pages;
    public int? Pages
    {
        get => _pages;
        set { _pages = value; OnChanged(nameof(Pages)); OnChanged(nameof(PagesText)); }
    }

    public string PagesText => Pages?.ToString() ?? Loc.T("pages.unknown");

    public PdfFileItem(string fullPath, FileKind kind)
    {
        FullPath = fullPath;
        Kind = kind;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged(string prop) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}
