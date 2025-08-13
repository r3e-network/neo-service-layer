using Xunit;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NeoServiceLayer.Integration.Tests.Framework;

namespace NeoServiceLayer.Integration.Tests
{
    /// <summary>
    /// Comprehensive end-to-end tests for complex workflows involving multiple services.
    /// </summary>
    [Collection("Integration")]
    public class ComplexWorkflowTests : IClassFixture<ServiceInteroperabilityTestFramework>
    {
        private readonly ServiceInteroperabilityTestFramework _framework;
        private readonly ILogger<ComplexWorkflowTests> _logger;

        public ComplexWorkflowTests(ServiceInteroperabilityTestFramework framework)
        {
            _framework = framework;
            _logger = framework.GetService<ILogger<ComplexWorkflowTests>>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Type", "EndToEnd")]
        public async Task CompleteUserJourney_CreateAccountToTransaction_ShouldSucceed()
        {
            // Arrange
            var workflow = new ComplexWorkflowDefinition
            {
                Name = "CompleteUserJourney_CreateAccountToTransaction",
                RequiredServices = new List<string>
                {
                    "AuthenticationService",
                    "KeyManagementService", 
                    "AbstractAccountService",
                    "SmartContractsService",
                    "NotificationService",
                    "MonitoringService"
                },
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = "CreateUserAccount",
                        ServiceName = "AuthenticationService",
                        MethodName = "RegisterUserAsync",
                        Parameters = new Dictionary<string, object>
                        {
                            ["username"] = "testuser@example.com",
                            ["password"] = "SecurePassword123!",
                            ["userInfo"] = new { firstName = "Test", lastName = "User" }
                        },
                        ResultKey = "userId"
                    },
                    new WorkflowStep
                    {
                        Name = "GenerateUserKeys",
                        ServiceName = "KeyManagementService",
                        MethodName = "GenerateKeyPairAsync",
                        Parameters = new Dictionary<string, object>
                        {
                            ["userId"] = "{userId}",
                            ["keyType"] = "secp256r1"
                        },
                        ResultKey = "keyPair"
                    },
                    new WorkflowStep
                    {
                        Name = "CreateAbstractAccount",
                        ServiceName = "AbstractAccountService",
                        MethodName = "CreateAccountAsync",
                        Parameters = new Dictionary<string, object>
                        {
                            ["userId"] = "{userId}",
                            ["publicKey"] = "{keyPair.publicKey}",
                            ["accountType"] = "Standard"
                        },
                        ResultKey = "accountAddress"
                    },
                    new WorkflowStep
                    {
                        Name = "DeployUserContract",
                        ServiceName = "SmartContractsService",
                        MethodName = "DeployContractAsync",
                        Parameters = new Dictionary<string, object>
                        {
                            ["contractCode"] = "UserAccountContract",
                            ["ownerAddress"] = "{accountAddress}",
                            ["initParameters"] = new { userId = "{userId}" }
                        },
                        ResultKey = "contractHash"
                    },
                    new WorkflowStep
                    {
                        Name = "SetupNotifications",
                        ServiceName = "NotificationService",
                        MethodName = "SubscribeToEventsAsync",
                        Parameters = new Dictionary<string, object>
                        {
                            ["userId"] = "{userId}",
                            ["contractHash"] = "{contractHash}",
                            ["eventTypes"] = new[] { "Transfer", "Invoke" }
                        },
                        ResultKey = "subscriptionId"
                    },
                    new WorkflowStep
                    {
                        Name = "InitializeMonitoring",
                        ServiceName = "MonitoringService",
                        MethodName = "RegisterAccountForMonitoringAsync",
                        Parameters = new Dictionary<string, object>
                        {
                            ["accountAddress"] = "{accountAddress}",
                            ["monitoringLevel"] = "Standard"
                        },
                        ResultKey = "monitoringId"
                    }
                },
                ValidationRules = new List<ValidationRule>
                {
                    new ValidationRule 
                    { 
                        Name = "ValidateAccountCreation",
                        Type = "AccountExists",
                        Parameters = new Dictionary<string, object> { ["accountAddress"] = "{accountAddress}" }
                    },
                    new ValidationRule 
                    { 
                        Name = "ValidateContractDeployment",
                        Type = "ContractExists",
                        Parameters = new Dictionary<string, object> { ["contractHash"] = "{contractHash}" }
                    }
                }
            };

