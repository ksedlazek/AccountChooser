namespace AccountChooser;

internal class Program
{
    static void Main(string[] args)
    {
        var path = $"C:/Workspace/csharp/AccountChooser/Data/model.zip";
        Trainer.TrainWithoutPreview(path);
        Trainer.Predict(path);
        Common.ConsoleHelper.ConsolePressAnyKey();
    }
}
