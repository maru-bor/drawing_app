using System.IO;
using System.Text.Json;
using SkiaSharp;

namespace drawing_app;

public class BrushStorage
{
     private static readonly string BrushFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DrawingApp", "Brushes");

    static BrushStorage()
    {
        Directory.CreateDirectory(BrushFolder);
    }

    public static void SaveBrush(BrushPreset brush)
    {
        if (brush.BrushTip == null) return;

        string basePath = Path.Combine(BrushFolder, brush.Name);

        using var image = SKImage.FromBitmap(brush.BrushTip);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(basePath + ".png", data.ToArray());

        var meta = new BrushMetaData
        {
            Name = brush.Name,
            Size = brush.Size,
            Opacity = brush.Opacity,
            Spacing = brush.Spacing,
            IsEraser = brush.IsEraser,
            IsImported = true
        };

        File.WriteAllText(basePath + ".json",
            JsonSerializer.Serialize(meta));
    }

    public static List<BrushPreset> LoadAll()
    {
        var list = new List<BrushPreset>();

        foreach (var json in Directory.GetFiles(BrushFolder, "*.json"))
        {
            var meta = JsonSerializer.Deserialize<BrushMetaData>(
                File.ReadAllText(json));

            string png = Path.ChangeExtension(json, ".png");

            if (!File.Exists(png)) continue;

            using var stream = File.OpenRead(png);
            var bmp = SKBitmap.Decode(stream);

            list.Add(new BrushPreset
            {
                Name = meta.Name,
                Size = meta.Size,
                Opacity = meta.Opacity,
                Spacing = meta.Spacing,
                IsEraser = meta.IsEraser,
                IsImported = true,
                BrushTip = bmp
            });
        }

        return list;
    }

    public static void DeleteBrush(BrushPreset brush)
    {
        string basePath = Path.Combine(BrushFolder, brush.Name);

        File.Delete(basePath + ".json");
        File.Delete(basePath + ".png");
    }
}