using System.Drawing.Drawing2D;

namespace CubeBubbles.Utilities;

public static class ScaleUtil
{
    public const float ScaleFactor = 3f;

    /// <summary>
    /// Масштабирует размер
    /// </summary>
    /// <param name="originalSize">Исходный размер</param>
    /// <returns>Масштабированный размер</returns>
    public static Size ScaleSize(Size originalSize)
    {
        return new Size((int)(originalSize.Width * ScaleFactor), (int)(originalSize.Height * ScaleFactor));
    }

    /// <summary>
    /// Масштабирует точку
    /// </summary>
    /// <param name="originalPosition">Исходная позиция</param>
    /// <returns>Масштабированная позиция</returns>
    public static Point ScalePoint(Point originalPosition)
    {
        return new Point((int)(originalPosition.X * ScaleFactor), (int)(originalPosition.Y * ScaleFactor));
    }

    /// <summary>
    /// Масштабирует целочисленное значение
    /// </summary>
    /// <param name="value">Исходное значение</param>
    /// <returns>Масштабированное значение</returns>
    public static int ScaleValue(int value)
    {
        return (int)(value * ScaleFactor);
    }

    /// <summary>
    /// Масштабирует изображение с пиксельной интерполяцией
    /// </summary>
    /// <param name="original">Исходное изображение</param>
    /// <returns>Масштабированное изображение</returns>
    public static Image ScaleImage(Image original)
    {
        var scaledSize = ScaleSize(original.Size);
        var scaledImage = new Bitmap(scaledSize.Width, scaledSize.Height);
        using var graphics = Graphics.FromImage(scaledImage);
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.DrawImage(original, 0, 0, scaledSize.Width, scaledSize.Height);
        return scaledImage;
    }
}