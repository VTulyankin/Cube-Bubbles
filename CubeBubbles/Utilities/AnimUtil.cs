namespace CubeBubbles.Utilities;

public class AnimUtil(float initialAmplitude, float decayRate = 15f, float phaseSpeed = 30f, float minAmplitude = 0.01f)
{
    private float _phase = 0f;
    private float _amplitude = initialAmplitude;
    private readonly float _initialAmplitude = initialAmplitude;

    public bool IsComplete => _amplitude <= minAmplitude;

    /// <summary>
    /// Сбрасывает анимацию к начальному состоянию
    /// </summary>
    public void Reset()
    {
        _phase = 0f;
        _amplitude = _initialAmplitude;
    }

    /// <summary>
    /// Обновляет фазу и амплитуду анимации
    /// </summary>
    /// <param name="deltaTime">Время с последнего кадра</param>
    public void Update(float deltaTime)
    {
        _phase += phaseSpeed * deltaTime;
        _amplitude *= MathF.Pow(0.1f, deltaTime * decayRate / 10f);
    }

    /// <summary>
    /// Рассчитывает текущее смещение анимации
    /// </summary>
    /// <returns>Смещение по синусоиде</returns>
    public float GetOffset()
    {
        return MathF.Sin(_phase) * _amplitude;
    }

    /// <summary>
    /// Применяет квадратичное ускорение к прогрессу
    /// </summary>
    /// <param name="t">Прогресс от 0 до 1</param>
    /// <returns>Прогресс с ease-in</returns>
    private static float EaseInQuad(float t)
    {
        return t * t;
    }

    /// <summary>
    /// Вычисляет позицию падающего шарика с ускорением
    /// </summary>
    /// <param name="progress">Прогресс от 0 до 1</param>
    /// <param name="startY">Начальная Y-позиция</param>
    /// <param name="distance">Дистанция падения</param>
    /// <param name="duration">Длительность анимации</param>
    /// <returns>Текущая Y-позиция</returns>
    public static float CalculateFallingPosition(float progress, float startY, float distance, float duration = 1.2f)
    {
        float normalizedProgress = Math.Min(progress / duration, 1f);
        float easedProgress = EaseInQuad(normalizedProgress);
        return startY + distance * easedProgress;
    }

    /// <summary>
    /// Вычисляет смещение ряда при добавлении нового ряда сверху
    /// </summary>
    /// <param name="globalProgress">Общий прогресс анимации</param>
    /// <param name="rowIndex">Индекс ряда</param>
    /// <param name="rowHeight">Высота одного ряда</param>
    /// <param name="delayBetweenRows">Задержка между рядами</param>
    /// <param name="duration">Длительность анимации ряда</param>
    /// <returns>Смещение по Y</returns>
    public static float CalculateRowOffset(float globalProgress, int rowIndex, float rowHeight, float delayBetweenRows = 0.12f, float duration = 1.2f)
    {
        float rowStartTime = rowIndex * delayBetweenRows;
        float rowProgress = Math.Clamp((globalProgress - rowStartTime) / duration, 0f, 1f);
        return (1f - rowProgress) * (-rowHeight);
    }

    /// <summary>
    /// Проверяет, завершена ли анимация всех рядов
    /// </summary>
    /// <param name="globalProgress">Общий прогресс анимации</param>
    /// <param name="totalRows">Количество рядов</param>
    /// <param name="delayBetweenRows">Задержка между рядами</param>
    /// <param name="duration">Длительность анимации ряда</param>
    /// <returns>true, если анимация завершена</returns>
    public static bool IsRowAnimationComplete(float globalProgress, int totalRows, float delayBetweenRows = 0.12f, float duration = 1.2f)
    {
        float totalDuration = duration + (totalRows - 1) * delayBetweenRows;
        return globalProgress >= totalDuration;
    }
}
