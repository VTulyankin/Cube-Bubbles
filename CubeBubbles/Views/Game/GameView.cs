using System.Drawing.Drawing2D;
using CubeBubbles.Models;
using CubeBubbles.Utilities;

namespace CubeBubbles.Views.Game;

/// <summary>
/// Контроллер для координации модели игры, аниматоров и рендереров
/// </summary>
public sealed class GameView : UserControl
{
    private GameModel? _gameModel;
    private GameTimeManager? _timeManager;
    private GameCursor? _cursor;

    private Bitmap? _backBuffer;
    private Graphics? _backBufferGraphics;
    private Bitmap? _backgroundCache;

    private HeaderRenderer? _headerRenderer;
    private FieldRenderer? _fieldRenderer;
    private PlayerRenderer? _playerRenderer;
    private FlyingBubbleRenderer? _flyingBubbleRenderer;

    private FieldAnimator? _fieldAnimator;
    private FlyingBubbleAnimator? _flyingBubbleAnimator;

    private bool _dragging;
    private Point _startPos = Point.Empty;

    public event EventHandler<int>? MouseMovedOverField;
    public event EventHandler? PlayerBubblesClicked;
    public event EventHandler<MouseEventArgs>? FieldMouseDown;

    public GameView()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | 
                 ControlStyles.Opaque | ControlStyles.ResizeRedraw, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, false);
        SetStyle(ControlStyles.Selectable, false);

        Location = Point.Empty;
        Size = ScaleUtil.ScaleSize(new Size(320, 240));
    }

    /// <summary>
    /// Инициализирует GameView с моделью и зависимостями
    /// </summary>
    /// <param name="model">Модель игры</param>
    /// <param name="timeManager">Менеджер времени</param>
    /// <param name="cursor">Игровой курсор</param>
    public void Initialize(GameModel model, GameTimeManager timeManager, GameCursor cursor)
    {
        _gameModel = model;
        _timeManager = timeManager;
        _cursor = cursor;

        _headerRenderer = new HeaderRenderer(model);

        _fieldRenderer = new FieldRenderer();
        _fieldRenderer.Initialize(model);

        _flyingBubbleRenderer = new FlyingBubbleRenderer();

        _playerRenderer = new PlayerRenderer();
        _playerRenderer.Initialize(model.Player, timeManager);

        _fieldAnimator = new FieldAnimator();
        _fieldAnimator.Initialize(model);

        _flyingBubbleAnimator = new FlyingBubbleAnimator();

        _gameModel.BubbleShotAnimation += OnBubbleShotAnimation;
        _gameModel.BubblesMatchAnimation += OnBubblesMatchAnimation;
        _gameModel.BubblesFloatAnimation += OnBubblesFloatAnimation;
        _gameModel.NewRowAnimation += OnNewRowAnimation;
        _gameModel.GameOverFallAnimation += OnGameOverFallAnimation;
        _gameModel.StartBackgroundFallAnimation += OnStartBackgroundFallAnimation;
        _gameModel.StatusChanged += OnStatusChanged;
        _gameModel.StartBackgroundChanged += OnStartBackgroundChanged;
        _gameModel.BombExplosionAnimation += OnBombExplosionAnimation;
        _gameModel.RocketLaunchAnimation += OnRocketLaunchAnimation;

        _timeManager.TimeUpdate += OnTimeUpdate;

        CreateBackBuffer();
    }

    /// <summary>
    /// Обновляет анимации каждый кадр
    /// </summary>
    /// <param name="deltaTime">Время с последнего кадра</param>
    private void OnTimeUpdate(float deltaTime)
    {
        if (_gameModel == null) return;

        if (_gameModel.Status is GameStatus.Playing or GameStatus.Pause or GameStatus.GameOver)
        {
            var fieldEvents = _fieldAnimator?.Update(deltaTime);
            var landedBubbles = _flyingBubbleAnimator?.Update(deltaTime, _fieldAnimator!);

            if (fieldEvents?.MatchAnimationComplete == true)
            {
                _gameModel.OnMatchAnimationComplete();
            }

            if (fieldEvents?.FloatAnimationComplete == true)
            {
                _gameModel.OnFloatAnimationComplete();
            }

            if (fieldEvents?.NewRowAnimationComplete == true)
            {
                _gameModel.OnNewRowAnimationComplete();
            }

            if (landedBubbles != null)
            {
                foreach (var (row, col) in landedBubbles)
                {
                    _gameModel.OnBubbleLanded(row, col);
                }
            }

            _playerRenderer?.UpdateAnimations(deltaTime);
        }
        else if (_gameModel.Status == GameStatus.Start)
        {
            _fieldAnimator?.Update(deltaTime);
        }

        Invalidate();
    }

    /// <summary>
    /// Отрисовывает игровое поле и элементы
    /// </summary>
    protected override void OnPaint(PaintEventArgs e)
    {
        if (_backBufferGraphics == null || _backBuffer == null || _gameModel == null)
            return;

        if (_backgroundCache != null)
        {
            _backBufferGraphics.DrawImageUnscaled(_backgroundCache, 0, 0);
        }
        else
        {
            _backBufferGraphics.Clear(Color.Black);
        }

        float scale = ScaleUtil.ScaleFactor;
        float fieldLeft = GameModel.FieldOriginX * scale;
        float fieldTop = GameModel.FieldOriginY * scale;
        float fieldWidth = GameModel.MaxColumns * GameModel.BubbleWidth * scale;
        float fieldHeight = ((GameModel.MaxRows - 1) * GameModel.BubbleHeight + 3) * scale;

        var clipRegion = new RectangleF(fieldLeft, fieldTop, fieldWidth, fieldHeight);

        _fieldRenderer?.RenderTo(_backBufferGraphics, clipRegion, _fieldAnimator!);
        _flyingBubbleRenderer?.RenderTo(_backBufferGraphics, _flyingBubbleAnimator!);

        if (_gameModel.Status is GameStatus.Playing or GameStatus.Pause or GameStatus.GameOver)
        {
            _playerRenderer?.RenderTo(_backBufferGraphics, scale);
        }

        _headerRenderer?.RenderTo(_backBufferGraphics);

        e.Graphics.DrawImageUnscaled(_backBuffer, 0, 0);
    }

    /// <summary>
    /// Обрабатывает движение мыши
    /// </summary>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_dragging)
        {
            var point = PointToScreen(e.Location);
            var form = FindForm();
            if (form != null)
            {
                form.Location = new Point(point.X - _startPos.X, point.Y - _startPos.Y);
            }
            return;
        }

        var isOverPlayer = false;
        if (_gameModel?.Status == GameStatus.Playing)
        {
            isOverPlayer = _playerRenderer?.IsPointOverPlayer(e.Location) ?? false;
        }

        _cursor?.SetHoverState(this, isOverPlayer);

        if (_gameModel is { IsMouseControlMode: true, Status: GameStatus.Playing })
        {
            var playerPosition = CalculatePlayerPositionFromMouse(e.X);
            MouseMovedOverField?.Invoke(this, playerPosition);
        }
    }

    /// <summary>
    /// Обрабатывает нажатие мыши
    /// </summary>
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Y < ScaleUtil.ScaleValue(22))
        {
            _dragging = true;
            _startPos = e.Location;
            return;
        }

        if (_gameModel?.Status == GameStatus.Playing)
        {
            var isOverPlayer = _playerRenderer?.IsPointOverPlayer(e.Location) ?? false;

            if (isOverPlayer && e.Button == MouseButtons.Left && _gameModel?.IsMouseControlMode != true)
            {
                PlayerBubblesClicked?.Invoke(this, EventArgs.Empty);
                return;
            }

            FieldMouseDown?.Invoke(this, e);
        }
    }

    /// <summary>
    /// Обрабатывает отпускание мыши
    /// </summary>
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragging = false;
    }

    /// <summary>
    /// Вычисляет позицию игрока по X-координате мыши
    /// </summary>
    /// <param name="mouseX">X-координата мыши</param>
    /// <returns>Позиция игрока от 0 до 15</returns>
    private int CalculatePlayerPositionFromMouse(int mouseX)
    {
        const float scaleFactor = ScaleUtil.ScaleFactor;
        const float fieldLeft = GameModel.FieldOriginX * scaleFactor;
        const float bubbleWidth = GameModel.BubbleWidth * scaleFactor;

        var relativeX = mouseX - fieldLeft;
        var position = (int)(relativeX / bubbleWidth);

        position = Math.Max(0, Math.Min(GameModel.MaxColumns - 1, position));
        return position;
    }

    /// <summary>
    /// Создаёт буфер обратной отрисовки
    /// </summary>
    private void CreateBackBuffer()
    {
        _backBuffer?.Dispose();
        _backBufferGraphics?.Dispose();
        _backgroundCache?.Dispose();

        _backBuffer = new Bitmap(Width, Height);
        _backBufferGraphics = Graphics.FromImage(_backBuffer);
        _backBufferGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        _backBufferGraphics.PixelOffsetMode = PixelOffsetMode.Half;
        _backBufferGraphics.CompositingMode = CompositingMode.SourceOver;
        _backBufferGraphics.CompositingQuality = CompositingQuality.HighSpeed;
        _backBufferGraphics.SmoothingMode = SmoothingMode.None;

        CacheBackground();
    }

    /// <summary>
    /// Кеширует фоновое изображение
    /// </summary>
    private void CacheBackground()
    {
        _backgroundCache?.Dispose();
        _backgroundCache = null;

        if (Parent?.BackgroundImage == null) return;

        try
        {
            _backgroundCache = new Bitmap(Width, Height);
            using var g = Graphics.FromImage(_backgroundCache);
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            var destRect = new Rectangle(0, 0, Width, Height);
            var srcRect = new Rectangle(Left, Top, Width, Height);

            g.DrawImage(Parent.BackgroundImage, destRect, srcRect, GraphicsUnit.Pixel);
        }
        catch
        {
            _backgroundCache?.Dispose();
            _backgroundCache = null;
        }
    }

    /// <summary>
    /// Запускает анимацию выстрела шарика
    /// </summary>
    private void OnBubbleShotAnimation(BubbleAnimationData data)
    {
        _flyingBubbleAnimator?.OnBubbleShot(data);
        _fieldAnimator?.SetBubbleHidden(data.Row, data.Column, true);
    }

    /// <summary>
    /// Запускает анимацию исчезновения совпавших шариков
    /// </summary>
    private void OnBubblesMatchAnimation(List<BubbleAnimationData> bubbles)
    {
        _fieldAnimator?.OnBubblesMatched(bubbles);
    }

    /// <summary>
    /// Запускает анимацию падения отсоединенных шариков
    /// </summary>
    private void OnBubblesFloatAnimation(List<BubbleAnimationData> bubbles)
    {
        _fieldAnimator?.OnBubblesFloat(bubbles);
    }

    /// <summary>
    /// Запускает анимацию добавления нового ряда
    /// </summary>
    private void OnNewRowAnimation()
    {
        _fieldAnimator?.OnNewRow();
    }

    /// <summary>
    /// Запускает анимацию падения поля при Game Over
    /// </summary>
    private void OnGameOverFallAnimation()
    {
        _fieldAnimator?.OnGameOverFall();
    }

    /// <summary>
    /// Запускает анимацию перехода с начального экрана
    /// </summary>
    private void OnStartBackgroundFallAnimation()
    {
        _fieldAnimator?.OnStartTransition();
    }

    /// <summary>
    /// Обновляет фон начального экрана
    /// </summary>
    private void OnStartBackgroundChanged()
    {
        _fieldAnimator?.OnStartBackgroundChanged();
    }

    /// <summary>
    /// Запускает анимацию взрыва бомбы
    /// </summary>
    private void OnBombExplosionAnimation(int row, int column)
    {
        _fieldAnimator?.OnBombExplosion(row, column);
    }

    /// <summary>
    /// Запускает анимацию запуска ракеты
    /// </summary>
    private void OnRocketLaunchAnimation(int row, int column, List<(int, int)> targets)
    {
        _fieldAnimator?.OnRocketLaunch(row, column, targets);
    }

    /// <summary>
    /// Обрабатывает изменение статуса игры
    /// </summary>
    private void OnStatusChanged(GameStatus status)
    {
        if (status == GameStatus.Playing && _gameModel!.WasPreviousStatus(GameStatus.Start))
        {
            _gameModel.TriggerStartTransition();
        }
        else if (status == GameStatus.Playing && _gameModel!.WasPreviousStatus(GameStatus.GameOver))
        {
            _gameModel.PopulateInitialField();
            _fieldAnimator?.ClearAnimations();
            _flyingBubbleAnimator?.Clear();
        }
        else if (status == GameStatus.Start)
        {
            _fieldAnimator?.ClearAnimations();
            _flyingBubbleAnimator?.Clear();
            _gameModel?.InitializeStartBackground();
            _timeManager?.ResetBackgroundTimer();
        }
    }

    /// <summary>
    /// Обновляет кеш фона при смене родителя
    /// </summary>
    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        CacheBackground();
    }

    /// <summary>
    /// Пересоздает буфер при изменении размера
    /// </summary>
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        CreateBackBuffer();
    }

    /// <summary>
    /// Освобождает графические ресурсы
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _backBuffer?.Dispose();
            _backBufferGraphics?.Dispose();
            _backgroundCache?.Dispose();
        }

        base.Dispose(disposing);
    }
}