            // Act
            var result = await _framework.ExecuteComplexWorkflowAsync(workflow);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue($"Workflow failed: {result.FailureReason}");
            result.Steps.Should().HaveCount(6);
            result.Steps.Should().OnlyContain(s => s.Success);
            result.Duration.Should().BeLessThan(TimeSpan.FromMinutes(2));

            _logger.LogInformation("Complete user journey workflow completed successfully in {Duration}ms", 
                result.Duration.TotalMilliseconds);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Type", "CrossService")]
        public async Task CrossChainTransaction_NeoToNeoX_ShouldMaintainConsistency()
        {
            // Arrange
            var workflow = new ComplexWorkflowDefinition
            {
                Name = "CrossChainTransaction_NeoToNeoX",
                RequiredServices = new List<string>
                {
                    "CrossChainService",
                    "KeyManagementService",
                    "SmartContractsService",
                    "EnclaveStorageService",
                    "MonitoringService"
                },
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = "InitiateCrossChainTransfer",
                        ServiceName = "CrossChainService",
                        MethodName = "InitiateTransferAsync",
                        Parameters = new Dictionary<string, object>
                        {
                            ["fromChain"] = "Neo",
                            ["toChain"] = "NeoX",
                            ["amount"] = 100.0,
                            ["sourceAddress"] = "NQ1234567890",
                            ["targetAddress"] = "0x1234567890abcdef"
                        },
                        ResultKey = "transferId"
                    },
                    new WorkflowStep
                    {
                        Name = "ValidateTransferAuthorization",
                        ServiceName = "KeyManagementService", 
                        MethodName = "ValidateSignatureAsync",
                        Parameters = new Dictionary<string, object>
                        {
                            ["transferId"] = "{transferId}",
                            ["signature"] = "signature_data",
                            ["publicKey"] = "user_public_key"
                        },
                        ResultKey = "authorizationValid"
                    },
                    new WorkflowStep
                    {
                        Name = "ExecuteSourceChainLock",
                        ServiceName = "SmartContractsService",
                        MethodName = "InvokeContractAsync",
                        Parameters = new Dictionary<string, object>
                        {
                            ["contractHash"] = "CrossChainLockContract",
                            ["method"] = "lockTokens",
                            ["parameters"] = new[] { "{transferId}", 100.0 }
                        },
                        ResultKey = "lockTransactionHash"
                    },
                    new WorkflowStep
                    {
                        Name = "StoreTransferProof",
                        ServiceName = "EnclaveStorageService",
                        MethodName = "StoreSecureDataAsync",
                        Parameters = new Dictionary<string, object>
                        {
                            ["key"] = "crosschain_proof_{transferId}",
                            ["data"] = new { 
                                transferId = "{transferId}",
                                lockTxHash = "{lockTransactionHash}",
                                timestamp = DateTime.UtcNow 
                            }
                        },
                        ResultKey = "proofStored"
                    },
                    new WorkflowStep
                    {
                        Name = "ExecuteTargetChainRelease",
                        ServiceName = "SmartContractsService",
                        MethodName = "InvokeContractAsync",
                        Parameters = new Dictionary<string, object>
                        {
                            ["contractHash"] = "CrossChainReleaseContract",
                            ["method"] = "releaseTokens",
                            ["parameters"] = new[] { "{transferId}", "0x1234567890abcdef", 100.0 }
                        },
                        ResultKey = "releaseTransactionHash"
                    },
                    new WorkflowStep
                    {
                        Name = "LogCrossChainEvent",
                        ServiceName = "MonitoringService",
                        MethodName = "LogEventAsync",
                        Parameters = new Dictionary<string, object>
                        {
                            ["eventType"] = "CrossChainTransfer",
                            ["transferId"] = "{transferId}",
                            ["details"] = new {
                                lockTx = "{lockTransactionHash}",
                                releaseTx = "{releaseTransactionHash}"
                            }
                        },
                        ResultKey = "eventLogged"
                    }
                }
            };

