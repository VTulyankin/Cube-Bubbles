using CubeBubbles.Models;

namespace CubeBubbles.Tests.Models;

[TestFixture]
public class GameModelTests
{
    private GameModel _model;

    [SetUp]
    public void Setup()
    {
        _model = new GameModel();
    }

    /// <summary>
    /// Проверяет начальное состояние игры
    /// </summary>
    [Test]
    public void InitialState_ShouldBeStart()
    {
        Assert.That(_model.Status, Is.EqualTo(GameStatus.Start));
        Assert.That(_model.Score, Is.EqualTo(0));
    }

    /// <summary>
    /// Проверяет что StartGame переводит игру в статус Playing
    /// </summary>
    [Test]
    public void StartGame_ShouldChangeStatusToPlaying()
    {
        _model.StartGame();

        Assert.That(_model.Status, Is.EqualTo(GameStatus.Playing));
        Assert.That(_model.Score, Is.EqualTo(0));
    }

    /// <summary>
    /// Проверяет что поле пустое после старта игры
    /// </summary>
    [Test]
    public void StartGame_ShouldClearField()
    {
        _model.PopulateInitialField();
        _model.StartGame();

        for (int row = 0; row < GameModel.MaxRows; row++)
        {
            for (int col = 0; col < GameModel.MaxColumns; col++)
            {
                Assert.That(_model.BubbleMatrix[row, col], Is.Null);
            }
        }
    }

    /// <summary>
    /// Проверяет заполнение начальных рядов
    /// </summary>
    [Test]
    public void PopulateInitialField_ShouldFillFirst4Rows()
    {
        _model.PopulateInitialField();

        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < GameModel.MaxColumns; col++)
            {
                Assert.That(_model.BubbleMatrix[row, col], Is.Not.Null);
            }
        }

        for (int row = 4; row < GameModel.MaxRows; row++)
        {
            for (int col = 0; col < GameModel.MaxColumns; col++)
            {
                Assert.That(_model.BubbleMatrix[row, col], Is.Null);
            }
        }
    }

    /// <summary>
    /// Проверяет что игра переходит в паузу при статусе Playing
    /// </summary>
    [Test]
    public void HandleEscapeKey_WhilePlaying_ShouldPause()
    {
        _model.StartGame();

        _model.HandleEscapeKey();

        Assert.That(_model.Status, Is.EqualTo(GameStatus.Pause));
    }

    /// <summary>
    /// Проверяет что игра возобновляется из паузы
    /// </summary>
    [Test]
    public void ResumeGame_ShouldChangeStatusToPlaying()
    {
        _model.StartGame();
        _model.HandleEscapeKey();

        _model.ResumeGame();

        Assert.That(_model.Status, Is.EqualTo(GameStatus.Playing));
    }

    /// <summary>
    /// Проверяет смену статуса на ExitConfirmation со стартового экрана
    /// </summary>
    [Test]
    public void ShowExitConfirmation_FromStart_ShouldChangeStatus()
    {
        _model.ShowExitConfirmation();

        Assert.That(_model.Status, Is.EqualTo(GameStatus.ExitConfirmation));
        Assert.That(_model.WasPreviousStatus(GameStatus.Start), Is.True);
    }

    /// <summary>
    /// Проверяет смену статуса на ExitConfirmation из игры
    /// </summary>
    [Test]
    public void ShowExitConfirmation_FromPlaying_ShouldRememberPreviousStatus()
    {
        _model.StartGame();

        _model.ShowExitConfirmation();

        Assert.That(_model.Status, Is.EqualTo(GameStatus.ExitConfirmation));
        Assert.That(_model.WasPreviousStatus(GameStatus.Playing), Is.True);
    }

    /// <summary>
    /// Проверяет возврат к стартовому экрану
    /// </summary>
    [Test]
    public void ReturnToStart_ShouldClearFieldAndChangeStatus()
    {
        _model.StartGame();
        _model.PopulateInitialField();

        _model.ReturnToStart();

        Assert.That(_model.Status, Is.EqualTo(GameStatus.Start));

        for (int row = 0; row < GameModel.MaxRows; row++)
        {
            for (int col = 0; col < GameModel.MaxColumns; col++)
            {
                Assert.That(_model.BubbleMatrix[row, col], Is.Null);
            }
        }
    }

    /// <summary>
    /// Проверяет что добавление ряда сдвигает шарики вниз и создаёт новый верхний ряд
    /// </summary>
    [Test]
    public void AddRow_ShouldShiftTopBubbleDownAndCreateNewRow()
    {
        _model.StartGame();
        _model.BubbleMatrix[0, 0] = new Bubble(BubbleColor.Red, 0, 0);
        var originalBubble = _model.BubbleMatrix[0, 0];

        _model.AddRow();

        Assert.That(_model.BubbleMatrix[1, 0], Is.Not.Null);
        Assert.That(_model.BubbleMatrix[1, 0]!.Color, Is.EqualTo(originalBubble!.Color));
        Assert.That(_model.BubbleMatrix[0, 0], Is.Not.Null);
    }

    /// <summary>
    /// Проверяет game over когда шарики достигают нижней границы
    /// </summary>
    [Test]
    public void OnNewRowAnimationComplete_WithBubblesAtBottom_ShouldTriggerGameOver()
    {
        _model.StartGame();

        for (int row = 0; row < GameModel.MaxRows - 1; row++)
        {
            _model.BubbleMatrix[row, 0] = new Bubble(BubbleColor.Red, row, 0);
        }

        _model.AddRow();
        _model.OnNewRowAnimationComplete();

        Assert.That(_model.Status, Is.EqualTo(GameStatus.GameOver));
    }

    /// <summary>
    /// Проверяет включение режима мыши
    /// </summary>
    [Test]
    public void EnableMouseControl_ShouldSetFlag()
    {
        _model.StartGame();

        _model.EnableMouseControl();

        Assert.That(_model.IsMouseControlMode, Is.True);
    }

    /// <summary>
    /// Проверяет что нельзя выстрелить если игра не в статусе Playing
    /// </summary>
    [Test]
    public void PlayerShoot_WhileNotPlaying_ShouldNotShoot()
    {
        int initialBubbleCount = 0;
        for (int row = 0; row < GameModel.MaxRows; row++)
        {
            for (int col = 0; col < GameModel.MaxColumns; col++)
            {
                if (_model.BubbleMatrix[row, col] != null)
                    initialBubbleCount++;
            }
        }

        _model.PlayerShoot();

        int afterBubbleCount = 0;
        for (int row = 0; row < GameModel.MaxRows; row++)
        {
            for (int col = 0; col < GameModel.MaxColumns; col++)
            {
                if (_model.BubbleMatrix[row, col] != null)
                    afterBubbleCount++;
            }
        }

        Assert.That(afterBubbleCount, Is.EqualTo(initialBubbleCount));
    }
}

