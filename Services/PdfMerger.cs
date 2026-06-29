using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PdfDrive.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PdfDrive.Services;

/// <summary>
/// Сборка одного PDF из набора файлов (PDF и изображения) — через QuestPDF.
/// Изображения сначала превращаются во временные одностраничные PDF,
/// затем все части объединяются в заданном порядке через DocumentOperation.
/// </summary>
public static class PdfMerger
{
    /// <summary>
    /// Собирает один PDF из <paramref name="items"/> в порядке списка
    /// и сохраняет результат в <paramref name="outputPath"/>.
    /// </summary>
    public static void Build(IReadOnlyList<PdfFileItem> items, string outputPath)
    {
        if (items == null || items.Count == 0)
            throw new ArgumentException("Не задано ни одного входного файла.");

        foreach (var it in items)
            if (!File.Exists(it.FullPath))
                throw new FileNotFoundException($"Файл не найден: {it.FullPath}", it.FullPath);

        var tempFiles = new List<string>();
        try
        {
            // Готовим список путей-частей: PDF берём как есть, изображения конвертируем.
            var parts = new List<string>(items.Count);
            foreach (var it in items)
            {
                if (it.Kind == FileKind.Image || it.Kind == FileKind.Text)
                {
                    string tmp = Path.Combine(Path.GetTempPath(),
                        "pdfdrive_" + Guid.NewGuid().ToString("N") + ".pdf");
                    if (it.Kind == FileKind.Image) ImageToPdf(it.FullPath, tmp);
                    else                            TextToPdf(it.FullPath, tmp);
                    tempFiles.Add(tmp);
                    parts.Add(tmp);
                }
                else
                {
                    parts.Add(it.FullPath);
                }
            }

            var operation = DocumentOperation.LoadFile(parts[0]);
            for (int i = 1; i < parts.Count; i++)
                operation = operation.MergeFile(parts[i]);

            operation.Save(outputPath);
        }
        finally
        {
            foreach (var tmp in tempFiles)
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { /* не критично */ }
        }
    }

    /// <summary>Создаёт одностраничный PDF (A4) с изображением, вписанным в страницу.</summary>
    public static void ImageToPdf(string imagePath, string outputPath)
    {
        Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.Content().AlignCenter().AlignMiddle()
                    .Image(imagePath).FitArea();
            });
        }).GeneratePdf(outputPath);
    }

    /// <summary>
    /// Создаёт PDF из текстового файла. Текст переносится по словам и
    /// автоматически разбивается на страницы A4. Внизу — номер страницы.
    /// </summary>
    public static void TextToPdf(string textPath, string outputPath)
    {
        string content = ReadTextSmart(textPath);
        string title = Path.GetFileName(textPath);

        Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(11).LineHeight(1.35f));

                page.Header().PaddingBottom(8).Text(title)
                    .FontSize(9).FontColor(Colors.Grey.Darken1);

                // QuestPDF автоматически переносит и разбивает длинный текст на страницы.
                page.Content().Text(content);

                page.Footer().AlignCenter().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(9).FontColor(Colors.Grey.Darken1));
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        }).GeneratePdf(outputPath);
    }

    /// <summary>
    /// Читает текстовый файл с определением кодировки: BOM → UTF-8 (строго) →
    /// откат на Windows-1251 (для кириллических ANSI-файлов).
    /// </summary>
    private static string ReadTextSmart(string path)
    {
        var bytes = File.ReadAllBytes(path);

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return System.Text.Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return System.Text.Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return System.Text.Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

        try
        {
            var strictUtf8 = new System.Text.UTF8Encoding(false, true);
            return strictUtf8.GetString(bytes);
        }
        catch (System.Text.DecoderFallbackException)
        {
            try { return System.Text.Encoding.GetEncoding(1251).GetString(bytes); }
            catch { return System.Text.Encoding.UTF8.GetString(bytes); }
        }
    }

    /// <summary>
    /// Best-effort подсчёт страниц PDF без сторонних библиотек.
    /// Для PDF со сжатым xref (object streams) вернёт null — в UI «?».
    /// </summary>
    public static int? TryCountPages(string path)
    {
        try
        {
            var text = File.ReadAllText(path, System.Text.Encoding.Latin1);

            if (Regex.IsMatch(text, @"/Type\s*/Pages\b"))
            {
                int best = 0;
                foreach (Match m in Regex.Matches(text, @"/Count\s+(\d+)"))
                    if (int.TryParse(m.Groups[1].Value, out var n) && n > best)
                        best = n;
                if (best > 0) return best;
            }

            int pages = Regex.Matches(text, @"/Type\s*/Page(?![s])").Count;
            return pages > 0 ? pages : (int?)null;
        }
        catch
        {
            return null;
        }
    }
}
