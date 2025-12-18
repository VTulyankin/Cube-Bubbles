using CubeBubbles.Utilities;

namespace CubeBubbles.Views.Components;

public class CustomButton : Button
{
    private readonly Image _normalImage;
    private readonly Image _pressedImage;
    private readonly Image _highlightedImage;
    private readonly int _heightDifference;
    private bool _isHighlighted;
    private bool _isPressed;

    public CustomButton(string path, Point location)
    {
        DoubleBuffered = true;
        const string defaultFolder = "Resources/buttons/";
        Location = ScaleUtil.ScalePoint(location);
            
        _normalImage = ScaleUtil.ScaleImage(Image.FromFile(defaultFolder + path + ".png"));
        _pressedImage = ScaleUtil.ScaleImage(Image.FromFile(defaultFolder + path + "_pressed.png"));
        
        // Пытаемся загрузить highlighted изображение, если существует
        var highlightedPath = defaultFolder + path + "_highlighted.png";
        _highlightedImage = File.Exists(highlightedPath) 
            ? ScaleUtil.ScaleImage(Image.FromFile(highlightedPath)) 
            : _pressedImage;

        _heightDifference = _normalImage.Height - _pressedImage.Height;
            
        InitializeButton();
    }

    protected CustomButton((Image normal, Image pressed, Image highlighted) images, Point location)
    {
        DoubleBuffered = true;
        Location = ScaleUtil.ScalePoint(location);
            
        _normalImage = ScaleUtil.ScaleImage(images.normal);
        _pressedImage = ScaleUtil.ScaleImage(images.pressed);
        _highlightedImage = ScaleUtil.ScaleImage(images.highlighted);
        _heightDifference = _normalImage.Height - _pressedImage.Height;
        
        InitializeButton();
    }

    /// <summary>
    /// Инициализирует стиль и свойства кнопки
    /// </summary>
    private void InitializeButton()
    {
        BackgroundImage = _normalImage;
        BackgroundImageLayout = ImageLayout.Stretch;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        FlatAppearance.MouseDownBackColor = Color.Transparent;
        BackColor = Color.Transparent;
        Size = _normalImage.Size;
        SetStyle(ControlStyles.Selectable, true);
    }

    protected override bool ShowFocusCues => false;

    /// <summary>
    /// Обрабатывает получение фокуса кнопкой
    /// </summary>
    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        _isHighlighted = true;
        UpdateAppearance();
    }

    /// <summary>
    /// Обрабатывает потерю фокуса кнопкой
    /// </summary>
    protected override void OnLostFocus(EventArgs e)
    {
        _isHighlighted = false;
        if (_isPressed)
        {
            _isPressed = false;
            Height += _heightDifference;
            Top -= _heightDifference;
        }
        UpdateAppearance();
        base.OnLostFocus(e);
    }

    /// <summary>
    /// Разрешает форме обрабатывать клавиши навигации
    /// </summary>
    /// <param name="keyData">Нажатая клавиша</param>
    /// <returns>false для передачи клавиши форме</returns>
    protected override bool ProcessDialogKey(Keys keyData)
    {
        if (keyData is Keys.Up or Keys.Down or Keys.Left or Keys.Right or Keys.Tab or Keys.Enter or Keys.Shift)
            return false;
        return base.ProcessDialogKey(keyData);
    }

    /// <summary>
    /// Обрабатывает нажатие клавиши на кнопке
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            if (!_isPressed)
            {
                _isPressed = true;
                UpdateAppearance();
            }
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }
        base.OnKeyDown(e);
    }

    /// <summary>
    /// Обрабатывает отпускание клавиши на кнопке
    /// </summary>
    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            if (_isPressed)
            {
                _isPressed = false;
                UpdateAppearance();
                Height += _heightDifference;
                Top -= _heightDifference;
                PerformClick();
            }
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }
        base.OnKeyUp(e);
    }

    /// <summary>
    /// Обновляет визуальное состояние кнопки
    /// </summary>
    private void UpdateAppearance()
    {
        if (_isPressed)
        {
            BackgroundImage = _pressedImage;
            Height -= _heightDifference;
            Top += _heightDifference;
        }
        else if (_isHighlighted)
        {
            BackgroundImage = _highlightedImage;
        }
        else
        {
            BackgroundImage = _normalImage;
        }
        Invalidate();
    }

    /// <summary>
    /// Обрабатывает нажатие мыши на кнопке
    /// </summary>
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            if (Focused)
            {
                FindForm()!.ActiveControl = null;
            }
            _isPressed = true;
            UpdateAppearance();
            Capture = true;
        }
    }

    /// <summary>
    /// Обрабатывает отпускание мыши на кнопке
    /// </summary>
    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_isPressed && e.Button == MouseButtons.Left)
        {
            _isPressed = false;
            UpdateAppearance();
            Height += _heightDifference;
            Top -= _heightDifference;
            if (ClientRectangle.Contains(e.Location))
            {
                PerformClick();
            }
            Capture = false;
        }
        base.OnMouseUp(e);
    }

    /// <summary>
    /// Обрабатывает уход курсора за пределы кнопки
    /// </summary>
    protected override void OnMouseLeave(EventArgs e)
    {
        if (_isPressed)
        {
            _isPressed = false;
            UpdateAppearance();
            Height += _heightDifference;
            Top -= _heightDifference;
        }
        base.OnMouseLeave(e);
    }

    /// <summary>
    /// Освобождает ресурсы изображений кнопки
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _normalImage.Dispose();
            _pressedImage.Dispose();
            _highlightedImage.Dispose();
        }
        base.Dispose(disposing);
    }
}
