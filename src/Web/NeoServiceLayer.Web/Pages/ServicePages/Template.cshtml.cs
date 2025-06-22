using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NeoServiceLayer.Web.Pages.ServicePages;

public class TemplateModel : PageModel
{
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceDisplayName { get; set; } = string.Empty;
    public string ServiceSubtitle { get; set; } = string.Empty;
    public string ServiceDescription { get; set; } = string.Empty;
    public string ServiceIcon { get; set; } = string.Empty;
    public string ServiceColor { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string ServiceLayer { get; set; } = string.Empty;
    public List<string> ServiceFeatures { get; set; } = new();
    public List<string> ServiceOperations { get; set; } = new();

    public void OnGet(string serviceName)
    {
        ServiceName = serviceName?.ToLowerInvariant() ?? string.Empty;
        ConfigureServiceDetails();
    }

    private void ConfigureServiceDetails()
    {
        switch (ServiceName)
        {
            case "compliance":
                ServiceDisplayName = "Compliance";
                ServiceSubtitle = "Regulatory Compliance & Audit Management";
                ServiceDescription = "Regulatory compliance monitoring, audit trails, and violation detection with automated reporting.";
                ServiceIcon = "fas fa-check-circle";
                ServiceColor = "#28a745";
                ServiceType = "Security";
                ServiceLayer = "Security";
                ServiceFeatures = new List<string> { "Compliance Monitoring", "Audit Trails", "Violation Detection", "Automated Reporting", "Regulatory Updates" };
                ServiceOperations = new List<string> { "Check Compliance", "Generate Report", "Start Audit", "Update Rules" };
                break;

            case "zeroknowledge":
                ServiceDisplayName = "Zero Knowledge";
                ServiceSubtitle = "Privacy-Preserving Cryptographic Proofs";
                ServiceDescription = "Privacy-preserving cryptographic proofs and verifications with zk-SNARK support.";
                ServiceIcon = "fas fa-user-secret";
                ServiceColor = "#6f42c1";
                ServiceType = "Security";
                ServiceLayer = "Security";
                ServiceFeatures = new List<string> { "zk-SNARK Proofs", "Privacy Protection", "Verification", "Commitment Schemes", "Range Proofs" };
                ServiceOperations = new List<string> { "Generate Proof", "Verify Proof", "Create Commitment", "Generate Range Proof" };
                break;

            case "backup":
                ServiceDisplayName = "Backup";
                ServiceSubtitle = "Automated Backup & Recovery";
                ServiceDescription = "Automated backup and restore operations with encryption, compression, and integrity verification.";
                ServiceIcon = "fas fa-save";
                ServiceColor = "#fd7e14";
                ServiceType = "Security";
                ServiceLayer = "Security";
                ServiceFeatures = new List<string> { "Automated Backups", "Encryption", "Compression", "Integrity Verification", "Point-in-Time Recovery" };
                ServiceOperations = new List<string> { "Create Backup", "Restore Data", "Verify Integrity", "Schedule Backup" };
                break;

            case "prediction":
                ServiceDisplayName = "AI Prediction";
                ServiceSubtitle = "Machine Learning & Risk Assessment";
                ServiceDescription = "Machine learning predictions, risk assessment, and price forecasting with secure enclave execution.";
                ServiceIcon = "fas fa-brain";
                ServiceColor = "#e83e8c";
                ServiceType = "Intelligence";
                ServiceLayer = "Intelligence";
                ServiceFeatures = new List<string> { "ML Predictions", "Risk Assessment", "Price Forecasting", "Model Training", "Pattern Analysis" };
                ServiceOperations = new List<string> { "Run Prediction", "Train Model", "Assess Risk", "Forecast Price" };
                break;

            case "patternrecognition":
                ServiceDisplayName = "Pattern Recognition";
                ServiceSubtitle = "Advanced Analytics & Anomaly Detection";
                ServiceDescription = "Advanced pattern analysis, anomaly detection, and fraud detection with AI-powered insights.";
                ServiceIcon = "fas fa-search";
                ServiceColor = "#20c997";
                ServiceType = "Intelligence";
                ServiceLayer = "Intelligence";
                ServiceFeatures = new List<string> { "Pattern Analysis", "Anomaly Detection", "Fraud Detection", "Real-time Monitoring", "AI Insights" };
                ServiceOperations = new List<string> { "Analyze Patterns", "Detect Anomalies", "Scan for Fraud", "Generate Report" };
                break;

            case "abstractaccount":
                ServiceDisplayName = "Abstract Account";
                ServiceSubtitle = "Smart Contract Wallet Management";
                ServiceDescription = "Smart contract wallet management with session keys and multi-signature support.";
                ServiceIcon = "fas fa-user-cog";
                ServiceColor = "#fd7e14";
                ServiceType = "Blockchain";
                ServiceLayer = "Blockchain";
                ServiceFeatures = new List<string> { "Smart Wallets", "Session Keys", "Multi-Signature", "Account Abstraction", "Gas Management" };
                ServiceOperations = new List<string> { "Create Account", "Execute Transaction", "Add Guardian", "Create Session Key" };
                break;

            case "crosschain":
                ServiceDisplayName = "Cross Chain";
                ServiceSubtitle = "Inter-blockchain Communication";
                ServiceDescription = "Inter-blockchain communication and asset bridging with secure validation.";
                ServiceIcon = "fas fa-link";
                ServiceColor = "#0dcaf0";
                ServiceType = "Blockchain";
                ServiceLayer = "Blockchain";
                ServiceFeatures = new List<string> { "Asset Bridging", "Cross-chain Messaging", "Validation", "Multi-chain Support", "Atomic Swaps" };
                ServiceOperations = new List<string> { "Bridge Assets", "Send Message", "Validate Transaction", "Execute Swap" };
                break;

            case "proofofreserve":
                ServiceDisplayName = "Proof of Reserve";
                ServiceSubtitle = "Asset Reserve Verification";
                ServiceDescription = "Cryptographic proof of asset reserves with periodic attestation and auditing.";
                ServiceIcon = "fas fa-certificate";
                ServiceColor = "#198754";
                ServiceType = "Blockchain";
                ServiceLayer = "Blockchain";
                ServiceFeatures = new List<string> { "Reserve Proofs", "Periodic Attestation", "Auditing", "Transparency", "Cryptographic Verification" };
                ServiceOperations = new List<string> { "Generate Proof", "Verify Reserves", "Audit Assets", "Publish Report" };
                break;

            case "compute":
                ServiceDisplayName = "Compute";
                ServiceSubtitle = "Distributed Computing & Resource Allocation";
                ServiceDescription = "Distributed computation with resource allocation and secure job execution.";
                ServiceIcon = "fas fa-microchip";
                ServiceColor = "#6610f2";
                ServiceType = "Automation";
                ServiceLayer = "Automation";
                ServiceFeatures = new List<string> { "Distributed Computing", "Resource Allocation", "Job Scheduling", "Load Balancing", "Secure Execution" };
                ServiceOperations = new List<string> { "Submit Job", "Allocate Resources", "Monitor Progress", "Get Results" };
                break;

            case "automation":
                ServiceDisplayName = "Automation";
                ServiceSubtitle = "Workflow Automation & Task Scheduling";
                ServiceDescription = "Workflow automation and task scheduling with trigger-based execution.";
                ServiceIcon = "fas fa-robot";
                ServiceColor = "#dc3545";
                ServiceType = "Automation";
                ServiceLayer = "Automation";
                ServiceFeatures = new List<string> { "Workflow Automation", "Task Scheduling", "Trigger-based Execution", "Process Management", "Monitoring" };
                ServiceOperations = new List<string> { "Create Workflow", "Schedule Task", "Execute Automation", "Monitor Jobs" };
                break;

            case "notification":
                ServiceDisplayName = "Notification";
                ServiceSubtitle = "Multi-channel Communication";
                ServiceDescription = "Multi-channel notification system with template support and delivery tracking.";
                ServiceIcon = "fas fa-bell";
                ServiceColor = "#ffc107";
                ServiceType = "Automation";
                ServiceLayer = "Automation";
                ServiceFeatures = new List<string> { "Multi-channel Delivery", "Template Support", "Delivery Tracking", "Subscription Management", "Filtering" };
                ServiceOperations = new List<string> { "Send Notification", "Create Template", "Manage Subscriptions", "Track Delivery" };
                break;

            case "randomness":
                ServiceDisplayName = "Randomness";
                ServiceSubtitle = "Cryptographically Secure Random Numbers";
                ServiceDescription = "Cryptographically secure random number generation with bias resistance.";
                ServiceIcon = "fas fa-random";
                ServiceColor = "#6f42c1";
                ServiceType = "Automation";
                ServiceLayer = "Automation";
                ServiceFeatures = new List<string> { "Secure Generation", "Bias Resistance", "Entropy Collection", "Verifiable Randomness", "High Throughput" };
                ServiceOperations = new List<string> { "Generate Random", "Get Entropy", "Verify Randomness", "Request Seed" };
                break;

            case "monitoring":
                ServiceDisplayName = "Monitoring";
                ServiceSubtitle = "Performance Metrics & Analytics";
                ServiceDescription = "Performance metrics collection and analysis with real-time dashboards.";
                ServiceIcon = "fas fa-chart-line";
                ServiceColor = "#198754";
                ServiceType = "Infrastructure";
                ServiceLayer = "Infrastructure";
                ServiceFeatures = new List<string> { "Metrics Collection", "Real-time Dashboards", "Performance Analysis", "Alerting", "Historical Data" };
                ServiceOperations = new List<string> { "Collect Metrics", "Generate Dashboard", "Analyze Performance", "Set Alerts" };
                break;

            case "configuration":
                ServiceDisplayName = "Configuration";
                ServiceSubtitle = "Dynamic Settings Management";
                ServiceDescription = "Dynamic configuration management with validation and encrypted storage.";
                ServiceIcon = "fas fa-cog";
                ServiceColor = "#6c757d";
                ServiceType = "Infrastructure";
                ServiceLayer = "Infrastructure";
                ServiceFeatures = new List<string> { "Dynamic Updates", "Validation", "Encrypted Storage", "Version Control", "Rollback Support" };
                ServiceOperations = new List<string> { "Update Config", "Validate Settings", "Get Configuration", "Rollback Changes" };
                break;

            case "eventsubscription":
                ServiceDisplayName = "Event Subscription";
                ServiceSubtitle = "Real-time Event Management";
                ServiceDescription = "Event subscription and real-time notification system with filtering support.";
                ServiceIcon = "fas fa-calendar-alt";
                ServiceColor = "#0d6efd";
                ServiceType = "Infrastructure";
                ServiceLayer = "Infrastructure";
                ServiceFeatures = new List<string> { "Event Subscription", "Real-time Notifications", "Filtering", "Delivery Guarantees", "Event Replay" };
                ServiceOperations = new List<string> { "Subscribe to Events", "Filter Events", "Replay Events", "Manage Subscriptions" };
                break;

            case "fairordering":
                ServiceDisplayName = "Fair Ordering";
                ServiceSubtitle = "Advanced Transaction Sequencing";
                ServiceDescription = "Advanced transaction ordering and fair sequencing for blockchain consensus with bias prevention.";
                ServiceIcon = "fas fa-sort-amount-up";
                ServiceColor = "#8e44ad";
                ServiceType = "Advanced";
                ServiceLayer = "Advanced";
                ServiceFeatures = new List<string> { "Fair Sequencing", "Bias Prevention", "Transaction Ordering", "Consensus Optimization", "MEV Protection" };
                ServiceOperations = new List<string> { "Order Transactions", "Check Fairness", "Validate Sequence", "Generate Proof" };
                break;

            default:
                ServiceDisplayName = "Unknown Service";
                ServiceSubtitle = "Service Information Not Available";
                ServiceDescription = "The requested service information is not available.";
                ServiceIcon = "fas fa-question-circle";
                ServiceColor = "#6c757d";
                ServiceType = "Unknown";
                ServiceLayer = "Unknown";
                ServiceFeatures = new List<string> { "Contact Administrator" };
                ServiceOperations = new List<string> { "Check Status" };
                break;
        }
    }
}
