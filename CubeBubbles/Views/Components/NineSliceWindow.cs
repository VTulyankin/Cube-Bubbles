using CubeBubbles.Utilities;

namespace CubeBubbles.Views.Components;

public static class NineSliceWindow
{
    /// <summary>
    /// Создает окно с nine-slice масштабированием и текстом
    /// </summary>
    /// <param name="size">Размер окна</param>
    /// <param name="location">Позиция окна</param>
    /// <param name="text">Текст заголовка</param>
    /// <param name="backgroundImagePath">Путь к фоновому изображению</param>
    /// <returns>PictureBox с окном</returns>
    public static PictureBox CreateWindow(Size size, Point location,
        string? text = null, string backgroundImagePath = "Resources/windows/background.png")
    {
        var sourceImage = Image.FromFile(backgroundImagePath);
        var image = NineSliceUtil.CreateNineSlice(sourceImage, size, 3, 3, 3, 6);
        sourceImage.Dispose();
        if (!string.IsNullOrEmpty(text))
        {
            using var font = TextRenderUtil.LoadFont(16);
            var textSize = TextRenderUtil.MeasureText(text);
            var textImage = TextRenderUtil.RenderText(image, text,
                new Point((size.Width - textSize.Width) / 2, 8),
                Color.Black, Color.Transparent, 0);
            image.Dispose();
            image = textImage;
        }

        var scaledImage = ScaleUtil.ScaleImage(image);
        var scaledLocation = ScaleUtil.ScalePoint(location);
        var scaledSize = ScaleUtil.ScaleSize(size);
        return new PictureBox
        {
            Image = scaledImage,
            Location = scaledLocation,
            Size = scaledSize,
            BackColor = Color.Transparent
        };
    }
}