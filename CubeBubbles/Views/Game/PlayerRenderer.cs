using CubeBubbles.Models;
using CubeBubbles.Utilities;

namespace CubeBubbles.Views.Game;

public class PlayerRenderer
{
    private Player? _player;

    private readonly Bitmap _bubbleBackground;

    private MoveAnimation? _moveAnim;
    private SwapAnimation? _swapAnim;
    private ShotAnimation? _shotAnim;

    private const float CurrentBubbleY = 0f;
    private const float NextBubbleY = 3f;

    private readonly float _moveSpeed;
    private readonly float _swapSpeed;

    public PlayerRenderer()
    {
        const float scale = ScaleUtil.ScaleFactor;
        _moveSpeed = 100f * scale;
        _swapSpeed = 20f * scale;
        _bubbleBackground = CreateBubbleBackground();
    }

    /// <summary>
    /// Инициализирует рендерер данными игрока и подписывается на события
    /// </summary>
    /// <param name="player">Модель игрока</param>
    /// <param name="timeManager"></param>
    public void Initialize(Player player, GameTimeManager timeManager)
    {
        _player = player;

        _player.PositionChanging += OnPositionChanging;
        _player.BubblesSwapped += OnBubblesSwapped;
        _player.BubbleShot += OnBubbleShot;
    }

    /// <summary>
    /// Обновляет все активные анимации игрока
    /// </summary>
    /// <param name="deltaTime">Время с последнего кадра в секундах</param>
    public void UpdateAnimations(float deltaTime)
    {
        if (_player == null) return;

        if (_moveAnim != null)
            UpdateMoveAnim(_moveAnim, deltaTime);

        if (_swapAnim != null)
            UpdateSwapAnim(_swapAnim, deltaTime);

        if (_shotAnim != null)
            UpdateShotAnim(_shotAnim, deltaTime);
    }

    /// <summary>
    /// Рендерит игрока на указанном графическом контексте
    /// </summary>
    /// <param name="g">Графический контекст</param>
    /// <param name="scale">Масштабный коэффициент</param>
    public void RenderTo(Graphics g, float scale)
    {
        if (_player == null) return;

        var playerAreaTop = (GameModel.FieldOriginY + (GameModel.MaxRows - 1) * GameModel.BubbleHeight + 2 - 5) * scale;
        var fieldLeft = GameModel.FieldOriginX * scale;
        var fieldWidth = GameModel.MaxColumns * GameModel.BubbleWidth * scale;
        var playerHeight = (GameModel.BubbleHeight * 2 + NextBubbleY + 5) * scale;

        var oldClip = g.Clip;
        g.SetClip(new RectangleF(fieldLeft, playerAreaTop, fieldWidth, playerHeight));

        var playerX = GetPlayerX();
        var absoluteX = fieldLeft + playerX;

        DrawBubbles(g, absoluteX, playerAreaTop + 5 * scale, scale);

        if (playerX < 0)
            DrawBubbles(g, absoluteX + fieldWidth, playerAreaTop + 5 * scale, scale);
        else if (playerX + GameModel.BubbleWidth * scale > fieldWidth)
            DrawBubbles(g, absoluteX - fieldWidth, playerAreaTop + 5 * scale, scale);

        g.Clip = oldClip;
    }

