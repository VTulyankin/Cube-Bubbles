using CubeBubbles.Views.Components;

namespace CubeBubbles.Views.Menus;

public class StartMenu
{
    private readonly PictureBox _background;
    private readonly NineSliceButton _btnStart;
    private readonly Control.ControlCollection _parentControls;

    public event EventHandler? StartClicked;

    public StartMenu(Control.ControlCollection parentControls)
    {
        _parentControls = parentControls;
        _background = NineSliceWindow.CreateWindow(new Size(86, 34), new Point(111, 105));
        _btnStart = new NineSliceButton("Начать игру", "00BC00", new Size(78, 23), new Point(115, 109));
        _background.Visible = false;
        _btnStart.Visible = false;
        _btnStart.Click += (s, e) => StartClicked?.Invoke(this, EventArgs.Empty);
        parentControls.Add(_background);
        parentControls.Add(_btnStart);
    }

    /// <summary>
    /// Показывает стартовое меню
    /// </summary>
    public void Show()
    {
        if (_parentControls.Owner is Form f) f.SuspendLayout();
        _background.Visible = true;
        _btnStart.Visible = true;
        _background.BringToFront();
        _btnStart.BringToFront();
        if (_parentControls.Owner is Form form) form.ResumeLayout();
    }

    /// <summary>
    /// Скрывает стартовое меню
    /// </summary>
    public void Hide()
    {
        _background.Visible = false;
        _btnStart.Visible = false;
    }

    /// <summary>
    /// Возвращает список интерактивных кнопок меню
    /// </summary>
    /// <returns>Список кнопок</returns>
    public List<CustomButton> GetInteractiveButtons() => [_btnStart];
}