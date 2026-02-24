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
    private int _activeLayerIndex;
    private List<SKPoint> _currentStroke = new();
    public ObservableCollection<Layer> Layers => _layers;
    
    public float BrushThickness { get; set; } = 4f;
    public byte BrushOpacity { get; set; } = 255;
    public SKColor BrushColor { get; set; } = new SKColor(0, 0, 0);
    public float BrushSpacing { get; set; } = 0.25f;
    public bool IsEraser { get; set; } = false;
    public SKBitmap? ActiveBrushTip { get; set; }
    private SKBitmap? _livePreviewBackup;

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
        ActiveBrushTip = brush.BrushTip;
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

        var layer = _layers[_activeLayerIndex];
        _livePreviewBackup?.Dispose();
        _livePreviewBackup = layer.Bitmap.Copy();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        
        if (_currentStroke == null || e.LeftButton != MouseButtonState.Pressed)
            return;

        var newPoint = GetMousePosition(e);
        _currentStroke.Add(newPoint);

        var layer = _layers[_activeLayerIndex];

        layer.Bitmap.Dispose();
        layer.Bitmap = _livePreviewBackup!.Copy();

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

        DrawSmoothStroke(canvas, _currentStroke, paint, BrushThickness, BrushSpacing, ActiveBrushTip);

        InvalidateVisual();
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_currentStroke == null || _currentStroke.Count <= 1)
        {
            _currentStroke = null;
            return;
        }

        var layer = _layers[_activeLayerIndex];

        layer.UndoStack.Push(new Stroke(
            new List<SKPoint>(_currentStroke),
            BrushThickness,
            BrushOpacity,
            BrushColor,
            IsEraser,
            BrushSpacing,
            ActiveBrushTip?.Copy()
        ));

        layer.RedoStack.Clear();

        _currentStroke = null;

        _livePreviewBackup?.Dispose();
        _livePreviewBackup = null;
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
    
    private void RebuildLayerBitmap(Layer layer)
    {
        int width = layer.Bitmap.Width;
        int height = layer.Bitmap.Height;

        layer.Bitmap.Dispose();
        layer.Bitmap = new SKBitmap(width, height);

        using var canvas = new SKCanvas(layer.Bitmap);
        canvas.Clear(SKColors.Transparent);

        foreach (var stroke in layer.UndoStack.Reverse())
        {
            using var paint = new SKPaint
            {
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round,
                Color = stroke.IsEraser ? SKColors.Transparent : stroke.Color.WithAlpha(stroke.Opacity),
                BlendMode = stroke.IsEraser ? SKBlendMode.Clear : SKBlendMode.SrcOver
            };

            DrawSmoothStroke(canvas, stroke.Points, paint, stroke.Thickness, stroke.Spacing, stroke.BrushTip);
        }
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

        DrawSmoothStroke(canvas, stroke, paint, BrushThickness, BrushSpacing, ActiveBrushTip);
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
        var layer = _layers[_activeLayerIndex];

        if (layer.UndoStack.Count == 0)
            return;

        layer.RedoStack.Push(layer.UndoStack.Pop());
        RebuildLayerBitmap(layer);
        InvalidateVisual();
    }

    

    public void Redo()
    {
        var layer = _layers[_activeLayerIndex];

        if (layer.RedoStack.Count == 0)
            return;

        layer.UndoStack.Push(layer.RedoStack.Pop());
        RebuildLayerBitmap(layer);
        InvalidateVisual();
    }
    
    private void DrawDab(SKCanvas canvas, SKPoint position, SKPaint paint, float size, SKBitmap? brushTip)
    {
        if (brushTip != null)
        {
            float half = size / 2f;

            var dest = new SKRect(
                position.X - half,
                position.Y - half,
                position.X + half,
                position.Y + half
            );

            using var bitmapPaint = new SKPaint
            {
                IsAntialias = true,
                BlendMode = paint.BlendMode,
                ColorFilter = SKColorFilter.CreateBlendMode(
                    paint.Color,
                    SKBlendMode.SrcIn)
            };

            canvas.DrawBitmap(brushTip, dest, bitmapPaint);
        }
        else
        {
            canvas.DrawCircle(position, size / 2f, paint);
        }
    }
    
    
    public void DrawSmoothStroke(SKCanvas canvas, List<SKPoint> points, SKPaint paint, float brushSize, float spacing, SKBitmap? brushTip = null)
    {
        if (points == null || points.Count == 0)
            return;

        float radius = brushSize / 2f;
        SKPoint lastDab = points[0];

        float actualSpacing = brushSize * spacing;
        DrawDab(canvas, lastDab, paint, brushSize, brushTip);

        for (int i = 1; i < points.Count; i++)
        {
            var p0 = points[i - 1];
            var p1 = points[i];

            float distance = SKPoint.Distance(p0, p1);
            int steps = Math.Max(1, (int)(distance / actualSpacing));

            for (int j = 1; j <= steps; j++)
            {
                float t = j / (float)steps;
                var pos = new SKPoint(
                    p0.X + (p1.X - p0.X) * t,
                    p0.Y + (p1.Y - p0.Y) * t
                );

                if (SKPoint.Distance(lastDab, pos) >= actualSpacing)
                {
                    DrawDab(canvas, pos, paint, brushSize, brushTip);
                    lastDab = pos;
                }
            }
        }
    }
}