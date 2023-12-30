using AccountChooser;
using ExcelDna.Integration;
using Microsoft.ML;

namespace AccountsXLL
{
    public static class MyFunctions
    {
        private static ITransformer _model;
        private static MLContext _mlContext;

        static MyFunctions()
        {
            _mlContext = new MLContext(seed: 1);
            var modelPath = "C:/Workspace/AccountChooser/model.zip";
            _model = _mlContext.Model.Load(modelPath, out var modelInputSchema);
        }

        [ExcelFunction(Description = "My first .NET function")]
        public static string SayHello(string name)
        {
            var predEngine = _mlContext.Model.CreatePredictionEngine<TransactionData, ModelOutput>(_model);
            var tx = new TransactionData { Description = name };
            var prediction = predEngine.Predict(tx);
            return prediction.FullName;
        }

        [ExcelFunction(Description = "Load specific mapping values")]
        public static string LoadMap(string description, float amount, string category)
        {
        }

        [ExcelFunction(Description = "Predicts the account name based on the trained model")]
        public static string PredictAccount(string description, float amount, string category)
        {
            var predEngine = _mlContext.Model.CreatePredictionEngine<TransactionData, ModelOutput>(_model);
            var tx = new TransactionData { Description = description, Category = category, Amount = amount };
            var prediction = predEngine.Predict(tx);
            return prediction.FullName;
        }
    }
}