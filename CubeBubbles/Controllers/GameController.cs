using CubeBubbles.Models;
using CubeBubbles.Views;
using CubeBubbles.Sounds;

namespace CubeBubbles.Controllers;

public class GameController
{
    private readonly MainForm _view;
    private readonly GameModel _model;
    private readonly GameSound _sound;

    public GameController(MainForm view, GameModel model)
    {
        _view = view;
        _model = model;
        _sound = new GameSound();
        _sound.LoadSounds();

        SubscribeToViewEvents();
        SubscribeToModelEvents();
        _view.ShowStartMenu();
    }

    /// <summary>
    /// Подписывается на события из View
    /// </summary>
    private void SubscribeToViewEvents()
    {
        _view.LeftButtonClicked += (_, _) => _model.MovePlayerLeft();
        _view.RightButtonClicked += (_, _) => _model.MovePlayerRight();
        _view.StartButtonClicked += (_, _) => OnStartGame();
        _view.CloseButtonClicked += (_, _) => OnClose();
        _view.ResumeButtonClicked += (_, _) => _model.ResumeGame();
        _view.ExitToMenuButtonClicked += (_, _) => OnExitToMenu();
        _view.OkButtonClicked += (_, _) => OnGameOverOk();
        _view.CancelButtonClicked += (_, _) => _model.CancelExit();
        _view.ConfirmExitButtonClicked += (_, _) => OnClose();
        _view.EscapeKeyPressed += (_, _) => OnEscapePressed();
        _view.ShootButtonClicked += (_, _) => _model.PlayerShoot();
        _view.SwapBubblesClicked += (_, _) => _model.SwapPlayerBubbles();
        _view.PlayerBubblesClicked += (_, _) => OnPlayerClicked();
        _view.MouseMovedOverField += (_, pos) => _model.SetPlayerPositionByMouse(pos);
    }

    /// <summary>
    /// Подписывается на события из Model
    /// </summary>
    private void SubscribeToModelEvents()
    {
        _model.StatusChanged += OnStatusChanged;
        _model.BubbleShotAnimation += OnBubbleShot;
        _model.BubblesMatchAnimation += OnBubblesMatched;
    }

    /// <summary>
    /// Обрабатывает запуск новой игры
    /// </summary>
    private void OnStartGame()
    {
        _model.StartGame();
    }

    /// <summary>
    /// Обрабатывает выход в главное меню
    /// </summary>
    private void OnExitToMenu()
    {
        _model.ReturnToStart();
    }

    /// <summary>
    /// Обрабатывает нажатие OK в меню Game Over
    /// </summary>
    private void OnGameOverOk()
    {
        _model.StartGame();
    }

    /// <summary>
    /// Обрабатывает клик по игроку
    /// </summary>
    private void OnPlayerClicked()
    {
        if (_model.Status == GameStatus.Playing && !_model.IsMouseControlMode)
        {
            _model.EnableMouseControl();
        }
    }

    /// <summary>
    /// Обрабатывает нажатие клавиши Escape
    /// </summary>
    private void OnEscapePressed()
    {
        if (_model.Status == GameStatus.Start)
        {
            _model.ShowExitConfirmation();
        }
        else
        {
            _model.HandleEscapeKey();
        }
    }

    /// <summary>
    /// Обрабатывает смену статуса игры
    /// </summary>
    /// <param name="status">Новый статус</param>
    private void OnStatusChanged(GameStatus status)
    {
        switch (status)
        {
            case GameStatus.Start:
                _view.ShowStartMenu();
                break;
            case GameStatus.Playing:
                _view.ShowGame();
                break;
            case GameStatus.Pause:
                _view.ShowPauseMenu();
                break;
            case GameStatus.GameOver:
                _view.ShowGameOverMenu();
                _sound.Play(SoundType.GameOver);
                break;
            case GameStatus.ExitConfirmation:
                _view.ShowExitConfirmation();
                break;
        }
    }

    /// <summary>
    /// Обрабатывает выстрел шариком
    /// </summary>
    /// <param name="data">Данные анимации</param>
    private void OnBubbleShot(BubbleAnimationData data)
    {
        _sound.Play(SoundType.Add);
    }

    /// <summary>
    /// Обрабатывает исчезновение группы шариков
    /// </summary>
    /// <param name="bubbles">Список шариков</param>
    private void OnBubblesMatched(List<BubbleAnimationData> bubbles)
    {
        _sound.Play(SoundType.Hit);
    }

    /// <summary>
    /// Освобождает ресурсы при закрытии
    /// </summary>
    private void OnClose()
    {
        _sound.Dispose();
        _view.Close();
    }
}