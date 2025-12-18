using CubeBubbles.Models;
using CubeBubbles.Utilities;

namespace CubeBubbles.Views.Game;

public class HeaderRenderer(GameModel gameModel)
{
    /// <summary>
    /// Рендерит заголовок (название игры или счёт) в верхнем левом углу
    /// </summary>
    /// <param name="g">Графический контекст для рисования</param>
    public void RenderTo(Graphics g)
    {
        var text = gameModel.Status == GameStatus.Start 
            ? MainForm.Title 
            : $"Счет: {gameModel.Score}";

        var textColor = Color.White;
        var shadowColor = SpriteRecolorUtil.HexToColor("000056");
        
        TextRenderUtil.RenderText(g, text, new Point(7, 4), textColor, shadowColor, 2);
    }
}