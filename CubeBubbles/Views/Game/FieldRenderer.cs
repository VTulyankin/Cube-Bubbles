using System.Drawing.Drawing2D;
using CubeBubbles.Models;
using CubeBubbles.Utilities;

namespace CubeBubbles.Views.Game;

/// <summary>
/// Отрисовывает игровое поле и его элементы
/// </summary>
public class FieldRenderer
{
    private GameModel? _gameModel;

    /// <summary>
    /// Инициализирует рендерер с моделью игры
    /// </summary>
    /// <param name="model">Модель игры</param>
    public void Initialize(GameModel model)
    {
        _gameModel = model;
        Bubble.PreloadSprites();
    }

    /// <summary>
    /// Отрисовывает игровое поле
    /// </summary>
    /// <param name="g">Графический контекст</param>
    /// <param name="clipRegion">Область обрезки</param>
    /// <param name="animator">Аниматор поля</param>
    public void RenderTo(Graphics g, RectangleF clipRegion, FieldAnimator animator)
    {
        if (_gameModel == null) return;

        var oldClip = g.Clip;
        g.SetClip(clipRegion);

        if (_gameModel.Status == GameStatus.Start)
        {
            RenderStartBackground(g, ScaleUtil.ScaleFactor, animator);
        }
        else if (animator.CurrentStartTransition != null)
        {
            RenderStartTransition(g, ScaleUtil.ScaleFactor, animator);
        }
        else
        {
            DrawBubbleMatrix(g, animator);
        }

        DrawFallingBubbles(g, animator);
        DrawDisappearingBubbles(g, animator);

        g.Clip = oldClip;
    }

    /// <summary>
    /// Отрисовывает фон стартового экрана
    /// </summary>
    /// <param name="g">Графический контекст</param>
    /// <param name="scale">Коэффициент масштабирования</param>
    /// <param name="animator">Аниматор поля</param>
    private void RenderStartBackground(Graphics g, float scale, FieldAnimator animator)
    {
        float fieldLeft = GameModel.FieldOriginX * scale;
        float fieldTop = GameModel.FieldOriginY * scale;
        float bubbleHeight = GameModel.BubbleHeight * scale;
        float bubbleWidth = GameModel.BubbleWidth * scale;

        for (int row = 0; row < 14; row++)
        {
            for (int col = 0; col < GameModel.MaxColumns; col++)
            {
                var bubble = _gameModel!.StartBackgroundMatrix[row, col];
                if (bubble == null) continue;

                float x = fieldLeft + col * bubbleWidth;
                float y = fieldTop + (row - 1) * bubbleHeight + animator.StartScrollProgress;

                g.DrawImage(Bubble.LoadSprite(bubble.Color), x, y);
            }
        }
    }

    /// <summary>
    /// Отрисовывает переход от стартового экрана к игре
    /// </summary>
    /// <param name="g">Графический контекст</param>
    /// <param name="scale">Коэффициент масштабирования</param>
    /// <param name="animator">Аниматор поля</param>
    private void RenderStartTransition(Graphics g, float scale, FieldAnimator animator)
    {
        var transition = animator.CurrentStartTransition!;
        float fieldLeft = GameModel.FieldOriginX * scale;
        float fieldTop = GameModel.FieldOriginY * scale;
        float bubbleHeight = GameModel.BubbleHeight * scale;
        float bubbleWidth = GameModel.BubbleWidth * scale;

        float targetDistance = bubbleHeight;
        float currentOffset = Math.Min(transition.FirstRowsProgress, targetDistance);
        float remainingDistance = targetDistance - currentOffset;

        for (int row = 0; row < GameModel.MaxRows; row++)
        {
            for (int col = 0; col < GameModel.MaxColumns; col++)
            {
                var bubble = _gameModel!.BubbleMatrix[row, col];
                if (bubble == null) continue;
                if (animator.IsBubbleHidden(row, col)) continue;

                var (offsetX, offsetY) = bubble.GetOffset();
                float x = fieldLeft + col * bubbleWidth + offsetX * scale;
                float baseY = fieldTop + row * bubbleHeight + offsetY * scale;
                float y = baseY - remainingDistance;

                var (wobbleX, wobbleY) = animator.GetWobbleOffset(row, col);

                g.DrawImage(bubble.GetSprite(), x + wobbleX, y + wobbleY);
            }
        }

        for (int row = 4; row < 14; row++)
        {
            var rowAnim = transition.RowAnimations[row];

            if (transition.ElapsedTime < rowAnim.StartDelay)
            {
                for (int col = 0; col < GameModel.MaxColumns; col++)
                {
                    var bubble = _gameModel!.StartBackgroundMatrix[row, col];
                    if (bubble == null) continue;

                    float x = fieldLeft + col * bubbleWidth;
                    float baseY = fieldTop + (row - 1) * bubbleHeight;
                    float y = baseY + animator.StartScrollProgress;

                    g.DrawImage(Bubble.LoadSprite(bubble.Color), x, y);
                }
            }
            else
            {
                for (int col = 0; col < GameModel.MaxColumns; col++)
                {
                    var bubble = _gameModel!.StartBackgroundMatrix[row, col];
                    if (bubble == null) continue;

                    float x = fieldLeft + col * bubbleWidth;
                    float baseY = fieldTop + (row - 1) * bubbleHeight;
                    float scrollY = baseY + animator.StartScrollProgress;

                    float distance = 1000f * scale;
                    float fallOffset = AnimUtil.CalculateFallingPosition(
                        rowAnim.Progress,
                        0f,
                        distance,
                        1.5f
                    );

                    float y = scrollY + fallOffset;
                    g.DrawImage(Bubble.LoadSprite(bubble.Color), x, y);
                }
            }
        }
    }

