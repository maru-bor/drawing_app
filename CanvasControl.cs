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
    private readonly Stack<List<SKPoint>> _redoStack = new();
    private List<SKPoint> _currentStroke = null;

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
    
    public void Undo()
    {
        if (_strokes.Count == 0) return;
        var last = _strokes[_strokes.Count - 1];
        _strokes.RemoveAt(_strokes.Count - 1);
        _redoStack.Push(last);
        InvalidateVisual();
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;
        var stroke = _redoStack.Pop();
        _strokes.Add(stroke);
        InvalidateVisual();
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.White);

        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 4,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

        foreach (var stroke in _strokes)
        {
            for (int i = 1; i < stroke.Count; i++)
                canvas.DrawLine(stroke[i - 1], stroke[i], paint);
        }
    }


}