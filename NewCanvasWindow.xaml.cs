using System.Windows;

namespace drawing_app;

public partial class NewCanvasWindow : Window
{
    public int CanvasWidth { get; private set; }
    public int CanvasHeight { get; private set; }

    public NewCanvasWindow()
    {
        InitializeComponent();
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(WidthBox.Text, out int w) &&
            int.TryParse(HeightBox.Text, out int h) &&
            w > 0 && h > 0)
        {
            CanvasWidth = w;
            CanvasHeight = h;
            DialogResult = true;
        }
        else
        {
            MessageBox.Show("Enter valid numbers.");
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}