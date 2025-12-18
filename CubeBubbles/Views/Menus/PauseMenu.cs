using CubeBubbles.Models;
using CubeBubbles.Views.Components;

namespace CubeBubbles.Views.Menus;

public class PauseMenu
{
    private readonly NineSliceButton _btnResume;
    private readonly NineSliceButton _btnExit;
    private readonly MenuView _menuView;

    public event EventHandler? ResumeClicked;
    public event EventHandler? ExitClicked;

    /// <summary>
    /// Подключает менеджер времени к анимациям меню
    /// </summary>
    /// <param name="timeManager">Менеджер времени</param>
    public void SubscribeToTime(GameTimeManager timeManager) => _menuView.SetTimeManager(timeManager);

    public PauseMenu(Control.ControlCollection parentControls)
    {
        var window = NineSliceWindow.CreateWindow(new Size(87, 76), new Point(111, 84), "Пауза");
        _btnResume = new NineSliceButton("Продолжить", "00BC00", new Size(79, 23), new Point(115, 105));
        _btnExit = new NineSliceButton("Выйти", "C10000", new Size(79, 23), new Point(115, 130));
        window.Visible = false;
        _btnResume.Visible = false;
        _btnExit.Visible = false;
        _btnResume.Click += (s, e) => ResumeClicked?.Invoke(this, EventArgs.Empty);
        _btnExit.Click += (s, e) => ExitClicked?.Invoke(this, EventArgs.Empty);
        parentControls.Add(window);
        parentControls.Add(_btnResume);
        parentControls.Add(_btnExit);
        _menuView = new MenuView(window, [_btnResume, _btnExit]);
    }

    /// <summary>
    /// Показывает меню паузы
    /// </summary>
    public void Show() => _menuView.Show();

    /// <summary>
    /// Скрывает меню паузы
    /// </summary>
    public void Hide() => _menuView.Hide();

    /// <summary>
    /// Возвращает список интерактивных кнопок меню
    /// </summary>
    /// <returns>Список кнопок</returns>
    public List<CustomButton> GetInteractiveButtons() => [_btnResume, _btnExit];
}