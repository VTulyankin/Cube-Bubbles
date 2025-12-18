namespace CubeBubbles.Models;

public enum GameStatus { Start, Playing, Pause, GameOver, ExitConfirmation }

public record struct BubbleAnimationData(int Row, int Column, BubbleColor Color, BubbleType Type = BubbleType.Normal);

public class GameModel
{
    public const int MaxRows = 13;
    public const int MaxColumns = 16;
    public const int BubbleWidth = 16;
    public const int BubbleHeight = 16;
    public const int FieldOriginX = 26;
    public const int FieldOriginY = 22;

    private const int PointsPerMatchedBubble = 10;
    private const int PointsPerDroppedBubble = 20;
    private const float BoxSpawnChance = 0.10f;
    private const int MaxBoxesOnField = 5;
    private const int BoxCheckRadius = 2;

    private static readonly Dictionary<BubbleType, float> SpecialBubbleWeights = new()
    {
        { BubbleType.Chameleon, 0.5f },
        { BubbleType.Bomb, 0.3f },
        { BubbleType.Rocket, 0.2f }
    };

    public GameStatus Status { get; private set; } = GameStatus.Start;
    public int Score { get; private set; }
    public Bubble?[,] BubbleMatrix { get; private set; } = new Bubble?[MaxRows, MaxColumns];
    public Bubble?[,] StartBackgroundMatrix { get; } = new Bubble?[14, MaxColumns];
    public Player Player { get; } = new();
    public bool IsMouseControlMode { get; private set; }

    public event Action<GameStatus>? StatusChanged;
    public event Action<BubbleAnimationData>? BubbleShotAnimation;
    public event Action<List<BubbleAnimationData>>? BubblesMatchAnimation;
    public event Action<List<BubbleAnimationData>>? BubblesFloatAnimation;
    public event Action? NewRowAnimation;
    public event Action? GameOverFallAnimation;
    public event Action? StartBackgroundFallAnimation;
    public event Action<bool>? MouseControlModeChanged;
    public event Action? StartBackgroundChanged;
    public event Action<int, int>? BombExplosionAnimation;
    public event Action<int, int, List<(int, int)>>? RocketLaunchAnimation;

    private bool _pendingGameOverCheck;
    private bool _pendingFloatingCheck;
    private int _pendingBoxRewards;
    private GameStatus _previousStatus = GameStatus.Start;
    private static readonly Random Random = new();
    private bool _isBubbleFlying;

    public GameModel()
    {
        InitializeStartBackground();
    }

    /// <summary>
    /// Начинает новую игру
    /// </summary>
    public void StartGame()
    {
        ClearField();
        SetStatus(GameStatus.Playing);
        SetScore(0);
        Player.Reset();
        IsMouseControlMode = false;
        _pendingGameOverCheck = false;
        _pendingFloatingCheck = false;
        _pendingBoxRewards = 0;
        _isBubbleFlying = false;
    }

    /// <summary>
    /// Очищает игровое поле
    /// </summary>
    private void ClearField()
    {
        BubbleMatrix = new Bubble?[MaxRows, MaxColumns];
    }

    /// <summary>
    /// Инициализирует фон стартового экрана
    /// </summary>
    public void InitializeStartBackground()
    {
        for (int row = 0; row < 14; row++)
        {
            for (int col = 0; col < MaxColumns; col++)
            {
                var color = GetRandomBubbleColor();
                StartBackgroundMatrix[row, col] = new Bubble(color, row, col);
            }
        }
    }

    /// <summary>
    /// Сдвигает фон стартового экрана вниз и генерирует новый ряд
    /// </summary>
    public void ShiftStartBackgroundDown()
    {
        if (Status != GameStatus.Start) return;

        for (int row = 13; row > 0; row--)
        {
            for (int col = 0; col < MaxColumns; col++)
            {
                StartBackgroundMatrix[row, col] = StartBackgroundMatrix[row - 1, col];
                if (StartBackgroundMatrix[row, col] != null)
                    StartBackgroundMatrix[row, col]!.Row = row;
            }
        }

        for (int col = 0; col < MaxColumns; col++)
        {
            var color = GetRandomBubbleColor();
            StartBackgroundMatrix[0, col] = new Bubble(color, 0, col);
        }

        StartBackgroundChanged?.Invoke();
    }