[TestFixture]
public class PlayerTests
{
    private Player _player;

    [SetUp]
    public void Setup()
    {
        _player = new Player();
    }

    /// <summary>
    /// Проверяет начальную позицию игрока
    /// </summary>
    [Test]
    public void InitialPosition_ShouldBeZero()
    {
        Assert.That(_player.Position, Is.EqualTo(0));
    }

    /// <summary>
    /// Проверяет движение вправо
    /// </summary>
    [Test]
    public void MoveRight_ShouldIncreasePosition()
    {
        _player.MoveRight();

        Assert.That(_player.Position, Is.EqualTo(1));
    }

    /// <summary>
    /// Проверяет движение влево с переходом на последний столбец
    /// </summary>
    [Test]
    public void MoveLeft_FromZero_ShouldWrapToMaxPosition()
    {
        _player.MoveLeft();

        Assert.That(_player.Position, Is.EqualTo(GameModel.MaxColumns - 1));
    }

    /// <summary>
    /// Проверяет движение влево с обычной позиции
    /// </summary>
    [Test]
    public void MoveLeft_ShouldDecreasePosition()
    {
        _player.MoveRight();
        _player.MoveRight();

        _player.MoveLeft();

        Assert.That(_player.Position, Is.EqualTo(1));
    }

    /// <summary>
    /// Проверяет циклическое движение вправо
    /// </summary>
    [Test]
    public void MoveRight_FromMaxPosition_ShouldWrapToZero()
    {
        _player.SetPosition(GameModel.MaxColumns - 1);

        _player.MoveRight();

        Assert.That(_player.Position, Is.EqualTo(0));
    }

    /// <summary>
    /// Проверяет установку позиции
    /// </summary>
    [Test]
    public void SetPosition_ShouldUpdatePosition()
    {
        _player.SetPosition(5);

        Assert.That(_player.Position, Is.EqualTo(5));
    }

    /// <summary>
    /// Проверяет что нельзя установить позицию вне диапазона
    /// </summary>
    [Test]
    public void SetPosition_OutOfRange_ShouldNotChange()
    {
        int originalPosition = _player.Position;

        _player.SetPosition(-1);
        Assert.That(_player.Position, Is.EqualTo(originalPosition));

        _player.SetPosition(GameModel.MaxColumns);
        Assert.That(_player.Position, Is.EqualTo(originalPosition));
    }

    /// <summary>
    /// Проверяет смену шариков местами
    /// </summary>
    [Test]
    public void SwapBubbles_ShouldExchangeCurrentAndNext()
    {
        var initialCurrent = _player.CurrentBubble;
        var initialNext = _player.NextBubble;

        _player.SwapBubbles();

        Assert.That(_player.CurrentBubble, Is.EqualTo(initialNext));
        Assert.That(_player.NextBubble, Is.EqualTo(initialCurrent));
    }

