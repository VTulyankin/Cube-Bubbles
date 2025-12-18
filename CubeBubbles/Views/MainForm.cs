using CubeBubbles.Models;
using CubeBubbles.Utilities;
using CubeBubbles.Views.Components;
using CubeBubbles.Views.Game;
using CubeBubbles.Views.Menus;

namespace CubeBubbles.Views;

public sealed class MainForm : Form
{
    public event EventHandler? LeftButtonClicked;
    public event EventHandler? RightButtonClicked;
    public event EventHandler? StartButtonClicked;
    public event EventHandler? CloseButtonClicked;
    public event EventHandler? ResumeButtonClicked;
    public event EventHandler? ExitToMenuButtonClicked;
    public event EventHandler? OkButtonClicked;
    public event EventHandler? CancelButtonClicked;
    public event EventHandler? ConfirmExitButtonClicked;
    public event EventHandler? EscapeKeyPressed;
    public event EventHandler? ShootButtonClicked;
    public event EventHandler? SwapBubblesClicked;
    public event EventHandler? PlayerBubblesClicked;
    public event EventHandler<int>? MouseMovedOverField;

    public static readonly string Title = "Cube Bubbles";

    private CustomButton? _btnLeft;
    private CustomButton? _btnRight;
    private CustomButton? _btnExit;
    private GameModel? _gameModel;
    private GameTimeManager? _timeManager;
    private StartMenu? _startMenu;
    private PauseMenu? _pauseMenu;
    private GameOverMenu? _gameOverMenu;
    private ExitConfirmationMenu? _exitConfirmationMenu;
    private GameView? _gameView;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly List<CustomButton> _activeButtons = [];
    private readonly GameCursor? _cursor;

    public MainForm()
    {
        InitializeForm();
        CreateUiComponents();
        _cursor = GameCursor.Initialize(this);

        _timer = new System.Windows.Forms.Timer { Interval = 1 };
        _timer.Tick += (_, _) =>
        {
            _timeManager?.Update();
        };
        _timer.Start();
    }

    /// <summary>
    /// Инициализирует форму с фоном и настройками
    /// </summary>
    private void InitializeForm()
    {
        var bgImage = ScaleUtil.ScaleImage(Image.FromFile("Resources/windows/main.png"));
        BackgroundImage = bgImage;
        BackgroundImageLayout = ImageLayout.Stretch;
        ClientSize = bgImage.Size;
        FormBorderStyle = FormBorderStyle.None;
        DoubleBuffered = true;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        KeyPreview = true;

        Activated += (_, _) => ActiveControl = null;
        Shown += (_, _) => ActiveControl = null;

        Text = Title;
        Icon = CustomIcon.GetIcon(BubbleColor.Red);
    }
    
    
    
    /// <summary>
    /// Создает UI-компоненты формы
    /// </summary>
    private void CreateUiComponents()
    {
        _btnExit = new CustomButton("close", new Point(293, 6));
        _btnExit.Click += (_, _) => CloseButtonClicked?.Invoke(this, EventArgs.Empty);

        _btnLeft = new CustomButton("left", new Point(4, 220));
        _btnRight = new CustomButton("right", new Point(286, 220));

        _btnLeft.Click += (_, _) => LeftButtonClicked?.Invoke(this, EventArgs.Empty);
        _btnRight.Click += (_, _) => RightButtonClicked?.Invoke(this, EventArgs.Empty);

        Controls.Add(_btnExit);
        Controls.Add(_btnLeft);
        Controls.Add(_btnRight);

        _gameView = new GameView();
        Controls.Add(_gameView);
        _gameView.SendToBack();

        _startMenu = new StartMenu(Controls);
        _startMenu.StartClicked += (_, _) => StartButtonClicked?.Invoke(this, EventArgs.Empty);

        _pauseMenu = new PauseMenu(Controls);
        _pauseMenu.ResumeClicked += (_, _) => ResumeButtonClicked?.Invoke(this, EventArgs.Empty);
        _pauseMenu.ExitClicked += (_, _) => ExitToMenuButtonClicked?.Invoke(this, EventArgs.Empty);

        _gameOverMenu = new GameOverMenu(Controls);
        _gameOverMenu.OkClicked += (_, _) => OkButtonClicked?.Invoke(this, EventArgs.Empty);
        _gameOverMenu.ExitClicked += (_, _) => ExitToMenuButtonClicked?.Invoke(this, EventArgs.Empty);

        _exitConfirmationMenu = new ExitConfirmationMenu(Controls);
        _exitConfirmationMenu.CancelClicked += (_, _) => CancelButtonClicked?.Invoke(this, EventArgs.Empty);
        _exitConfirmationMenu.ConfirmClicked += (_, _) => ConfirmExitButtonClicked?.Invoke(this, EventArgs.Empty);

        _btnExit.BringToFront();
        _btnLeft.BringToFront();
        _btnRight.BringToFront();
    }