    /// <summary>
    /// Отрисовывает матрицу шариков на поле
    /// </summary>
    /// <param name="g">Графический контекст</param>
    /// <param name="animator">Аниматор поля</param>
    private void DrawBubbleMatrix(Graphics g, FieldAnimator animator)
    {
        var matrix = _gameModel!.BubbleMatrix;
        float scaleFactor = ScaleUtil.ScaleFactor;

        for (int row = 0; row < GameModel.MaxRows; row++)
        {
            for (int col = 0; col < GameModel.MaxColumns; col++)
            {
                var bubble = matrix[row, col];
                if (bubble == null) continue;
                if (animator.IsBubbleHidden(row, col)) continue;

                var (offsetX, offsetY) = bubble.GetOffset();
                float baseX = (GameModel.FieldOriginX + col * GameModel.BubbleWidth + offsetX) * scaleFactor;
                float baseY = (GameModel.FieldOriginY + row * GameModel.BubbleHeight + offsetY) * scaleFactor;

                var (wobbleX, wobbleY) = animator.GetWobbleOffset(row, col);
                float rowOffsetY = animator.GetRowShiftOffset(row);

                g.DrawImage(bubble.GetSprite(), baseX + wobbleX, baseY + wobbleY + rowOffsetY);
            }

            if (animator.CurrentRocketAnimation != null && animator.CurrentRocketAnimation.CenterRow == row)
            {
                DrawRocketAnimationAtRow(g, row, animator);
            }
        }
    }

    /// <summary>
    /// Отрисовывает анимацию ракеты на определённом ряду
    /// </summary>
    /// <param name="g">Графический контекст</param>
    /// <param name="row">Ряд для отрисовки</param>
    /// <param name="animator">Аниматор поля</param>
    private void DrawRocketAnimationAtRow(Graphics g, int row, FieldAnimator animator)
    {
        var rocket = animator.CurrentRocketAnimation!;
        float scale = ScaleUtil.ScaleFactor;
        float fieldLeft = GameModel.FieldOriginX * scale;
        float fieldTop = GameModel.FieldOriginY * scale;
        float bubbleWidth = GameModel.BubbleWidth * scale;
        float bubbleHeight = GameModel.BubbleHeight * scale;

        float centerX = fieldLeft + rocket.CenterCol * bubbleWidth;
        float centerY = fieldTop + rocket.CenterRow * bubbleHeight;

        var upOffset = Bubble.GetSpriteOffset(BubbleType.Rocket, RocketDirection.Up);
        var upSprite = Bubble.LoadSpecialSprite(BubbleType.Rocket, RocketDirection.Up);
        float upY = centerY - rocket.UpProgress + upOffset.offsetY * scale;
        float upX = centerX + upOffset.offsetX * scale;
        g.DrawImage(upSprite, upX, upY);

        var leftOffset = Bubble.GetSpriteOffset(BubbleType.Rocket, RocketDirection.Left);
        var leftSprite = Bubble.LoadSpecialSprite(BubbleType.Rocket, RocketDirection.Left);
        float leftX = centerX - rocket.LeftProgress + leftOffset.offsetX * scale;
        float leftY = centerY + leftOffset.offsetY * scale;
        g.DrawImage(leftSprite, leftX, leftY);

        var rightOffset = Bubble.GetSpriteOffset(BubbleType.Rocket, RocketDirection.Right);
        var rightSprite = Bubble.LoadSpecialSprite(BubbleType.Rocket, RocketDirection.Right);
        float rightX = centerX + rocket.RightProgress + rightOffset.offsetX * scale;
        float rightY = centerY + rightOffset.offsetY * scale;
        g.DrawImage(rightSprite, rightX, rightY);
    }

    /// <summary>
    /// Отрисовывает падающие шарики
    /// </summary>
    /// <param name="g">Графический контекст</param>
    /// <param name="animator">Аниматор поля</param>
    private void DrawFallingBubbles(Graphics g, FieldAnimator animator)
    {
        foreach (var bubble in animator.FallingBubbles)
        {
            g.DrawImage(bubble.Sprite, bubble.X, bubble.Y);
        }
    }

    /// <summary>
    /// Отрисовывает исчезающие шарики с эффектом мигания
    /// </summary>
    /// <param name="g">Графический контекст</param>
    /// <param name="animator">Аниматор поля</param>
    private void DrawDisappearingBubbles(Graphics g, FieldAnimator animator)
    {
        foreach (var bubble in animator.DisappearingBubbles)
        {
            if ((int)(bubble.ElapsedTime * 10) % 2 == 0)
            {
                float rowOffsetY = animator.GetRowShiftOffset(bubble.Row);
                g.DrawImage(bubble.Sprite, bubble.X, bubble.Y + rowOffsetY);
            }
        }
    }
}
