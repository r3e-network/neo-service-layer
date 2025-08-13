using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Core.Events.SecurityEvents
{
    /// <summary>
    /// Event raised when a security threat is detected
    /// </summary>
    public class SecurityThreatDetectedEvent : DomainEventBase
    {
        public SecurityThreatDetectedEvent(
            string aggregateId,
            long aggregateVersion,
            string initiatedBy,
            string threatType,
            double riskScore,
            string sourceIpAddress,
            string userAgent,
            string requestPath,
            List&lt;string&gt; detectionPatterns,
            Guid? causationId = null,
            Guid? correlationId = null)
            : base(aggregateId, "SecurityService", aggregateVersion, initiatedBy, causationId, correlationId)
        {
            ThreatType = threatType ?? throw new ArgumentNullException(nameof(threatType));
            RiskScore = riskScore;
            SourceIpAddress = sourceIpAddress ?? throw new ArgumentNullException(nameof(sourceIpAddress));
            UserAgent = userAgent ?? string.Empty;
            RequestPath = requestPath ?? string.Empty;
            DetectionPatterns = detectionPatterns ?? new List&lt;string&gt;();

            AddMetadata("threat_category", GetThreatCategory(threatType));
            AddMetadata("severity_level", GetSeverityLevel(riskScore));
            AddMetadata("requires_immediate_action", riskScore &gt; 0.8);
        }

        /// <summary>
        /// Type of security threat detected
        /// </summary>
        public string ThreatType { get; }

        /// <summary>
        /// Risk score from 0.0 to 1.0
        /// </summary>
        public double RiskScore { get; }

        /// <summary>
        /// Source IP address of the threat
        /// </summary>
        public string SourceIpAddress { get; }

        /// <summary>
        /// User agent string of the request
        /// </summary>
        public string UserAgent { get; }

        /// <summary>
        /// Request path that triggered the detection
        /// </summary>
        public string RequestPath { get; }

        /// <summary>
        /// List of detection patterns that matched
        /// </summary>
        public List&lt;string&gt; DetectionPatterns { get; }

        private static string GetThreatCategory(string threatType)
        {
            return threatType.ToLowerInvariant() switch
            {
                var t when t.Contains("sql") =&gt; "injection",
                var t when t.Contains("xss") =&gt; "cross_site_scripting",
                var t when t.Contains("code") =&gt; "code_injection",
                var t when t.Contains("path") =&gt; "path_traversal",
                var t when t.Contains("ddos") =&gt; "denial_of_service",
                _ =&gt; "unknown"
            };
        }

        private static string GetSeverityLevel(double riskScore)
        {
            return riskScore switch
            {
                &gt;= 0.9 =&gt; "critical",
                &gt;= 0.7 =&gt; "high",
                &gt;= 0.5 =&gt; "medium",
                &gt;= 0.3 =&gt; "low",
                _ =&gt; "info"
            };
        }
    }
}