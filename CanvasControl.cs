using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace drawing_app;

public class CanvasControl : SKElement
{
    private SKBitmap _canvasBitmap;
    private SKCanvas _canvas;
    private bool _isDrawing;
    private SKPoint _lastPoint;

    public CanvasControl()
    {
        Loaded += (_, _) => InitBitmap();
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
    }
    private void InitBitmap()
    {
        int pixelWidth = Math.Max(1, (int)ActualWidth);
        int pixelHeight = Math.Max(1, (int)ActualHeight);

        if (_canvasBitmap != null)
            _canvasBitmap.Dispose();

        _canvasBitmap = new SKBitmap(pixelWidth, pixelHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        _canvas = new SKCanvas(_canvasBitmap);
        _canvas.Clear(SKColors.White);
    }


    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _isDrawing = true;
            var pos = e.GetPosition(this);

            float scaleX = _canvasBitmap.Width / (float)ActualWidth;
            float scaleY = _canvasBitmap.Height / (float)ActualHeight;

            _lastPoint = new SKPoint((float)pos.X * scaleX, (float)pos.Y * scaleY);
        }
    }
    
    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Released)
        {
            _isDrawing = false;
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDrawing) return;

        var pos = e.GetPosition(this);
        float scaleX = _canvasBitmap.Width / (float)ActualWidth;
        float scaleY = _canvasBitmap.Height / (float)ActualHeight;
        var currentPoint = new SKPoint((float)pos.X * scaleX, (float)pos.Y * scaleY);

        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 4,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };
        
        _canvas.DrawLine(_lastPoint, currentPoint, paint);

        _lastPoint = currentPoint;
        InvalidateVisual();

    }
    
    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);
        if (_canvasBitmap != null)
        {
            var canvas = e.Surface.Canvas;
            canvas.DrawBitmap(_canvasBitmap, 0, 0);
        }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);

        int pixelWidth = Math.Max(1, (int)ActualWidth);
        int pixelHeight = Math.Max(1, (int)ActualHeight);

        _canvasBitmap?.Dispose();
        _canvasBitmap = new SKBitmap(pixelWidth, pixelHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        _canvas = new SKCanvas(_canvasBitmap);
        _canvas.Clear(SKColors.White);

        InvalidateVisual();
    }


}