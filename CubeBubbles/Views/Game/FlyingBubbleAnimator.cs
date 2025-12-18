using CubeBubbles.Models;
using CubeBubbles.Utilities;

namespace CubeBubbles.Views.Game;

/// <summary>
/// Управляет состоянием и обновлением летящих шариков
/// </summary>
public class FlyingBubbleAnimator
{
    private readonly List<FlyingBubble> _flyingBubbles = [];
    private readonly float _bubbleSpeed;

    public FlyingBubbleAnimator()
    {
        float scaleFactor = ScaleUtil.ScaleFactor;
        _bubbleSpeed = 500f * scaleFactor;
    }

    /// <summary>
    /// Обновляет позиции летящих шариков
    /// </summary>
    /// <param name="deltaTime">Время с последнего кадра</param>
    /// <param name="fieldAnimator">Аниматор поля для получения сдвигов</param>
    /// <returns>Список приземлившихся шариков</returns>
    public List<(int row, int col)> Update(float deltaTime, FieldAnimator fieldAnimator)
    {
        var landedBubbles = new List<(int row, int col)>();

        for (int i = _flyingBubbles.Count - 1; i >= 0; i--)
        {
            var bubble = _flyingBubbles[i];
            bubble.CurrentY -= _bubbleSpeed * deltaTime;

            float targetY = CalculateTargetY(bubble, fieldAnimator);

            if (bubble.CurrentY <= targetY)
            {
                bubble.CurrentY = targetY;

                if (bubble.Type != BubbleType.Bomb && bubble.Type != BubbleType.Rocket)
                {
                    fieldAnimator.StartWobble(bubble.Row, bubble.Column);
                }

                fieldAnimator.SetBubbleHidden(bubble.Row, bubble.Column, false);
                landedBubbles.Add((bubble.Row, bubble.Column));
                _flyingBubbles.RemoveAt(i);
            }
        }

        return landedBubbles;
    }

    /// <summary>
    /// Вычисляет динамическую целевую Y-координату с учётом анимаций поля
    /// </summary>
    /// <param name="bubble">Летящий шарик</param>
    /// <param name="fieldAnimator">Аниматор поля</param>
    /// <returns>Целевая Y-координата в пикселях</returns>
    private float CalculateTargetY(FlyingBubble bubble, FieldAnimator fieldAnimator)
    {
        return bubble.LogicalTargetY + fieldAnimator.GetRowShiftOffset(bubble.Row);
    }

    /// <summary>
    /// Обрабатывает событие выстрела шарика
    /// </summary>
    /// <param name="data">Данные выстрела</param>
    public void OnBubbleShot(BubbleAnimationData data)
    {
        float scaleFactor = ScaleUtil.ScaleFactor;
        var bubble = new Bubble(data.Color, data.Row, data.Column, data.Type);

        if (data.Type == BubbleType.Rocket)
        {
            bubble.RocketDir = RocketDirection.Box;
        }

        var (offsetX, offsetY) = bubble.GetOffset();

        float startX = (GameModel.FieldOriginX + data.Column * GameModel.BubbleWidth + offsetX) * scaleFactor;
        float startY = 216f * scaleFactor;
        float logicalTargetY = (GameModel.FieldOriginY + data.Row * GameModel.BubbleHeight + offsetY) * scaleFactor;

        _flyingBubbles.Add(new FlyingBubble
        {
            Row = data.Row,
            Column = data.Column,
            Type = data.Type,
            Sprite = bubble.GetSprite(),
            X = startX,
            CurrentY = startY,
            LogicalTargetY = logicalTargetY
        });
    }

    /// <summary>
    /// Очищает все летящие шарики
    /// </summary>
    public void Clear()
    {
        _flyingBubbles.Clear();
    }

    public IReadOnlyList<FlyingBubble> FlyingBubbles => _flyingBubbles;

    public class FlyingBubble
    {
        public int Row { get; init; }
        public int Column { get; init; }
        public BubbleType Type { get; init; }
        public Image Sprite { get; init; } = null!;
        public float X { get; init; }
        public float CurrentY { get; set; }
        public float LogicalTargetY { get; init; }
    }
}
