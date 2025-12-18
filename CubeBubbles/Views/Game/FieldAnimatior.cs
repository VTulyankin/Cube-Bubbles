using CubeBubbles.Models;
using CubeBubbles.Utilities;

namespace CubeBubbles.Views.Game;

/// <summary>
/// Управляет состоянием и обновлением всех анимаций игрового поля
/// </summary>
public class FieldAnimator
{
    private const float DisappearDuration = 0.3f;
    private const float NewRowAnimDuration = 0.5f;
    private const float FallingAnimDuration = 1.5f;
    private const float RowDelayBetween = 0.05f;
    private const float RocketSpeed = 800f;
    private const float RowShiftInterval = 2f;

    private readonly List<FallingBubble> _fallingBubbles = [];
    private readonly List<DisappearingBubble> _disappearingBubbles = [];
    private readonly Dictionary<int, BubbleWobble> _wobbles = new();
    private readonly List<int> _keysToRemove = new();
    private readonly HashSet<(int row, int col)> _hiddenBubbles = new();

    private NewRowAnimation? _newRowAnimation;
    private GameOverAnimation? _gameOverAnimation;
    private StartTransitionAnimation? _startTransitionAnimation;
    private RocketAnimation? _rocketAnimation;
    private float _startScrollProgress;

    private GameModel? _gameModel;

    /// <summary>
    /// Инициализирует аниматор с моделью игры
    /// </summary>
    /// <param name="model">Модель игры</param>
    public void Initialize(GameModel model)
    {
        _gameModel = model;
    }

    /// <summary>
    /// Обновляет все анимации поля
    /// </summary>
    /// <param name="deltaTime">Время с последнего кадра</param>
    /// <returns>События, произошедшие за этот кадр</returns>
    public FieldAnimationEvents Update(float deltaTime)
    {
        var events = new FieldAnimationEvents();

        UpdateStartScrolling(deltaTime);
        UpdateStartTransition(deltaTime);
        UpdateNewRowAnimation(deltaTime, events);
        UpdateGameOverAnimation(deltaTime);
        UpdateRocketAnimation(deltaTime);
        UpdateDisappearingBubbles(deltaTime, events);
        UpdateFallingBubbles(deltaTime, events);
        UpdateWobbles(deltaTime);

        return events;
    }

    /// <summary>
    /// Обновляет скроллинг стартового фона
    /// </summary>
    /// <param name="deltaTime">Время с последнего кадра</param>
    private void UpdateStartScrolling(float deltaTime)
    {
        if (_gameModel?.Status == GameStatus.Start && _startTransitionAnimation == null)
        {
            float bubbleHeight = GameModel.BubbleHeight * ScaleUtil.ScaleFactor;
            float scrollSpeed = bubbleHeight / RowShiftInterval;
            _startScrollProgress += scrollSpeed * deltaTime;

            if (_startScrollProgress >= bubbleHeight)
            {
                _startScrollProgress -= bubbleHeight;
            }
        }
    }

    /// <summary>
    /// Обновляет анимацию перехода от старта к игре
    /// </summary>
    /// <param name="deltaTime">Время с последнего кадра</param>
    private void UpdateStartTransition(float deltaTime)
    {
        if (_startTransitionAnimation == null) return;

        float scale = ScaleUtil.ScaleFactor;
        float bubbleHeight = GameModel.BubbleHeight * scale;
        float scrollSpeed = bubbleHeight / RowShiftInterval;

        _startTransitionAnimation.FirstRowsProgress += scrollSpeed * deltaTime;
        _startTransitionAnimation.ElapsedTime += deltaTime;

        float targetDistance = bubbleHeight;
        bool firstRowsComplete = _startTransitionAnimation.FirstRowsProgress >= targetDistance;

        bool allFallen = true;
        for (int row = 4; row < 14; row++)
        {
            var rowAnim = _startTransitionAnimation.RowAnimations[row];
            if (_startTransitionAnimation.ElapsedTime >= rowAnim.StartDelay)
            {
                rowAnim.Progress += deltaTime / FallingAnimDuration;
                if (rowAnim.Progress < 1f)
                    allFallen = false;
            }
            else
            {
                allFallen = false;
            }
        }

        if (firstRowsComplete && allFallen)
        {
            _startTransitionAnimation = null;
            _startScrollProgress = 0f;
        }
    }

