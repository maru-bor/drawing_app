using SkiaSharp;

namespace drawing_app;

public class Layer
{
    public string Name { get; set; }
    public SKBitmap Bitmap { get; set; }
    public bool Visible { get; set; } = true;
    public float Opacity { get; set; } = 1f;

    public Layer(int width, int height, string name)
    {
        Name = name;
        Bitmap = new SKBitmap(width, height, true);
    }
}