using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PdfDrive.Services;

/// <summary>
/// Генерация иконки приложения (app.ico) в цветах GardenPlanner.
/// Рисует векторно для каждого размера и собирает многоразмерный ICO с PNG-кадрами.
/// Вызывается dev-режимом: PDFDRIVE_MAKE_ICON=&lt;путь к .ico&gt;.
/// </summary>
public static class IconGenerator
{
    private static readonly int[] Sizes = { 16, 24, 32, 48, 64, 128, 256 };

    public static void Generate(string icoPath)
    {
        var pngs = new List<byte[]>();
        foreach (var s in Sizes) pngs.Add(RenderPng(s));
        WriteIco(icoPath, Sizes, pngs);
    }

    private static byte[] RenderPng(int s)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
            Draw(dc, s);

        var rtb = new RenderTargetBitmap(s, s, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(rtb));
        using var ms = new MemoryStream();
        enc.Save(ms);
        return ms.ToArray();
    }

    // ── рисунок: зелёный фон + стопка страниц с загнутым уголком ──
    private static void Draw(DrawingContext dc, double s)
    {
        // Фон — зелёный градиент GardenPlanner, скруглённый квадрат.
        var bg = new LinearGradientBrush(
            Color.FromRgb(0x35, 0x6F, 0x36),
            Color.FromRgb(0x23, 0x4C, 0x24),
            new Point(0, 0), new Point(1, 1));
        double r = s * 0.18;
        dc.DrawRoundedRectangle(bg, null, new Rect(0, 0, s, s), r, r);

        double pageW = s * 0.44, pageH = s * 0.56;
        double cx = s * 0.5, cy = s * 0.53;

        // Задняя страница (стопка) — светло-зелёный, со сдвигом вверх-влево.
        var backRect = new Rect(cx - pageW / 2 - s * 0.055, cy - pageH / 2 - s * 0.055, pageW, pageH);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(0xBC, 0xE1, 0xAF)),
            null, backRect, s * 0.04, s * 0.04);

        // Передняя страница — белая, с загнутым правым верхним уголком.
        var front = new Rect(cx - pageW / 2 + s * 0.03, cy - pageH / 2 + s * 0.03, pageW, pageH);
        double fold = s * 0.14;

        var page = new StreamGeometry();
        using (var ctx = page.Open())
        {
            double x0 = front.Left, y0 = front.Top, x1 = front.Right, y1 = front.Bottom;
            ctx.BeginFigure(new Point(x0, y0), true, true);
            ctx.LineTo(new Point(x1 - fold, y0), true, true);
            ctx.LineTo(new Point(x1, y0 + fold), true, true);
            ctx.LineTo(new Point(x1, y1), true, true);
            ctx.LineTo(new Point(x0, y1), true, true);
        }
        page.Freeze();
        dc.DrawGeometry(Brushes.White, null, page);

        // Треугольник загнутого уголка.
        var foldGeo = new StreamGeometry();
        using (var ctx = foldGeo.Open())
        {
            double x1 = front.Right, y0 = front.Top;
            ctx.BeginFigure(new Point(x1 - fold, y0), true, true);
            ctx.LineTo(new Point(x1 - fold, y0 + fold), true, true);
            ctx.LineTo(new Point(x1, y0 + fold), true, true);
        }
        foldGeo.Freeze();
        dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(0xD0, 0xE8, 0xD0)), null, foldGeo);

        // Строки «текста» на странице.
        var lineBrush = new SolidColorBrush(Color.FromRgb(0x6F, 0x9E, 0x70));
        double lx = front.Left + s * 0.055;
        double lw = pageW - s * 0.11;
        double ly = front.Top + pageH * 0.42;
        double gap = pageH * 0.18;
        double lh = Math.Max(1.0, s * 0.035);
        for (int i = 0; i < 3; i++)
        {
            double w = i == 2 ? lw * 0.55 : lw;
            dc.DrawRoundedRectangle(lineBrush, null,
                new Rect(lx, ly + gap * i, w, lh), lh / 2, lh / 2);
        }
    }

    // ── сборка ICO с PNG-кадрами ──
    private static void WriteIco(string path, int[] sizes, List<byte[]> pngs)
    {
        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);

        bw.Write((short)0);              // reserved
        bw.Write((short)1);              // type = icon
        bw.Write((short)sizes.Length);   // count

        int offset = 6 + 16 * sizes.Length;
        for (int i = 0; i < sizes.Length; i++)
        {
            int sz = sizes[i];
            bw.Write((byte)(sz >= 256 ? 0 : sz)); // width  (0 = 256)
            bw.Write((byte)(sz >= 256 ? 0 : sz)); // height (0 = 256)
            bw.Write((byte)0);                    // palette
            bw.Write((byte)0);                    // reserved
            bw.Write((short)1);                   // color planes
            bw.Write((short)32);                  // bits per pixel
            bw.Write(pngs[i].Length);             // bytes of PNG data
            bw.Write(offset);                     // offset
            offset += pngs[i].Length;
        }
        foreach (var p in pngs) bw.Write(p);
    }
}
