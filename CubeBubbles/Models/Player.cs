namespace CubeBubbles.Models;

public enum MoveDirection { Left, Right }

public class Player
{
    public int Position { get; private set; }
    public BubbleColor CurrentBubble { get; private set; } = GameModel.GetRandomBubbleColor();
    public BubbleColor NextBubble { get; private set; } = GameModel.GetRandomBubbleColor();
    public BubbleType CurrentBubbleType { get; private set; } = BubbleType.Normal;
    public BubbleType NextBubbleType { get; private set; } = BubbleType.Normal;

    public event Action<int, int, MoveDirection>? PositionChanging;
    public event Action? BubblesChanged;
    public event Action<BubbleColor, BubbleColor>? BubblesSwapped;
    public event Action<BubbleColor, BubbleColor>? BubbleShot;

    /// <summary>
    /// Двигает игрока влево
    /// </summary>
    public void MoveLeft()
    {
        int oldPos = Position;
        Position = Position == 0 ? GameModel.MaxColumns - 1 : Position - 1;
        PositionChanging?.Invoke(oldPos, Position, MoveDirection.Left);
    }

    /// <summary>
    /// Двигает игрока вправо
    /// </summary>
    public void MoveRight()
    {
        int oldPos = Position;
        Position = (Position + 1) % GameModel.MaxColumns;
        PositionChanging?.Invoke(oldPos, Position, MoveDirection.Right);
    }

    /// <summary>
    /// Устанавливает позицию напрямую
    /// </summary>
    /// <param name="newPosition">Новая позиция</param>
    public void SetPosition(int newPosition)
    {
        if (newPosition < 0 || newPosition >= GameModel.MaxColumns)
            return;

        if (newPosition == Position)
            return;

        int oldPos = Position;
        Position = newPosition;
        var direction = newPosition > oldPos ? MoveDirection.Right : MoveDirection.Left;
        PositionChanging?.Invoke(oldPos, Position, direction);
    }

    /// <summary>
    /// Выстреливает текущим шариком
    /// </summary>
    /// <returns>Цвет выстреленного шарика</returns>
    public void Shoot()
    {
        var shotBubble = CurrentBubble;
        CurrentBubble = NextBubble;
        CurrentBubbleType = NextBubbleType;
        
        (NextBubble, NextBubbleType) = GenerateRandomBubble();

        BubbleShot?.Invoke(shotBubble, NextBubble);
        BubblesChanged?.Invoke();
    }

    /// <summary>
    /// Меняет местами текущий и следующий шарики
    /// </summary>
    public void SwapBubbles()
    {
        var oldCurrent = CurrentBubble;
        var oldNext = NextBubble;

        (CurrentBubble, NextBubble) = (NextBubble, CurrentBubble);
        (CurrentBubbleType, NextBubbleType) = (NextBubbleType, CurrentBubbleType);

        BubblesSwapped?.Invoke(oldCurrent, oldNext);
        BubblesChanged?.Invoke();
    }

    /// <summary>
    /// Добавляет спец. шарик в текущий шарик игрока
    /// </summary>
    /// <param name="specialType">Тип спец. шарика</param>
    public void AddSpecialBubble(BubbleType specialType)
    {
        CurrentBubble = GameModel.GetRandomBubbleColor();
        CurrentBubbleType = specialType;
        BubblesChanged?.Invoke();
    }

    /// <summary>
    /// Сбрасывает состояние игрока
    /// </summary>
    public void Reset()
    {
        Position = 0;
        (CurrentBubble, CurrentBubbleType) = GenerateRandomBubble();
        (NextBubble, NextBubbleType) = GenerateRandomBubble();
        BubblesChanged?.Invoke();
    }

    /// <summary>
    /// Генерирует случайный шарик (обычный цветной)
    /// </summary>
    /// <returns>Цвет и тип шарика</returns>
    private static (BubbleColor, BubbleType) GenerateRandomBubble()
    {
        return (GameModel.GetRandomBubbleColor(), BubbleType.Normal);
    }
}
