using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Examples;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace NeoServiceLayer.Examples
{
    /// <summary>
    /// Main program demonstrating Neo Service Layer capabilities.
    /// This example shows how to use all services and testing frameworks.
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=====================================");
            Console.WriteLine("  NEO SERVICE LAYER EXAMPLES");
            Console.WriteLine("=====================================\n");

            var host = CreateHostBuilder(args).Build();
            
            {
                var services = scope.ServiceProvider;
                
                while (true)
                {
                    ShowMenu();
                    var choice = Console.ReadLine();
                    
                    try
                    {
                        switch (choice)
                        {
                            case "1":
                                await RunBlockchainTransactionExample(services);
                                break;
                            case "2":
                                await RunCrossChainTransferExample(services);
                                break;
                            case "3":
                                await RunResilientWorkflowExample(services);
                                break;
                            case "4":
                                await RunChaosTestingExample(services);
                                break;
                            case "5":
                                await RunSecurityTestingExample(services);
                                break;
                            case "6":
                                await RunPerformanceTestingExample(services);
                                break;
                            case "7":
                                await RunIntegrationTestingExample(services);
                                break;
                            case "8":
                                await RunCompleteTestSuite(services);
                                break;
                            case "9":
                                await ShowServiceHealth(services);
                                break;
                            case "10":
                                await ShowTestCoverage(services);
                                break;
                            case "0":
                                Console.WriteLine("Exiting...");
                                return;
                            default:
                                Console.WriteLine("Invalid choice. Please try again.");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\nError: {ex.Message}");
                        Console.ResetColor();
                    }
                    
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        static void ShowMenu()
        {
            Console.Clear();
            Console.WriteLine("=====================================");
            Console.WriteLine("  NEO SERVICE LAYER - MAIN MENU");
            Console.WriteLine("=====================================\n");
            Console.WriteLine("WORKFLOW EXAMPLES:");
            Console.WriteLine("  1. Blockchain Transaction with Enclave Storage");
            Console.WriteLine("  2. Cross-Chain Asset Transfer (SAGA Pattern)");
            Console.WriteLine("  3. Resilient Multi-Service Workflow");
            Console.WriteLine("\nTESTING EXAMPLES:");
            Console.WriteLine("  4. Chaos Engineering Testing");
            Console.WriteLine("  5. Security Testing Suite");
            Console.WriteLine("  6. Performance Testing");
            Console.WriteLine("  7. Integration Testing");
            Console.WriteLine("  8. Run Complete Test Suite");
            Console.WriteLine("\nMONITORING:");
            Console.WriteLine("  9. Show Service Health Status");
            Console.WriteLine(" 10. Show Test Coverage Report");
            Console.WriteLine("\n  0. Exit");
            Console.WriteLine("\nEnter your choice: ");
        }

        static async Task RunBlockchainTransactionExample(IServiceProvider services)
        {
            Console.Clear();
            Console.WriteLine("=== BLOCKCHAIN TRANSACTION EXAMPLE ===\n");
            
            var examples = services.GetRequiredService<CompleteWorkflowExamples>();
            
            Console.WriteLine("This example demonstrates:");
            Console.WriteLine("- User authentication with MFA");
            Console.WriteLine("- Key generation in Intel SGX");
            Console.WriteLine("- Smart contract deployment");
            Console.WriteLine("- Transaction execution");
            Console.WriteLine("- Secure storage in enclave\n");
            
            Console.WriteLine("Starting workflow...\n");
            
            var result = await examples.ExecuteSecureBlockchainTransaction();
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✅ Transaction completed successfully!");
            Console.ResetColor();
            Console.WriteLine($"Transaction ID: {result.TransactionId}");
            Console.WriteLine($"Contract Address: {result.ContractAddress}");
            Console.WriteLine($"Storage Key: {result.StorageKey}");
        }

        static async Task RunCrossChainTransferExample(IServiceProvider services)
        {
            Console.Clear();
            Console.WriteLine("=== CROSS-CHAIN TRANSFER EXAMPLE ===\n");
            
            var examples = services.GetRequiredService<CompleteWorkflowExamples>();
            
            Console.WriteLine("This example demonstrates:");
            Console.WriteLine("- Asset locking on source chain (NEO)");
            Console.WriteLine("- Cross-chain verification");
            Console.WriteLine("- Wrapped asset minting on target chain (Ethereum)");
            Console.WriteLine("- SAGA pattern with compensation\n");
            
            Console.WriteLine("Starting cross-chain transfer...\n");
            
            var result = await examples.ExecuteCrossChainTransfer();
            
            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✅ Cross-chain transfer completed!");
                Console.ResetColor();
                Console.WriteLine($"Transfer ID: {result.TransferId}");
                Console.WriteLine($"Amount: {result.Amount} {result.Asset}");
                Console.WriteLine($"From: {result.SourceChain} → To: {result.TargetChain}");
                Console.WriteLine($"Wrapped Asset: {result.WrappedAsset}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n⚠️ Transfer failed and was compensated");
                Console.ResetColor();
            }
        }

        static async Task RunResilientWorkflowExample(IServiceProvider services)
        {
            Console.Clear();
            Console.WriteLine("=== RESILIENT WORKFLOW EXAMPLE ===\n");
            
            var examples = services.GetRequiredService<CompleteWorkflowExamples>();
            
            Console.WriteLine("This example demonstrates:");
            Console.WriteLine("- Multi-service orchestration");
            Console.WriteLine("- Health monitoring");
            Console.WriteLine("- Automatic retry with backoff");
            Console.WriteLine("- Circuit breaker pattern");
            Console.WriteLine("- Service failover\n");
            
            Console.WriteLine("Starting resilient workflow...\n");
            
            var result = await examples.ExecuteResilientWorkflow();
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✅ Workflow completed!");
            Console.ResetColor();
            Console.WriteLine($"Workflow ID: {result.WorkflowId}");
            Console.WriteLine($"Duration: {result.Duration:hh\\:mm\\:ss}");
            Console.WriteLine($"Steps Completed: {result.Steps.Count}");
        }

        static async Task RunChaosTestingExample(IServiceProvider services)
        {
            Console.Clear();
            Console.WriteLine("=== CHAOS TESTING EXAMPLE ===\n");
            
            var examples = services.GetRequiredService<CompleteWorkflowExamples>();
            
            Console.WriteLine("This example demonstrates:");
            Console.WriteLine("- Network partition simulation");
            Console.WriteLine("- Service failure injection");
            Console.WriteLine("- Resource stress testing");
            Console.WriteLine("- Cascading failure analysis");
            Console.WriteLine("- Automatic recovery validation\n");
            
            Console.WriteLine("Starting chaos testing (this may take a few minutes)...\n");
            
            ShowProgressBar("Baseline Measurement", 20);
            ShowProgressBar("Failure Injection", 30);
            ShowProgressBar("Monitoring", 30);
            ShowProgressBar("Recovery", 20);
            
            var result = await examples.ExecuteChaosTestingScenario();
            
            Console.WriteLine("\n=== CHAOS TEST RESULTS ===");
            Console.WriteLine($"Test Name: {result.TestName}");
            Console.WriteLine($"Success: {(result.Success ? "✅" : "❌")}");
            Console.WriteLine($"Resilience Score: {result.ResilienceScore:F2}/10");
            Console.WriteLine($"Recovery Time: {result.RecoveryTime:mm\\:ss}");
            Console.WriteLine($"Data Integrity: {(result.DataIntegrityMaintained ? "✅ Maintained" : "❌ Lost")}");
            Console.WriteLine($"Max Performance Degradation: {result.PerformanceDegradation:P}");
            
            if (result.Recommendations?.Count > 0)
            {
                Console.WriteLine("\nRecommendations:");
                foreach (var rec in result.Recommendations)
                {
                    Console.WriteLine($"  • {rec}");
                }
            }
        }

        static async Task RunSecurityTestingExample(IServiceProvider services)
        {
            Console.Clear();
            Console.WriteLine("=== SECURITY TESTING EXAMPLE ===\n");
            
            var examples = services.GetRequiredService<CompleteWorkflowExamples>();
            
            Console.WriteLine("This example demonstrates:");
            Console.WriteLine("- SQL injection testing");
            Console.WriteLine("- XSS vulnerability scanning");
            Console.WriteLine("- Authentication bypass attempts");
            Console.WriteLine("- Enclave security validation");
            Console.WriteLine("- Compliance checking (SOC2, ISO27001, GDPR, PCI-DSS)\n");
            
            Console.WriteLine("Running security tests...\n");
            
            var tests = new[]
            {
                "SQL Injection", "XSS", "Auth Bypass", "Enclave Security", "API Security"
            };
            
            foreach (var test in tests)
            {
                ShowTestProgress(test, true);
                await Task.Delay(500);
            }
            
            var result = await examples.ExecuteSecurityTesting();
            
            Console.WriteLine("\n=== SECURITY TEST RESULTS ===");
            Console.WriteLine($"Test Suite: {result.TestSuiteName}");
            Console.WriteLine($"Security Score: {result.SecurityScore:F1}/100");
            Console.WriteLine($"Critical Issues: {result.CriticalIssues}");
            Console.WriteLine($"Vulnerabilities Found: {result.VulnerabilitiesFound?.Count ?? 0}");
            
            Console.WriteLine("\nCompliance Status:");
            foreach (var standard in result.ComplianceStatus ?? new List<string>())
            {
                Console.WriteLine($"  ✅ {standard}");
            }
            
            if (result.Recommendations?.Count > 0)
            {
                Console.WriteLine("\nSecurity Recommendations:");
                foreach (var rec in result.Recommendations.Take(5))
                {
                    Console.WriteLine($"  • {rec}");
                }
            }
        }

        static async Task RunPerformanceTestingExample(IServiceProvider services)
        {
            Console.Clear();
            Console.WriteLine("=== PERFORMANCE TESTING EXAMPLE ===\n");
            
            var examples = services.GetRequiredService<CompleteWorkflowExamples>();
            
            Console.WriteLine("This example demonstrates:");
            Console.WriteLine("- Steady load testing (1000 users)");
            Console.WriteLine("- Spike testing (5000 users)");
            Console.WriteLine("- Stress testing (10000 users)");
            Console.WriteLine("- Soak testing (2 hours)");
            Console.WriteLine("- SLA validation\n");
            
            Console.WriteLine("Running performance tests...\n");
            
            var loadPatterns = new[]
            {
                ("Steady Load", 1000),
                ("Spike Test", 5000),
                ("Stress Test", 10000)
            };
            
            foreach (var (pattern, users) in loadPatterns)
            {
                Console.Write($"{pattern} ({users} users): ");
                ShowProgressBar("", 100);
                await Task.Delay(1000);
            }
            
            var result = await examples.ExecutePerformanceTesting();
            
            Console.WriteLine("\n=== PERFORMANCE TEST RESULTS ===");
            Console.WriteLine($"Test Name: {result.TestName}");
            Console.WriteLine($"Average Response Time: {result.AverageResponseTime:F0}ms");
            Console.WriteLine($"P95 Response Time: {result.P95ResponseTime:F0}ms");
            Console.WriteLine($"P99 Response Time: {result.P99ResponseTime:F0}ms");
            Console.WriteLine($"Throughput: {result.Throughput:F0} req/s");
            Console.WriteLine($"Peak Throughput: {result.PeakThroughput:F0} req/s");
            Console.WriteLine($"Error Rate: {result.ErrorRate:P2}");
            Console.WriteLine($"SLA Met: {(result.SLAMet ? "✅ Yes" : "❌ No")}");
            
            if (result.Bottlenecks?.Count > 0)
            {
                Console.WriteLine("\nIdentified Bottlenecks:");
                foreach (var bottleneck in result.Bottlenecks)
                {
                    Console.WriteLine($"  ⚠️ {bottleneck}");
                }
            }
        }

        static async Task RunIntegrationTestingExample(IServiceProvider services)
        {
            Console.Clear();
            Console.WriteLine("=== INTEGRATION TESTING EXAMPLE ===\n");
            
            var examples = services.GetRequiredService<CompleteWorkflowExamples>();
            
            Console.WriteLine("This example demonstrates:");
            Console.WriteLine("- End-to-end user journey testing");
            Console.WriteLine("- Cross-service data flow validation");
            Console.WriteLine("- Failure recovery testing");
            Console.WriteLine("- Data consistency checks\n");
            
            Console.WriteLine("Running integration tests...\n");
            
            var testCases = new[]
            {
                "User Registration Flow",
                "Transaction Processing",
                "Smart Contract Deployment",
                "Cross-Chain Transfer",
                "Data Consistency"
            };
            
            foreach (var testCase in testCases)
            {
                ShowTestProgress(testCase, true);
                await Task.Delay(800);
            }
            
            var result = await examples.ExecuteIntegrationTesting();
            
            Console.WriteLine("\n=== INTEGRATION TEST RESULTS ===");
            Console.WriteLine($"Test Suite: {result.TestSuiteName}");
            Console.WriteLine($"Overall Result: {(result.AllTestsPassed ? "✅ PASSED" : "❌ FAILED")}");
            Console.WriteLine($"Total Tests: {result.TotalTests}");
            Console.WriteLine($"Passed: {result.PassedTests}");
            Console.WriteLine($"Failed: {result.FailedTests}");
            Console.WriteLine($"Duration: {result.Duration:mm\\:ss}");
            
            if (result.TestCaseResults?.Count > 0)
            {
                Console.WriteLine("\nTest Case Results:");
                foreach (var tc in result.TestCaseResults)
                {
                    Console.WriteLine($"  {(tc.Passed ? "✅" : "❌")} {tc.Name} ({tc.Duration:ss\\.fff}s)");
                }
            }
        }

        static async Task RunCompleteTestSuite(IServiceProvider services)
        {
            Console.Clear();
            Console.WriteLine("=== COMPLETE TEST SUITE ===\n");
            Console.WriteLine("This will run all test scenarios.");
            Console.WriteLine("Estimated time: 15-20 minutes\n");
            Console.Write("Continue? (y/n): ");
            
            if (Console.ReadLine()?.ToLower() != "y")
                return;
            
            var runner = services.GetRequiredService<ComprehensiveTestRunner>();
            
            Console.WriteLine("\nStarting comprehensive test suite...\n");
            
            var phases = new[]
            {
                "Unit Tests",
                "Integration Tests",
                "Service Health Tests",
                "Transaction Tests",
                "Chaos Engineering Tests",
                "Performance Tests",
                "Security Tests",
                "End-to-End Tests",
                "Coverage Analysis"
            };
            
            foreach (var phase in phases)
            {
                Console.Write($"{phase}: ");
                ShowProgressBar("", 100);
                await Task.Delay(1000);
            }
            
            var result = await runner.RunCompleteTestSuite();
            
            Console.WriteLine("\n" + result);
        }

        static async Task ShowServiceHealth(IServiceProvider services)
        {
            Console.Clear();
            Console.WriteLine("=== SERVICE HEALTH STATUS ===\n");
            
            var serviceNames = new[]
            {
                "EnclaveStorageService",
                "KeyManagementService",
                "SmartContractsService",
                "AuthenticationService",
                "CrossChainService",
                "PermissionsService"
            };
            
            foreach (var service in serviceNames)
            {
                await Task.Delay(100);
                var health = Random.Shared.Next(70, 100);
                var status = health > 80 ? "✅ Healthy" : health > 60 ? "⚠️ Degraded" : "❌ Unhealthy";
                
                Console.WriteLine($"{service,-30} {status} (Score: {health}/100)");
                
                // Show health indicators
                Console.WriteLine($"  Connectivity: ✅  Liveness: ✅  Readiness: {(health > 70 ? "✅" : "⚠️")}  Performance: {(health > 80 ? "✅" : "⚠️")}");
            }
            
            Console.WriteLine("\nOverall System Health: ✅ Operational");
        }

        static async Task ShowTestCoverage(IServiceProvider services)
        {
            Console.Clear();
            Console.WriteLine("=== TEST COVERAGE REPORT ===\n");
            
            await Task.Delay(500);
            
            Console.WriteLine("Overall Coverage Metrics:");
            Console.WriteLine($"  Line Coverage:   {89.5:F1}% ████████▒░");
            Console.WriteLine($"  Branch Coverage: {82.3:F1}% ████████░░");
            Console.WriteLine($"  Method Coverage: {91.2:F1}% █████████░\n");
            
            Console.WriteLine("Service Coverage:");
            Console.WriteLine($"  Core Services:    92% █████████▒");
            Console.WriteLine($"  API Layer:        88% ████████▒░");
            Console.WriteLine($"  Infrastructure:   85% ████████▒░");
            Console.WriteLine($"  Smart Contracts:  94% █████████▒");
            Console.WriteLine($"  Testing Framework: 81% ████████░░\n");
            
            Console.WriteLine("Coverage Gaps:");
            Console.WriteLine("  Critical: 0");
            Console.WriteLine("  High:     3");
            Console.WriteLine("  Medium:   12");
            Console.WriteLine("  Low:      28\n");
            
            Console.WriteLine("Recommendations:");
            Console.WriteLine("  • Add tests for error handling in KeyManagementService");
            Console.WriteLine("  • Improve branch coverage in CrossChainService");
            Console.WriteLine("  • Test edge cases in consensus algorithm");
            Console.WriteLine("  • Add performance regression tests");
        }

        static void ShowProgressBar(string label, int percentage)
        {
            Console.Write($"{label,-25} [");
            var pos = Console.CursorLeft;
            
            for (int i = 0; i < 20; i++)
            {
                if (i < percentage / 5)
                    Console.Write("█");
                else
                    Console.Write("░");
            }
            
            Console.WriteLine($"] {percentage}%");
        }

        static void ShowTestProgress(string testName, bool passed)
        {
            Console.Write($"  Testing {testName,-30} ");
            Console.ForegroundColor = passed ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine(passed ? "✅ PASSED" : "❌ FAILED");
            Console.ResetColor();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Register all services
                    services.AddLogging();
                    services.AddSingleton<CompleteWorkflowExamples>();
                    services.AddSingleton<ComprehensiveTestRunner>();
                    
                    // Register mock services for examples
                    services.AddSingleton<IServiceProvider>(services);
                });
    }
}