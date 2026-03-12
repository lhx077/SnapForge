using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using ScreenshotPlugin.App;
using ScreenshotPlugin.GUI.Tools;

namespace ScreenshotPlugin.GUI;

public class AnnotationEditor : Form
{
    private readonly Bitmap _originalImage;
    private readonly List<AnnotationItem> _annotations = new();
    private AnnotationToolType _currentTool = AnnotationToolType.None;
    private Color _currentColor = Color.Red;
    private int _penWidth = 2;
    private int _fontSize = 16;
    private readonly AppConfig _config;
    private readonly Panel _canvas;
    private bool _isDrawing;
    private Point _drawStart;
    private Point _drawCurrent;
    private List<Point>? _freeHandPoints;

    public enum EditorResult { None, CopyToClipboard, Save, Cancel }
    public EditorResult Result { get; private set; } = EditorResult.Cancel;

    public AnnotationEditor(Bitmap image, AppConfig config)
    {
        _config = config;
        _originalImage = (Bitmap)image.Clone();
        _currentColor = ColorTranslator.FromHtml(config.DefaultColor);
        _penWidth = config.DefaultPenWidth;
        _fontSize = config.DefaultFontSize;

        Text = "ScreenshotPlugin - 编辑";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        BackColor = Color.FromArgb(30, 30, 30);
        TopMost = true;
        KeyPreview = true;

        var w = Math.Min(image.Width + 40, Screen.PrimaryScreen!.WorkingArea.Width - 100);
        var h = Math.Min(image.Height + 120, Screen.PrimaryScreen.WorkingArea.Height - 100);
        ClientSize = new Size(Math.Max(w, 500), Math.Max(h, 400));

        var toolbar = CreateToolbar();
        toolbar.Dock = DockStyle.Top;
        Controls.Add(toolbar);

        var actionBar = CreateActionBar();
        actionBar.Dock = DockStyle.Bottom;
        Controls.Add(actionBar);

        _canvas = new DoubleBufferedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(20, 20, 20),
        };
        _canvas.Paint += Canvas_Paint;
        _canvas.MouseDown += Canvas_MouseDown;
        _canvas.MouseMove += Canvas_MouseMove;
        _canvas.MouseUp += Canvas_MouseUp;
        Controls.Add(_canvas);
    }

    private Point ImagePoint(Point clientPoint)
    {
        var imgRect = GetImageRect();
        if (imgRect.Width <= 0 || imgRect.Height <= 0) return Point.Empty;
        float scaleX = (float)_originalImage.Width / imgRect.Width;
        float scaleY = (float)_originalImage.Height / imgRect.Height;
        return new Point(
            (int)((clientPoint.X - imgRect.X) * scaleX),
            (int)((clientPoint.Y - imgRect.Y) * scaleY));
    }

    private Rectangle GetImageRect()
    {
        float ratioX = (float)_canvas.ClientSize.Width / _originalImage.Width;
        float ratioY = (float)_canvas.ClientSize.Height / _originalImage.Height;
        float ratio = Math.Min(ratioX, ratioY);
        int w = (int)(_originalImage.Width * ratio);
        int h = (int)(_originalImage.Height * ratio);
        int x = (_canvas.ClientSize.Width - w) / 2;
        int y = (_canvas.ClientSize.Height - h) / 2;
        return new Rectangle(x, y, w, h);
    }

    private ToolStrip CreateToolbar()
    {
        var ts = new ToolStrip
        {
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.White,
            GripStyle = ToolStripGripStyle.Hidden,
            Renderer = new DarkToolStripRenderer(),
            Padding = new Padding(4),
        };

        var tools = new (string label, AnnotationToolType type)[]
        {
            ("\U0001F5B1 选择", AnnotationToolType.None),
            ("\u27A1 箭头", AnnotationToolType.Arrow),
            ("\u25AD 矩形", AnnotationToolType.Rectangle),
            ("\u2B2D 椭圆", AnnotationToolType.Ellipse),
            ("\u2571 直线", AnnotationToolType.Line),
            ("\u270F 画笔", AnnotationToolType.FreeHand),
            ("T 文字", AnnotationToolType.Text),
            ("\u25A6 打码", AnnotationToolType.Mosaic),
        };

        foreach (var (label, type) in tools)
        {
            var btn = new ToolStripButton(label) { Tag = type, ForeColor = Color.White };
            btn.Click += (_, _) =>
            {
                _currentTool = type;
                foreach (ToolStripItem item in ts.Items)
                    if (item is ToolStripButton b) b.Checked = Equals(b.Tag, type);
                _canvas.Cursor = type == AnnotationToolType.None ? Cursors.Default : Cursors.Cross;
            };
            ts.Items.Add(btn);
        }

        ts.Items.Add(new ToolStripSeparator());

        var colorBtn = new ToolStripButton("\U0001F3A8 颜色") { ForeColor = Color.White };
        colorBtn.Click += (_, _) =>
        {
            using var dlg = new ColorDialog { Color = _currentColor, FullOpen = true };
            if (dlg.ShowDialog() == DialogResult.OK) _currentColor = dlg.Color;
        };
        ts.Items.Add(colorBtn);

        var widthLabel = new ToolStripLabel("粗细:") { ForeColor = Color.LightGray };
        ts.Items.Add(widthLabel);
        var widthBox = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (var w in new[] { 1, 2, 3, 4, 6, 8 }) widthBox.Items.Add(w);
        widthBox.SelectedItem = _penWidth;
        widthBox.SelectedIndexChanged += (_, _) =>
        {
            if (widthBox.SelectedItem is int w) _penWidth = w;
        };
        ts.Items.Add(widthBox);

        ts.Items.Add(new ToolStripSeparator());

        var undoBtn = new ToolStripButton("\u21A9 撤销") { ForeColor = Color.White };
        undoBtn.Click += (_, _) =>
        {
            if (_annotations.Count > 0)
            {
                _annotations.RemoveAt(_annotations.Count - 1);
                _canvas.Invalidate();
            }
        };
        ts.Items.Add(undoBtn);

        return ts;
    }

    private Panel CreateActionBar()
    {
        var panel = new Panel
        {
            Height = 48,
            BackColor = Color.FromArgb(40, 40, 40),
            Padding = new Padding(8),
        };

        var btnConfirm = CreateActionButton("\u2714 复制", Color.FromArgb(0, 150, 80));
        btnConfirm.Click += (_, _) => { Result = EditorResult.CopyToClipboard; Close(); };

        var btnSave = CreateActionButton("\U0001F4BE 保存", Color.FromArgb(0, 120, 200));
        btnSave.Click += (_, _) => DoSaveInPlace();

        var btnCancel = CreateActionButton("\u2716 取消", Color.FromArgb(160, 40, 40));
        btnCancel.Click += (_, _) => { Result = EditorResult.Cancel; Close(); };

        btnCancel.Dock = DockStyle.Right;
        btnSave.Dock = DockStyle.Right;
        btnConfirm.Dock = DockStyle.Right;

        panel.Controls.Add(btnConfirm);
        panel.Controls.Add(btnSave);
        panel.Controls.Add(btnCancel);

        return panel;
    }

    private void DoSaveInPlace()
    {
        using var dlg = new SaveFileDialog
        {
            Title = "保存截图",
            InitialDirectory = _config.SaveDirectory,
            FileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}",
            Filter = "PNG Image|*.png|JPEG Image|*.jpg|BMP Image|*.bmp",
            FilterIndex = _config.ImageFormat.ToLower() switch
            {
                "jpg" or "jpeg" => 2,
                "bmp" => 3,
                _ => 1,
            },
        };

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            using var finalImage = GetFinalImage();
            var format = Path.GetExtension(dlg.FileName).ToLower() switch
            {
                ".jpg" or ".jpeg" => ImageFormat.Jpeg,
                ".bmp" => ImageFormat.Bmp,
                _ => ImageFormat.Png,
            };
            finalImage.Save(dlg.FileName, format);
            Result = EditorResult.Save;
            Close();
        }
        // If user cancelled the save dialog, stay in editor
    }

    private static Button CreateActionButton(string text, Color bgColor)
    {
        return new Button
        {
            Text = text,
            Width = 100,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = bgColor,
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei", 10),
            Margin = new Padding(4),
            Cursor = Cursors.Hand,
        };
    }

    private void Canvas_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var imgRect = GetImageRect();
        g.DrawImage(_originalImage, imgRect);

        var state = g.Save();
        float scaleX = (float)imgRect.Width / _originalImage.Width;
        float scaleY = (float)imgRect.Height / _originalImage.Height;
        g.TranslateTransform(imgRect.X, imgRect.Y);
        g.ScaleTransform(scaleX, scaleY);

        foreach (var item in _annotations)
            AnnotationRenderer.DrawItem(g, item, _originalImage);

        if (_isDrawing && _currentTool != AnnotationToolType.None)
        {
            var preview = CreateCurrentItem();
            if (preview != null)
                AnnotationRenderer.DrawItem(g, preview, _originalImage);
        }

        g.Restore(state);
    }

    private AnnotationItem? CreateCurrentItem()
    {
        return new AnnotationItem
        {
            Type = _currentTool,
            Color = _currentColor,
            PenWidth = _penWidth,
            Start = _drawStart,
            End = _drawCurrent,
            FontSize = _fontSize,
            FontFamily = _config.DefaultFontFamily,
            MosaicBlockSize = _config.MosaicBlockSize,
            FreeHandPoints = _freeHandPoints != null ? new List<Point>(_freeHandPoints) : null,
        };
    }

    private void Canvas_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _currentTool == AnnotationToolType.None) return;

        if (_currentTool == AnnotationToolType.Text)
        {
            var imgPt = ImagePoint(e.Location);
            var input = PromptText();
            if (!string.IsNullOrEmpty(input))
            {
                _annotations.Add(new AnnotationItem
                {
                    Type = AnnotationToolType.Text,
                    Color = _currentColor,
                    Start = imgPt,
                    Text = input,
                    FontSize = _fontSize,
                    FontFamily = _config.DefaultFontFamily,
                });
                _canvas.Invalidate();
            }
            return;
        }

        _isDrawing = true;
        _drawStart = ImagePoint(e.Location);
        _drawCurrent = _drawStart;

        if (_currentTool == AnnotationToolType.FreeHand)
            _freeHandPoints = new List<Point> { _drawStart };
    }

    private void Canvas_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_isDrawing) return;
        _drawCurrent = ImagePoint(e.Location);
        if (_currentTool == AnnotationToolType.FreeHand)
            _freeHandPoints?.Add(_drawCurrent);
        _canvas.Invalidate();
    }

    private void Canvas_MouseUp(object? sender, MouseEventArgs e)
    {
        if (!_isDrawing) return;
        _isDrawing = false;
        _drawCurrent = ImagePoint(e.Location);

        var item = CreateCurrentItem();
        if (item != null)
        {
            _annotations.Add(item);
            _canvas.Invalidate();
        }
        _freeHandPoints = null;
    }

    private string? PromptText()
    {
        using var dlg = new Form
        {
            Text = "输入文字",
            Size = new Size(350, 150),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            BackColor = Color.FromArgb(40, 40, 40),
            MaximizeBox = false,
            MinimizeBox = false,
            TopMost = true,
        };
        var tb = new TextBox
        {
            Location = new Point(12, 12),
            Size = new Size(310, 28),
            Font = new Font("Microsoft YaHei", 12),
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
        };
        var ok = new Button
        {
            Text = "确定",
            DialogResult = DialogResult.OK,
            Location = new Point(230, 55),
            Size = new Size(90, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 120, 200),
            ForeColor = Color.White,
        };
        dlg.Controls.Add(tb);
        dlg.Controls.Add(ok);
        dlg.AcceptButton = ok;
        return dlg.ShowDialog() == DialogResult.OK ? tb.Text : null;
    }

    public Bitmap GetFinalImage()
    {
        var result = (Bitmap)_originalImage.Clone();
        using var g = Graphics.FromImage(result);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        foreach (var item in _annotations)
            AnnotationRenderer.DrawItem(g, item, _originalImage);
        return result;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Result = EditorResult.Cancel;
            Close();
        }
        else if (e.KeyCode == Keys.Enter && !_isDrawing)
        {
            Result = EditorResult.CopyToClipboard;
            Close();
        }
        else if (e.Control && e.KeyCode == Keys.S)
        {
            DoSaveInPlace();
        }
        else if (e.Control && e.KeyCode == Keys.Z && _annotations.Count > 0)
        {
            _annotations.RemoveAt(_annotations.Count - 1);
            _canvas.Invalidate();
        }
        base.OnKeyDown(e);
    }
}

public class DoubleBufferedPanel : Panel
{
    public DoubleBufferedPanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }
}

public class DarkToolStripRenderer : ToolStripProfessionalRenderer
{
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Color.FromArgb(45, 45, 45));
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item is ToolStripButton btn && btn.Checked)
        {
            using var brush = new SolidBrush(Color.FromArgb(70, 130, 200));
            e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
        }
        else if (e.Item.Selected)
        {
            using var brush = new SolidBrush(Color.FromArgb(60, 60, 60));
            e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
        }
    }
}
