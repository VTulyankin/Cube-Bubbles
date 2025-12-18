using CubeBubbles.Utilities;

namespace CubeBubbles.Views.Components;

public class NineSliceButton(string text, string hexColor, Size size, Point location)
    : CustomButton(CreateButtonImages(text, hexColor, size), location)
{
    private static readonly Image NormalSource = Image.FromFile("Resources/buttons/button.png");
    private static readonly Image PressedSource = Image.FromFile("Resources/buttons/button_pressed.png");
    private static readonly Image HighlightedSource = Image.FromFile("Resources/buttons/button_highlighted.png");

    /// <summary>
    /// Создает набор изображений кнопки с текстом и цветом
    /// </summary>
    /// <param name="text">Текст кнопки</param>
    /// <param name="hexColor">Цвет кнопки в HEX</param>
    /// <param name="size">Размер кнопки</param>
    /// <returns>Кортеж из трех изображений состояний кнопки</returns>
    private static (Image normal, Image pressed, Image highlighted) CreateButtonImages(string text, string hexColor, Size size)
    {
        var primaryColor = SpriteRecolorUtil.HexToColor(hexColor);
        var shadowColor = SpriteRecolorUtil.DarkenColor(primaryColor, 0.5f);
        var recoloredNormal = SpriteRecolorUtil.RecolorImage(NormalSource, primaryColor);
        var recoloredPressed = SpriteRecolorUtil.RecolorImage(PressedSource, primaryColor);
        var recoloredHighlighted = SpriteRecolorUtil.RecolorImage(HighlightedSource, primaryColor);
        using var font = TextRenderUtil.LoadFont(16);
        var textSize = TextRenderUtil.MeasureText(text);
        var textPosition = new Point((size.Width - textSize.Width) / 2, 6);
        var normalImage = NineSliceUtil.CreateNineSlice(recoloredNormal, size, 3, 3, 3, 6);
        var pressedImage = NineSliceUtil.CreateNineSlice(recoloredPressed, size with { Height = size.Height - 2 }, 3, 3, 3, 4);
        var highlightedImage = NineSliceUtil.CreateNineSlice(recoloredHighlighted, size, 3, 3, 3, 6);
        recoloredNormal.Dispose();
        recoloredPressed.Dispose();
        recoloredHighlighted.Dispose();
        return (
            TextRenderUtil.RenderText(normalImage, text, textPosition, Color.White, shadowColor, 2),
            TextRenderUtil.RenderText(pressedImage, text, textPosition, Color.White, shadowColor, 2),
            TextRenderUtil.RenderText(highlightedImage, text, textPosition, Color.White, shadowColor, 2)
        );
    }
}
