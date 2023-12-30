using AccountChooser;
using Common;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.IO;
using Tensorboard;

namespace AccountChooser;

public class Trainer
{
    public const string SDCA = "SdcaMultiClassTrainer";
    public const string OVA = "OVAAveragedPerceptronTrainer";

    public static void Train(string modelPath, string selectedStrategy = Trainer.OVA)
    {
        var mlContext = new MLContext(seed: 1);

        // STEP 1: Common data loading configuration
        var trainingDataView = mlContext.Data.LoadFromTextFile<TransactionData>("C:/Workspace/AccountChooser/Data/Input.csv", ',', true);

        // STEP 2: Common data process configuration with pipeline data transformations
        var dataProcessPipeline = mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "Label", inputColumnName: "FullName")
            .Append(mlContext.Transforms.Text.NormalizeText(outputColumnName: "DescriptionNormalized", inputColumnName: "Description", caseMode: Microsoft.ML.Transforms.Text.TextNormalizingEstimator.CaseMode.Upper))
            .Append(mlContext.Transforms.Text.FeaturizeText(outputColumnName: "DescriptionFeaturized", inputColumnName: "DescriptionNormalized"))
            //.Append(mlContext.Transforms.Text.NormalizeText(outputColumnName: "CategoryNormalized", inputColumnName: "Category", caseMode: Microsoft.ML.Transforms.Text.TextNormalizingEstimator.CaseMode.Upper))
            //.Append(mlContext.Transforms.Text.FeaturizeText(outputColumnName: "CategoryFeaturized", inputColumnName: "CategoryNormalized"))
            .Append(mlContext.Transforms.Concatenate(outputColumnName: "Features", "DescriptionFeaturized")) //, "CategoryFeaturized", "Amount"))
            .AppendCacheCheckpoint(mlContext);
        // Use in-memory cache for small/medium datasets to lower training time. 
        // Do NOT use it (remove .AppendCacheCheckpoint()) when handling very large datasets.

        // (OPTIONAL) Peek data (such as 2 records) in training DataView after applying the ProcessPipeline's transformations into "Features" 
        ConsoleHelper.PeekDataViewInConsole(mlContext, trainingDataView, dataProcessPipeline, 2);

        // STEP 3: Create the selected training algorithm/trainer
        IEstimator<ITransformer> trainer = null;
        switch (selectedStrategy)
        {
            case SDCA:
                trainer = mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features");
                break;
            case OVA:
                {
                    // Create a binary classification trainer.
                    var averagedPerceptronBinaryTrainer = mlContext.BinaryClassification.Trainers.AveragedPerceptron("Label", "Features", numberOfIterations: 10);
                    // Compose an OVA (One-Versus-All) trainer with the BinaryTrainer.
                    // In this strategy, a binary classification algorithm is used to train one classifier for each class, "
                    // which distinguishes that class from all other classes. Prediction is then performed by running these binary classifiers, "
                    // and choosing the prediction with the highest confidence score.
                    trainer = mlContext.MulticlassClassification.Trainers.OneVersusAll(averagedPerceptronBinaryTrainer);

                    break;
                }
            default:
                break;
        }

        //Set the trainer/algorithm and map label to value (original readable state)
        var trainingPipeline = dataProcessPipeline.Append(trainer)
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        // STEP 4: Cross-Validate with single dataset (since we don't have two datasets, one for training and for evaluate)
        // in order to evaluate and get the model's accuracy metrics

        Console.WriteLine("=============== Cross-validating to get model's accuracy metrics ===============");
        var crossValidationResults = mlContext.MulticlassClassification.CrossValidate(data: trainingDataView, estimator: trainingPipeline, numberOfFolds: 6, labelColumnName: "Label");

        ConsoleHelper.PrintMulticlassClassificationFoldsAverageMetrics(trainer.ToString(), crossValidationResults);

        // STEP 5: Train the model fitting to the DataSet
        Console.WriteLine("=============== Training the model ===============");
        var trainedModel = trainingPipeline.Fit(trainingDataView);

        Common.ConsoleHelper.ConsoleWriteHeader("Training process finalized");

        // STEP 6: Save/persist the trained model to a .ZIP file
        Console.WriteLine("=============== Saving the model to a file ===============");
        mlContext.Model.Save(trainedModel, trainingDataView.Schema, modelPath);
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
         new TransactionData() { Description = "AMZN*324234", Category = "Shopping", Amount = 1000 }
    };

        foreach (var tx in testData)
        {
            var prediction = predEngine.Predict(tx);
            Console.WriteLine($"{tx.Description,30} ---> {prediction.FullName}");
        }
    }
}