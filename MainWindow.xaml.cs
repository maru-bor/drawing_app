using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using SkiaSharp;
using Xceed.Wpf.Toolkit;
using MessageBox = System.Windows.MessageBox;
using WindowState = System.Windows.WindowState;

namespace drawing_app;
public partial class MainWindow : Window
{
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    
    public double CurrentZoom => ZoomSlider?.Value ?? 1.0;
    
    private Point _dragStartPoint;
    private Layer _draggedLayer;
    
    private string? _currentFilePath;
    public MainWindow()
    {
        InitializeComponent();
        
        UndoCommand = new RelayCommand(_ => DrawingCanvas.Undo());
        RedoCommand = new RelayCommand(_ => DrawingCanvas.Redo());
        
        DataContext = this;
        
        Closing += MainWindow_Closing;
        InputBindings.Add(new KeyBinding(UndoCommand, new KeyGesture(Key.Z, ModifierKeys.Control)));
        InputBindings.Add(new KeyBinding(RedoCommand, new KeyGesture(Key.Y, ModifierKeys.Control)));
        
        DrawingCanvas.ColorPicked += OnColorPicked;
        Loaded += (_, __) =>
        {
            LayerList.ItemsSource = DrawingCanvas.Layers;
            LayerList.SelectedIndex = DrawingCanvas.ActiveLayerIndex;
            
            BrushList.ItemsSource = BrushLibrary.DefaultBrushes;
            foreach (var imported in BrushStorage.LoadAll())
            {
                BrushLibrary.Add(imported);
            }

            if (BrushList.Items.Count > 0)
                BrushList.SelectedIndex = 0;
        };
        WindowState = WindowState.Maximized;
    }
    
