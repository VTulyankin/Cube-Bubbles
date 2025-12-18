using System.Diagnostics;

namespace CubeBubbles.Models;

/// <summary>
/// Глобальный менеджер времени и автоматического добавления рядов
/// </summary>
public class GameTimeManager
{
    private readonly Stopwatch _stopwatch = new();
    private long _lastTickTime;
    private float _timeSinceLastRow;
    private float _timeSinceLastBackgroundShift;
    private const float BaseRowInterval = 10f;
    private const float StartBackgroundShiftInterval = 2f;
    private const float RowFactor = 0.04f;
    private const float ScoreFactor = 0.00008f;

    public event Action<float>? TimeUpdate;

    private GameModel? _gameModel;

    public GameTimeManager()
    {
        _stopwatch.Start();
        _lastTickTime = _stopwatch.ElapsedMilliseconds;
        _timeSinceLastRow = 0f;
        _timeSinceLastBackgroundShift = 0f;
    }

    /// <summary>
    /// Устанавливает модель игры для управления
    /// </summary>
    /// <param name="model">Модель игры</param>
    public void SetGameModel(GameModel model)
    {
        _gameModel = model;
    }

    /// <summary>
    /// Обновляет время и проверяет условия для добавления рядов
    /// </summary>
    public void Update()
    {
        var currentTime = _stopwatch.ElapsedMilliseconds;
        var deltaTime = (currentTime - _lastTickTime) / 1000f;
        _lastTickTime = currentTime;

        deltaTime = Math.Min(deltaTime, 0.033f);

        TimeUpdate?.Invoke(deltaTime);

        if (_gameModel?.Status == GameStatus.Playing)
        {
            _timeSinceLastRow += deltaTime;
            float nextRowInterval = CalculateNextRowInterval();
            if (_timeSinceLastRow >= nextRowInterval)
            {
                _timeSinceLastRow = 0f;
                _gameModel.AddRow();
            }
        }
        else if (_gameModel?.Status == GameStatus.Start)
        {
            _timeSinceLastBackgroundShift += deltaTime;
            if (_timeSinceLastBackgroundShift >= StartBackgroundShiftInterval)
            {
                _timeSinceLastBackgroundShift = 0f;
                _gameModel.ShiftStartBackgroundDown();
            }
        }
        else
        {
            _timeSinceLastRow = 0f;
        }
    }

    /// <summary>
    /// Вычисляет интервал до следующего добавления ряда
    /// </summary>
    /// <returns>Время в секундах</returns>
    private float CalculateNextRowInterval()
    {
        if (_gameModel == null) return BaseRowInterval;

        int filledRowCount = CountFilledRows();
        float rowModifier = 1f + (filledRowCount * RowFactor);
        float scoreModifier = 1f + (_gameModel.Score * ScoreFactor);
        float interval = BaseRowInterval * rowModifier / scoreModifier;
        interval = Math.Clamp(interval, 15f, 45f);

        return interval;
    }

    /// <summary>
    /// Подсчитывает количество рядов с хотя бы одним шариком
    /// </summary>
    /// <returns>Количество заполненных рядов</returns>
    private int CountFilledRows()
    {
        if (_gameModel == null) return 0;
        int count = 0;

        for (int row = 0; row < GameModel.MaxRows - 1; row++)
        {
            for (int col = 0; col < GameModel.MaxColumns; col++)
            {
                if (_gameModel.BubbleMatrix[row, col] != null)
                {
                    count++;
                    break;
                }
            }
        }

        return count;
    }
    

    /// <summary>
    /// Сбрасывает таймер сдвига стартового фона
    /// </summary>
    public void ResetBackgroundTimer()
    {
        _timeSinceLastBackgroundShift = 0f;
    }
}
