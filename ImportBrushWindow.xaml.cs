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
        _brushTip = SKBitmap.Decode(stream);

        // Preview
        PreviewImage.Source = _brushTip.ToWriteableBitmap();
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
}