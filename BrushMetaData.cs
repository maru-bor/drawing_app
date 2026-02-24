namespace drawing_app;

public class BrushMetaData
{
    public string Name { get; set; }
    public float Size { get; set; }
    public byte Opacity { get; set; }
    public float Spacing { get; set; }
    public bool IsEraser { get; set; }
    public bool IsImported { get; set; }
}