    /// <summary>
    /// Подписывается на события моделей и инициализирует GameView
    /// </summary>
    /// <param name="model">Модель игры</param>
    /// <param name="timeManager">Менеджер времени</param>
    public void SubscribeToModelEvents(GameModel model, GameTimeManager timeManager)
    {
        _gameModel = model;
        _timeManager = timeManager;

        _gameView?.Initialize(model, timeManager, _cursor!);
        _gameView!.MouseMovedOverField += (_, pos) => MouseMovedOverField?.Invoke(this, pos);
        _gameView.PlayerBubblesClicked += (_, _) => PlayerBubblesClicked?.Invoke(this, EventArgs.Empty);
        _gameView.FieldMouseDown += OnFieldMouseDown;

        _pauseMenu?.SubscribeToTime(timeManager);
        _gameOverMenu?.SubscribeToTime(timeManager);
        _exitConfirmationMenu?.SubscribeToTime(timeManager);

        model.MouseControlModeChanged += OnMouseControlModeChanged;
        model.StatusChanged += OnGameStatusChanged;
        model.Player.BubblesChanged += OnPlayerBubblesChanged; // Новая подписка

        TrackAllInteractiveControls();
    }

    /// <summary>
    /// Отслеживает все интерактивные элементы для курсора
    /// </summary>
    private void TrackAllInteractiveControls()
    {
        if (_cursor == null) return;

        _cursor.Track(_btnExit!);
        _cursor.Track(_btnLeft!);
        _cursor.Track(_btnRight!);
        _cursor.Track(_gameView!);

        foreach (var btn in _startMenu!.GetInteractiveButtons()) _cursor.Track(btn);
        foreach (var btn in _pauseMenu!.GetInteractiveButtons()) _cursor.Track(btn);
        foreach (var btn in _gameOverMenu!.GetInteractiveButtons()) _cursor.Track(btn);
        foreach (var btn in _exitConfirmationMenu!.GetInteractiveButtons()) _cursor.Track(btn);
    }

    /// <summary>
    /// Обрабатывает изменение статуса игры
    /// </summary>
    /// <param name="status">Новый статус</param>
    private void OnGameStatusChanged(GameStatus status)
    {
        if (status == GameStatus.GameOver && _gameModel?.IsMouseControlMode == true)
        {
            Cursor.Show();
            Cursor.Clip = Rectangle.Empty;
        }
    }

    /// <summary>
    /// Обрабатывает нажатие мыши на игровом поле
    /// </summary>
    /// <param name="sender">Источник события</param>
    /// <param name="e">Данные события мыши</param>
    private void OnFieldMouseDown(object? sender, MouseEventArgs e)
    {
        if (_gameModel?.IsMouseControlMode == true)
        {
            if (e.Button == MouseButtons.Left)
                ShootButtonClicked?.Invoke(this, EventArgs.Empty);
            else if (e.Button == MouseButtons.Right)
                SwapBubblesClicked?.Invoke(this, EventArgs.Empty);
            else if (e.Button == MouseButtons.Middle)
                EscapeKeyPressed?.Invoke(this, EventArgs.Empty);
        }
    }
    
    private void OnPlayerBubblesChanged()
    {
        if (_gameModel?.Status == GameStatus.Playing)
        {
            Icon?.Dispose();
            Icon = CustomIcon.GetIcon(_gameModel.Player.CurrentBubble);
        }
    }

    /// <summary>
    /// Обновляет список активных кнопок для навигации
    /// </summary>
    /// <param name="buttons">Список кнопок меню</param>
    private void UpdateActiveButtons(List<CustomButton> buttons)
    {
        _activeButtons.Clear();
        if (buttons.Count != 0) _activeButtons.Add(_btnExit!);
        _activeButtons.AddRange(buttons);

        ActiveControl = null;
        foreach (Control c in Controls)
            if (c is Button b) b.TabStop = false;

        foreach (var b in _activeButtons)
            b.TabStop = true;
    }

    /// <summary>
    /// Скрывает все меню
    /// </summary>
    private void HideAllMenus()
    {
        _startMenu?.Hide();
        _pauseMenu?.Hide();
        _gameOverMenu?.Hide();
        _exitConfirmationMenu?.Hide();
    }

    /// <summary>
    /// Показывает стартовое меню
    /// </summary>
    public void ShowStartMenu()
    {
        HideAllMenus();
        _startMenu?.Show();
        _btnLeft!.Enabled = false;
        _btnRight!.Enabled = false;
        UpdateActiveButtons(_startMenu!.GetInteractiveButtons());
    }

    /// <summary>
    /// Показывает игровой процесс
    /// </summary>
    public void ShowGame()
    {
        HideAllMenus();
        UpdateGameButtonsState();
        UpdateActiveButtons([]);
    }

