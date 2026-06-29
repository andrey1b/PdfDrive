using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PdfDrive.Models;

namespace PdfDrive.Services;

/// <summary>
/// Распознавание поддерживаемых типов файлов по расширению.
/// Единый источник правды для диалогов и движка сборки.
/// </summary>
public static class FileTypes
{
    public static readonly string[] PdfExt = { ".pdf" };

    // Растровые форматы, которые умеет открывать QuestPDF (через SkiaSharp).
    public static readonly string[] ImageExt =
        { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };

    // Текстовые форматы (рендерятся как простой текст).
    public static readonly string[] TextExt = { ".txt", ".log", ".md" };

    public static bool IsPdf(string path) =>
        PdfExt.Contains(Path.GetExtension(path).ToLowerInvariant());

    public static bool IsImage(string path) =>
        ImageExt.Contains(Path.GetExtension(path).ToLowerInvariant());

    public static bool IsText(string path) =>
        TextExt.Contains(Path.GetExtension(path).ToLowerInvariant());

    public static bool IsSupported(string path) => Detect(path) != null;

    /// <summary>Определить тип файла; null — если формат не поддерживается.</summary>
    public static FileKind? Detect(string path)
    {
        if (IsPdf(path)) return FileKind.Pdf;
        if (IsImage(path)) return FileKind.Image;
        if (IsText(path)) return FileKind.Text;
        return null;
    }

    /// <summary>Фильтр для OpenFileDialog (все поддерживаемые + по отдельности).</summary>
    public static string OpenFilter
    {
        get
        {
            string all  = string.Join(";", PdfExt.Concat(ImageExt).Concat(TextExt).Select(e => "*" + e));
            string imgs = string.Join(";", ImageExt.Select(e => "*" + e));
            string txts = string.Join(";", TextExt.Select(e => "*" + e));
            return
                $"{Loc.T("filter.supported")}|{all}|" +
                $"{Loc.T("filter.pdf")}|*.pdf|" +
                $"{Loc.T("filter.images")}|{imgs}|" +
                $"{Loc.T("filter.text")}|{txts}|" +
                $"{Loc.T("filter.all")}|*.*";
        }
    }
}
