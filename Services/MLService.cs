using Microsoft.ML;
using TextClassification.Models;

namespace TextClassification.Services
{
    public class MLService
    {
        private readonly MLContext _mlContext;
        private ITransformer _model;
        private PredictionEngine<SentimentData, SentimentPrediction> _predictionEngine;

        private readonly string _modelPath = Path.Combine(Directory.GetCurrentDirectory(), "MLModel", "sentiment_model.zip");
        private readonly string _dataPath = Path.Combine(Directory.GetCurrentDirectory(), "Dataset", "TextData.csv");

        public MLService()
        {
            _mlContext = new MLContext();

            if (File.Exists(_modelPath))
            {
                LoadModel();
            }
            else
            {
                TrainAndSaveModel();
            }
        }

        // 🚀 TRAIN FROM CSV + SAVE
        private void TrainAndSaveModel()
        {
            Console.WriteLine("🔄 Training model from CSV...");

            var dataView = _mlContext.Data.LoadFromTextFile<SentimentData>(
                path: _dataPath,
                hasHeader: true,
                separatorChar: ','
            );

            var splitData = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

            // ✅ IMPORTANT: STRING LABEL PIPELINE
            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label")
                .Append(_mlContext.Transforms.Text.FeaturizeText("Features", nameof(SentimentData.Text)))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            _model = pipeline.Fit(splitData.TrainSet);

            // ✅ Evaluate
            var predictions = _model.Transform(splitData.TestSet);
            var metrics = _mlContext.MulticlassClassification.Evaluate(predictions);

            Console.WriteLine($"✅ Accuracy: {metrics.MicroAccuracy:P2}");

            // Create folder if not exists
            var dir = Path.GetDirectoryName(_modelPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir!);

            // Save model
            _mlContext.Model.Save(_model, dataView.Schema, _modelPath);

            Console.WriteLine("💾 Model saved at: " + _modelPath);

            _predictionEngine = _mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(_model);
        }

        // 📦 LOAD MODEL
        private void LoadModel()
        {
            Console.WriteLine("📦 Loading model...");

            using var stream = new FileStream(_modelPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            _model = _mlContext.Model.Load(stream, out var schema);

            _predictionEngine = _mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(_model);

            Console.WriteLine("✅ Model loaded");
        }

        // 🔮 PREDICT
        public SentimentPrediction Predict(string text)
        {
            return _predictionEngine.Predict(new SentimentData
            {
                Text = text
            });
        }
    }
}