using System.Windows.Input;
using System.Windows.Media;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace drawing_app;

public class CanvasControl : SKElement
{
    private readonly List<List<SKPoint>> _strokes = new();
    private readonly List<float> _strokeWidths = new();
    private readonly List<byte> _strokeAlphas = new();
    private readonly Stack<(List<SKPoint>, float, byte)> _redoStack = new();
    private List<SKPoint> _currentStroke = new();
    
    public float BrushThickness { get; set; } = 4f;
    public byte BrushOpacity { get; set; } = 255;

    public CanvasControl()
    {
       
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
        SizeChanged += (_, __) => InvalidateVisual();
    }

    private SKPoint GetMousePosition(MouseEventArgs e)
    {
        var pos = e.GetPosition(this);
        double dpiScale = VisualTreeHelper.GetDpi(this).DpiScaleX; 
        return new SKPoint((float)(pos.X * dpiScale), (float)(pos.Y * dpiScale));
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _currentStroke = new List<SKPoint> { GetMousePosition(e) };
            _strokes.Add(_currentStroke);
            _strokeWidths.Add(BrushThickness);
            _strokeAlphas.Add(BrushOpacity);
            _redoStack.Clear();
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        _currentStroke.Add(GetMousePosition(e));
        InvalidateVisual(); 
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _currentStroke = null;
    }
    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.White);

        for (int i = 0; i < _strokes.Count; i++)
        {
            var stroke = _strokes[i];
            using var paint = new SKPaint
            {
                Color = new SKColor(0, 0, 0, _strokeAlphas[i]),
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round,
            };

            float brushSize = _strokeWidths[i];

            DrawSmoothStroke(canvas, stroke, paint, brushSize);
        }
    }
    public void Undo()
    {
        if (_strokes.Count == 0) return;
        var lastStroke = _strokes[_strokes.Count - 1];
        var lastWidth = _strokeWidths[_strokeWidths.Count - 1];
        var lastAlpha = _strokeAlphas[_strokeAlphas.Count - 1];

        _strokes.RemoveAt(_strokes.Count - 1);
        _strokeWidths.RemoveAt(_strokeWidths.Count - 1);
        _strokeAlphas.RemoveAt(_strokeAlphas.Count - 1);

        _redoStack.Push((lastStroke, lastWidth, lastAlpha));
        InvalidateVisual();
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;
        var (stroke, width, alpha) = _redoStack.Pop();
        _strokes.Add(stroke);
        _strokeWidths.Add(width);
        _strokeAlphas.Add(alpha);
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