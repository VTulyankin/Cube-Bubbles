using System.Runtime.InteropServices;
using CubeBubbles.Utilities;

namespace CubeBubbles.Models;

public sealed class GameCursor : IDisposable
{
    private readonly Cursor _default;
    private readonly Cursor _active;
    private readonly Cursor _pressed;
    private Form? _form;
    private bool _isMousePressed;
    private readonly HashSet<Control> _hoveredInteractiveControls = new();
    private readonly HashSet<Control> _trackedControls = new();

    private GameCursor(Cursor @default, Cursor active, Cursor pressed)
    {
        _default = @default;
        _active = active;
        _pressed = pressed;
    }

    /// <summary>
    /// Создает и инициализирует курсор из файлов
    /// </summary>
    /// <param name="form">Форма для привязки курсора</param>
    /// <returns>Инициализированный объект GameCursor</returns>
    public static GameCursor Initialize(Form form)
    {
        const string folder = "Resources/cursors/";
        var defaultCursor = LoadCursor(Path.Combine(folder, "cursor.png"), 0, 0);
        var activeCursor = LoadCursor(Path.Combine(folder, "cursor_active.png"), 0, 0);
        var pressedCursor = LoadCursor(Path.Combine(folder, "cursor_pressed.png"), 0, 0);
        var gameCursor = new GameCursor(defaultCursor, activeCursor, pressedCursor);
        gameCursor.AttachToForm(form);
        return gameCursor;
    }

    /// <summary>
    /// Привязывает курсор к форме и подписывается на события мыши
    /// </summary>
    /// <param name="form">Форма для привязки</param>
    private void AttachToForm(Form form)
    {
        _form = form;
        _form.MouseDown += OnFormMouseDown;
        _form.MouseUp += OnFormMouseUp;
        ApplyCursor(_default);
    }

    /// <summary>
    /// Регистрирует контрол для отслеживания hover-состояния
    /// </summary>
    /// <param name="control">Контрол для отслеживания</param>
    public void Track(Control control)
    {
        if (!_trackedControls.Add(control))
            return;
        control.MouseEnter += OnControlMouseEnter;
        control.MouseLeave += OnControlMouseLeave;
        control.MouseDown += OnControlMouseDown;
        control.MouseUp += OnControlMouseUp;
        control.EnabledChanged += (_, _) => SyncHoverState(control);
        control.VisibleChanged += (_, _) => SyncHoverState(control);
        control.Disposed += (_, _) =>
        {
            _hoveredInteractiveControls.Remove(control);
            _trackedControls.Remove(control);
            UpdateCursor();
        };
    }

    /// <summary>
    /// Устанавливает hover-состояние для контрола вручную
    /// </summary>
    /// <param name="control">Контрол</param>
    /// <param name="isHovering">Состояние hover</param>
    public void SetHoverState(Control control, bool isHovering)
    {
        if (isHovering && IsInteractive(control))
            _hoveredInteractiveControls.Add(control);
        else
            _hoveredInteractiveControls.Remove(control);
        UpdateCursor();
    }