    /// <summary>
    /// Проверяет выстрел шариком
    /// </summary>
    [Test]
    public void Shoot_ShouldMoveNextToCurrent()
    {
        var initialNext = _player.NextBubble;

        _player.Shoot();

        Assert.That(_player.CurrentBubble, Is.EqualTo(initialNext));
    }

    /// <summary>
    /// Проверяет что после выстрела генерируется новый следующий шарик
    /// </summary>
    [Test]
    public void Shoot_ShouldGenerateNewNextBubble()
    {
        _player.Shoot();

        Assert.That(Enum.IsDefined(typeof(BubbleColor), _player.NextBubble), Is.True);
    }

    /// <summary>
    /// Проверяет сброс игрока
    /// </summary>
    [Test]
    public void Reset_ShouldResetPosition()
    {
        _player.SetPosition(5);

        _player.Reset();

        Assert.That(_player.Position, Is.EqualTo(0));
    }

    /// <summary>
    /// Проверяет добавление спец. шарика
    /// </summary>
    [Test]
    public void AddSpecialBubble_ShouldSetCurrentBubbleType()
    {
        _player.AddSpecialBubble(BubbleType.Bomb);

        Assert.That(_player.CurrentBubbleType, Is.EqualTo(BubbleType.Bomb));
    }

    /// <summary>
    /// Проверяет что по умолчанию шарики обычные
    /// </summary>
    [Test]
    public void InitialBubbleType_ShouldBeNormal()
    {
        Assert.That(_player.CurrentBubbleType, Is.EqualTo(BubbleType.Normal));
        Assert.That(_player.NextBubbleType, Is.EqualTo(BubbleType.Normal));
    }
}

[TestFixture]
public class GameLogicTests
{
    private GameModel _model;

    [SetUp]
    public void Setup()
    {
        _model = new GameModel();
        _model.StartGame();
    }

    /// <summary>
    /// Проверяет что игрок может двигаться через методы модели
    /// </summary>
    [Test]
    public void MovePlayerLeft_ShouldUpdatePlayerPosition()
    {
        _model.Player.SetPosition(5);

        _model.MovePlayerLeft();

        Assert.That(_model.Player.Position, Is.EqualTo(4));
    }

    /// <summary>
    /// Проверяет движение игрока вправо через модель
    /// </summary>
    [Test]
    public void MovePlayerRight_ShouldUpdatePlayerPosition()
    {
        _model.MovePlayerRight();

        Assert.That(_model.Player.Position, Is.EqualTo(1));
    }

    /// <summary>
    /// Проверяет смену шариков через модель
    /// </summary>
    [Test]
    public void SwapPlayerBubbles_ShouldSwapBubbles()
    {
        var initialCurrent = _model.Player.CurrentBubble;
        var initialNext = _model.Player.NextBubble;

        _model.SwapPlayerBubbles();

        Assert.That(_model.Player.CurrentBubble, Is.EqualTo(initialNext));
        Assert.That(_model.Player.NextBubble, Is.EqualTo(initialCurrent));
    }

    /// <summary>
    /// Проверяет что выстрел добавляет шарик на поле
    /// </summary>
    [Test]
    public void PlayerShoot_ShouldAddBubbleToField()
    {
        int playerPosition = _model.Player.Position;

        _model.PlayerShoot();

        bool foundBubble = false;
        for (int row = 0; row < GameModel.MaxRows; row++)
        {
            if (_model.BubbleMatrix[row, playerPosition] != null)
            {
                foundBubble = true;
                break;
            }
        }

        Assert.That(foundBubble, Is.True);
    }

    /// <summary>
    /// Проверяет установку позиции мышью
    /// </summary>
    [Test]
    public void SetPlayerPositionByMouse_WhileMouseModeEnabled_ShouldUpdatePosition()
    {
        _model.EnableMouseControl();

        _model.SetPlayerPositionByMouse(7);

        Assert.That(_model.Player.Position, Is.EqualTo(7));
    }

    /// <summary>
    /// Проверяет что мышь не работает если режим выключен
    /// </summary>
    [Test]
    public void SetPlayerPositionByMouse_WhileMouseModeDisabled_ShouldNotUpdate()
    {
        int originalPosition = _model.Player.Position;

        _model.SetPlayerPositionByMouse(7);

        Assert.That(_model.Player.Position, Is.EqualTo(originalPosition));
    }

