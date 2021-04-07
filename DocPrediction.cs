using Microsoft.ML.Data;

namespace PdfSorter
{
    class DocPrediction
    {
        [ColumnName("PredictedLabel")]
        public string Category { get; set; }
        public float[] Score { get; set; }
    }
}