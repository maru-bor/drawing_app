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
    public MainWindow()
    {
        InitializeComponent();
        
        UndoCommand = new RelayCommand(_ => DrawingCanvas.Undo());
        RedoCommand = new RelayCommand(_ => DrawingCanvas.Redo());
        
        DataContext = this;
        
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
    }
    
    private void PickColor_Click(object sender, RoutedEventArgs e)
    {
        DrawingCanvas.IsColorPicker = true;
    }
    
    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

}