            // Act
            var result = await _framework.ExecuteComplexWorkflowAsync(workflow);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue($"Cross-chain workflow failed: {result.FailureReason}");
            result.Steps.Should().HaveCount(6);
            
            // Validate data consistency across chains
            var consistencyResult = await _framework.ValidateDataConsistencyAsync(
                new List<string> { "CrossChainService", "EnclaveStorageService", "MonitoringService" },
                $"crosschain_transfer_{result.Steps[0].Result}",
                new { status = "completed", amount = 100.0 }
            );
            
            consistencyResult.IsConsistent.Should().BeTrue("Cross-chain data should be consistent");
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Type", "Resilience")]
        public async Task ServiceFailureRecovery_KeyManagementDown_ShouldGracefullyDegrade()
        {
            // Test resilience when key management service fails
            var resilienceResult = await _framework.TestServiceResilienceAsync(
                "KeyManagementService",
                FailureScenario.ServiceUnavailable,
                TimeSpan.FromMinutes(1)
            );

            // Assert
            resilienceResult.Should().NotBeNull();
            resilienceResult.Success.Should().BeTrue("Service should handle key management failure gracefully");
            resilienceResult.ServiceBehavior.MaintainedBasicFunctionality.Should().BeTrue();
            resilienceResult.RecoveryTime.Should().BeLessThan(TimeSpan.FromMinutes(5));
            resilienceResult.DependencyImpact.ImpactedServices.Should().NotBeEmpty();
            
            _logger.LogInformation("Service resilience test completed. Recovery time: {RecoveryTime}",
                resilienceResult.RecoveryTime);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Type", "Performance")]
        public async Task HighLoadScenario_MultipleServicesUnderLoad_ShouldMaintainPerformance()
        {
            // Arrange
            var tasks = new List<Task<WorkflowExecutionResult>>();
            const int concurrentWorkflows = 10;

            var loadTestWorkflow = new ComplexWorkflowDefinition
            {
                Name = "LoadTest_BasicOperations",
                RequiredServices = new List<string>
                {
                    "AuthenticationService",
                    "KeyManagementService",
                    "EnclaveStorageService"
                },
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = "GenerateKey",
                        ServiceName = "KeyManagementService",
                        MethodName = "GenerateKeyPairAsync",
                        Parameters = new Dictionary<string, object>
                        {
                            ["keyType"] = "secp256r1"
                        }
                    },
                    new WorkflowStep
                    {
                        Name = "StoreData",
                        ServiceName = "EnclaveStorageService",
                        MethodName = "StoreSecureDataAsync",
                        Parameters = new Dictionary<string, object>
                        {
                            ["key"] = $"load_test_{Guid.NewGuid()}",
                            ["data"] = new { testData = "load_test_data", timestamp = DateTime.UtcNow }
                        }
                    }
                }
            };

