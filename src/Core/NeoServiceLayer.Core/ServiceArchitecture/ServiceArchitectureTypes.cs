using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Core.ServiceArchitecture
{
    /// <summary>
    /// Service discovery query parameters
    /// </summary>
    public class ServiceDiscoveryQuery
    {
        public string ServiceType { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string[] Tags { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }

    /// <summary>
    /// Endpoint selection criteria
    /// </summary>
    public class EndpointSelectionCriteria
    {
        public LoadBalancingStrategy Strategy { get; set; } = LoadBalancingStrategy.RoundRobin;
        public bool PreferLocal { get; set; } = true;
        public string PreferredRegion { get; set; }
        public Dictionary<string, string> RequiredCapabilities { get; set; }
    }

}