    /// <summary>
    /// Показывает меню паузы
    /// </summary>
    public void ShowPauseMenu()
    {
        HideAllMenus();
        _pauseMenu?.Show();
        _btnLeft!.Enabled = false;
        _btnRight!.Enabled = false;
        UpdateActiveButtons(_pauseMenu!.GetInteractiveButtons());
    }

    /// <summary>
    /// Показывает меню окончания игры
    /// </summary>
    public void ShowGameOverMenu()
    {
        HideAllMenus();
        _gameOverMenu?.Show();
        _btnLeft!.Enabled = false;
        _btnRight!.Enabled = false;
        UpdateActiveButtons(_gameOverMenu!.GetInteractiveButtons());
    }

    /// <summary>
    /// Показывает меню подтверждения выхода
    /// </summary>
    public void ShowExitConfirmation()
    {
        HideAllMenus();
        _exitConfirmationMenu?.Show();
        _btnLeft!.Enabled = false;
        _btnRight!.Enabled = false;
        UpdateActiveButtons(_exitConfirmationMenu!.GetInteractiveButtons());
    }

    /// <summary>
    /// Обрабатывает изменение режима управления мышью
    /// </summary>
    /// <param name="isEnabled">True если режим включен</param>
    private void OnMouseControlModeChanged(bool isEnabled)
    {
        if (isEnabled)
        {
            Cursor.Hide();

            float scale = ScaleUtil.ScaleFactor;
            int fieldLeft = (int)(GameModel.FieldOriginX * scale);
            int fieldWidth = (int)(GameModel.MaxColumns * GameModel.BubbleWidth * scale);
            int fieldTop = (int)(GameModel.FieldOriginY * scale);
            int fieldBottom = (int)((GameModel.MaxRows * GameModel.BubbleHeight + GameModel.FieldOriginY) * scale);

            int playerAreaTop = fieldTop + (int)((fieldBottom - fieldTop) * 0.5f);

            Rectangle playerArea = new Rectangle(
                _gameView!.Left + fieldLeft,
                _gameView.Top + playerAreaTop,
                fieldWidth,
                fieldBottom - playerAreaTop
            );

            Cursor.Clip = RectangleToScreen(playerArea);

            _btnLeft!.Enabled = false;
            _btnRight!.Enabled = false;
        }
        else
        {
            Cursor.Show();
            Cursor.Clip = Rectangle.Empty;
            UpdateGameButtonsState();
        }
    }

    /// <summary>
    /// Обновляет состояние игровых кнопок
    /// </summary>
    private void UpdateGameButtonsState()
    {
        bool shouldBeEnabled = _gameModel?.Status == GameStatus.Playing && !(_gameModel?.IsMouseControlMode ?? false);
        _btnLeft!.Enabled = shouldBeEnabled;
        _btnRight!.Enabled = shouldBeEnabled;
        _btnExit!.Enabled = true;
    }

    /// <summary>
    /// Обрабатывает нажатия клавиш
    /// </summary>
    /// <param name="msg">Сообщение</param>
    /// <param name="keyData">Данные о клавише</param>
    /// <returns>True если клавиша обработана</returns>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            EscapeKeyPressed?.Invoke(this, EventArgs.Empty);
            return true;
        }

        if (_gameModel?.Status == GameStatus.Playing && !(_gameModel?.IsMouseControlMode ?? false))
        {
            switch (keyData)
            {
                case Keys.Left or Keys.A:
                    LeftButtonClicked?.Invoke(this, EventArgs.Empty);
                    return true;
                case Keys.Right or Keys.D:
                    RightButtonClicked?.Invoke(this, EventArgs.Empty);
                    return true;
                case Keys.Up or Keys.W or Keys.Space:
                    ShootButtonClicked?.Invoke(this, EventArgs.Empty);
                    return true;
                case Keys.Q or Keys.ControlKey:
                    SwapBubblesClicked?.Invoke(this, EventArgs.Empty);
                    return true;
            }
        }

        if (keyData is Keys.Up or Keys.Down or Keys.Left or Keys.Right or Keys.Tab or (Keys.Tab | Keys.Shift))
        {
            if (_activeButtons.Count == 0) return true;

            int delta = keyData is Keys.Up or Keys.Left or (Keys.Tab | Keys.Shift) ? -1 : 1;
            var currentIndex = _activeButtons.IndexOf((ActiveControl as CustomButton)!);
            int nextIndex = currentIndex == -1 ? (_activeButtons.Count > 1 ? 1 : 0) :
                (currentIndex + delta + _activeButtons.Count) % _activeButtons.Count;

            if (nextIndex >= 0 && nextIndex < _activeButtons.Count)
                _activeButtons[nextIndex].Focus();

            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>
    /// Обрабатывает закрытие формы
    /// </summary>
    /// <param name="e">Данные о закрытии</param>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        Cursor.Clip = Rectangle.Empty;
        Cursor.Show();
        _timer.Stop();
        _timer.Dispose();
        _cursor?.Dispose();
        base.OnFormClosing(e);
    }
}
