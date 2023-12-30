using AccountChooser;
using ExcelDna.Integration;
using Microsoft.ML;
using System.Formats.Asn1;
using System.Globalization;
using System;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;

namespace AccountsXLL
{
    public static class MyFunctions
    {
        private static ITransformer _model;
        private static MLContext _mlContext;
        private static IList<Map> _mappings = new List<Map>(); 

        private class Map
        {
            public string Description { get; set; } = "";
            public string Type { get; set; } = "";
            public string Category { get; set; } = "";
            public string Account { get; set; } = "";
        }

        static MyFunctions()
        {
            _mlContext = new MLContext(seed: 1);
            var modelPath = "C:/Workspace/AccountChooser/Data/model.zip";
            _model = _mlContext.Model.Load(modelPath, out var modelInputSchema);
        }

        [ExcelFunction(Description = "My first .NET function")]
        public static string SayHello(string name)
        {
            return "Hello " + name;
        }

        [ExcelFunction(Description = "Load specific mapping values")]
        public static string LoadMap(string filename) 
        {
            var configPersons = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            };
            using (var reader = new StreamReader(filename))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<Map>();
                _mappings = records.ToList();
            }
            return $"Loaded {_mappings.Count} mappings";
        }

        [ExcelFunction(Description = "Checks for a strict lookup")]
        public static string LookupAccount(string description, double amount, string category, string type)
        {
            var map = _mappings.FirstOrDefault(x => x.Description == description && x.Category == category);
            if (map != null) return map.Account;
            return "";
        }

        [ExcelFunction(Description = "Predicts the account name based on the trained model")]
        public static string PredictAccount(string description, double amount, string category)
        {
            var predEngine = _mlContext.Model.CreatePredictionEngine<TransactionData, ModelOutput>(_model);
            var tx = new TransactionData { Description = description, Category = category, Amount = (float)amount };
            var prediction = predEngine.Predict(tx);
            return prediction.FullName;
        }
    }
}