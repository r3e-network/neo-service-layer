using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    public class TrainingRequest
    {
        public string ModelId { get; set; } = string.Empty;
        public string ModelType { get; set; } = string.Empty;
        public double[] TrainingData { get; set; } = Array.Empty<double>();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
    
    public class PredictionRequest
    {
        public string ModelId { get; set; } = string.Empty;
        public double[] InputData { get; set; } = Array.Empty<double>();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
    
    public class PredictionResult
    {
        public double[] Predictions { get; set; } = Array.Empty<double>();
        public double Confidence { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string Prediction { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
    
    public class AbstractAccountRequest
    {
        public string AccountId { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}
