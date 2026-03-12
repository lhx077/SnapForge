using System.Drawing;

namespace ScreenshotPlugin.GUI.Tools;

public enum AnnotationToolType
{
    None,
    Arrow,
    Rectangle,
    Ellipse,
    Line,
    Text,
    Mosaic,
    FreeHand
}

public abstract class AnnotationTool
{
    public Color Color { get; set; } = Color.Red;
    public int PenWidth { get; set; } = 2;
    public abstract void OnMouseDown(Point p);
    public abstract void OnMouseMove(Point p);
    public abstract void OnMouseUp(Point p);
    public abstract void Draw(Graphics g);
    public abstract bool IsComplete { get; }
}

public class AnnotationItem
{
    public AnnotationToolType Type { get; set; }
    public Color Color { get; set; }
    public int PenWidth { get; set; }
    public Point Start { get; set; }
    public Point End { get; set; }
    public string? Text { get; set; }
    public int FontSize { get; set; } = 16;
    public string FontFamily { get; set; } = "Microsoft YaHei";
    public List<Point>? FreeHandPoints { get; set; }
    public int MosaicBlockSize { get; set; } = 12;
}
