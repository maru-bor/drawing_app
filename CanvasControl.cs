using System.Windows.Input;
using System.Windows.Media;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace drawing_app;

public class CanvasControl : SKElement
{
    private List<Layer> _layers = new();
    private int _activeLayerIndex = 0;
    private readonly List<float> _strokeWidths = new();
    private readonly List<byte> _strokeAlphas = new();
    private readonly Stack<(List<SKPoint>, float, byte, SKColor)> _redoStack = new();
    private readonly List<SKColor> _strokeColors = new();
    private List<SKPoint> _currentStroke = new();
    public IReadOnlyList<Layer> Layers => _layers;
    
    public float BrushThickness { get; set; } = 4f;
    public byte BrushOpacity { get; set; } = 255;
    public SKColor BrushColor { get; set; } = new SKColor(0, 0, 0);

    public CanvasControl()
    {
       
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
        SizeChanged += (_, __) => InvalidateVisual();
        Loaded += (_, __) => InitializeBaseLayer();
    }
    
    private void InitializeBaseLayer()
    {
        if (_layers.Count > 0)
            return;

        int width = (int)(ActualWidth * VisualTreeHelper.GetDpi(this).DpiScaleX);
        int height = (int)(ActualHeight * VisualTreeHelper.GetDpi(this).DpiScaleY);

        _layers.Add(new Layer(width, height, "Layer 1"));
        _activeLayerIndex = 0;
    }
    
    public int ActiveLayerIndex
    {
        get => _activeLayerIndex;
        set
        {
            if (value >= 0 && value < _layers.Count)
                _activeLayerIndex = value;
        }
    }



    private SKPoint GetMousePosition(MouseEventArgs e)
    {
        var pos = e.GetPosition(this);
        double dpiScale = VisualTreeHelper.GetDpi(this).DpiScaleX; 
        return new SKPoint((float)(pos.X * dpiScale), (float)(pos.Y * dpiScale));
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        _currentStroke = new List<SKPoint>
        {
            GetMousePosition(e)
        };
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        
        if (_currentStroke == null || e.LeftButton != MouseButtonState.Pressed)
            return;

        _currentStroke.Add(GetMousePosition(e));

        DrawStrokeOnActiveLayer(_currentStroke);
        InvalidateVisual(); 
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _currentStroke = null;
    }
    
    public void AddLayer()
    {
        var baseLayer = _layers[0];
        var newLayer = new Layer(baseLayer.Bitmap.Width, baseLayer.Bitmap.Height,
            $"Layer {_layers.Count + 1}");

        _layers.Add(newLayer);
        _activeLayerIndex = _layers.Count - 1;

        InvalidateVisual();
    }

    public void DeleteActiveLayer()
    {
        if (_layers.Count <= 1)
            return; // Never delete last layer

        _layers.RemoveAt(_activeLayerIndex);

        if (_activeLayerIndex >= _layers.Count)
            _activeLayerIndex = _layers.Count - 1;

        InvalidateVisual();
    }
    
    private void DrawStrokeOnActiveLayer(List<SKPoint> stroke)
    {
        if (stroke.Count < 2)
            return;

        var layer = _layers[_activeLayerIndex];

        using var canvas = new SKCanvas(layer.Bitmap);
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = BrushColor.WithAlpha(BrushOpacity),
            StrokeCap = SKStrokeCap.Round
        };

        DrawSmoothStroke(canvas, stroke, paint, BrushThickness);
    }
    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.White);

        foreach (var layer in _layers)
        {
            if (!layer.Visible)
                continue;

            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.White.WithAlpha((byte)(layer.Opacity * 255))
            };

            canvas.DrawBitmap(layer.Bitmap, 0, 0, paint);
        }
    }
    public void Undo()
    {
        if (_strokes.Count == 0) return;
        var lastStroke = _strokes[_strokes.Count - 1];
        var lastWidth = _strokeWidths[_strokeWidths.Count - 1];
        var lastAlpha = _strokeAlphas[_strokeAlphas.Count - 1];
        var lastColor = _strokeColors[_strokeColors.Count - 1];

        _strokeColors.RemoveAt(_strokeColors.Count - 1);
        _strokes.RemoveAt(_strokes.Count - 1);
        _strokeWidths.RemoveAt(_strokeWidths.Count - 1);
        _strokeAlphas.RemoveAt(_strokeAlphas.Count - 1);

        _redoStack.Push((lastStroke, lastWidth, lastAlpha, lastColor));
        InvalidateVisual();
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;
        var (stroke, width, alpha,color) = _redoStack.Pop();
        _strokes.Add(stroke);
        _strokeWidths.Add(width);
        _strokeAlphas.Add(alpha);
        _strokeColors.Add(color);
        InvalidateVisual();
    }
    
    private void DrawSmoothStroke(SKCanvas canvas, List<SKPoint> points, SKPaint paint, float brushSize)
    {
        if (points == null || points.Count == 0)
            return;

        float spacing = brushSize * 0.25f; 
        float radius = brushSize / 2f;

        SKPoint lastDab = points[0];
        canvas.DrawCircle(lastDab, radius, paint);

        for (int i = 1; i < points.Count; i++)
        {
            var p0 = points[i - 1];
            var p1 = points[i];

            float distance = SKPoint.Distance(p0, p1);
            int steps = Math.Max(1, (int)(distance / (spacing / 2)));

            for (int j = 0; j <= steps; j++)
            {
                float t = j / (float)steps;
                var pos = new SKPoint(
                    p0.X + (p1.X - p0.X) * t,
                    p0.Y + (p1.Y - p0.Y) * t
                );

                float dabDistance = SKPoint.Distance(lastDab, pos);

                if (dabDistance >= spacing)
                {
                    canvas.DrawCircle(pos, radius, paint);
                    lastDab = pos;
                }
            }
        }
    }


   


}