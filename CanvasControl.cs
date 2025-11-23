using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace drawing_app;

public class CanvasControl : SKElement
{
    private SKBitmap _canvasBitmap;
    private SKCanvas _canvas;
    private bool _isDrawing;
    private SKPoint _lastPoint;

    private void InitBitmap()
    {
        var pixelWidth = (int)ActualWidth;
        var pixelHeight = (int)ActualHeight;

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
            _lastPoint = new SKPoint((float)pos.X, (float)pos.Y);
        }
    }
    
    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Released)
        {
            _isDrawing = false;
        }
    }
    
    public CanvasControl()
    {
        Loaded += (_, _) => InitBitmap();
    }
}