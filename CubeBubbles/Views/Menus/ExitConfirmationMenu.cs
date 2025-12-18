using CubeBubbles.Models;
using CubeBubbles.Views.Components;

namespace CubeBubbles.Views.Menus;

public class ExitConfirmationMenu
{
    private readonly NineSliceButton _btnCancel;
    private readonly NineSliceButton _btnConfirm;
    private readonly MenuView _menuView;

    public event EventHandler? CancelClicked;
    public event EventHandler? ConfirmClicked;

    /// <summary>
    /// Подключает менеджер времени к анимациям меню
    /// </summary>
    /// <param name="timeManager">Менеджер времени</param>
    public void SubscribeToTime(GameTimeManager timeManager) => _menuView.SetTimeManager(timeManager);

    public ExitConfirmationMenu(Control.ControlCollection parentControls)
    {
        var window = NineSliceWindow.CreateWindow(new Size(110, 51), new Point(99, 96), "Выйти из игры?");
        _btnCancel = new NineSliceButton("Отмена", "00BC00", new Size(50, 23), new Point(103, 117));
        _btnConfirm = new NineSliceButton("Выйти", "C10000", new Size(50, 23), new Point(155, 117));
        window.Visible = false;
        _btnCancel.Visible = false;
        _btnConfirm.Visible = false;
        _btnCancel.Click += (s, e) => CancelClicked?.Invoke(this, EventArgs.Empty);
        _btnConfirm.Click += (s, e) => ConfirmClicked?.Invoke(this, EventArgs.Empty);
        parentControls.Add(window);
        parentControls.Add(_btnCancel);
        parentControls.Add(_btnConfirm);
        _menuView = new MenuView(window, [_btnCancel, _btnConfirm]);
    }

    /// <summary>
    /// Показывает меню подтверждения выхода
    /// </summary>
    public void Show() => _menuView.Show();

    /// <summary>
    /// Скрывает меню подтверждения выхода
    /// </summary>
    public void Hide() => _menuView.Hide();

    /// <summary>
    /// Возвращает список интерактивных кнопок меню
    /// </summary>
    /// <returns>Список кнопок</returns>
    public List<CustomButton> GetInteractiveButtons() => [_btnCancel, _btnConfirm];
}