    /// <summary>
    /// Обновляет анимацию добавления нового ряда
    /// </summary>
    /// <param name="deltaTime">Время с последнего кадра</param>
    /// <param name="events">События для заполнения</param>
    private void UpdateNewRowAnimation(float deltaTime, FieldAnimationEvents events)
    {
        if (_newRowAnimation == null) return;

        _newRowAnimation.Progress += deltaTime;

        if (AnimUtil.IsRowAnimationComplete(_newRowAnimation.Progress, GameModel.MaxRows, 
            RowDelayBetween, NewRowAnimDuration))
        {
            _newRowAnimation = null;
            events.NewRowAnimationComplete = true;
        }
    }

    /// <summary>
    /// Обновляет анимацию проигрыша
    /// </summary>
    /// <param name="deltaTime">Время с последнего кадра</param>
    private void UpdateGameOverAnimation(float deltaTime)
    {
        if (_gameOverAnimation == null) return;

        _gameOverAnimation.ElapsedTime += deltaTime;
        bool anyRowActive = false;

        for (int row = GameModel.MaxRows - 1; row >= 0; row--)
        {
            var rowAnim = _gameOverAnimation.RowAnimations[row];
            if (_gameOverAnimation.ElapsedTime >= rowAnim.StartDelay)
            {
                if (rowAnim.Progress == 0f)
                {
                    float scaleFactor = ScaleUtil.ScaleFactor;
                    for (int col = 0; col < GameModel.MaxColumns; col++)
                    {
                        var bubble = _gameModel?.BubbleMatrix[row, col];
                        if (bubble != null)
                        {
                            var (offsetX, offsetY) = bubble.GetOffset();
                            float startX = (GameModel.FieldOriginX + col * GameModel.BubbleWidth + offsetX) * scaleFactor;
                            float startY = (GameModel.FieldOriginY + row * GameModel.BubbleHeight + offsetY) * scaleFactor;
                            AddFallingBubble(bubble, startX, startY);
                            _hiddenBubbles.Add((row, col));
                        }
                    }
                }

                rowAnim.Progress += deltaTime / FallingAnimDuration;
                if (rowAnim.Progress < 1f)
                    anyRowActive = true;
            }
            else
            {
                anyRowActive = true;
            }
        }

        if (!anyRowActive && _fallingBubbles.Count == 0)
            _gameOverAnimation = null;
    }

    /// <summary>
    /// Обновляет анимацию ракеты
    /// </summary>
    /// <param name="deltaTime">Время с последнего кадра</param>
    private void UpdateRocketAnimation(float deltaTime)
    {
        if (_rocketAnimation == null) return;

        float scale = ScaleUtil.ScaleFactor;
        float speed = RocketSpeed * scale * deltaTime;

        _rocketAnimation.UpProgress += speed;
        _rocketAnimation.LeftProgress += speed;
        _rocketAnimation.RightProgress += speed;

        var destroyedThisFrame = new List<(int, int)>();
        float bubbleHeight = GameModel.BubbleHeight * scale;
        float bubbleWidth = GameModel.BubbleWidth * scale;

        foreach (var (r, c) in _rocketAnimation.UpTargets)
        {
            if (!_rocketAnimation.DestroyedBubbles.Contains((r, c)))
            {
                int rowsFromCenter = _rocketAnimation.CenterRow - r;
                float distanceNeeded = rowsFromCenter * bubbleHeight;

                if (_rocketAnimation.UpProgress >= distanceNeeded)
                {
                    _rocketAnimation.DestroyedBubbles.Add((r, c));
                    destroyedThisFrame.Add((r, c));
                }
            }
        }

        foreach (var (r, c) in _rocketAnimation.LeftTargets)
        {
            if (!_rocketAnimation.DestroyedBubbles.Contains((r, c)))
            {
                int colsFromCenter = _rocketAnimation.CenterCol - c;
                float distanceNeeded = colsFromCenter * bubbleWidth;

                if (_rocketAnimation.LeftProgress >= distanceNeeded)
                {
                    _rocketAnimation.DestroyedBubbles.Add((r, c));
                    destroyedThisFrame.Add((r, c));
                }
            }
        }

        foreach (var (r, c) in _rocketAnimation.RightTargets)
        {
            if (!_rocketAnimation.DestroyedBubbles.Contains((r, c)))
            {
                int colsFromCenter = c - _rocketAnimation.CenterCol;
                float distanceNeeded = colsFromCenter * bubbleWidth;

                if (_rocketAnimation.RightProgress >= distanceNeeded)
                {
                    _rocketAnimation.DestroyedBubbles.Add((r, c));
                    destroyedThisFrame.Add((r, c));
                }
            }
        }

        if (destroyedThisFrame.Count > 0)
        {
            _gameModel?.OnRocketAnimationStep(destroyedThisFrame);
        }

        float maxDistance = Math.Max(
            GameModel.MaxRows * bubbleHeight,
            GameModel.MaxColumns * bubbleWidth
        );

        if (_rocketAnimation.UpProgress >= maxDistance &&
            _rocketAnimation.LeftProgress >= maxDistance &&
            _rocketAnimation.RightProgress >= maxDistance)
        {
            _rocketAnimation = null;
        }
    }

