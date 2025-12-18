using CubeBubbles.Models;
using CubeBubbles.Utilities;
using CubeBubbles.Views.Components;

namespace CubeBubbles.Views.Menus;

public class MenuView(PictureBox window, List<NineSliceButton> buttons)
{
    private GameTimeManager? _timeManager;
    private int _previousOffset;
    private AnimUtil? _wobbleUtil;
    private bool _isSubscribed;

    /// <summary>
    /// Подключает менеджер времени для анимаций
    /// </summary>
    /// <param name="timeManager">Менеджер времени</param>
    public void SetTimeManager(GameTimeManager timeManager)
    {
        if (ReferenceEquals(_timeManager, timeManager))
            return;
        UnsubscribeFromTime();
        _timeManager = timeManager;
    }

    /// <summary>
    /// Показывает меню и запускает анимацию
    /// </summary>
    public void Show()
    {
        window.Visible = true;
        foreach (var button in buttons)
            button.Visible = true;
        window.BringToFront();
        foreach (var button in buttons)
            button.BringToFront();
        StartWobbleAnimation();
    }

    /// <summary>
    /// Скрывает меню и останавливает анимацию
    /// </summary>
    public void Hide()
    {
        StopAnimationAndResetOffset();
        window.Visible = false;
        foreach (var button in buttons)
            button.Visible = false;
    }

    /// <summary>
    /// Запускает анимацию колебания меню
    /// </summary>
    private void StartWobbleAnimation()
    {
        StopAnimationAndResetOffset();
        const float wobbleInitialAmplitude = 15f * ScaleUtil.ScaleFactor;
        _wobbleUtil = new AnimUtil(wobbleInitialAmplitude, 30f);
        _previousOffset = 0;
        SubscribeToTime();
    }

    /// <summary>
    /// Подписывается на события времени
    /// </summary>
    private void SubscribeToTime()
    {
        if (_timeManager == null || _isSubscribed)
            return;
        _timeManager.TimeUpdate += OnTimeUpdate;
        _isSubscribed = true;
    }

    /// <summary>
    /// Отписывается от событий времени
    /// </summary>
    private void UnsubscribeFromTime()
    {
        if (_timeManager == null || !_isSubscribed)
            return;
        _timeManager.TimeUpdate -= OnTimeUpdate;
        _isSubscribed = false;
    }

    /// <summary>
    /// Останавливает анимацию и сбрасывает смещение
    /// </summary>
    private void StopAnimationAndResetOffset()
    {
        UnsubscribeFromTime();
        if (_previousOffset != 0)
        {
            window.Location = new Point(window.Left, window.Top - _previousOffset);
            foreach (var button in buttons)
                button.Location = new Point(button.Left, button.Top - _previousOffset);
            _previousOffset = 0;
        }
        _wobbleUtil = null;
    }

    /// <summary>
    /// Обновляет анимацию колебания меню
    /// </summary>
    /// <param name="deltaTime">Время с последнего кадра</param>
    private void OnTimeUpdate(float deltaTime)
    {
        if (_wobbleUtil == null)
            return;
        _wobbleUtil.Update(deltaTime);
        if (_wobbleUtil.IsComplete)
        {
            StopAnimationAndResetOffset();
            return;
        }

        float wave = _wobbleUtil.GetOffset();
        int newOffset = -(int)wave;
        int deltaOffset = newOffset - _previousOffset;
        window.Location = new Point(window.Left, window.Top + deltaOffset);
        foreach (var button in buttons)
            button.Location = new Point(button.Left, button.Top + deltaOffset);
        _previousOffset = newOffset;
    }
}
