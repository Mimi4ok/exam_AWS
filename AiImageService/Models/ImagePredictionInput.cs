using Microsoft.ML.Data;

namespace AiImageService.Models
{
    public class ImagePredictionInput
    {
        [ColumnName(@"ImageSource")]
        public byte[] ImageBytes { get; set; } = System.Array.Empty<byte>();

        [ColumnName(@"Label")]
        public string Label { get; set; } = string.Empty;
    }
}