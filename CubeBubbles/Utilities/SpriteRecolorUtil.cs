namespace CubeBubbles.Utilities;

public static class SpriteRecolorUtil
{
    /// <summary>
    /// Перекрашивает изображение в заданный цвет с сохранением яркости
    /// </summary>
    /// <param name="source">Исходное изображение</param>
    /// <param name="baseColor">Целевой базовый цвет</param>
    /// <returns>Перекрашенное изображение</returns>
    public static Image RecolorImage(Image source, Color baseColor)
    {
        var bitmap = new Bitmap(source);
        for (var x = 1; x < bitmap.Width - 1; x++)
            for (var y = 1; y < bitmap.Height - 1; y++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.A == 0) continue;
                var brightness = pixel.GetBrightness();
                var newColor = ApplyBrightnessToColor(baseColor, brightness, pixel.A);
                bitmap.SetPixel(x, y, newColor);
            }

        return bitmap;
    }

    /// <summary>
    /// Затемняет цвет на заданный процент
    /// </summary>
    /// <param name="color">Исходный цвет</param>
    /// <param name="factor">Коэффициент затемнения от 0 до 1</param>
    /// <returns>Затемненный цвет</returns>
    public static Color DarkenColor(Color color, float factor)
    {
        return Color.FromArgb(
            Math.Max(0, (int)(color.R * (1 - factor))),
            Math.Max(0, (int)(color.G * (1 - factor))),
            Math.Max(0, (int)(color.B * (1 - factor))));
    }

    /// <summary>
    /// Конвертирует HEX-строку в цвет
    /// </summary>
    /// <param name="hex">HEX-код цвета</param>
    /// <returns>Цвет</returns>
    public static Color HexToColor(string hex)
    {
        hex = hex.TrimStart('#');
        return Color.FromArgb(
            int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
            int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
            int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber));
    }

    /// <summary>
    /// Применяет яркость к базовому цвету
    /// </summary>
    /// <param name="baseColor">Базовый цвет</param>
    /// <param name="brightness">Яркость от 0 до 1</param>
    /// <param name="alpha">Прозрачность</param>
    /// <returns>Цвет с примененной яркостью</returns>
    private static Color ApplyBrightnessToColor(Color baseColor, float brightness, byte alpha)
    {
        return Color.FromArgb(alpha,
            (int)(baseColor.R * brightness),
            (int)(baseColor.G * brightness),
            (int)(baseColor.B * brightness));
    }
}
