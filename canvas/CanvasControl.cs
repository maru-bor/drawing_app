using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using drawing_app.brushes;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace drawing_app.canvas;

public class CanvasControl : SKElement
{
    private ObservableCollection<Layer> _layers = new();
    private int _activeLayerIndex;
    private List<SKPoint>? _currentStroke = new();
    public ObservableCollection<Layer> Layers => _layers;

    public float BrushThickness { get; set; } = 4f;
    public byte BrushOpacity { get; set; } = 255;
    public SKColor BrushColor { get; set; } = new SKColor(0, 0, 0);
    public float BrushSpacing { get; set; } = 0.25f;
    public bool IsEraser { get; set; }
    public SKBitmap? ActiveBrushTip { get; set; }
    private SKBitmap? _livePreviewBackup;
    public bool IsColorPicker { get; set; }
    public event Action<SKColor>? ColorPicked;
    public float Zoom { get; set; } = 1f;

    public CanvasControl()
    {

        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
        SizeChanged += (_, _) => InvalidateVisual();
        Loaded += (_, _) => InitializeBaseLayer();
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

        float x = (float)(pos.X * dpiScale / Zoom);
        float y = (float)(pos.Y * dpiScale / Zoom);

        return new SKPoint(x, y);
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        _currentStroke = new List<SKPoint>
        {
            GetMousePosition(e)
        };

        if (IsColorPicker)
        {
            var point = GetMousePosition(e);

            using var merged = GetMergedBitmap();

            int x = (int)point.X;
            int y = (int)point.Y;

            if (x >= 0 && merged != null && x < merged.Width && y >= 0 && y < merged.Height)
            {
                var color = merged.GetPixel(x, y);

                if (ColorPicked != null) ColorPicked.Invoke(color);
            }

            return;
        }

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

        if (_livePreviewBackup == null)
            return;

        layer.Bitmap = _livePreviewBackup.Copy();

        using var canvas = new SKCanvas(layer.Bitmap);
        using var paint = new SKPaint();
        paint.IsAntialias = true;
        paint.StrokeCap = SKStrokeCap.Round;
        paint.Color = IsEraser
            ? SKColors.Transparent
            : BrushColor.WithAlpha(BrushOpacity);
        paint.BlendMode = IsEraser ? SKBlendMode.Clear : SKBlendMode.SrcOver;

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
        if (_layers.Count == 0)
            return;

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

    public void NewCanvas(int width, int height)
    {

        foreach (var layer in _layers)
        {
            layer.Bitmap.Dispose();
        }

        _layers.Clear();


        _layers.Add(new Layer(width, height, "Layer 1"));
        _activeLayerIndex = 0;


        var dpi = VisualTreeHelper.GetDpi(this);
        double dipWidth = width / dpi.DpiScaleX;
        double dipHeight = height / dpi.DpiScaleY;


        Width = dipWidth;
        Height = dipHeight;


        InvalidateMeasure();
        UpdateLayout();
        InvalidateVisual();
    }

    private void RebuildLayerBitmap(Layer layer)
    {
        int width = layer.Bitmap.Width;
        int height = layer.Bitmap.Height;

        layer.Bitmap = new SKBitmap(width, height);

        using var canvas = new SKCanvas(layer.Bitmap);
        canvas.Clear(SKColors.Transparent);

        foreach (var stroke in layer.UndoStack.Reverse())
        {
            using var paint = new SKPaint();
            paint.IsAntialias = true;
            paint.StrokeCap = SKStrokeCap.Round;
            paint.Color = stroke.IsEraser ? SKColors.Transparent : stroke.Color.WithAlpha(stroke.Opacity);
            paint.BlendMode = stroke.IsEraser ? SKBlendMode.Clear : SKBlendMode.SrcOver;

            DrawSmoothStroke(canvas, stroke.Points, paint, stroke.Thickness, stroke.Spacing, stroke.BrushTip);
        }
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.White);

        if (_layers.Count == 0)
            return;

        var bitmap = _layers[0].Bitmap;

        float canvasWidth = bitmap.Width * Zoom;
        float canvasHeight = bitmap.Height * Zoom;

        float offsetX = (e.Info.Width - canvasWidth) / 2f;
        float offsetY = (e.Info.Height - canvasHeight) / 2f;

        canvas.Translate(offsetX, offsetY);
        canvas.Scale(Zoom);
        
        foreach (var layer in _layers)
        {
            if (!layer.Visible)
                continue;

            using var paint = new SKPaint();
            paint.Color = new SKColor(255,255,255,(byte)(255 * layer.Opacity));

            canvas.DrawBitmap(layer.Bitmap, 0, 0, paint);
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

            using var bitmapPaint = new SKPaint();
            bitmapPaint.IsAntialias = true;
            bitmapPaint.BlendMode = paint.BlendMode;
            bitmapPaint.ColorFilter = SKColorFilter.CreateBlendMode(
                paint.Color,
                SKBlendMode.SrcIn);

            canvas.DrawBitmap(brushTip, dest, bitmapPaint);
        }
        else
        {
            canvas.DrawCircle(position, size / 2f, paint);
        }
    }


    public void DrawSmoothStroke(SKCanvas canvas, List<SKPoint>? points, SKPaint paint, float brushSize, float spacing,
        SKBitmap? brushTip = null)
    {
        if (points == null || points.Count == 0)
            return;

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

    private SKBitmap? GetMergedBitmap()
    {
        if (_layers.Count == 0)
            return null;

        int width = _layers[0].Bitmap.Width;
        int height = _layers[0].Bitmap.Height;

        var merged = new SKBitmap(width, height);

        using var canvas = new SKCanvas(merged);
        canvas.Clear(SKColors.Transparent);

        foreach (var layer in _layers)
        {
            if (!layer.Visible)
                continue;

            using var paint = new SKPaint();
            paint.Color = new SKColor(255, 255, 255, (byte)(255 * layer.Opacity));

            canvas.DrawBitmap(layer.Bitmap, 0, 0, paint);
        }

        return merged;
    }
}    