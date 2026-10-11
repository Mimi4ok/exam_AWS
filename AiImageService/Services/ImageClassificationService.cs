using AiImageService.ML;
using AiImageService.Models;
using Microsoft.ML;
using System;
using System.IO;

namespace AiImageService.Services
{
    public class ImageClassificationService
    {
        private readonly PredictionEngine<ImagePredictionInput, ImagePredictionOutput>? _predictionEngine;
        private readonly LabelTranslationService _translationService;

        public ImageClassificationService(LabelTranslationService translationService)
        {
            _translationService = translationService;

            var mlContext = new MLContext();
            string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MLModel.zip");

            if (!File.Exists(modelPath))
            {
                modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MLModel.mlnet");
            }

            if (File.Exists(modelPath))
            {
                ITransformer trainedModel = mlContext.Model.Load(modelPath, out _);
                _predictionEngine = mlContext.Model.CreatePredictionEngine<ImagePredictionInput, ImagePredictionOutput>(trainedModel);
            }
        }

        public (string Label, float Confidence) ClassifyImage(byte[] imageBytes)
        {
            if (_predictionEngine == null)
                return ("Model not loaded", 0.0f);

            var input = new ImagePredictionInput { ImageBytes = imageBytes };
            var prediction = _predictionEngine.Predict(input);

            // Отримуємо сиру мітку від ML.NET (наприклад, "equus-quagga")
            string rawLabel = prediction.PredictedLabel;

            // Перекладаємо її у зрозумілу назву ("Plains Zebra")
            string displayLabel = _translationService.Translate(rawLabel);

            // Знаходимо максимальний Confidence score
            float maxConfidence = 0f;
            if (prediction.Score != null && prediction.Score.Length > 0)
            {
                foreach (var score in prediction.Score)
                {
                    if (score > maxConfidence)
                        maxConfidence = score;
                }
            }

            return (displayLabel, maxConfidence);
        }
    }
}