            // Act - Execute multiple workflows concurrently
            for (int i = 0; i < concurrentWorkflows; i++)
            {
                tasks.Add(_framework.ExecuteComplexWorkflowAsync(loadTestWorkflow));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(concurrentWorkflows);
            results.Should().OnlyContain(r => r.Success, "All concurrent workflows should succeed");
            
            var averageDuration = results.Average(r => r.Duration.TotalMilliseconds);
            averageDuration.Should().BeLessThan(5000, "Average workflow duration should be under 5 seconds under load");
            
            _logger.LogInformation("Load test completed. {Count} workflows with average duration: {Duration}ms",
                concurrentWorkflows, averageDuration);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Type", "DataConsistency")]
        public async Task DistributedTransaction_MultipleServices_ShouldMaintainACID()
        {
            // Arrange
            var transactionScenario = new TransactionTestScenario
            {
                Name = "DistributedUserOperation",
                Services = new List<string>
                {
                    "AuthenticationService",
                    "KeyManagementService",
                    "AbstractAccountService",
                    "EnclaveStorageService"
                },
                Operations = new List<TransactionOperation>
                {
                    new TransactionOperation
                    {
                        ServiceName = "AuthenticationService",
                        OperationType = "CreateUser",
                        Parameters = new Dictionary<string, object>
                        {
                            ["username"] = $"transaction_test_{Guid.NewGuid()}",
                            ["password"] = "SecurePassword123!"
                        },
                        Order = 1
                    },
                    new TransactionOperation
                    {
                        ServiceName = "KeyManagementService",
                        OperationType = "GenerateKeys",
                        Parameters = new Dictionary<string, object>
                        {
                            ["userId"] = "{userId}",
                            ["keyType"] = "secp256r1"
                        },
                        Order = 2
                    },
                    new TransactionOperation
                    {
                        ServiceName = "AbstractAccountService",
                        OperationType = "CreateAccount",
                        Parameters = new Dictionary<string, object>
                        {
                            ["userId"] = "{userId}",
                            ["publicKey"] = "{publicKey}"
                        },
                        Order = 3
                    },
                    new TransactionOperation
                    {
                        ServiceName = "EnclaveStorageService",
                        OperationType = "StoreProfile",
                        Parameters = new Dictionary<string, object>
                        {
                            ["userId"] = "{userId}",
                            ["profile"] = new { created = DateTime.UtcNow, accountAddress = "{accountAddress}" }
                        },
                        Order = 4
                    }
                },
                ConsistencyRules = new List<ConsistencyRule>
                {
                    new ConsistencyRule
                    {
                        Name = "UserDataConsistency",
                        Type = "CrossServiceDataMatch",
                        Parameters = new Dictionary<string, object>
                        {
                            ["userId"] = "{userId}",
                            ["services"] = new[] { "AuthenticationService", "AbstractAccountService", "EnclaveStorageService" }
                        }
                    }
                }
            };

            // Act
            var result = await _framework.TestTransactionConsistencyAsync(transactionScenario);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue($"Distributed transaction failed: {result.ErrorMessage}");
            result.OperationResults.Should().HaveCount(4);
            result.OperationResults.Should().OnlyContain(op => op.Success);
            result.FinalConsistency?.IsConsistent.Should().BeTrue("Final state should be consistent across all services");
            result.RolledBack.Should().BeFalse("Transaction should not have been rolled back");
            
            _logger.LogInformation("Distributed transaction test completed successfully in {Duration}ms",
                result.Duration.TotalMilliseconds);
        }

        [Theory]
        [Trait("Category", "Integration")]
        [Trait("Type", "Communication")]
        [InlineData("AuthenticationService", "KeyManagementService", CommunicationPattern.RequestResponse)]
        [InlineData("SmartContractsService", "NotificationService", CommunicationPattern.EventDriven)]
        [InlineData("MonitoringService", "EnclaveStorageService", CommunicationPattern.StreamingData)]
        public async Task ServiceCommunication_DifferentPatterns_ShouldWork(
            string sourceService, 
            string targetService, 
            CommunicationPattern pattern)
        {
            // Arrange
            var testPayload = new
            {
                messageId = Guid.NewGuid().ToString(),
                timestamp = DateTime.UtcNow,
                data = "test_communication_data"
            };

            // Act
            var result = await _framework.TestServiceCommunicationAsync(
                sourceService, 
                targetService, 
                testPayload, 
                pattern);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue($"Communication failed: {result.ErrorMessage}");
            result.Duration.Should().BeLessThan(TimeSpan.FromSeconds(10));
            result.SourceService.Should().Be(sourceService);
            result.TargetService.Should().Be(targetService);
            result.Pattern.Should().Be(pattern);
            
            _logger.LogInformation("Service communication test {Source} -> {Target} ({Pattern}) completed in {Duration}ms",
                sourceService, targetService, pattern, result.Duration.TotalMilliseconds);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Type", "Security")]
        public async Task SecurityWorkflow_FullAuthenticationFlow_ShouldEnforceAllSecurityMeasures()
        {
            // Arrange
            var securityWorkflow = new ComplexWorkflowDefinition
            {
                Name = "SecurityWorkflow_FullAuthentication",
                RequiredServices = new List<string>
                {
                    "AuthenticationService",
                    "KeyManagementService",
                    "EnclaveStorageService",
                    "MonitoringService"
                },
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Name = "InitiateSecureLogin",
                        ServiceName = "AuthenticationService",
                        MethodName = "InitiateLoginAsync",
                        Parameters = new Dictionary<string, object>
                        {
                            ["username"] = "security_test_user",
                            ["challenge"] = "security_challenge_data"
                        },
                        ResultKey = "loginChallenge"
                    },
                    new WorkflowStep
                    {
                        Name = "SignChallenge",
                        ServiceName = "KeyManagementService",
                        MethodName = "SignDataAsync",
                        Parameters = new Dictionary<string, object>
                        {
                            ["data"] = "{loginChallenge}",
                            ["keyId"] = "user_private_key"
                        },
                        ResultKey = "challengeSignature"
                    },
                    new WorkflowStep
                    {
                        Name = "VerifyAndAuthenticate",
                        ServiceName = "AuthenticationService",
                        MethodName = "CompleteLoginAsync",
                        Parameters = new Dictionary<string, object>
                        {
                            ["challenge"] = "{loginChallenge}",
                            ["signature"] = "{challengeSignature}"
                        },
                        ResultKey = "authToken"
                    },
                    new WorkflowStep
                    {
                        Name = "StoreAuthSession",
                        ServiceName = "EnclaveStorageService",
                        MethodName = "StoreSecureDataAsync",
                        Parameters = new Dictionary<string, object>
                        {
                            ["key"] = "auth_session_{authToken}",
                            ["data"] = new { 
                                token = "{authToken}",
                                loginTime = DateTime.UtcNow,
                                securityLevel = "High"
                            }
                        },
                        ResultKey = "sessionStored"
                    },
                    new WorkflowStep
                    {
                        Name = "LogSecurityEvent",
                        ServiceName = "MonitoringService",
                        MethodName = "LogSecurityEventAsync",
                        Parameters = new Dictionary<string, object>
                        {
                            ["eventType"] = "SecureLogin",
                            ["userId"] = "security_test_user",
                            ["details"] = new { 
                                authToken = "{authToken}",
                                timestamp = DateTime.UtcNow 
                            }
                        },
                        ResultKey = "securityEventLogged"
                    }
                },
                ValidationRules = new List<ValidationRule>
                {
                    new ValidationRule
                    {
                        Name = "ValidateAuthToken",
                        Type = "TokenValid",
                        Parameters = new Dictionary<string, object> { ["token"] = "{authToken}" }
                    },
                    new ValidationRule
                    {
                        Name = "ValidateSecurityLog",
                        Type = "SecurityEventExists",
                        Parameters = new Dictionary<string, object> { ["eventId"] = "{securityEventLogged}" }
                    }
                }
            };

            // Act
            var result = await _framework.ExecuteComplexWorkflowAsync(securityWorkflow);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue($"Security workflow failed: {result.FailureReason}");
            result.Steps.Should().HaveCount(5);
            result.Steps.Should().OnlyContain(s => s.Success);
            
            // Additional security validations
            var authTokenStep = result.Steps.Find(s => s.Step.Name == "VerifyAndAuthenticate");
            authTokenStep?.Result.Should().NotBeNull("Authentication should produce a valid token");
            
            _logger.LogInformation("Security workflow completed successfully with all security measures enforced");
        }
    }
}