    /// <summary>
    /// Запускает анимацию перехода к игре
    /// </summary>
    public void TriggerStartTransition()
    {
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < MaxColumns; col++)
            {
                var bubble = StartBackgroundMatrix[row, col];
                if (bubble != null)
                {
                    BubbleMatrix[row, col] = new Bubble(bubble.Color, row, col);
                }
            }
        }

        StartBackgroundFallAnimation?.Invoke();
    }

    /// <summary>
    /// Заполняет поле начальными рядами без анимации
    /// </summary>
    public void PopulateInitialField()
    {
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < MaxColumns; col++)
            {
                var color = GetRandomBubbleColor();
                BubbleMatrix[row, col] = new Bubble(color, row, col);
            }
        }

    }

    /// <summary>
    /// Генерирует шарик с возможностью создания коробки
    /// </summary>
    /// <param name="row">Ряд</param>
    /// <param name="col">Колонка</param>
    private void GenerateBubble(int row, int col)
    {
        if (Random.NextDouble() < BoxSpawnChance && CanPlaceBox(row, col))
        {
            BubbleMatrix[row, col] = new Bubble(BubbleColor.Red, row, col, BubbleType.Box);
        }
        else
        {
            var color = GetRandomBubbleColor();
            BubbleMatrix[row, col] = new Bubble(color, row, col);
        }
    }

    /// <summary>
    /// Проверяет можно ли поставить коробку в позицию
    /// </summary>
    /// <param name="row">Ряд</param>
    /// <param name="col">Колонка</param>
    /// <returns>True если можно поставить коробку</returns>
    private bool CanPlaceBox(int row, int col)
    {
        int currentBoxCount = CountBoxesOnField();
        if (currentBoxCount >= MaxBoxesOnField)
            return false;

        for (int r = row - BoxCheckRadius; r <= row + BoxCheckRadius; r++)
        {
            for (int c = col - BoxCheckRadius; c <= col + BoxCheckRadius; c++)
            {
                if (r is >= 0 and < MaxRows && c is >= 0 and < MaxColumns)
                {
                    if (BubbleMatrix[r, c]?.Type == BubbleType.Box)
                        return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Считает количество коробок на поле
    /// </summary>
    /// <returns>Количество коробок</returns>
    private int CountBoxesOnField()
    {
        int count = 0;
        for (int row = 0; row < MaxRows; row++)
        {
            for (int col = 0; col < MaxColumns; col++)
            {
                if (BubbleMatrix[row, col]?.Type == BubbleType.Box)
                    count++;
            }
        }

        return count;
    }
    

    /// <summary>
    /// Добавляет новый ряд шариков сверху и сдвигает все вниз
    /// </summary>
    public void AddRow()
    {
        if (Status != GameStatus.Playing) return;
        if (_isBubbleFlying) return;

        for (int row = MaxRows - 1; row > 0; row--)
        {
            for (int col = 0; col < MaxColumns; col++)
            {
                BubbleMatrix[row, col] = BubbleMatrix[row - 1, col];
                if (BubbleMatrix[row, col] != null)
                    BubbleMatrix[row, col]!.Row = row;
            }
        }

        for (int col = 0; col < MaxColumns; col++)
        {
            GenerateBubble(0, col);
        }

        _pendingGameOverCheck = true;
        NewRowAnimation?.Invoke();
    }

    /// <summary>
    /// Вызывается после завершения анимации добавления ряда
    /// </summary>
    public void OnNewRowAnimationComplete()
    {
        if (_pendingGameOverCheck)
        {
            _pendingGameOverCheck = false;

            for (int col = 0; col < MaxColumns; col++)
            {
                if (BubbleMatrix[MaxRows - 1, col] != null)
                {
                    TriggerGameOver();
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Запускает процесс окончания игры
    /// </summary>
    private void TriggerGameOver()
    {
        if (IsMouseControlMode)
        {
            DisableMouseControl();
        }

        SetStatus(GameStatus.GameOver);
        GameOverFallAnimation?.Invoke();
    }

    /// <summary>
    /// Стреляет текущим шариком игрока
    /// </summary>
    public void PlayerShoot()
    {
        if (Status != GameStatus.Playing) return;

        var bubbleColor = Player.CurrentBubble;
        var bubbleType = Player.CurrentBubbleType;
        Player.Shoot();
        AddBubbleToColumn(Player.Position, bubbleColor, bubbleType);
    }

    /// <summary>
    /// Добавляет шарик в колонку
    /// </summary>
    /// <param name="column">Индекс колонки</param>
    /// <param name="color">Цвет шарика</param>
    /// <param name="type">Тип шарика</param>
    private void AddBubbleToColumn(int column, BubbleColor color, BubbleType type)
    {
        int targetRow = FindTargetRow(column);

        if (targetRow == -1 || targetRow >= MaxRows - 1)
        {
            TriggerGameOver();
            return;
        }

        _isBubbleFlying = true;
        BubbleMatrix[targetRow, column] = new Bubble(color, targetRow, column, type);
        BubbleShotAnimation?.Invoke(new BubbleAnimationData(targetRow, column, color, type));
    }

    /// <summary>
    /// Находит целевой ряд для шарика в колонке
    /// </summary>
    /// <param name="column">Индекс колонки</param>
    /// <returns>Индекс ряда или -1</returns>
    private int FindTargetRow(int column)
    {
        for (var row = MaxRows - 2; row >= 0; row--)
        {
            if (BubbleMatrix[row, column] != null)
            {
                int targetRow = row + 1;
                return targetRow >= MaxRows - 1 ? -1 : targetRow;
            }
        }

        return 0;
    }

    /// <summary>
    /// Вызывается после приземления шарика
    /// </summary>
    /// <param name="row">Ряд шарика</param>
    /// <param name="column">Колонка шарика</param>
    public void OnBubbleLanded(int row, int column)
    {
        _isBubbleFlying = false;

        if (Status != GameStatus.Playing) return;

        var bubble = BubbleMatrix[row, column];
        if (bubble == null) return;

        switch (bubble.Type)
        {
            case BubbleType.Bomb:
                ProcessBombExplosion(row, column);
                break;
            case BubbleType.Rocket:
                ProcessRocketLaunch(row, column);
                break;
            case BubbleType.Box:
                CheckForMatches(row, column);
                break;
            default:
                CheckForMatches(row, column);
                break;
        }
    }

    /// <summary>
    /// Обрабатывает взрыв бомбы
    /// </summary>
    /// <param name="row">Ряд бомбы</param>
    /// <param name="column">Колонка бомбы</param>
    private void ProcessBombExplosion(int row, int column)
    {
        var bubblesInRadius = GetBubblesInRadius(row, column, 1);

        BubbleMatrix[row, column] = null;
        foreach (var (r, c) in bubblesInRadius)
        {
            BubbleMatrix[r, c] = null;
        }

        BombExplosionAnimation?.Invoke(row, column);
        _pendingFloatingCheck = true;
    }

    /// <summary>
    /// Получает список шариков в радиусе (включая диагонали)
    /// </summary>
    /// <param name="centerRow">Центральный ряд</param>
    /// <param name="centerCol">Центральная колонка</param>
    /// <param name="radius">Радиус</param>
    /// <returns>Список координат</returns>
    private List<(int, int)> GetBubblesInRadius(int centerRow, int centerCol, int radius)
    {
        var result = new List<(int, int)>();

        for (int r = centerRow - radius; r <= centerRow + radius; r++)
        {
            for (int c = centerCol - radius; c <= centerCol + radius; c++)
            {
                if (r == centerRow && c == centerCol) continue;

                if (r is >= 0 and < MaxRows && c is >= 0 and < MaxColumns)
                {
                    if (BubbleMatrix[r, c] != null)
                    {
                        result.Add((r, c));
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Обрабатывает запуск ракеты
    /// </summary>
    /// <param name="row">Ряд ракеты</param>
    /// <param name="column">Колонка ракеты</param>
    private void ProcessRocketLaunch(int row, int column)
    {
        var destroyedBubbles = new List<(int, int)>();

        for (int r = 0; r < MaxRows; r++)
        {
            if (r != row && BubbleMatrix[r, column] != null)
            {
                destroyedBubbles.Add((r, column));
            }
        }

        for (int c = 0; c < MaxColumns; c++)
        {
            if (c != column && BubbleMatrix[row, c] != null)
            {
                destroyedBubbles.Add((row, c));
            }
        }

        BubbleMatrix[row, column] = null;
        RocketLaunchAnimation?.Invoke(row, column, destroyedBubbles);
    }

    /// <summary>
    /// Вызывается после завершения анимации ракеты для уничтожения шариков
    /// </summary>
    /// <param name="destroyedBubbles">Список уничтоженных шариков</param>
    public void OnRocketAnimationStep(List<(int row, int col)> destroyedBubbles)
    {
        foreach (var (r, c) in destroyedBubbles)
        {
            BubbleMatrix[r, c] = null;
        }

        CheckFloatingIslands();
    }

    /// <summary>
    /// Проверяет комбинации из 3+ шариков
    /// </summary>
    /// <param name="row">Ряд шарика</param>
    /// <param name="column">Колонка шарика</param>
    private void CheckForMatches(int row, int column)
    {
        var placedBubble = BubbleMatrix[row, column];
        if (placedBubble == null) return;

        if (placedBubble.Type == BubbleType.Box)
        {
            return;
        }

        var connectedBubbles = placedBubble.Type == BubbleType.Chameleon
            ? FindConnectedBubblesWithChameleon(row, column)
            : FindConnectedBubbles(row, column, placedBubble.Color);

        if (connectedBubbles.Count >= 3)
        {
            AddScore(connectedBubbles.Count * PointsPerMatchedBubble);

            var animData = connectedBubbles.Select(b =>
                new BubbleAnimationData(b.Row, b.Column, b.Color, b.Type)).ToList();

            foreach (var bubble in connectedBubbles)
            {
                BubbleMatrix[bubble.Row, bubble.Column] = null;
            }

            BubblesMatchAnimation?.Invoke(animData);
            _pendingFloatingCheck = true;
        }
    }

    /// <summary>
    /// Находит соединённые шарики с учетом хамелеона
    /// </summary>
    /// <param name="startRow">Начальный ряд</param>
    /// <param name="startCol">Начальная колонка</param>
    /// <returns>Список соединённых шариков</returns>
    private List<Bubble> FindConnectedBubblesWithChameleon(int startRow, int startCol)
    {
        var connected = new List<Bubble>();
        var neighbors = new[] { (startRow - 1, startCol), (startRow + 1, startCol), (startRow, startCol - 1), (startRow, startCol + 1) };

        connected.Add(BubbleMatrix[startRow, startCol]!);

        foreach (var (nRow, nCol) in neighbors)
        {
            if (nRow is >= 0 and < MaxRows && nCol is >= 0 and < MaxColumns)
            {
                var neighborBubble = BubbleMatrix[nRow, nCol];
                if (neighborBubble != null && neighborBubble.Type != BubbleType.Box)
                {
                    var groupBubbles = neighborBubble.Type == BubbleType.Chameleon
                        ? FindConnectedBubblesWithChameleon(nRow, nCol)
                        : FindConnectedBubbles(nRow, nCol, neighborBubble.Color);

                    foreach (var b in groupBubbles.Where(
                                 b => !connected.Any(
                                     cb => cb.Row == b.Row && cb.Column == b.Column)))
                    {
                        connected.Add(b);
                    }
                }
            }
        }

        return connected;
    }

    /// <summary>
    /// Вызывается после анимации исчезновения шариков
    /// </summary>
    public void OnMatchAnimationComplete()
    {
        if (_pendingFloatingCheck)
        {
            _pendingFloatingCheck = false;
            CheckFloatingIslands();
        }
    }

    /// <summary>
    /// Проверяет зависшие группы шариков
    /// </summary>
    private void CheckFloatingIslands()
    {
        var floatingBubbles = FindFloatingIslands();

        if (floatingBubbles.Count > 0)
        {
            int boxCount = floatingBubbles.Count(b => b.Type == BubbleType.Box);
            AddScore((floatingBubbles.Count - boxCount) * PointsPerDroppedBubble);

            var animData = floatingBubbles.Select(b =>
                new BubbleAnimationData(b.Row, b.Column, b.Color, b.Type)).ToList();

            foreach (var bubble in floatingBubbles)
            {
                BubbleMatrix[bubble.Row, bubble.Column] = null;
            }

            BubblesFloatAnimation?.Invoke(animData);
            _pendingBoxRewards = boxCount;
        }
    }

    /// <summary>
    /// Вызывается после завершения анимации падения зависших шариков
    /// </summary>
    public void OnFloatAnimationComplete()
    {
        if (_pendingBoxRewards > 0)
        {
            for (int i = 0; i < _pendingBoxRewards; i++)
            {
                Player.AddSpecialBubble(GetRandomSpecialBubble());
            }

            _pendingBoxRewards = 0;
        }
    }

    /// <summary>
    /// Выбирает случайный спец. шарик по весам
    /// </summary>
    /// <returns>Тип спец. шарика</returns>
    private BubbleType GetRandomSpecialBubble()
    {
        float totalWeight = SpecialBubbleWeights.Values.Sum();
        float randomValue = (float)(Random.NextDouble() * totalWeight);
        float currentWeight = 0f;

        foreach (var kvp in SpecialBubbleWeights)
        {
            currentWeight += kvp.Value;
            if (randomValue <= currentWeight)
            {
                return kvp.Key;
            }
        }

        return BubbleType.Bomb;
    }

    /// <summary>
    /// Находит соединённые шарики одного цвета
    /// </summary>
    /// <param name="startRow">Начальный ряд</param>
    /// <param name="startCol">Начальная колонка</param>
    /// <param name="color">Цвет для поиска</param>
    /// <returns>Список соединённых шариков</returns>
    private List<Bubble> FindConnectedBubbles(int startRow, int startCol, BubbleColor color)
    {
        var connected = new List<Bubble>();
        var visited = new bool[MaxRows, MaxColumns];
        var queue = new Queue<(int row, int col)>();

        queue.Enqueue((startRow, startCol));
        visited[startRow, startCol] = true;

        while (queue.Count > 0)
        {
            var (row, col) = queue.Dequeue();
            var bubble = BubbleMatrix[row, col];

            if (bubble != null && (bubble.Color == color || bubble.Type == BubbleType.Chameleon) && bubble.Type != BubbleType.Box)
            {
                connected.Add(bubble);

                var neighbors = new[] { (row - 1, col), (row + 1, col), (row, col - 1), (row, col + 1) };
                foreach (var (nRow, nCol) in neighbors)
                {
                    if (nRow is >= 0 and < MaxRows && nCol is >= 0 and < MaxColumns && !visited[nRow, nCol])
                    {
                        visited[nRow, nCol] = true;
                        if (BubbleMatrix[nRow, nCol]?.Color == color || BubbleMatrix[nRow, nCol]?.Type == BubbleType.Chameleon)
                        {
                            queue.Enqueue((nRow, nCol));
                        }
                    }
                }
            }
        }

        return connected;
    }

    /// <summary>
    /// Находит все зависшие шарики
    /// </summary>
    /// <returns>Список зависших шариков</returns>
    private List<Bubble> FindFloatingIslands()
    {
        var supported = new bool[MaxRows, MaxColumns];
        var queue = new Queue<(int row, int col)>();

        for (int col = 0; col < MaxColumns; col++)
        {
            if (BubbleMatrix[0, col] != null)
            {
                queue.Enqueue((0, col));
                supported[0, col] = true;
            }
        }

        while (queue.Count > 0)
        {
            var (row, col) = queue.Dequeue();
            var neighbors = new[] { (row - 1, col), (row + 1, col), (row, col - 1), (row, col + 1) };

            foreach (var (nRow, nCol) in neighbors)
            {
                if (nRow is >= 0 and < MaxRows && nCol is >= 0 and < MaxColumns &&
                    !supported[nRow, nCol] && BubbleMatrix[nRow, nCol] != null)
                {
                    supported[nRow, nCol] = true;
                    queue.Enqueue((nRow, nCol));
                }
            }
        }

        var floating = new List<Bubble>();
        for (var row = 0; row < MaxRows - 1; row++)
        {
            for (var col = 0; col < MaxColumns; col++)
            {
                if (BubbleMatrix[row, col] != null && !supported[row, col])
                    floating.Add(BubbleMatrix[row, col]!);
            }
        }

        return floating;
    }

    public void MovePlayerLeft() => Player.MoveLeft();
    public void MovePlayerRight() => Player.MoveRight();
    public void SwapPlayerBubbles() => Player.SwapBubbles();

    /// <summary>
    /// Устанавливает позицию игрока мышью
    /// </summary>
    /// <param name="mouseX">Индекс столбца</param>
    public void SetPlayerPositionByMouse(int mouseX)
    {
        if (Status != GameStatus.Playing || !IsMouseControlMode) return;
        Player.SetPosition(mouseX);
    }

    /// <summary>
    /// Включает режим управления мышью
    /// </summary>
    public void EnableMouseControl()
    {
        if (Status == GameStatus.Playing && !IsMouseControlMode)
        {
            IsMouseControlMode = true;
            MouseControlModeChanged?.Invoke(true);
        }
    }

    /// <summary>
    /// Выключает режим управления мышью
    /// </summary>
    private void DisableMouseControl()
    {
        if (IsMouseControlMode)
        {
            IsMouseControlMode = false;
            MouseControlModeChanged?.Invoke(false);
        }
    }

    /// <summary>
    /// Генерирует случайный цвет шарика
    /// </summary>
    /// <returns>Случайный цвет</returns>
    public static BubbleColor GetRandomBubbleColor()
    {
        var colors = Enum.GetValues(typeof(BubbleColor));
        return (BubbleColor)colors.GetValue(Random.Next(colors.Length))!;
    }

    /// <summary>
    /// Ставит игру на паузу
    /// </summary>
    private void PauseGame()
    {
        if (Status == GameStatus.Playing)
        {
            if (IsMouseControlMode)
                DisableMouseControl();
            SetStatus(GameStatus.Pause);
        }
    }

    /// <summary>
    /// Возобновляет игру после паузы
    /// </summary>
    public void ResumeGame()
    {
        if (Status == GameStatus.Pause)
            SetStatus(GameStatus.Playing);
    }
    
    /// <summary>
    /// Показывает диалог выхода
    /// </summary>
    public void ShowExitConfirmation()
    {
        if (Status == GameStatus.Start)
        {
            SetStatus(GameStatus.ExitConfirmation);
        }
        else if (Status == GameStatus.Playing)
        {
            if (IsMouseControlMode)
            {
                DisableMouseControl();
            }
            SetStatus(GameStatus.ExitConfirmation);
        }
    }

    /// <summary>
    /// Возвращает на стартовый экран
    /// </summary>
    public void ReturnToStart()
    {
        ClearField();
        SetStatus(GameStatus.Start);
    }

    /// <summary>
    /// Отменяет выход
    /// </summary>
    public void CancelExit()
    {
        if (Status == GameStatus.ExitConfirmation)
            SetStatus(GameStatus.Playing);
    }

    /// <summary>
    /// Обрабатывает нажатие Escape
    /// </summary>
    public void HandleEscapeKey()
    {
        if (IsMouseControlMode)
        {
            DisableMouseControl();
        }
        else if (Status == GameStatus.Playing)
        {
            PauseGame();
        }
        else if (Status == GameStatus.Pause)
        {
            ResumeGame();
        }
    }

    /// <summary>
    /// Устанавливает новый статус игры
    /// </summary>
    /// <param name="newStatus">Новый статус</param>
    private void SetStatus(GameStatus newStatus)
    {
        _previousStatus = Status;
        Status = newStatus;
        StatusChanged?.Invoke(newStatus);
    }

    /// <summary>
    /// Проверяет предыдущий статус
    /// </summary>
    /// <param name="status">Статус для проверки</param>
    /// <returns>True если предыдущий статус совпадает</returns>
    public bool WasPreviousStatus(GameStatus status) => _previousStatus == status;

    /// <summary>
    /// Устанавливает очки
    /// </summary>
    /// <param name="score">Новое значение очков</param>
    private void SetScore(int score)
    {
        Score = score;
    }

    /// <summary>
    /// Добавляет очки к счёту
    /// </summary>
    /// <param name="points">Количество очков</param>
    private void AddScore(int points) => SetScore(Score + points);
}