using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace PdfSorter
{
    class DocLabelService
    {
        private readonly MLContext _mlContext;
        private PredictionEngine<Doc, DocPrediction> _predictionEngine;
        private string[] _categories;

        public DocLabelService(MLContext mlContext)
        {
            _mlContext = mlContext;
        }

        public void LoadModelFromFile(string modelPath)
        {
            using var stream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var transformer = _mlContext.Model.Load(stream, out _);

            _predictionEngine = _mlContext.Model.CreatePredictionEngine<Doc, DocPrediction>(transformer);

            var labelBuffer = new VBuffer<ReadOnlyMemory<char>>();
            _predictionEngine.OutputSchema["Score"].Annotations.GetValue("SlotNames", ref labelBuffer);

            _categories = labelBuffer.DenseValues().Select(l => l.ToString()).ToArray();
        }

        public DocPrediction Predict(Doc doc)
        {
            return _predictionEngine.Predict(doc);
        }

        public List<string> GetSortedCategories(DocPrediction prediction)
        {
            return prediction.Score
                .Select((score, ordinal) => (score, _categories[ordinal]))
                .OrderByDescending(pair => pair.score)
                .Select(pair => pair.Item2).ToList();
        }
    }
}
