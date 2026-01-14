using System.Collections.ObjectModel;


namespace drawing_app;

public class BrushLibrary
{
    public static ObservableCollection<BrushPreset> DefaultBrushes { get; } =
        new ObservableCollection<BrushPreset>
        {
            new BrushPreset("Pencil", 2f, 255, 0.6f),
            new BrushPreset("Ink Pen", 5f, 255, 0.25f),
            new BrushPreset("Soft Brush", 20f, 80, 0.15f),
            new BrushPreset("Marker", 15f, 180, 0.35f),
            new BrushPreset("Eraser", 20f, 255, 0.25f) { IsEraser = true }
        };
}