using System;
using System.Collections.Generic;
using Microsoft.ML;
using Microsoft.ML.AutoML;

namespace PdfSorter
{
    class DocTrainingService
    {
        private readonly MLContext _mlContext;
        private IDataView _trainingDataView;

        public DocTrainingService(MLContext mlContext)
        {
            _mlContext = mlContext;
        }

        public ITransformer AutoTrain(IEnumerable<Doc> trainingData, TimeSpan maxTime)
        {
            _trainingDataView = _mlContext.Data.LoadFromEnumerable(trainingData);

            var experimentSettings = new MulticlassExperimentSettings
            {
                MaxExperimentTimeInSeconds = (uint) maxTime.TotalSeconds,
                OptimizingMetric = MulticlassClassificationMetric.MacroAccuracy
            };

            var experiment = _mlContext.Auto().CreateMulticlassClassificationExperiment(experimentSettings);
            var columnInfo = new ColumnInformation
            {
                LabelColumnName = nameof(Doc.Category)
            };
            columnInfo.TextColumnNames.Add(nameof(Doc.Text));

            var result = experiment.Execute(_trainingDataView, columnInfo);
            return result.BestRun.Model;
        }

        public void SaveModel(string path, ITransformer model)
        {
            _mlContext.Model.Save(model, _trainingDataView.Schema, path);
        }
    }
}
