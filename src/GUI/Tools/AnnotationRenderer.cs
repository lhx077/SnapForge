using System.Drawing;
using System.Drawing.Drawing2D;

namespace ScreenshotPlugin.GUI.Tools;

public static class AnnotationRenderer
{
    public static void DrawItem(Graphics g, AnnotationItem item, Bitmap? source = null)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;

        switch (item.Type)
        {
            case AnnotationToolType.Arrow:
                DrawArrow(g, item);
                break;
            case AnnotationToolType.Rectangle:
                DrawRectangle(g, item);
                break;
            case AnnotationToolType.Ellipse:
                DrawEllipse(g, item);
                break;
            case AnnotationToolType.Line:
                DrawLine(g, item);
                break;
            case AnnotationToolType.Text:
                DrawText(g, item);
                break;
            case AnnotationToolType.Mosaic:
                if (source != null) DrawMosaic(g, item, source);
                break;
            case AnnotationToolType.FreeHand:
                DrawFreeHand(g, item);
                break;
        }
    }

    private static void DrawArrow(Graphics g, AnnotationItem item)
    {
        using var pen = new Pen(item.Color, item.PenWidth);
        pen.CustomEndCap = new AdjustableArrowCap(item.PenWidth + 3, item.PenWidth + 4);
        g.DrawLine(pen, item.Start, item.End);
    }

    private static void DrawRectangle(Graphics g, AnnotationItem item)
    {
        using var pen = new Pen(item.Color, item.PenWidth);
        var rect = GetNormalizedRect(item.Start, item.End);
        g.DrawRectangle(pen, rect);
    }

    private static void DrawEllipse(Graphics g, AnnotationItem item)
    {
        using var pen = new Pen(item.Color, item.PenWidth);
        var rect = GetNormalizedRect(item.Start, item.End);
        g.DrawEllipse(pen, rect);
    }

    private static void DrawLine(Graphics g, AnnotationItem item)
    {
        using var pen = new Pen(item.Color, item.PenWidth);
        g.DrawLine(pen, item.Start, item.End);
    }

    private static void DrawText(Graphics g, AnnotationItem item)
    {
        if (string.IsNullOrEmpty(item.Text)) return;
        using var font = new Font(item.FontFamily, item.FontSize, FontStyle.Regular, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(item.Color);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        g.DrawString(item.Text, font, brush, item.Start);
    }

    private static void DrawMosaic(Graphics g, AnnotationItem item, Bitmap source)
    {
        var rect = GetNormalizedRect(item.Start, item.End);
        if (rect.Width <= 0 || rect.Height <= 0) return;

        int block = item.MosaicBlockSize;
        for (int y = rect.Y; y < rect.Y + rect.Height; y += block)
        {
            for (int x = rect.X; x < rect.X + rect.Width; x += block)
            {
                int avgR = 0, avgG = 0, avgB = 0, count = 0;
                int bw = Math.Min(block, rect.Right - x);
                int bh = Math.Min(block, rect.Bottom - y);

                for (int dy = 0; dy < bh; dy++)
                {
                    for (int dx = 0; dx < bw; dx++)
                    {
                        int px = x + dx, py = y + dy;
                        if (px >= 0 && px < source.Width && py >= 0 && py < source.Height)
                        {
                            var c = source.GetPixel(px, py);
                            avgR += c.R; avgG += c.G; avgB += c.B; count++;
                        }
                    }
                }

                if (count > 0)
                {
                    using var brush = new SolidBrush(Color.FromArgb(avgR / count, avgG / count, avgB / count));
                    g.FillRectangle(brush, x, y, bw, bh);
                }
            }
        }
    }

    private static void DrawFreeHand(Graphics g, AnnotationItem item)
    {
        if (item.FreeHandPoints == null || item.FreeHandPoints.Count < 2) return;
        using var pen = new Pen(item.Color, item.PenWidth)
        {
            LineJoin = LineJoin.Round,
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        g.DrawLines(pen, item.FreeHandPoints.ToArray());
    }

    public static Rectangle GetNormalizedRect(Point p1, Point p2)
    {
        int x = Math.Min(p1.X, p2.X);
        int y = Math.Min(p1.Y, p2.Y);
        int w = Math.Abs(p2.X - p1.X);
        int h = Math.Abs(p2.Y - p1.Y);
        return new Rectangle(x, y, w, h);
    }
}
