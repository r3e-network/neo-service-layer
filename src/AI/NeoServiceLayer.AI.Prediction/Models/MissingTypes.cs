using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.AI.Prediction
{
    public enum PredictionModelType
    {
        Linear,
        Polynomial,
        Exponential,
        Neural,
        RandomForest,
        GradientBoosting
    }

    public interface IMetricsCollector
    {
        void RecordMetric(string name, double value);
        void RecordEvent(string name, Dictionary<string, object> properties);
    }
}