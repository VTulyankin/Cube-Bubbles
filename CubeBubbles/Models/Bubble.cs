using CubeBubbles.Utilities;

namespace CubeBubbles.Models;

public enum BubbleColor
{
    Blue,
    Cyan,
    Green,
    Purple,
    Red,
    Yellow
}

public enum BubbleType
{
    Normal,
    Box,
    Bomb,
    Rocket,
    Chameleon
}

public class Bubble(BubbleColor color, int row, int column, BubbleType type = BubbleType.Normal)
{
    private static readonly Dictionary<BubbleColor, Image> SpriteCache = new();
    private static readonly Dictionary<string, Image> SpecialSpriteCache = new();

    public BubbleColor Color { get; } = color;
    public int Row { get; set; } = row;
    public int Column { get; } = column;
    public BubbleType Type { get; } = type;
    public RocketDirection? RocketDir { get; set; }

    /// <summary>
    /// Возвращает смещение спрайта для корректного позиционирования
    /// </summary>
    /// <param name="type">Тип шарика</param>
    /// <param name="rocketDir">Направление ракеты</param>
    /// <returns>Смещение по X и Y</returns>
    public static (int offsetX, int offsetY) GetSpriteOffset(BubbleType type, RocketDirection? rocketDir = null)
    {
        return type switch
        {
            BubbleType.Normal => (0, 0),
            BubbleType.Chameleon => (0, 0),
            BubbleType.Box => (0, 0),
            BubbleType.Bomb => (0, -5),
            BubbleType.Rocket when rocketDir == RocketDirection.Box => (-1, -1),
            BubbleType.Rocket when rocketDir == RocketDirection.Up => (-1, -1),
            BubbleType.Rocket when rocketDir == RocketDirection.Left => (-1, -1),
            BubbleType.Rocket when rocketDir == RocketDirection.Right => (-1, -1),
            _ => (0, 0)
        };
    }

    /// <summary>
    /// Загружает спрайт обычного шарика по цвету
    /// </summary>
    /// <param name="color">Цвет шарика</param>
    /// <returns>Изображение шарика</returns>
    public static Image LoadSprite(BubbleColor color)
    {
        if (SpriteCache.TryGetValue(color, out var cached))
            return cached;

        var colorName = color.ToString().ToLower();
        var filePath = Path.Combine("Resources", "bubbles", $"{colorName}.png");
        var sprite = ScaleUtil.ScaleImage(Image.FromFile(filePath));
        SpriteCache[color] = sprite;
        return sprite;
    }

    /// <summary>
    /// Загружает спрайт спец. шарика
    /// </summary>
    /// <param name="type">Тип спец. шарика</param>
    /// <param name="rocketDir">Направление ракеты (если это ракета)</param>
    /// <returns>Изображение спец. шарика</returns>
    public static Image LoadSpecialSprite(BubbleType type, RocketDirection? rocketDir = null)
    {
        var cacheKey = type switch
        {
            BubbleType.Rocket when rocketDir == RocketDirection.Box => "rocket_box",
            BubbleType.Rocket when rocketDir == RocketDirection.Up => "rocket_up",
            BubbleType.Rocket when rocketDir == RocketDirection.Left => "rocket_left",
            BubbleType.Rocket when rocketDir == RocketDirection.Right => "rocket_right",
            BubbleType.Box => "box",
            BubbleType.Bomb => "bomb",
            BubbleType.Chameleon => "chameleon",
            _ => "box"
        };

        if (SpecialSpriteCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var fileName = type switch
        {
            BubbleType.Box => "box.png",
            BubbleType.Bomb => "bomb.png",
            BubbleType.Chameleon => "chameleon.png",
            BubbleType.Rocket when rocketDir == RocketDirection.Box => "rocket_box.png",
            BubbleType.Rocket when rocketDir == RocketDirection.Up => "rocket_up.png",
            BubbleType.Rocket when rocketDir == RocketDirection.Left => "rocket_left.png",
            BubbleType.Rocket when rocketDir == RocketDirection.Right => "rocket_right.png",
            _ => "box.png"
        };
        
        var filePath = Path.Combine("Resources", "bubbles", fileName);
        var sprite = ScaleUtil.ScaleImage(Image.FromFile(filePath));
        SpecialSpriteCache[cacheKey] = sprite;
        return sprite;
    }

    /// <summary>
    /// Предзагружает все спрайты при старте
    /// </summary>
    public static void PreloadSprites()
    {
        foreach (BubbleColor color in Enum.GetValues(typeof(BubbleColor)))
        {
            LoadSprite(color);
        }

        LoadSpecialSprite(BubbleType.Box);
        LoadSpecialSprite(BubbleType.Bomb);
        LoadSpecialSprite(BubbleType.Chameleon);
        LoadSpecialSprite(BubbleType.Rocket, RocketDirection.Box);
        LoadSpecialSprite(BubbleType.Rocket, RocketDirection.Up);
        LoadSpecialSprite(BubbleType.Rocket, RocketDirection.Left);
        LoadSpecialSprite(BubbleType.Rocket, RocketDirection.Right);
    }

    /// <summary>
    /// Возвращает спрайт текущего шарика
    /// </summary>
    /// <returns>Изображение шарика</returns>
    public Image GetSprite()
    {
        if (Type == BubbleType.Normal)
            return LoadSprite(Color);
        
        if (Type == BubbleType.Rocket)
            return LoadSpecialSprite(BubbleType.Rocket, RocketDir);
        
        return LoadSpecialSprite(Type);
    }

    /// <summary>
    /// Возвращает смещение спрайта текущего шарика
    /// </summary>
    /// <returns>Смещение по X и Y</returns>
    public (int offsetX, int offsetY) GetOffset()
    {
        return GetSpriteOffset(Type, RocketDir);
    }
}

public enum RocketDirection
{
    Box = 0,
    Up = 1,
    Left = 2,
    Right = 3
}
