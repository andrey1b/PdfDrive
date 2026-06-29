using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Word = DocumentFormat.OpenXml.Wordprocessing;

namespace PdfDrive.Services;

/// <summary>
/// Конвертация Word .docx → PDF через OpenXML + QuestPDF.
/// Поддерживает абзацы (жирный/курсив, заголовки, списки), таблицы и
/// встроенные изображения. Сложное оформление Word упрощается.
/// </summary>
public static class DocxConverter
{
    public static void DocxToPdf(string docxPath, string outputPath)
    {
        using var word = WordprocessingDocument.Open(docxPath, false);
        var main = word.MainDocumentPart;
        var body = main?.Document?.Body;
        string title = Path.GetFileName(docxPath);

        Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(11).LineHeight(1.3f));

                page.Header().PaddingBottom(8).Text(title)
                    .FontSize(9).FontColor(Colors.Grey.Darken1);

                page.Content().Column(col =>
                {
                    col.Spacing(6);
                    if (body != null)
                        RenderBody(col, body, main!);
                });

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

    // ──────────────────────────────────────────────────────── тело документа
    private static void RenderBody(ColumnDescriptor col, Word.Body body, MainDocumentPart main)
    {
        foreach (var element in body.ChildElements)
        {
            switch (element)
            {
                case Word.Paragraph p:
                    RenderParagraph(col, p, main);
                    break;
                case Word.Table t:
                    RenderTable(col, t, main);
                    break;
            }
        }
    }

    // ──────────────────────────────────────────────────────── абзац
    private static void RenderParagraph(ColumnDescriptor col, Word.Paragraph p, MainDocumentPart main)
    {
        // Встроенные изображения абзаца — выводим отдельными блоками.
        var images = ExtractImages(p, main);

        string styleId = p.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? "";
        float? headingSize = HeadingSize(styleId);
        bool isList = p.ParagraphProperties?.NumberingProperties != null;

        // Собираем прогоны (runs) с форматированием.
        var runs = CollectRuns(p);
        bool hasText = runs.Any(r => !string.IsNullOrEmpty(r.Text));

        if (hasText)
        {
            col.Item().Text(text =>
            {
                if (headingSize is float hs)
                    text.DefaultTextStyle(s => s.FontSize(hs).Bold());

                if (isList)
                    text.Span("•  ");

                foreach (var r in runs)
                {
                    if (string.IsNullOrEmpty(r.Text)) continue;
                    var span = text.Span(r.Text);
                    if (r.Bold) span = span.Bold();
                    if (r.Italic) span = span.Italic();
                }
            });
        }

        foreach (var imgBytes in images)
        {
            try { col.Item().PaddingVertical(4).AlignLeft().MaxWidth(420).Image(imgBytes); }
            catch { /* нечитаемое изображение — пропускаем */ }
        }
    }

    private readonly record struct RunInfo(string Text, bool Bold, bool Italic);

    private static List<RunInfo> CollectRuns(Word.Paragraph p)
    {
        var list = new List<RunInfo>();
        foreach (var run in p.Elements<Word.Run>())
        {
            bool bold = run.RunProperties?.Bold != null && !IsExplicitlyOff(run.RunProperties.Bold);
            bool italic = run.RunProperties?.Italic != null && !IsExplicitlyOff(run.RunProperties.Italic);

            var sb = new System.Text.StringBuilder();
            foreach (var child in run.ChildElements)
            {
                switch (child)
                {
                    case Word.Text t:    sb.Append(t.Text); break;
                    case Word.TabChar:   sb.Append("    "); break;
                    case Word.Break:     sb.Append('\n'); break;
                }
            }
            if (sb.Length > 0)
                list.Add(new RunInfo(sb.ToString(), bold, italic));
        }
        return list;
    }

    private static bool IsExplicitlyOff(Word.OnOffType onOff) =>
        onOff.Val != null && onOff.Val.Value == false;

    /// <summary>Размер шрифта для стиля-заголовка, иначе null.</summary>
    private static float? HeadingSize(string styleId)
    {
        if (string.IsNullOrEmpty(styleId)) return null;
        var s = styleId.ToLowerInvariant();
        if (s.Contains("title") || s.Contains("заголовокдок")) return 20f;
        if (s.StartsWith("heading") || s.Contains("заголовок"))
        {
            int level = new string(s.Where(char.IsDigit).ToArray()) is { Length: > 0 } d
                && int.TryParse(d, out var n) ? n : 1;
            return level switch { 1 => 18f, 2 => 15f, 3 => 13f, _ => 12f };
        }
        return null;
    }

    // ──────────────────────────────────────────────────────── таблица
    private static void RenderTable(ColumnDescriptor col, Word.Table t, MainDocumentPart main)
    {
        var rows = t.Elements<Word.TableRow>().ToList();
        if (rows.Count == 0) return;

        int cols = rows.Max(r => r.Elements<Word.TableCell>().Count());
        if (cols == 0) return;

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(cd =>
            {
                for (int i = 0; i < cols; i++) cd.RelativeColumn();
            });

            foreach (var row in rows)
            {
                var cells = row.Elements<Word.TableCell>().ToList();
                for (int i = 0; i < cols; i++)
                {
                    string cellText = i < cells.Count ? CellText(cells[i]) : "";
                    table.Cell()
                        .Border(0.5f).BorderColor(Colors.Grey.Lighten1)
                        .Padding(5)
                        .Text(cellText).FontSize(10);
                }
            }
        });
    }

    private static string CellText(Word.TableCell cell)
    {
        var parts = cell.Descendants<Word.Paragraph>()
            .Select(p => string.Concat(p.Descendants<Word.Text>().Select(x => x.Text)));
        return string.Join("\n", parts).Trim();
    }

    // ──────────────────────────────────────────────────────── изображения
    private static List<byte[]> ExtractImages(Word.Paragraph p, MainDocumentPart main)
    {
        var result = new List<byte[]>();
        foreach (var blip in p.Descendants<DocumentFormat.OpenXml.Drawing.Blip>())
        {
            string? embed = blip.Embed?.Value;
            if (string.IsNullOrEmpty(embed)) continue;
            try
            {
                if (main.GetPartById(embed) is ImagePart img)
                {
                    using var stream = img.GetStream();
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    result.Add(ms.ToArray());
                }
            }
            catch { /* нет такого ресурса — пропускаем */ }
        }
        return result;
    }
}
