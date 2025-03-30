using AccountChooser;
using Common;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.IO;
using Tensorboard;
using TorchSharp.Modules;

namespace AccountChooser;

public class Trainer
{
    public const string SDCA = "SdcaMultiClassTrainer";
    public const string OVA = "OVAAveragedPerceptronTrainer";
    public const string AVGP = "AveragedPerceptronTrainer";

    public static void Train(string modelPath, string selectedStrategy = Trainer.SDCA)
    {
        var mlContext = new MLContext(seed: 1);

        // STEP 1: Common data loading configuration
        var dataView = mlContext.Data.LoadFromTextFile<TransactionData>("C:/Workspace/csharp/AccountChooser/Data/Input.csv", '|', true);

        // STEP 2: Common data process configuration with pipeline data transformations
        var dataProcessPipeline = mlContext.Transforms.Conversion.MapValueToKey("Label", "FullName")
            .Append(mlContext.Transforms.Text.NormalizeText("DescriptionNormalized", "Description", Microsoft.ML.Transforms.Text.TextNormalizingEstimator.CaseMode.Upper, keepNumbers: true))
            .Append(mlContext.Transforms.Text.FeaturizeText("DescriptionFeaturized", "DescriptionNormalized"))
            .Append(mlContext.Transforms.Categorical.OneHotEncoding("CategoryOneHotEncoded", "Category"))
            .Append(mlContext.Transforms.NormalizeMeanVariance("AmountNormalized", "Amount"))
            .Append(mlContext.Transforms.Concatenate("Features", "DescriptionFeaturized", "CategoryOneHotEncoded", "AmountNormalized"));

        // Use in-memory cache for small/medium datasets to lower training time. 
        // Do NOT use it (remove .AppendCacheCheckpoint()) when handling very large datasets.
        //dataProcessPipeline = dataProcessPipeline.AppendCacheCheckpoint(mlContext);

        // 'transformedData' is a 'promise' of data, lazy-loading. call Preview and iterate through the returned collection from preview.
        var transformedDataView = dataProcessPipeline.Fit(dataView).Transform(dataView);

        // (OPTIONAL) Peek data (such as 2 records) in training DataView after applying the ProcessPipeline's transformations into "Features" 
        ConsoleHelper.PeekDataViewInConsole(mlContext, transformedDataView);

        // STEP 3: Create the selected training algorithm/trainer
        var trainer = CreateTrainer(mlContext, selectedStrategy);

        //Set the trainer/algorithm and map label to value (original readable state)
        var pipeline = dataProcessPipeline.Append(trainer).Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        // STEP 4: Cross-Validate with single dataset (since we don't have two datasets, one for training and for evaluate)
        // in order to evaluate and get the model's accuracy metrics

        //Console.WriteLine("=============== Cross-validating to get model's accuracy metrics ===============");
        var crossValidationResults = mlContext.MulticlassClassification.CrossValidate(data: dataView, estimator: pipeline, numberOfFolds: 6, labelColumnName: "Label");
        ConsoleHelper.PrintMulticlassClassificationFoldsAverageMetrics(selectedStrategy, crossValidationResults);

        // STEP 5: Train the model fitting to the DataSet
        Console.WriteLine("=============== Training the model ===============");
        var trainedModel = pipeline.Fit(dataView);

        Common.ConsoleHelper.ConsoleWriteHeader("Training process finalized");

        // STEP 6: Save/persist the trained model to a .ZIP file
        Console.WriteLine("=============== Saving the model to a file ===============");
        mlContext.Model.Save(trainedModel, dataView.Schema, modelPath);
    }

