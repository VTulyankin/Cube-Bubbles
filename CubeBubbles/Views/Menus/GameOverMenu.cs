using CubeBubbles.Models;
using CubeBubbles.Views.Components;

namespace CubeBubbles.Views.Menus;

public class GameOverMenu
{
    private readonly NineSliceButton _btnOk;
    private readonly NineSliceButton _btnExit;
    private readonly MenuView _menuView;

    public event EventHandler? OkClicked;
    public event EventHandler? ExitClicked;

    /// <summary>
    /// Подключает менеджер времени к анимациям меню
    /// </summary>
    /// <param name="timeManager">Менеджер времени</param>
    public void SubscribeToTime(GameTimeManager timeManager) => _menuView.SetTimeManager(timeManager);

    public GameOverMenu(Control.ControlCollection parentControls)
    {
        var window = NineSliceWindow.CreateWindow(new Size(96, 51), new Point(106, 96), "Игра окончена");
        _btnOk = new NineSliceButton("Ок", "00BC00", new Size(43, 23), new Point(110, 117));
        _btnExit = new NineSliceButton("Выйти", "C10000", new Size(43, 23), new Point(155, 117));
        window.Visible = false;
        _btnOk.Visible = false;
        _btnExit.Visible = false;
        _btnOk.Click += (s, e) => OkClicked?.Invoke(this, EventArgs.Empty);
        _btnExit.Click += (s, e) => ExitClicked?.Invoke(this, EventArgs.Empty);
        parentControls.Add(window);
        parentControls.Add(_btnOk);
        parentControls.Add(_btnExit);
        _menuView = new MenuView(window, [_btnOk, _btnExit]);
    }

    /// <summary>
    /// Показывает меню Game Over
    /// </summary>
    public void Show() => _menuView.Show();

    /// <summary>
    /// Скрывает меню Game Over
    /// </summary>
    public void Hide() => _menuView.Hide();

    /// <summary>
    /// Возвращает список интерактивных кнопок меню
    /// </summary>
    /// <returns>Список кнопок</returns>
    public List<CustomButton> GetInteractiveButtons() => [_btnOk, _btnExit];
}