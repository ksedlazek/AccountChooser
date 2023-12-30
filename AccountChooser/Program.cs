namespace AccountChooser;

internal class Program
{
    static void Main(string[] args)
    {
        var path = $"C:/Workspace/AccountChooser/model.zip";
        Trainer.Train(path);
        Trainer.Predict(path);
        Common.ConsoleHelper.ConsolePressAnyKey();
    }
}
