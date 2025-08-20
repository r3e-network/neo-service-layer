using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Infrastructure.Observability.Metrics
{
    public interface IMetricsService
    {
        void RecordMetric(string name, double value, Dictionary<string, string> tags = null);
        void IncrementCounter(string name, Dictionary<string, string> tags = null);
        void RecordHistogram(string name, double value, Dictionary<string, string> tags = null);
        Task<Dictionary<string, double>> GetMetricsAsync();
    }

    public class MetricsService : IMetricsService
    {
        private readonly Dictionary<string, double> _metrics = new();
        private readonly object _lock = new();

        public void RecordMetric(string name, double value, Dictionary<string, string> tags = null)
        {
            lock (_lock)
            {
                _metrics[name] = value;
            }
        }

        public void IncrementCounter(string name, Dictionary<string, string> tags = null)
        {
            lock (_lock)
            {
                if (_metrics.ContainsKey(name))
                    _metrics[name]++;
                else
                    _metrics[name] = 1;
            }
        }

        public void RecordHistogram(string name, double value, Dictionary<string, string> tags = null)
        {
            RecordMetric(name, value, tags);
        }

        public Task<Dictionary<string, double>> GetMetricsAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(new Dictionary<string, double>(_metrics));
            }
        }
    }
}