    /// <summary>
    /// Проверяет, находится ли точка над игроком
    /// </summary>
    /// <param name="localPoint">Точка в координатах GameView</param>
    /// <returns>True если точка над игроком</returns>
    public bool IsPointOverPlayer(Point localPoint)
    {
        if (_player == null) return false;

        const float scale = ScaleUtil.ScaleFactor;
        const float playerAreaTop = ((GameModel.FieldOriginY + (GameModel.MaxRows - 1) * GameModel.BubbleHeight) + 2 - 5) * scale;
        const float fieldLeft = GameModel.FieldOriginX * scale;
        const float fieldWidth = GameModel.MaxColumns * GameModel.BubbleWidth * scale;
        const float bubbleWidth = GameModel.BubbleWidth * scale;
        const float playerHeight = (GameModel.BubbleHeight * 2 + NextBubbleY + 5) * scale;

        var playerX = GetPlayerX();

        var rect1 = new RectangleF(fieldLeft + playerX, playerAreaTop, bubbleWidth, playerHeight);
        if (rect1.Contains(localPoint))
            return true;

        if (playerX < 0)
        {
            var rect2 = new RectangleF(fieldLeft + playerX + fieldWidth, playerAreaTop, bubbleWidth, playerHeight);
            if (rect2.Contains(localPoint))
                return true;
        }
        else if (playerX + bubbleWidth > fieldWidth)
        {
            var rect2 = new RectangleF(fieldLeft + playerX - fieldWidth, playerAreaTop, bubbleWidth, playerHeight);
            if (rect2.Contains(localPoint))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Возвращает текущую X-позицию игрока в пикселях относительно поля
    /// </summary>
    /// <returns>X-координата в пикселях</returns>
    private float GetPlayerX()
    {
        const float scale = ScaleUtil.ScaleFactor;
        var baseX = _player!.Position * GameModel.BubbleWidth * scale;

        if (_moveAnim != null)
        {
            var t = _moveAnim.Progress;
            return _moveAnim.StartX + (_moveAnim.TargetX - _moveAnim.StartX) * t;
        }

        return baseX;
    }

    /// <summary>
    /// Обновляет анимацию движения игрока
    /// </summary>
    /// <param name="anim">Анимация движения</param>
    /// <param name="dt">Время кадра</param>
    /// <returns>True если анимация продолжается</returns>
    private void UpdateMoveAnim(MoveAnimation anim, float dt)
    {
        if (anim.Progress >= 1f) return;

        anim.Progress += dt * _moveSpeed / Math.Abs(anim.TargetX - anim.StartX);

        if (anim.Progress >= 1f)
        {
            anim.Progress = 1f;
            _moveAnim = null;
        }
    }

    /// <summary>
    /// Обновляет анимацию смены шариков
    /// </summary>
    /// <param name="anim">Анимация смены</param>
    /// <param name="dt">Время кадра</param>
    /// <returns>True если анимация продолжается</returns>
    private void UpdateSwapAnim(SwapAnimation anim, float dt)
    {
        if (anim.Progress >= 1f) return;

        const float scale = ScaleUtil.ScaleFactor;
        var distance = NextBubbleY * scale;

        anim.Progress += dt * _swapSpeed / distance;

        if (anim.Progress >= 1f)
        {
            anim.Progress = 1f;
            _swapAnim = null;
        }
    }

    /// <summary>
    /// Обновляет анимацию выстрела
    /// </summary>
    /// <param name="anim">Анимация выстрела</param>
    /// <param name="dt">Время кадра</param>
    /// <returns>True если анимация продолжается</returns>
    private void UpdateShotAnim(ShotAnimation anim, float dt)
    {
        if (anim.Progress >= 1f)
        {
            _shotAnim = null;
            return;
        }

        anim.Progress += dt * 3;

        if (anim.Progress >= 1f)
        {
            anim.Progress = 1f;
            _shotAnim = null;
        }
    }

    /// <summary>
    /// Рисует шарики игрока
    /// </summary>
    /// <param name="g">Графический контекст</param>
    /// <param name="x">X-координата</param>
    /// <param name="y">Y-координата</param>
    /// <param name="scale">Масштаб</param>
    private void DrawBubbles(Graphics g, float x, float y, float scale)
    {
        if (_shotAnim != null)
        {
            var startY = y + NextBubbleY * scale;
            var endY = y + CurrentBubbleY * scale;
            var currentNextY = startY + (_shotAnim.Progress * (endY - startY));

            var newNextYLocal = y + NextBubbleY * scale;

            if (!ShouldSkipBackground(_shotAnim.NewNextType))
            {
                g.DrawImage(_bubbleBackground, x, newNextYLocal);
            }
            DrawPlayerBubble(g, x, newNextYLocal, _shotAnim.NewNextColor, _shotAnim.NewNextType, scale);

            if (!ShouldSkipBackground(_shotAnim.NextType))
            {
                g.DrawImage(_bubbleBackground, x, currentNextY);
            }
            DrawPlayerBubble(g, x, currentNextY, _shotAnim.NextColor, _shotAnim.NextType, scale);
        }
        else if (_swapAnim != null)
        {
            var t = _swapAnim.Progress;
            var offset = NextBubbleY * scale * t;

            var topY = y + CurrentBubbleY * scale + offset;
            var bottomY = y + NextBubbleY * scale - offset;

            if (topY > bottomY)
            {
                if (!ShouldSkipBackground(_swapAnim.TopType))
                {
                    g.DrawImage(_bubbleBackground, x, topY);
                }
                DrawPlayerBubble(g, x, topY, _swapAnim.TopColor, _swapAnim.TopType, scale);

                if (!ShouldSkipBackground(_swapAnim.BottomType))
                {
                    g.DrawImage(_bubbleBackground, x, bottomY);
                }
                DrawPlayerBubble(g, x, bottomY, _swapAnim.BottomColor, _swapAnim.BottomType, scale);
            }
            else
            {
                if (!ShouldSkipBackground(_swapAnim.BottomType))
                {
                    g.DrawImage(_bubbleBackground, x, bottomY);
                }
                DrawPlayerBubble(g, x, bottomY, _swapAnim.BottomColor, _swapAnim.BottomType, scale);

                if (!ShouldSkipBackground(_swapAnim.TopType))
                {
                    g.DrawImage(_bubbleBackground, x, topY);
                }
                DrawPlayerBubble(g, x, topY, _swapAnim.TopColor, _swapAnim.TopType, scale);
            }
        }
        else
        {
            float currentY = y + CurrentBubbleY * scale;
            float nextY = y + NextBubbleY * scale;

            if (!ShouldSkipBackground(_player!.NextBubbleType))
            {
                g.DrawImage(_bubbleBackground, x, nextY);
            }
            DrawPlayerBubble(g, x, nextY, _player!.NextBubble, _player.NextBubbleType, scale);

            if (!ShouldSkipBackground(_player.CurrentBubbleType))
            {
                g.DrawImage(_bubbleBackground, x, currentY);
            }
            DrawPlayerBubble(g, x, currentY, _player.CurrentBubble, _player.CurrentBubbleType, scale);
        }
    }

    /// <summary>
    /// Проверяет нужно ли пропустить фон для типа шарика
    /// </summary>
    /// <param name="type">Тип шарика</param>
    /// <returns>True если фон не нужен</returns>
    private bool ShouldSkipBackground(BubbleType type)
    {
        return type == BubbleType.Bomb || type == BubbleType.Rocket;
    }

    /// <summary>
    /// Рисует шарик игрока с учетом типа и offset
    /// </summary>
    /// <param name="g">Графический контекст</param>
    /// <param name="x">X-координата</param>
    /// <param name="y">Y-координата</param>
    /// <param name="color">Цвет шарика</param>
    /// <param name="type">Тип шарика</param>
    /// <param name="scale">Масштаб</param>
    private void DrawPlayerBubble(Graphics g, float x, float y, BubbleColor color, BubbleType type, float scale)
    {
        if (type == BubbleType.Normal)
        {
            g.DrawImage(Bubble.LoadSprite(color), x, y);
        }
        else
        {
            RocketDirection? rocketDir = type == BubbleType.Rocket ? RocketDirection.Box : null;
            var (offsetX, offsetY) = Bubble.GetSpriteOffset(type, rocketDir);
            
            float finalX = x + offsetX * scale;
            float finalY = y + offsetY * scale;
            
            g.DrawImage(Bubble.LoadSpecialSprite(type, rocketDir), finalX, finalY);
        }
    }

    /// <summary>
    /// Обрабатывает событие изменения позиции игрока
    /// </summary>
    /// <param name="oldPos">Старая позиция</param>
    /// <param name="newPos">Новая позиция</param>
    /// <param name="direction">Направление движения</param>
    private void OnPositionChanging(int oldPos, int newPos, MoveDirection direction)
    {
        float scale = ScaleUtil.ScaleFactor;
        float oldX = oldPos * GameModel.BubbleWidth * scale;
        float newX = newPos * GameModel.BubbleWidth * scale;

        if (direction == MoveDirection.Right && newPos < oldPos)
            newX = oldX + GameModel.BubbleWidth * scale;
        else if (direction == MoveDirection.Left && newPos > oldPos)
            newX = oldX - GameModel.BubbleWidth * scale;

        _moveAnim = new MoveAnimation
        {
            StartX = oldX,
            TargetX = newX,
            Progress = 0f
        };
    }

    /// <summary>
    /// Обрабатывает событие смены шариков
    /// </summary>
    /// <param name="oldCurrent">Старый текущий шарик</param>
    /// <param name="oldNext">Старый следующий шарик</param>
    private void OnBubblesSwapped(BubbleColor oldCurrent, BubbleColor oldNext)
    {
        _swapAnim = new SwapAnimation
        {
            TopColor = oldCurrent,
            BottomColor = oldNext,
            TopType = _player!.NextBubbleType,
            BottomType = _player.CurrentBubbleType,
            Progress = 0f
        };
    }

    /// <summary>
    /// Обрабатывает событие выстрела
    /// </summary>
    /// <param name="shotBubble">Цвет выстреленного шарика</param>
    /// <param name="newNext">Цвет нового следующего шарика</param>
    private void OnBubbleShot(BubbleColor shotBubble, BubbleColor newNext)
    {
        _shotAnim = new ShotAnimation
        {
            NextColor = _player!.CurrentBubble,
            NewNextColor = newNext,
            NextType = _player.CurrentBubbleType,
            NewNextType = _player.NextBubbleType,
            Progress = 0f
        };
    }

    /// <summary>
    /// Создаёт фоновое изображение для шарика
    /// </summary>
    /// <returns>Bitmap с фоном</returns>
    private static Bitmap CreateBubbleBackground()
    {
        var bgColor = ColorTranslator.FromHtml("#E8E8E8");
        var size = ScaleUtil.ScaleValue(GameModel.BubbleWidth);
        var height = ScaleUtil.ScaleValue(GameModel.BubbleHeight + 3);

        var image = new Bitmap(size, height);
        using var g = Graphics.FromImage(image);
        g.Clear(bgColor);

        return image;
    }

    private class MoveAnimation
    {
        public float StartX { get; init; }
        public float TargetX { get; init; }
        public float Progress { get; set; }
    }

    private class SwapAnimation
    {
        public BubbleColor TopColor { get; init; }
        public BubbleColor BottomColor { get; init; }
        public BubbleType TopType { get; init; }
        public BubbleType BottomType { get; init; }
        public float Progress { get; set; }
    }

    private class ShotAnimation
    {
        public BubbleColor NextColor { get; init; }
        public BubbleColor NewNextColor { get; init; }
        public BubbleType NextType { get; init; }
        public BubbleType NewNextType { get; init; }
        public float Progress { get; set; }
    }
}
