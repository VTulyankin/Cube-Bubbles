using System.Drawing.Text;

namespace CubeBubbles.Utilities;

public static class TextRenderUtil
{
    private static readonly PrivateFontCollection FontCollection = new();
    private static readonly Font CachedFont;

    static TextRenderUtil()
    {
        FontCollection.AddFontFile("Resources/fonts/Nihonium113.ttf");
        CachedFont = new Font(FontCollection.Families[0], 16, GraphicsUnit.Pixel);
    }

    /// <summary>
    /// Загружает шрифт заданного размера
    /// </summary>
    /// <param name="size">Размер шрифта</param>
    /// <returns>Шрифт</returns>
    public static Font LoadFont(float size)
    {
        return Math.Abs(size - 16) > 0 ? new Font(FontCollection.Families[0], size, GraphicsUnit.Pixel) : CachedFont;
    }

    /// <summary>
    /// Рисует текст с тенью на изображении
    /// </summary>
    /// <param name="baseImage">Исходное изображение</param>
    /// <param name="text">Текст</param>
    /// <param name="position">Позиция текста</param>
    /// <param name="textColor">Цвет текста</param>
    /// <param name="shadowColor">Цвет тени</param>
    /// <param name="shadowWidth">Ширина тени</param>
    /// <returns>Изображение с текстом</returns>
    public static Bitmap RenderText(Image baseImage, string text, Point position,
        Color textColor, Color shadowColor, int shadowWidth)
    {
        var bitmap = new Bitmap(baseImage);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
        var font = LoadFont(16);
        position = position with { Y = position.Y - 3 };
        for (var i = shadowWidth; i >= 1; i--)
        {
            var shadowPos = position with { Y = position.Y + i };
            TextRenderer.DrawText(graphics, text, font, shadowPos, shadowColor,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }
        TextRenderer.DrawText(graphics, text, font, position, textColor,
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        return bitmap;
    }

    /// <summary>
    /// Рисует текст с тенью на графическом контексте с масштабированием
    /// </summary>
    /// <param name="graphics">Графический контекст</param>
    /// <param name="text">Текст</param>
    /// <param name="position">Позиция текста</param>
    /// <param name="textColor">Цвет текста</param>
    /// <param name="shadowColor">Цвет тени</param>
    /// <param name="shadowWidth">Ширина тени</param>
    public static void RenderText(Graphics graphics, string text, Point position,
        Color textColor, Color shadowColor, int shadowWidth)
    {
        graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
        var font = LoadFont(ScaleUtil.ScaleValue(16));
        position = ScaleUtil.ScalePoint(position);
        for (var i = shadowWidth; i >= 1; i--)
        {
            var shadowPos = position with { Y = position.Y + ScaleUtil.ScaleValue(i) };
            TextRenderer.DrawText(graphics, text, font, shadowPos, shadowColor,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }
        TextRenderer.DrawText(graphics, text, font, position, textColor,
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
    }

    /// <summary>
    /// Измеряет размер текста
    /// </summary>
    /// <param name="text">Текст для измерения</param>
    /// <returns>Размер текста</returns>
    public static Size MeasureText(string text)
    {
        var font = LoadFont(16);
        var size = TextRenderer.MeasureText(text, font, Size.Empty,
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        return size with { Width = size.Width - 1 };
    }
}
