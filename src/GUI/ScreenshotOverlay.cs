using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ScreenshotPlugin.GUI;

public class ScreenshotOverlay : Form
{
    private readonly Bitmap _screenCapture;
    private Point _startPoint;
    private Point _currentPoint;
    private bool _isSelecting;
    private Rectangle _selectedRect;

    public Bitmap? CapturedImage { get; private set; }
    public DialogResult CaptureResult { get; private set; } = DialogResult.Cancel;

    public ScreenshotOverlay()
    {
        _screenCapture = CaptureScreen();

        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        ShowInTaskbar = false;
        DoubleBuffered = true;
        Cursor = Cursors.Cross;
        StartPosition = FormStartPosition.Manual;
        Bounds = SystemInformation.VirtualScreen;
    }

    private static Bitmap CaptureScreen()
    {
        var bounds = SystemInformation.VirtualScreen;
        var bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        return bmp;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isSelecting = true;
            _startPoint = e.Location;
            _currentPoint = e.Location;
        }
        else if (e.Button == MouseButtons.Right)
        {
            CaptureResult = DialogResult.Cancel;
            Close();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_isSelecting)
        {
            _currentPoint = e.Location;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (!_isSelecting || e.Button != MouseButtons.Left) return;
        _isSelecting = false;
        _selectedRect = GetNormalizedRect(_startPoint, _currentPoint);

        if (_selectedRect.Width > 5 && _selectedRect.Height > 5)
        {
            CapturedImage = CropImage(_selectedRect);
            CaptureResult = DialogResult.OK;
            Close();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            CaptureResult = DialogResult.Cancel;
            Close();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.DrawImage(_screenCapture, Point.Empty);

        // Dark overlay
        using var overlay = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
        g.FillRectangle(overlay, ClientRectangle);

        if (_isSelecting || _selectedRect.Width > 0)
        {
            var rect = _isSelecting
                ? GetNormalizedRect(_startPoint, _currentPoint)
                : _selectedRect;

            // Draw clear region
            g.DrawImage(_screenCapture, rect, rect, GraphicsUnit.Pixel);

            // Selection border
            using var pen = new Pen(Color.FromArgb(0, 174, 255), 2) { DashStyle = DashStyle.Dash };
            g.DrawRectangle(pen, rect);

            // Size label
            var sizeText = $"{rect.Width} × {rect.Height}";
            using var font = new Font("Consolas", 10);
            var textSize = g.MeasureString(sizeText, font);
            var labelY = rect.Y > 25 ? rect.Y - 22 : rect.Bottom + 4;
            using var bgBrush = new SolidBrush(Color.FromArgb(200, 0, 0, 0));
            g.FillRectangle(bgBrush, rect.X, labelY, textSize.Width + 8, textSize.Height + 2);
            g.DrawString(sizeText, font, Brushes.White, rect.X + 4, labelY + 1);
        }

        // Crosshair info at cursor
        DrawCursorInfo(g);
    }

    private void DrawCursorInfo(Graphics g)
    {
        var pos = PointToClient(Cursor.Position);
        if (pos.X < 0 || pos.Y < 0 || pos.X >= _screenCapture.Width || pos.Y >= _screenCapture.Height)
            return;

        var color = _screenCapture.GetPixel(pos.X, pos.Y);
        var info = $"({pos.X}, {pos.Y})  #{color.R:X2}{color.G:X2}{color.B:X2}";
        using var font = new Font("Consolas", 9);
        var size = g.MeasureString(info, font);
        var x = pos.X + 15;
        var y = pos.Y + 20;
        if (x + size.Width > ClientSize.Width) x = pos.X - (int)size.Width - 10;
        if (y + size.Height > ClientSize.Height) y = pos.Y - (int)size.Height - 10;

        using var bg = new SolidBrush(Color.FromArgb(200, 0, 0, 0));
        g.FillRectangle(bg, x - 2, y - 1, size.Width + 6, size.Height + 2);
        g.DrawString(info, font, Brushes.LimeGreen, x, y);
    }

    private Bitmap CropImage(Rectangle rect)
    {
        var bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.DrawImage(_screenCapture, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
        return bmp;
    }

    private static Rectangle GetNormalizedRect(Point p1, Point p2)
    {
        return new Rectangle(
            Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y),
            Math.Abs(p2.X - p1.X), Math.Abs(p2.Y - p1.Y));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _screenCapture.Dispose();
        base.Dispose(disposing);
    }
}
