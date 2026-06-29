using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PdfDrive.Models;
using PdfDrive.Services;

namespace PdfDrive;

public partial class App : Application
{
    public App()
    {
        // QuestPDF Community Edition бесплатна для индивидуальных разработчиков,
        // open-source-проектов и компаний с годовым оборотом < $1M USD.
        QuestPDF.Settings.License = LicenseType.Community;

        // Кодовые страницы (Windows-1251 и др.) для чтения ANSI-текстовых файлов.
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }

    private void App_Startup(object sender, StartupEventArgs e)
    {
        // Smoke-режим для CI/ручной проверки движка объединения:
        //   PDFDRIVE_SMOKE_DIR=<папка> — создаёт 2 тестовых PDF, объединяет, печатает число страниц.
        var smokeDir = Environment.GetEnvironmentVariable("PDFDRIVE_SMOKE_DIR");
        if (!string.IsNullOrEmpty(smokeDir))
        {
            try { RunSmoke(smokeDir!); }
            finally { Shutdown(0); }
            return;
        }

        var settings = AppSettings.Load();
        Loc.SetLang(settings.Lang);
        ThemeService.SetTheme(settings.Theme);

        var main = new MainWindow(settings);
        MainWindow = main;
        main.Show();
    }

    /// <summary>Создаёт PDF (2 стр.) + PNG, собирает их в один и проверяет результат.</summary>
    private static void RunSmoke(string dir)
    {
        Directory.CreateDirectory(dir);
        string a   = Path.Combine(dir, "a.pdf");
        string img = Path.Combine(dir, "scan.png");
        string txt = Path.Combine(dir, "note.txt");
        string outPdf = Path.Combine(dir, "merged.pdf");

        MakeSamplePdf(a, "Документ A", pages: 2);
        MakeSamplePng(img, "Скан анализа");
        File.WriteAllText(txt, "Заметка врача.\nДавление в норме. Кириллица: ёжик, тест.\n", System.Text.Encoding.UTF8);

        var items = new[]
        {
            new PdfFileItem(a, FileKind.Pdf),
            new PdfFileItem(img, FileKind.Image),
            new PdfFileItem(txt, FileKind.Text),
        };
        PdfMerger.Build(items, outPdf);

        bool ok = File.Exists(outPdf) && new FileInfo(outPdf).Length > 0;
        int? pages = PdfMerger.TryCountPages(outPdf);  // ожидаем 4: 2 (pdf) + 1 (png) + 1 (txt)
        Console.WriteLine($"[smoke] merged exists={ok} size={new FileInfo(outPdf).Length} pages={pages?.ToString() ?? "?"} (expect 4) -> {outPdf}");
    }

    /// <summary>Рисует простой PNG средствами WPF (для smoke-теста конвертации изображений).</summary>
    private static void MakeSamplePng(string path, string text)
    {
        const int w = 600, h = 400;
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, w, h));
            dc.DrawRectangle(Brushes.LightSteelBlue, null, new Rect(20, 20, w - 40, h - 40));
            var ft = new FormattedText(text, System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 32, Brushes.Black, 1.0);
            dc.DrawText(ft, new System.Windows.Point(60, 170));
        }
        var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        using var fs = File.Create(path);
        encoder.Save(fs);
    }

    private static void MakeSamplePdf(string path, string title, int pages)
    {
        Document.Create(doc =>
        {
            for (int p = 1; p <= pages; p++)
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.Content().AlignCenter().AlignMiddle()
                        .Text($"{title} — страница {p}").FontSize(28);
                });
            }
        }).GeneratePdf(path);
    }
}
