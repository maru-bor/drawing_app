using System.Collections.ObjectModel;


namespace drawing_app;

public class BrushLibrary
{
    public static ObservableCollection<BrushPreset> DefaultBrushes { get; } =
        new ObservableCollection<BrushPreset>
        {
            new BrushPreset
            {
                Name = "Pencil",
                Size = 2f,
                Opacity = 255,
                Spacing = 0.6f,
                IsEraser = false,
                BrushTip = null
            },

            new BrushPreset
            {
                Name = "Ink Pen",
                Size = 5f,
                Opacity = 255,
                Spacing = 0.25f,
                IsEraser = false,
                BrushTip = null
            },

            new BrushPreset
            {
                Name = "Soft Brush",
                Size = 20f,
                Opacity = 80,
                Spacing = 0.15f,
                IsEraser = false,
                BrushTip = null
            },

            new BrushPreset
            {
                Name = "Marker",
                Size = 15f,
                Opacity = 180,
                Spacing = 0.35f,
                IsEraser = false,
                BrushTip = null
            },

            new BrushPreset
            {
                Name = "Eraser",
                Size = 20f,
                Opacity = 255,
                Spacing = 0.25f,
                IsEraser = true,
                BrushTip = null
            }
        };
    public static void Add(BrushPreset brush)
    {
        if (brush == null) return;
        DefaultBrushes.Add(brush);
    }
}