    /// <summary>
    /// Обновляет исчезающие шарики
    /// </summary>
    /// <param name="deltaTime">Время с последнего кадра</param>
    /// <param name="events">События для заполнения</param>
    private void UpdateDisappearingBubbles(float deltaTime, FieldAnimationEvents events)
    {
        bool hadDisappearing = _disappearingBubbles.Count > 0;

        for (int i = _disappearingBubbles.Count - 1; i >= 0; i--)
        {
            var bubble = _disappearingBubbles[i];
            bubble.ElapsedTime += deltaTime;

            if (bubble.ElapsedTime >= DisappearDuration)
            {
                _disappearingBubbles.RemoveAt(i);
            }
        }

        if (hadDisappearing && _disappearingBubbles.Count == 0)
        {
            events.MatchAnimationComplete = true;
        }
    }

    /// <summary>
    /// Обновляет падающие шарики
    /// </summary>
    /// <param name="deltaTime">Время с последнего кадра</param>
    /// <param name="events">События для заполнения</param>
    private void UpdateFallingBubbles(float deltaTime, FieldAnimationEvents events)
    {
        bool hadFalling = _fallingBubbles.Count > 0;
        float screenBottomThreshold = 240f * ScaleUtil.ScaleFactor + 50f;

        for (int i = _fallingBubbles.Count - 1; i >= 0; i--)
        {
            var bubble = _fallingBubbles[i];
            bubble.Progress += deltaTime;

            float distance = 1000f * ScaleUtil.ScaleFactor;
            bubble.Y = AnimUtil.CalculateFallingPosition(
                bubble.Progress,
                bubble.StartY,
                distance,
                FallingAnimDuration
            );

            if (bubble.Y >= screenBottomThreshold)
            {
                _fallingBubbles.RemoveAt(i);
            }
        }

        if (hadFalling && _fallingBubbles.Count == 0)
        {
            events.FloatAnimationComplete = true;
        }
    }

    /// <summary>
    /// Обновляет wobble-эффекты
    /// </summary>
    /// <param name="deltaTime">Время с последнего кадра</param>
    private void UpdateWobbles(float deltaTime)
    {
        _keysToRemove.Clear();

        if (_wobbles.Count > 0)
        {
            foreach (var kvp in _wobbles)
            {
                var wobble = kvp.Value;
                wobble.Util.Update(deltaTime);

                if (wobble.Util.IsComplete)
                    _keysToRemove.Add(kvp.Key);
            }

            foreach (var key in _keysToRemove)
                _wobbles.Remove(key);
        }
    }

    /// <summary>
    /// Вычисляет вертикальный сдвиг ряда из-за активных анимаций
    /// </summary>
    /// <param name="row">Номер ряда</param>
    /// <returns>Смещение в пикселях</returns>
    public float GetRowShiftOffset(int row)
    {
        if (_newRowAnimation != null)
        {
            float rowHeight = GameModel.BubbleHeight * ScaleUtil.ScaleFactor;
            return AnimUtil.CalculateRowOffset(_newRowAnimation.Progress, row, rowHeight, 
                RowDelayBetween, NewRowAnimDuration);
        }

        if (_startTransitionAnimation != null)
        {
            float bubbleHeight = GameModel.BubbleHeight * ScaleUtil.ScaleFactor;
            float targetDistance = bubbleHeight;
            float currentOffset = Math.Min(_startTransitionAnimation.FirstRowsProgress, targetDistance);
            return -(targetDistance - currentOffset);
        }

        return 0f;
    }

