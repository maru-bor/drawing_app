using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
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
    private List<SKPoint> _currentStroke = null;
    
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
        if (_currentStroke == null || e.LeftButton != MouseButtonState.Pressed)
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
                StrokeWidth = _strokeWidths[i],
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Bevel
            };

            for (int j = 1; j < stroke.Count; j++)
                canvas.DrawLine(stroke[j - 1], stroke[j], paint);
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

   


}