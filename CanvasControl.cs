using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace drawing_app;

public class CanvasControl : SKElement
{
    private ObservableCollection<Layer> _layers = new();
    private int _activeLayerIndex = 0;
    private readonly List<float> _strokeWidths = new();
    private readonly List<byte> _strokeAlphas = new();
    private readonly Stack<SKBitmap> _undoStack = new();
    private readonly Stack<SKBitmap> _redoStack = new();
    private readonly List<SKColor> _strokeColors = new();
    private List<SKPoint> _currentStroke = new();
    public ObservableCollection<Layer> Layers => _layers;
    
    public float BrushThickness { get; set; } = 4f;
    public byte BrushOpacity { get; set; } = 255;
    public SKColor BrushColor { get; set; } = new SKColor(0, 0, 0);
    public float BrushSpacing { get; set; } = 0.25f;
    public bool IsEraser { get; set; } = false;

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
    
    public void ApplyBrushPreset(BrushPreset brush)
    {
        BrushThickness = brush.Size;
        BrushOpacity = brush.Opacity;
        BrushSpacing = brush.Spacing;
        IsEraser = brush.IsEraser;
    }
    
    private static SKBitmap CloneBitmap(SKBitmap source)
    {
        var clone = new SKBitmap(source.Info);
        source.CopyTo(clone);
        return clone;
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
        
        var activeLayer = _layers[_activeLayerIndex];
        _undoStack.Push(CloneBitmap(activeLayer.Bitmap));
        _redoStack.Clear();

        _currentStroke = new List<SKPoint>
        {
            GetMousePosition(e)
        };
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        
        if (_currentStroke == null || e.LeftButton != MouseButtonState.Pressed)
            return;

        var newPoint = GetMousePosition(e);

        _currentStroke.Add(newPoint);

        if (_currentStroke.Count >= 2)
        {
            var lastTwo = new List<SKPoint>
            {
                _currentStroke[_currentStroke.Count - 2],
                _currentStroke[_currentStroke.Count - 1]
            };

            DrawStrokeOnActiveLayer(lastTwo);
        }

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
            return; 

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
            StrokeCap = SKStrokeCap.Round,
            Color = IsEraser
                ? SKColors.Transparent
                : BrushColor.WithAlpha(BrushOpacity),
            BlendMode = IsEraser ? SKBlendMode.Clear : SKBlendMode.SrcOver
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

            canvas.DrawBitmap(layer.Bitmap, 0, 0);
        }
    }
    public void Undo()
    {
        if (_undoStack.Count == 0)
            return;

        var layer = _layers[_activeLayerIndex];

        _redoStack.Push(CloneBitmap(layer.Bitmap));

        layer.Bitmap.Dispose();
        layer.Bitmap = _undoStack.Pop();

        InvalidateVisual();
    }

    public void Redo()
    {
        if (_redoStack.Count == 0)
            return;

        var layer = _layers[_activeLayerIndex];

        _undoStack.Push(CloneBitmap(layer.Bitmap));

        layer.Bitmap.Dispose();
        layer.Bitmap = _redoStack.Pop();

        InvalidateVisual();
    }
    
    private void DrawSmoothStroke(SKCanvas canvas, List<SKPoint> points, SKPaint paint, float brushSize)
    {
        if (points == null || points.Count == 0)
            return;

        float spacing = brushSize * 0.1f;
        float radius = brushSize / 2f;

        SKPoint lastDab = points[0];
        canvas.DrawCircle(lastDab, radius, paint);

        for (int i = 1; i < points.Count; i++)
        {
            var p0 = points[i - 1];
            var p1 = points[i];

            float distance = SKPoint.Distance(p0, p1);

            int steps = Math.Max(1, (int)(distance / spacing));

            for (int j = 1; j <= steps; j++)
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