    public static IEstimator<ITransformer> CreateTrainer(MLContext mlContext, string selectedStrategy)
    {
        switch (selectedStrategy)
        {
            case SDCA:
                return mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features");
            case AVGP:
                return mlContext.BinaryClassification.Trainers.AveragedPerceptron("Label", "Features", numberOfIterations: 10);
            case OVA:
                {
                    // Create a binary classification trainer.
                    var averagedPerceptronBinaryTrainer = mlContext.BinaryClassification.Trainers.AveragedPerceptron("Label", "Features", numberOfIterations: 10);
                    // Compose an OVA (One-Versus-All) trainer with the BinaryTrainer.
                    // In this strategy, a binary classification algorithm is used to train one classifier for each class,
                    // which distinguishes that class from all other classes. Prediction is then performed by running these binary classifiers,
                    // and choosing the prediction with the highest confidence score.
                    return mlContext.MulticlassClassification.Trainers.OneVersusAll(averagedPerceptronBinaryTrainer);
                }
            default:
                throw new ArgumentException($"{selectedStrategy} is not a valid trainer");
        }
    }

    public static void TrainWithoutPreview(string modelPath)
    {
        var mlContext = new MLContext(seed: 1);

        Console.WriteLine("=============== STEP 1: load the data ===============");
        var rawData = mlContext.Data.LoadFromTextFile<TransactionData>("C:/Workspace/csharp/AccountChooser/Data/Input.csv", '|', true);
        var trainData = mlContext.Data.ShuffleRows(rawData);

        Console.WriteLine("=============== STEP 2: Set up the ML pipeline ===============");
        var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label", "FullName")
            .Append(mlContext.Transforms.Text.NormalizeText("DescriptionNormalized", "Description", Microsoft.ML.Transforms.Text.TextNormalizingEstimator.CaseMode.Upper, keepNumbers: true))
            .Append(mlContext.Transforms.Text.FeaturizeText("DescriptionFeaturized", "DescriptionNormalized"))
            .Append(mlContext.Transforms.Categorical.OneHotEncoding("CategoryOneHotEncoded", "Category"))
            .Append(mlContext.Transforms.NormalizeMeanVariance("AmountNormalized", "Amount"))
            .Append(mlContext.Transforms.Concatenate("Features", "DescriptionFeaturized", "CategoryOneHotEncoded", "AmountNormalized"))
            .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features", maximumNumberOfIterations: 100))
            .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        Console.WriteLine("=============== STEP 3: Training the model ===============");
        var model = pipeline.Fit(trainData);

        Console.WriteLine("=============== STEP 4: Evaluate the model ===============");
        var predictions = model.Transform(trainData);
        var metrics = mlContext.MulticlassClassification.Evaluate(predictions, "Label");
        Console.WriteLine($"MicroAccuracy: {metrics.MicroAccuracy}");
        Console.WriteLine($"MacroAccuracy: {metrics.MacroAccuracy}");

        Console.WriteLine("=============== STEP 5: Saving the model to a file ===============");
        mlContext.Model.Save(model, trainData.Schema, modelPath);
    }

    public static void Predict(string modelPath)
    {
        var mlContext = new MLContext(seed: 1);
        // Load model from file.
        var trainedModel = mlContext.Model.Load(modelPath, out var modelInputSchema);

        var predEngine = mlContext.Model.CreatePredictionEngine<TransactionData, ModelOutput>(trainedModel);

        var testData = new TransactionData[] {
         new TransactionData() { Description = "Dick Clothing Sporting", Category = "Shopping", Amount = 29438 },
         new TransactionData() { Description = "Shoprite", Category = "Groceries", Amount = 9723 },
         new TransactionData() { Description = "SHOPHQ", Category = "Shopping", Amount = 12000 },
         new TransactionData() { Description = "Barnes & Noble", Category = "Shopping", Amount = 4437 },
         new TransactionData() { Description = "St. Stephens Pub", Category = "Food & Drink", Amount = 9800 },
         new TransactionData() { Description = "AMZN*324234", Category = "Shopping", Amount = 1000 } };

        foreach (var tx in testData)
        {
            var prediction = predEngine.Predict(tx);
            Console.WriteLine($"{tx.Description,30} ---> {prediction.FullName}");
        }
    }
}