    /// <summary>
    /// Получает смещение wobble для конкретного шарика
    /// </summary>
    /// <param name="row">Ряд шарика</param>
    /// <param name="col">Колонка шарика</param>
    /// <returns>Смещение по X и Y</returns>
    public (float x, float y) GetWobbleOffset(int row, int col)
    {
        int key = row * GameModel.MaxColumns + col;
        if (_wobbles.TryGetValue(key, out var wobble))
        {
            float wave = wobble.Util.GetOffset();
            return (wobble.DirectionX * wave, wobble.DirectionY * wave);
        }
        return (0f, 0f);
    }

    /// <summary>
    /// Проверяет, скрыт ли шарик в данной ячейке
    /// </summary>
    /// <param name="row">Ряд</param>
    /// <param name="col">Колонка</param>
    /// <returns>True если скрыт</returns>
    public bool IsBubbleHidden(int row, int col)
    {
        return _hiddenBubbles.Contains((row, col));
    }

    /// <summary>
    /// Показывает или скрывает шарик в ячейке
    /// </summary>
    /// <param name="row">Ряд</param>
    /// <param name="col">Колонка</param>
    /// <param name="hidden">True для скрытия, false для показа</param>
    public void SetBubbleHidden(int row, int col, bool hidden)
    {
        if (hidden)
            _hiddenBubbles.Add((row, col));
        else
            _hiddenBubbles.Remove((row, col));
    }

    /// <summary>
    /// Запускает волну wobble от центральной точки
    /// </summary>
    /// <param name="centerRow">Ряд центра</param>
    /// <param name="centerCol">Колонка центра</param>
    public void StartWobble(int centerRow, int centerCol)
    {
        float scaleFactor = ScaleUtil.ScaleFactor;
        float wobbleAmplitude = 3f * scaleFactor;

        ReadOnlySpan<(int dRow, int dCol, float dirX, float dirY)> neighbors = [
            (-1, 0, 0f, -1f),
            (1, 0, 0f, 1f),
            (0, -1, -1f, 0f),
            (0, 1, 1f, 0f)
        ];

        foreach (var (dRow, dCol, dirX, dirY) in neighbors)
        {
            int row = centerRow + dRow;
            int col = centerCol + dCol;

            if (row < 0 || row >= GameModel.MaxRows || col < 0 || col >= GameModel.MaxColumns)
                continue;

            if (_gameModel?.BubbleMatrix[row, col] == null)
                continue;

            int key = row * GameModel.MaxColumns + col;

            if (_wobbles.TryGetValue(key, out var existing))
            {
                existing.DirectionX = dirX;
                existing.DirectionY = dirY;
                existing.Util.Reset();
            }
            else
            {
                _wobbles[key] = new BubbleWobble
                {
                    DirectionX = dirX,
                    DirectionY = dirY,
                    Util = new AnimUtil(wobbleAmplitude)
                };
            }
        }
    }

    /// <summary>
    /// Запускает усиленную волну wobble от взрыва бомбы
    /// </summary>
    /// <param name="centerRow">Ряд центра взрыва</param>
    /// <param name="centerCol">Колонка центра взрыва</param>
    private void StartBombWobble(int centerRow, int centerCol)
    {
        float scaleFactor = ScaleUtil.ScaleFactor;
        float wobbleAmplitude = 5f * scaleFactor;

        for (int r = centerRow - 2; r <= centerRow + 2; r++)
        {
            for (int c = centerCol - 2; c <= centerCol + 2; c++)
            {
                if (r < 0 || r >= GameModel.MaxRows || c < 0 || c >= GameModel.MaxColumns)
                    continue;

                if (Math.Abs(r - centerRow) <= 1 && Math.Abs(c - centerCol) <= 1)
                    continue;

                if (_gameModel?.BubbleMatrix[r, c] == null)
                    continue;

                float dirX = c == centerCol ? 0f : (c > centerCol ? 1f : -1f);
                float dirY = r == centerRow ? 0f : (r > centerRow ? 1f : -1f);

                int key = r * GameModel.MaxColumns + c;

                if (_wobbles.TryGetValue(key, out var existing))
                {
                    existing.DirectionX = dirX;
                    existing.DirectionY = dirY;
                    existing.Util.Reset();
                }
                else
                {
                    _wobbles[key] = new BubbleWobble
                    {
                        DirectionX = dirX,
                        DirectionY = dirY,
                        Util = new AnimUtil(wobbleAmplitude)
                    };
                }
            }
        }
    }

