using SkiaSharp;

namespace drawing_app;

public class BrushPreset
{
    public string Name { get; set; }

    public float Size { get; set; }
    public byte Opacity { get; set; }
    public float Spacing { get; set; }

    public bool IsEraser { get; set; } = false;
    public SKBitmap? BrushTip { get; set; }

    public BrushPreset()
    {
    }

    public BrushPreset(string name, float size, byte opacity, float spacing, SKBitmap? brushTip)
    {
        Name = name;
        Size = size;
        Opacity = opacity;
        Spacing = spacing;
        BrushTip = brushTip;
    }
}