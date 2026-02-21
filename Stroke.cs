using SkiaSharp;

namespace drawing_app;

public class Stroke
{
    public List<SKPoint> Points { get; }
    public float Thickness { get; }
    public byte Opacity { get; }
    public SKColor Color { get; }
    public bool IsEraser { get; }
    public float Spacing { get; }
    public SKBitmap? BrushTip { get; }



    public Stroke(List<SKPoint> points, float thickness, byte opacity, SKColor color, bool isEraser, float spacing, SKBitmap? brushTip)
    {
        Points = points;
        Thickness = thickness;
        Opacity = opacity;
        Color = color;
        IsEraser = isEraser;
        Spacing = spacing;
        BrushTip = brushTip;
    }
}