    /// <summary>
    /// Обрабатывает нажатие мыши на форме
    /// </summary>
    private void OnFormMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isMousePressed = true;
            UpdateCursor();
        }
    }

    /// <summary>
    /// Обрабатывает отпускание мыши на форме
    /// </summary>
    private void OnFormMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isMousePressed = false;
            UpdateCursor();
        }
    }

    /// <summary>
    /// Обрабатывает наведение мыши на контрол
    /// </summary>
    private void OnControlMouseEnter(object? sender, EventArgs e)
    {
        if (sender is Control control && IsInteractive(control))
        {
            _hoveredInteractiveControls.Add(control);
            UpdateCursor();
        }
    }

    /// <summary>
    /// Обрабатывает уход мыши с контрола
    /// </summary>
    private void OnControlMouseLeave(object? sender, EventArgs e)
    {
        if (sender is Control control)
        {
            _hoveredInteractiveControls.Remove(control);
            UpdateCursor();
        }
    }

    /// <summary>
    /// Обрабатывает нажатие мыши на контроле
    /// </summary>
    private void OnControlMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isMousePressed = true;
            UpdateCursor();
        }
    }

    /// <summary>
    /// Обрабатывает отпускание мыши на контроле
    /// </summary>
    private void OnControlMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isMousePressed = false;
            UpdateCursor();
        }
    }

    /// <summary>
    /// Синхронизирует hover-состояние контрола с фактическим положением мыши
    /// </summary>
    /// <param name="control">Контрол для синхронизации</param>
    private void SyncHoverState(Control control)
    {
        if (!control.IsHandleCreated || !IsInteractive(control))
        {
            _hoveredInteractiveControls.Remove(control);
            UpdateCursor();
            return;
        }

        bool isUnderCursor = control.ClientRectangle.Contains(control.PointToClient(Cursor.Position));
        if (isUnderCursor && IsInteractive(control))
            _hoveredInteractiveControls.Add(control);
        else
            _hoveredInteractiveControls.Remove(control);
        UpdateCursor();
    }

    /// <summary>
    /// Проверяет, доступен ли контрол для взаимодействия
    /// </summary>
    /// <param name="control">Контрол для проверки</param>
    /// <returns>true, если контрол видим и активен</returns>
    private static bool IsInteractive(Control control) => control is { Visible: true, Enabled: true };

    /// <summary>
    /// Обновляет курсор в зависимости от текущего состояния
    /// </summary>
    private void UpdateCursor()
    {
        if (_form == null)
            return;
        bool isHoveringInteractive = _hoveredInteractiveControls.Count > 0;
        var newCursor = _isMousePressed ? _pressed
            : isHoveringInteractive ? _active
            : _default;
        ApplyCursor(newCursor);
    }

    /// <summary>
    /// Применяет курсор к форме и всем отслеживаемым контролам
    /// </summary>
    /// <param name="cursor">Курсор для применения</param>
    private void ApplyCursor(Cursor cursor)
    {
        if (_form == null)
            return;
        _form.Cursor = cursor;
        foreach (var control in _trackedControls)
            control.Cursor = cursor;
    }

    /// <summary>
    /// Освобождает ресурсы курсора
    /// </summary>
    public void Dispose()
    {
        if (_form != null)
        {
            _form.MouseDown -= OnFormMouseDown;
            _form.MouseUp -= OnFormMouseUp;
        }

        _hoveredInteractiveControls.Clear();
        _trackedControls.Clear();
        _default.Dispose();
        _active.Dispose();
        _pressed.Dispose();
        _form = null;
    }

    /// <summary>
    /// Загружает и масштабирует курсор из файла
    /// </summary>
    /// <param name="path">Путь к файлу изображения</param>
    /// <param name="hotX">Координата X точки клика</param>
    /// <param name="hotY">Координата Y точки клика</param>
    /// <returns>Загруженный курсор</returns>
    private static Cursor LoadCursor(string path, int hotX, int hotY)
    {
        using var raw = (Bitmap)Image.FromFile(path);
        using var scaled = new Bitmap(ScaleUtil.ScaleImage(raw));
        return CreateCursorWithHotspot(scaled, hotX, hotY);
    }

    /// <summary>
    /// Создает курсор с заданной точкой клика через WinAPI
    /// </summary>
    /// <param name="bitmap">Изображение курсора</param>
    /// <param name="hotX">Координата X точки клика</param>
    /// <param name="hotY">Координата Y точки клика</param>
    /// <returns>Созданный курсор</returns>
    private static Cursor CreateCursorWithHotspot(Bitmap bitmap, int hotX, int hotY)
    {
        IntPtr hIcon = bitmap.GetHicon();
        if (!GetIconInfo(hIcon, out var iconInfo))
        {
            DestroyIcon(hIcon);
            throw new InvalidOperationException("GetIconInfo failed");
        }

        iconInfo.fIcon = false;
        iconInfo.xHotspot = hotX;
        iconInfo.yHotspot = hotY;
        IntPtr hCursor = CreateIconIndirect(ref iconInfo);
        if (iconInfo.hbmColor != IntPtr.Zero) DeleteObject(iconInfo.hbmColor);
        if (iconInfo.hbmMask != IntPtr.Zero) DeleteObject(iconInfo.hbmMask);
        DestroyIcon(hIcon);
        if (hCursor == IntPtr.Zero)
            throw new InvalidOperationException("CreateIconIndirect failed");
        return new Cursor(hCursor);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Iconinfo
    {
        [MarshalAs(UnmanagedType.Bool)]
        public bool fIcon;
        public int xHotspot;
        public int yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    [DllImport("user32.dll")]
    private static extern bool GetIconInfo(IntPtr hIcon, out Iconinfo piconinfo);

    [DllImport("user32.dll")]
    private static extern IntPtr CreateIconIndirect(ref Iconinfo icon);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);
}
