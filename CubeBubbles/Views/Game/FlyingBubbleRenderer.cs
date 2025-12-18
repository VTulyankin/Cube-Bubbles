namespace CubeBubbles.Views.Game;

/// <summary>
/// Отрисовывает летящие шарики
/// </summary>
public class FlyingBubbleRenderer
{
    /// <summary>
    /// Рисует летящие шарики на экране
    /// </summary>
    /// <param name="g">Графический контекст</param>
    /// <param name="animator">Аниматор летящих шариков</param>
    public void RenderTo(Graphics g, FlyingBubbleAnimator animator)
    {
        foreach (var bubble in animator.FlyingBubbles)
        {
            g.DrawImage(bubble.Sprite, bubble.X, bubble.CurrentY);
        }
    }
}