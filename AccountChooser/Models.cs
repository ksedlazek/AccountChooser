using Microsoft.ML.Data;

namespace AccountChooser;

public class TransactionData
{
    //Date,Description,RowId,Name,FullName,Amount,AccountName,Category
    [LoadColumn(0)] public string Date { get; set; }
    [LoadColumn(1)] public string Description { get; set; }
    [LoadColumn(2)] public uint RowId { get; set; }
    [LoadColumn(3)] public string Name { get; set; }
    [LoadColumn(4)] public string FullName { get; set; }
    [LoadColumn(5)] public float Amount { get; set; }
    [LoadColumn(6)] public string AccountName { get; set; }
    [LoadColumn(7)] public string Category { get; set; }
}

public class TransactionFeatures
{
    [VectorType(100)] public float[] DescriptionFeaturized { get; set; }
    [VectorType(100)] public float[] CategoryFeaturized { get; set; }
}

public class ModelOutput
{
    [ColumnName("PredictedLabel")] public string FullName { get; set; }
    public float[] Score { get; set; }
}