    /// <summary>
    /// Добавляет падающий шарик
    /// </summary>
    /// <param name="bubble">Данные шарика</param>
    /// <param name="x">Позиция X</param>
    /// <param name="y">Позиция Y</param>
    private void AddFallingBubble(Bubble bubble, float x, float y)
    {
        _fallingBubbles.Add(new FallingBubble
        {
            Sprite = bubble.GetSprite(),
            X = x,
            Y = y,
            StartY = y,
            Progress = 0f,
        });
    }

    /// <summary>
    /// Очищает все анимации
    /// </summary>
    public void ClearAnimations()
    {
        _fallingBubbles.Clear();
        _disappearingBubbles.Clear();
        _wobbles.Clear();
        _hiddenBubbles.Clear();
        _newRowAnimation = null;
        _gameOverAnimation = null;
        _startTransitionAnimation = null;
        _rocketAnimation = null;
        _startScrollProgress = 0f;
    }

    /// <summary>
    /// Обрабатывает событие исчезновения шариков
    /// </summary>
    /// <param name="bubbles">Список исчезающих шариков</param>
    public void OnBubblesMatched(List<BubbleAnimationData> bubbles)
    {
        float scaleFactor = ScaleUtil.ScaleFactor;
        foreach (var bubbleData in bubbles)
        {
            var bubble = new Bubble(bubbleData.Color, bubbleData.Row, bubbleData.Column, bubbleData.Type);
            var (offsetX, offsetY) = bubble.GetOffset();

            float x = (GameModel.FieldOriginX + bubbleData.Column * GameModel.BubbleWidth + offsetX) * scaleFactor;
            float y = (GameModel.FieldOriginY + bubbleData.Row * GameModel.BubbleHeight + offsetY) * scaleFactor;

            _disappearingBubbles.Add(new DisappearingBubble
            {
                Row = bubbleData.Row,
                Sprite = bubble.GetSprite(),
                X = x,
                Y = y,
                ElapsedTime = 0f
            });
        }
    }

    /// <summary>
    /// Обрабатывает событие падения зависших шариков
    /// </summary>
    /// <param name="bubbles">Список падающих шариков</param>
    public void OnBubblesFloat(List<BubbleAnimationData> bubbles)
    {
        float scaleFactor = ScaleUtil.ScaleFactor;
        foreach (var bubbleData in bubbles)
        {
            var bubble = new Bubble(bubbleData.Color, bubbleData.Row, bubbleData.Column, bubbleData.Type);
            var (offsetX, offsetY) = bubble.GetOffset();

            float startX = (GameModel.FieldOriginX + bubbleData.Column * GameModel.BubbleWidth + offsetX) * scaleFactor;
            float startY = (GameModel.FieldOriginY + bubbleData.Row * GameModel.BubbleHeight + offsetY) * scaleFactor;

            AddFallingBubble(bubble, startX, startY);
        }
    }

    /// <summary>
    /// Обрабатывает событие добавления нового ряда
    /// </summary>
    public void OnNewRow()
    {
        _newRowAnimation = new NewRowAnimation
        {
            Progress = 0f
        };
    }

    /// <summary>
    /// Обрабатывает событие проигрыша
    /// </summary>
    public void OnGameOverFall()
    {
        _gameOverAnimation = new GameOverAnimation
        {
            RowAnimations = new RowFallAnimation[GameModel.MaxRows]
        };

        for (int i = 0; i < GameModel.MaxRows; i++)
        {
            _gameOverAnimation.RowAnimations[i] = new RowFallAnimation
            {
                StartDelay = (GameModel.MaxRows - 1 - i) * RowDelayBetween,
                Progress = 0f
            };
        }
    }

    /// <summary>
    /// Обрабатывает событие начала перехода от старта к игре
    /// </summary>
    public void OnStartTransition()
    {
        _startTransitionAnimation = new StartTransitionAnimation
        {
            RowAnimations = new RowFallAnimation[14],
            FirstRowsProgress = _startScrollProgress,
            ElapsedTime = 0f
        };

        for (int i = 4; i < 14; i++)
        {
            _startTransitionAnimation.RowAnimations[i] = new RowFallAnimation
            {
                StartDelay = (GameModel.MaxRows - 3 - i) * RowDelayBetween,
                Progress = 0f
            };
        }
    }

