using CubeBubbles.Controllers;
using CubeBubbles.Models;
using CubeBubbles.Views;

namespace CubeBubbles;

internal static class Program
{
    /// <summary>
    /// Точка входа в приложение
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var gameModel = new GameModel();
        var timeManager = new GameTimeManager();
        timeManager.SetGameModel(gameModel);
        
        var mainForm = new MainForm();
        var gameController = new GameController(mainForm, gameModel);

        mainForm.SubscribeToModelEvents(gameModel, timeManager);

        Application.Run(mainForm);
    }
}