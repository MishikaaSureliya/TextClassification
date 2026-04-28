using Microsoft.ML.Data;

namespace TextClassification.Models
{
    public class SentimentPrediction
    {
        [ColumnName("PredictedLabel")]
        public string Prediction { get; set; } = string.Empty;

        public float[] Score { get; set; }
    }
}

