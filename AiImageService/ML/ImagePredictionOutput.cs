using Microsoft.ML.Data;

namespace AiImageService.ML
{
    public class ImagePredictionOutput
    {
        [ColumnName(@"PredictedLabel")]
        public string PredictedLabel { get; set; } = string.Empty;

        [ColumnName(@"Score")]
        public float[] Score { get; set; } = System.Array.Empty<float>();
    }
}