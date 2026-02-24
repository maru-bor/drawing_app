using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SkiaSharp;

namespace drawing_app;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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
    
    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        DrawingCanvas.Undo();
    }

    private void Redo_Click(object sender, RoutedEventArgs e)
    {
        DrawingCanvas.Redo();
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
            DrawingCanvas.ActiveLayerIndex = LayerList.SelectedIndex;
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
    
    private void BrushList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BrushList.SelectedItem is BrushPreset brush)
        {
            DrawingCanvas.ApplyBrushPreset(brush);
            ThicknessSlider.Value = brush.Size;
            OpacitySlider.Value = brush.Opacity;
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
    
    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

}