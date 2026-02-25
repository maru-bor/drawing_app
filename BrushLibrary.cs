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
                Spacing = 0.10f,
                IsEraser = false,
                BrushTip = null,
                IsImported = false

            },

            new BrushPreset
            {
                Name = "Ink Pen",
                Size = 5f,
                Opacity = 255,
                Spacing = 0.25f,
                IsEraser = false,
                BrushTip = null,
                IsImported = false

            },

            new BrushPreset
            {
                Name = "Soft Brush",
                Size = 20f,
                Opacity = 60,
                Spacing = 0.10f,
                IsEraser = false,
                BrushTip = null,
                IsImported = false

            },

            new BrushPreset
            {
                Name = "Marker",
                Size = 15f,
                Opacity = 180,
                Spacing = 0.10f,
                IsEraser = false,
                BrushTip = null,
                IsImported = false

            },

            new BrushPreset
            {
                Name = "Eraser",
                Size = 20f,
                Opacity = 255,
                Spacing = 0.25f,
                IsEraser = true,
                BrushTip = null,
                IsImported = false
            }
        };
    public static void Add(BrushPreset brush)
    {
        if (brush == null) return;
        DefaultBrushes.Add(brush);
    }

    public static void Remove(BrushPreset brush)
    {
        if (brush == null) return;
        DefaultBrushes.Remove(brush);
    }
}