    /// <summary>
    /// Обрабатывает событие взрыва бомбы
    /// </summary>
    /// <param name="row">Ряд бомбы</param>
    /// <param name="col">Колонка бомбы</param>
    public void OnBombExplosion(int row, int col)
    {
        StartBombWobble(row, col);
    }

    /// <summary>
    /// Обрабатывает событие запуска ракеты
    /// </summary>
    /// <param name="row">Ряд запуска</param>
    /// <param name="col">Колонка запуска</param>
    /// <param name="targets">Список целей для уничтожения</param>
    public void OnRocketLaunch(int row, int col, List<(int, int)> targets)
    {
        var upTargets = new List<(int, int)>();
        var leftTargets = new List<(int, int)>();
        var rightTargets = new List<(int, int)>();

        foreach (var (r, c) in targets)
        {
            if (c == col && r < row)
                upTargets.Add((r, c));
            else if (r == row && c < col)
                leftTargets.Add((r, c));
            else if (r == row && c > col)
                rightTargets.Add((r, c));
        }

        _rocketAnimation = new RocketAnimation
        {
            CenterRow = row,
            CenterCol = col,
            UpTargets = upTargets,
            LeftTargets = leftTargets,
            RightTargets = rightTargets,
            UpProgress = 0f,
            LeftProgress = 0f,
            RightProgress = 0f,
            DestroyedBubbles = new HashSet<(int, int)>()
        };
    }

    /// <summary>
    /// Сбрасывает прогресс скроллинга стартового фона
    /// </summary>
    public void OnStartBackgroundChanged()
    {
        _startScrollProgress = 0f;
    }

    public IReadOnlyList<DisappearingBubble> DisappearingBubbles => _disappearingBubbles;
    public IReadOnlyList<FallingBubble> FallingBubbles => _fallingBubbles;
    public RocketAnimation? CurrentRocketAnimation => _rocketAnimation;
    public StartTransitionAnimation? CurrentStartTransition => _startTransitionAnimation;
    public float StartScrollProgress => _startScrollProgress;

    public class FallingBubble
    {
        public Image Sprite { get; init; } = null!;
        public float X { get; init; }
        public float Y { get; set; }
        public float StartY { get; init; }
        public float Progress { get; set; }
    }

    public class DisappearingBubble
    {
        public int Row { get; init; }
        public Image Sprite { get; init; } = null!;
        public float X { get; init; }
        public float Y { get; init; }
        public float ElapsedTime { get; set; }
    }

    private class BubbleWobble
    {
        public float DirectionX { get; set; }
        public float DirectionY { get; set; }
        public AnimUtil Util { get; init; } = null!;
    }

    public class RowFallAnimation
    {
        public float StartDelay { get; init; }
        public float Progress { get; set; }
    }

    private class NewRowAnimation
    {
        public float Progress { get; set; }
    }

    private class GameOverAnimation
    {
        public float ElapsedTime { get; set; }
        public RowFallAnimation[] RowAnimations { get; init; } = null!;
    }

    public class StartTransitionAnimation
    {
        public float FirstRowsProgress { get; set; }
        public float ElapsedTime { get; set; }
        public RowFallAnimation[] RowAnimations { get; init; } = null!;
    }

    public class RocketAnimation
    {
        public int CenterRow { get; init; }
        public int CenterCol { get; init; }
        public List<(int, int)> UpTargets { get; init; } = null!;
        public List<(int, int)> LeftTargets { get; init; } = null!;
        public List<(int, int)> RightTargets { get; init; } = null!;
        public float UpProgress { get; set; }
        public float LeftProgress { get; set; }
        public float RightProgress { get; set; }
        public HashSet<(int, int)> DestroyedBubbles { get; init; } = null!;
    }
}

/// <summary>
/// События, произошедшие за кадр обновления анимаций поля
/// </summary>
public class FieldAnimationEvents
{
    public bool MatchAnimationComplete { get; set; }
    public bool FloatAnimationComplete { get; set; }
    public bool NewRowAnimationComplete { get; set; }
}
