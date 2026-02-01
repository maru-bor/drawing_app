using SkiaSharp;

namespace drawing_app;

public class Stroke
{
    public List<SKPoint> Points { get; }
    public float Thickness { get; }
    public byte Opacity { get; }
    public SKColor Color { get; }

    public Stroke(List<SKPoint> points, float thickness, byte opacity, SKColor color)
    {
        Points = points;
        Thickness = thickness;
        Opacity = opacity;
        Color = color;
    }
}