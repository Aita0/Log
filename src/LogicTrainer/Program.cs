using LogicTrainer.Services;
using LogicTrainer.UI;

namespace LogicTrainer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var storage = new JsonStorage();
        Application.Run(new MainForm(storage));
    }
}
