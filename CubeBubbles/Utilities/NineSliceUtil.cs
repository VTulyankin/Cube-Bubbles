using System.Drawing.Drawing2D;

namespace CubeBubbles.Utilities;

public static class NineSliceUtil
{
    /// <summary>
    /// Создает изображение nine-slice масштабированием исходного изображения
    /// </summary>
    /// <param name="source">Исходное изображение</param>
    /// <param name="destinationSize">Целевой размер</param>
    /// <param name="leftWidth">Ширина левого края</param>
    /// <param name="rightWidth">Ширина правого края</param>
    /// <param name="topHeight">Высота верхнего края</param>
    /// <param name="bottomHeight">Высота нижнего края</param>
    /// <returns>Масштабированное изображение</returns>
    public static Image CreateNineSlice(Image source, Size destinationSize,
        int leftWidth, int rightWidth, int topHeight, int bottomHeight)
    {
        var result = new Bitmap(destinationSize.Width, destinationSize.Height);
        using var graphics = Graphics.FromImage(result);
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;

        var middleWidth = destinationSize.Width - leftWidth - rightWidth;
        var middleHeight = destinationSize.Height - topHeight - bottomHeight;

        // 1. Верхний левый угол
        graphics.DrawImage(source,
            new Rectangle(0, 0, leftWidth, topHeight),
            new Rectangle(0, 0, leftWidth, topHeight),
            GraphicsUnit.Pixel);

        // 2. Верхняя средняя часть
        graphics.DrawImage(source,
            new Rectangle(leftWidth, 0, middleWidth, topHeight),
            new Rectangle(leftWidth, 0, source.Width - leftWidth - rightWidth, topHeight),
            GraphicsUnit.Pixel);

        // 3. Верхний правый угол
        graphics.DrawImage(source,
            new Rectangle(leftWidth + middleWidth, 0, rightWidth, topHeight),
            new Rectangle(source.Width - rightWidth, 0, rightWidth, topHeight),
            GraphicsUnit.Pixel);

        // 4. Средняя левая часть
        graphics.DrawImage(source,
            new Rectangle(0, topHeight, leftWidth, middleHeight),
            new Rectangle(0, topHeight, leftWidth, source.Height - topHeight - bottomHeight),
            GraphicsUnit.Pixel);

        // 5. Центральная часть
        graphics.DrawImage(source,
            new Rectangle(leftWidth, topHeight, middleWidth, middleHeight),
            new Rectangle(leftWidth, topHeight, source.Width - leftWidth - rightWidth, source.Height - topHeight - bottomHeight),
            GraphicsUnit.Pixel);

        // 6. Средняя правая часть
        graphics.DrawImage(source,
            new Rectangle(leftWidth + middleWidth, topHeight, rightWidth, middleHeight),
            new Rectangle(source.Width - rightWidth, topHeight, rightWidth, source.Height - topHeight - bottomHeight),
            GraphicsUnit.Pixel);

        // 7. Нижний левый угол
        graphics.DrawImage(source,
            new Rectangle(0, topHeight + middleHeight, leftWidth, bottomHeight),
            new Rectangle(0, source.Height - bottomHeight, leftWidth, bottomHeight),
            GraphicsUnit.Pixel);

        // 8. Нижняя средняя часть
        graphics.DrawImage(source,
            new Rectangle(leftWidth, topHeight + middleHeight, middleWidth, bottomHeight),
            new Rectangle(leftWidth, source.Height - bottomHeight, source.Width - leftWidth - rightWidth, bottomHeight),
            GraphicsUnit.Pixel);

        // 9. Нижний правый угол
        graphics.DrawImage(source,
            new Rectangle(leftWidth + middleWidth, topHeight + middleHeight, rightWidth, bottomHeight),
            new Rectangle(source.Width - rightWidth, source.Height - bottomHeight, rightWidth, bottomHeight),
            GraphicsUnit.Pixel);

        return result;
    }
}
