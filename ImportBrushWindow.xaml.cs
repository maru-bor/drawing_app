using System.IO;
using System.Windows;
using Microsoft.Win32;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace drawing_app;

public partial class ImportBrushWindow : Window
{
    public BrushPreset? ResultBrush { get; private set; }

    private SKBitmap? _brushTip;
    
    public ImportBrushWindow()
    {
        InitializeComponent();
    }
    
    private void OnChooseImage(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp"
        };

        if (dialog.ShowDialog() != true)
            return;

        using var stream = File.OpenRead(dialog.FileName);

        var original = SKBitmap.Decode(stream);

        if (original == null)
            return;

        var resized = ResizeBrush(original);

        var mask = ConvertToMask(resized);

        _brushTip = mask;

        PreviewImage.Source = CreatePreviewBitmap(_brushTip).ToWriteableBitmap();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            MessageBox.Show("Brush name is required.");
            return;
        }

        ResultBrush = new BrushPreset
        {
            Name = NameBox.Text,
            Size = (float)SizeSlider.Value,
            Opacity = (byte)OpacitySlider.Value,
            Spacing = (float)SpacingSlider.Value,
            BrushTip = _brushTip
        };

        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        Close();
    }
    
    private SKBitmap ResizeBrush(SKBitmap original)
    {
        const int MAX_SIZE = 256;

        float scale = Math.Min(
            MAX_SIZE / (float)original.Width,
            MAX_SIZE / (float)original.Height);

        if (scale >= 1f)
            return original.Copy();

        int newW = (int)(original.Width * scale);
        int newH = (int)(original.Height * scale);

        var resized = new SKBitmap(newW, newH);
        original.ScalePixels(resized, SKFilterQuality.High);

        return resized;
    }
    
    private SKBitmap ConvertToMask(SKBitmap bmp)
    {
        if (bmp.ColorType == SKColorType.Bgra8888 || bmp.ColorType == SKColorType.Rgba8888)
        {
            return bmp.Copy();
        }

        var mask = new SKBitmap(bmp.Width, bmp.Height);

        for (int y = 0; y < bmp.Height; y++)
        {
            for (int x = 0; x < bmp.Width; x++)
            {
                var c = bmp.GetPixel(x, y);

                byte alpha = (byte)(255 - ((c.Red + c.Green + c.Blue) / 3));

                mask.SetPixel(x, y, new SKColor(255, 255, 255, alpha));
            }
        }

        return mask;
    }
    
    private SKBitmap CreatePreviewBitmap(SKBitmap mask)
    {
        var preview = new SKBitmap(mask.Width, mask.Height);

        using var canvas = new SKCanvas(preview);
        canvas.Clear(SKColors.Transparent);

        using var paint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateBlendMode(
                SKColors.Black,  
                SKBlendMode.SrcIn)
        };

        canvas.DrawBitmap(mask, 0, 0, paint);

        return preview;
    }
}