    /// <summary>
    /// Проверяет что после завершения анимации совпадения проверяются плавающие шарики
    /// </summary>
    [Test]
    public void OnMatchAnimationComplete_ShouldCheckFloatingBubbles()
    {
        _model.BubbleMatrix[0, 0] = new Bubble(BubbleColor.Red, 0, 0);
        _model.BubbleMatrix[0, 1] = new Bubble(BubbleColor.Red, 0, 1);
        _model.BubbleMatrix[0, 2] = new Bubble(BubbleColor.Red, 0, 2);
        _model.BubbleMatrix[2, 5] = new Bubble(BubbleColor.Blue, 2, 5);

        _model.OnBubbleLanded(0, 0);

        int initialScore = _model.Score;

        _model.OnMatchAnimationComplete();

        Assert.That(_model.BubbleMatrix[2, 5], Is.Null);
        Assert.That(_model.Score, Is.GreaterThan(initialScore));
    }

    /// <summary>
    /// Проверяет генерацию случайного цвета
    /// </summary>
    [Test]
    public void GetRandomBubbleColor_ShouldReturnValidColor()
    {
        var color = GameModel.GetRandomBubbleColor();

        Assert.That(Enum.IsDefined(typeof(BubbleColor), color), Is.True);
    }

    /// <summary>
    /// Проверяет сдвиг фона на стартовом экране
    /// </summary>
    [Test]
    public void ShiftStartBackgroundDown_ShouldGenerateNewTopRow()
    {
        _model.ReturnToStart();
        var oldTopRow = new Bubble?[GameModel.MaxColumns];
        for (int col = 0; col < GameModel.MaxColumns; col++)
        {
            oldTopRow[col] = _model.StartBackgroundMatrix[0, col];
        }

        _model.ShiftStartBackgroundDown();

        for (int col = 0; col < GameModel.MaxColumns; col++)
        {
            Assert.That(_model.StartBackgroundMatrix[0, col], Is.Not.SameAs(oldTopRow[col]));
        }
    }

    /// <summary>
    /// Проверяет что сдвиг фона не работает вне стартового экрана
    /// </summary>
    [Test]
    public void ShiftStartBackgroundDown_WhileNotStart_ShouldNotShift()
    {
        _model.StartGame();
        var oldTopColor = _model.StartBackgroundMatrix[0, 0]?.Color;

        _model.ShiftStartBackgroundDown();

        Assert.That(_model.StartBackgroundMatrix[0, 0]?.Color, Is.EqualTo(oldTopColor));
    }
}

[TestFixture]
public class BubbleTests
{
    /// <summary>
    /// Проверяет создание шарика с базовыми параметрами
    /// </summary>
    [Test]
    public void CreateBubble_ShouldHaveCorrectProperties()
    {
        var bubble = new Bubble(BubbleColor.Red, 5, 3);

        Assert.That(bubble.Color, Is.EqualTo(BubbleColor.Red));
        Assert.That(bubble.Row, Is.EqualTo(5));
        Assert.That(bubble.Column, Is.EqualTo(3));
        Assert.That(bubble.Type, Is.EqualTo(BubbleType.Normal));
    }

    /// <summary>
    /// Проверяет создание спец. шарика
    /// </summary>
    [Test]
    public void CreateSpecialBubble_ShouldHaveCorrectType()
    {
        var bubble = new Bubble(BubbleColor.Blue, 2, 1, BubbleType.Bomb);

        Assert.That(bubble.Type, Is.EqualTo(BubbleType.Bomb));
    }

    /// <summary>
    /// Проверяет изменение позиции шарика
    /// </summary>
    [Test]
    public void ChangeRow_ShouldUpdateRow()
    {
        var bubble = new Bubble(BubbleColor.Green, 0, 0)
        {
            Row = 5
        };

        Assert.That(bubble.Row, Is.EqualTo(5));
    }

    /// <summary>
    /// Проверяет смещение для обычного шарика
    /// </summary>
    [Test]
    public void GetSpriteOffset_ForNormalBubble_ShouldBeZero()
    {
        var offset = Bubble.GetSpriteOffset(BubbleType.Normal);

        Assert.That(offset, Is.EqualTo((0, 0)));
    }

    /// <summary>
    /// Проверяет смещение для бомбы
    /// </summary>
    [Test]
    public void GetSpriteOffset_ForBomb_ShouldHaveYOffset()
    {
        var offset = Bubble.GetSpriteOffset(BubbleType.Bomb);

        Assert.That(offset.offsetX, Is.EqualTo(0));
        Assert.That(offset.offsetY, Is.EqualTo(-5));
    }

    /// <summary>
    /// Проверяет получение смещения через метод шарика
    /// </summary>
    [Test]
    public void GetOffset_ShouldReturnCorrectOffset()
    {
        var bubble = new Bubble(BubbleColor.Red, 0, 0, BubbleType.Bomb);

        var offset = bubble.GetOffset();

        Assert.That(offset, Is.EqualTo((0, -5)));
    }
}
