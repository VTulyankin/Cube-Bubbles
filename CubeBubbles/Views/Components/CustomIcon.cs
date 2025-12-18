using System.Drawing.Imaging;
using CubeBubbles.Models;
using CubeBubbles.Utilities;

namespace CubeBubbles.Views.Components;

public static class CustomIcon
{
    /// <summary>
    /// Создает иконку из спрайта пузыря заданного цвета
    /// </summary>
    /// <param name="color">Цвет пузыря</param>
    /// <returns>Иконка для формы</returns>
    public static Icon GetIcon(BubbleColor color)
    {
        var iconPath = $"Resources/bubbles/{color.ToString().ToLower()}.png";

        using var originalBitmap = (Bitmap)Image.FromFile(iconPath);


        var whiteBg = new Bitmap(originalBitmap.Width, originalBitmap.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(whiteBg))
        {
            g.Clear(Color.White);
            g.DrawImage(originalBitmap, 0, 0, originalBitmap.Width, originalBitmap.Height);
        }

        const int maxSize = 20;
        var finalBitmap = new Bitmap(maxSize, maxSize);
        using (var g = Graphics.FromImage(finalBitmap))
        {
            g.Clear(Color.Transparent);
            g.DrawImage(whiteBg, 2, 1, whiteBg.Width, whiteBg.Height);
        }
        finalBitmap = (Bitmap)ScaleUtil.ScaleImage(finalBitmap);
        IntPtr hIcon = finalBitmap.GetHicon();
        return Icon.FromHandle(hIcon);
    }
}