    private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DrawingCanvas != null)
            DrawingCanvas.BrushThickness = (float)e.NewValue;
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DrawingCanvas != null)
            DrawingCanvas.BrushOpacity = (byte)e.NewValue;
    }
    
    private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
    {
        if (e.NewValue.HasValue)
            DrawingCanvas.BrushColor = new SKColor(e.NewValue.Value.R, e.NewValue.Value.G, e.NewValue.Value.B);
    }
    
    private void LayerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LayerList.SelectedIndex >= 0)
        {
            DrawingCanvas.ActiveLayerIndex = LayerList.SelectedIndex;

            if (LayerList.SelectedItem is Layer layer)
                LayerOpacitySlider.Value = layer.Opacity;
        }
    }
    
    private void LayerList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);

        var item = ItemsControl.ContainerFromElement(LayerList, e.OriginalSource as DependencyObject) as ListBoxItem;
        if (item != null)
        {
            _draggedLayer = item.DataContext as Layer;
        }
    }
    
    private void LayerList_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _draggedLayer == null)
            return;

        var currentPosition = e.GetPosition(null);

        if (Math.Abs(currentPosition.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(currentPosition.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            DragDrop.DoDragDrop(LayerList, _draggedLayer, DragDropEffects.Move);
        }
    }
    
    private void LayerList_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(Layer)))
            return;

        var droppedLayer = e.Data.GetData(typeof(Layer)) as Layer;

        var targetItem = ItemsControl.ContainerFromElement(LayerList, e.OriginalSource as DependencyObject) as ListBoxItem;
        if (targetItem == null)
            return;

        var targetLayer = targetItem.DataContext as Layer;

        var layers = DrawingCanvas.Layers;

        int oldIndex = layers.IndexOf(droppedLayer);
        int newIndex = layers.IndexOf(targetLayer);

        if (oldIndex != newIndex)
        {
            layers.Move(oldIndex, newIndex);
            LayerList.SelectedIndex = newIndex;

            // IMPORTANT: keep active layer in sync
            DrawingCanvas.ActiveLayerIndex = newIndex;
            DrawingCanvas.InvalidateVisual();
        }
    }
    
    private void BrushList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BrushList.SelectedItem is BrushPreset brush)
        {
            DrawingCanvas.ApplyBrushPreset(brush);
            ThicknessSlider.Value = brush.Size;
            OpacitySlider.Value = brush.Opacity;
        }
           
    }
    
    private void AddLayer_Click(object sender, RoutedEventArgs e)
    {
        DrawingCanvas.AddLayer();
        LayerList.SelectedIndex = DrawingCanvas.ActiveLayerIndex;
    }

    private void DeleteLayer_Click(object sender, RoutedEventArgs e)
    {
        DrawingCanvas.DeleteActiveLayer();
        LayerList.SelectedIndex = DrawingCanvas.ActiveLayerIndex;
    }
    
    private void LayerOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LayerList.SelectedItem is Layer layer)
        {
            layer.Opacity = (float)e.NewValue;
            DrawingCanvas.InvalidateVisual();
        }
    }
    
    
    
    private void ImportBrush_Click(object sender, RoutedEventArgs e)
    {
        var window = new ImportBrushWindow
        {
            Owner = this
        };

        if (window.ShowDialog() == true && window.ResultBrush != null)
        {
            BrushLibrary.Add(window.ResultBrush);
        }
    }
    
    private void DeleteBrush_Click(object sender, RoutedEventArgs e)
    {
        if (BrushList.SelectedItem is not BrushPreset brush)
            return;

        if (!brush.IsImported)
        {
            MessageBox.Show("Default brushes cannot be deleted.");
            return;
        }

        BrushStorage.DeleteBrush(brush);
        BrushLibrary.Remove(brush);
    }
    
    private void OnColorPicked(SKColor color)
    {
        DrawingCanvas.BrushColor = color;

        ColorPicker.SelectedColor = Color.FromArgb(
            color.Alpha, color.Red, color.Green, color.Blue);

        DrawingCanvas.IsColorPicker = false; 
        Mouse.OverrideCursor = null;
    }
    
    private void PickColor_Click(object sender, RoutedEventArgs e)
    {
        DrawingCanvas.IsColorPicker = true;
        Mouse.OverrideCursor = Cursors.Cross;
    }
    
    private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (CanvasScaleTransform != null)
        {
            CanvasScaleTransform.ScaleX = e.NewValue;
            CanvasScaleTransform.ScaleY = e.NewValue;
            
            CanvasScaleTransform.CenterX = DrawingCanvas.Width / 2;
            CanvasScaleTransform.CenterY = DrawingCanvas.Height / 2;
        }
    }
    
    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        ZoomSlider.Value = Math.Min(ZoomSlider.Value + 0.1, ZoomSlider.Maximum);
    }
    
    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        ZoomSlider.Value = Math.Max(ZoomSlider.Value - 0.1, ZoomSlider.Minimum);
    }
    private void Save_Click(object? sender, RoutedEventArgs? e)
    {
        if (!string.IsNullOrEmpty(_currentFilePath))
        {
            SaveCanvasToFile(_currentFilePath);
        }
        else
        {
            SaveAs_Click(sender, e);
        }
    }

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp",
            DefaultExt = "png",
            FileName = "drawing.png"
        };

        if (dlg.ShowDialog() == true)
        {
            _currentFilePath = dlg.FileName;
            SaveCanvasToFile(_currentFilePath);
        }
    }

   
    private void SaveCanvasToFile(string path)
    {
        if (DrawingCanvas.Layers.Count == 0)
            return;

        int width = DrawingCanvas.Layers[0].Bitmap.Width;
        int height = DrawingCanvas.Layers[0].Bitmap.Height;

        using var combinedBitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(combinedBitmap);
        canvas.Clear(SKColors.White);

        foreach (var layer in DrawingCanvas.Layers)
        {
            if (!layer.Visible) 
                continue;

            using var paint = new SKPaint
            {
                Color = new SKColor(255, 255, 255, (byte)(255 * layer.Opacity))
            };

            canvas.DrawBitmap(layer.Bitmap, 0, 0, paint);
        }

        using var image = SKImage.FromBitmap(combinedBitmap);
        using var data = image.Encode(GetSkEncodedImageFormat(path), 100);

        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);
    }
    
    private SKEncodedImageFormat GetSkEncodedImageFormat(string path)
    {
        var ext = Path.GetExtension(path).ToLower();
        return ext switch
        {
            ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
            ".bmp" => SKEncodedImageFormat.Bmp,
            _ => SKEncodedImageFormat.Png,
        };
    }
    
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        var result = MessageBox.Show(
            "Do you want to save your drawing before exiting?",
            "Exit",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            Save_Click(null, null);
        }
        else if (result == MessageBoxResult.Cancel)
        {
            e.Cancel = true;
        }
    }
    
    private void NewCanvas_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Do you want to save the current drawing?",
            "New Canvas",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Cancel)
            return;

        if (result == MessageBoxResult.Yes)
            Save_Click(null, null);
        
        
        const int defaultWidth = 1120;
        const int defaultHeight = 810;

        DrawingCanvas.NewCanvas(defaultWidth, defaultHeight);

        LayerList.SelectedIndex = 0;

        ZoomSlider.Value = 1.0;
        _currentFilePath = null;
    }
    
    
    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

}