using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Enclave;
using Xunit;
using Xunit.Abstractions;
using System.Text.Json;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Trait("Category", "OpenEnclave")]
    [Collection("SimulationMode")]
    public class EventTriggerTests
    {
        private readonly SimulationModeFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly ILogger _logger;

        public EventTriggerTests(SimulationModeFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            
            // Create a logger that writes to the test output
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));
            _logger = loggerFactory.CreateLogger<EventTriggerTests>();
            
            _logger.LogInformation("Test initialized with {UsingRealSdk}", 
                _fixture.UsingRealSdk ? "real SDK" : "mock implementation");
        }

        [Fact]
        public async Task EventTrigger_ShouldRegisterAndUnregisterTrigger()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string eventType = "blockchain";
            string functionId = "test-function-" + Guid.NewGuid().ToString();
            string userId = "test-user";
            string condition = @"{""event_type"": ""transfer"", ""contract_address"": ""0x1234567890abcdef""}";
            
            // Act - Register trigger
            string triggerId = await _fixture.TeeInterface.RegisterTriggerAsync(eventType, functionId, userId, condition);
            
            // Act - Get triggers for event
            var triggers = await _fixture.TeeInterface.GetTriggersForEventAsync(eventType);
            
            // Act - Get trigger info
            string triggerInfo = await _fixture.TeeInterface.GetTriggerInfoAsync(triggerId);
            
            // Act - Unregister trigger
            bool unregisterResult = await _fixture.TeeInterface.UnregisterTriggerAsync(triggerId);
            
            // Act - Get triggers for event after unregistering
            var triggersAfterUnregister = await _fixture.TeeInterface.GetTriggersForEventAsync(eventType);
            
            // Assert
            Assert.NotEmpty(triggerId);
            Assert.Contains(triggerId, triggers);
            Assert.NotEmpty(triggerInfo);
            Assert.True(unregisterResult);
            Assert.DoesNotContain(triggerId, triggersAfterUnregister);
            
            _logger.LogInformation("Trigger ID: {TriggerId}", triggerId);
            _logger.LogInformation("Trigger info: {TriggerInfo}", triggerInfo);
        }

        [Fact]
        public async Task EventTrigger_ShouldProcessBlockchainEvent()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string eventType = "blockchain";
            string functionId = "test-function-" + Guid.NewGuid().ToString();
            string userId = "test-user";
            string condition = @"{""event_type"": ""transfer"", ""contract_address"": ""0x1234567890abcdef""}";
            string jsCode = @"
                function main(input) {
                    return { 
                        event_processed: true,
                        event_type: input.event.type,
                        contract: input.event.contract,
                        trigger_id: input.trigger.id
                    };
                }
            ";
            
            // Act - Register trigger
            string triggerId = await _fixture.TeeInterface.RegisterTriggerAsync(eventType, functionId, userId, condition);
            
            // Act - Store JavaScript function
            await _fixture.TeeInterface.StoreJavaScriptFunctionAsync(functionId, jsCode, userId);
            
            // Act - Process blockchain event
            string eventData = @"{
                ""type"": ""transfer"",
                ""contract"": ""0x1234567890abcdef"",
                ""from"": ""0xabcdef1234567890"",
                ""to"": ""0x0987654321fedcba"",
                ""amount"": 100
            }";
            int processedCount = await _fixture.TeeInterface.ProcessBlockchainEventAsync(eventData);
            
            // Act - Clean up
            await _fixture.TeeInterface.UnregisterTriggerAsync(triggerId);
            
            // Assert
            Assert.Equal(1, processedCount);
            
            _logger.LogInformation("Processed {Count} triggers", processedCount);
        }

        [Fact]
        public async Task EventTrigger_ShouldProcessScheduledTriggers()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange
            string eventType = "schedule";
            string functionId = "test-function-" + Guid.NewGuid().ToString();
            string userId = "test-user";
            string condition = @"{""interval_seconds"": 60}";
            string jsCode = @"
                function main(input) {
                    return { 
                        scheduled_execution: true,
                        timestamp: input.timestamp,
                        trigger_id: input.trigger.id
                    };
                }
            ";
            
            // Act - Register trigger
            string triggerId = await _fixture.TeeInterface.RegisterTriggerAsync(eventType, functionId, userId, condition);
            
            // Act - Store JavaScript function
            await _fixture.TeeInterface.StoreJavaScriptFunctionAsync(functionId, jsCode, userId);
            
            // Act - Process scheduled triggers
            ulong currentTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int processedCount = await _fixture.TeeInterface.ProcessScheduledTriggersAsync(currentTime + 120); // 2 minutes in the future
            
            // Act - Clean up
            await _fixture.TeeInterface.UnregisterTriggerAsync(triggerId);
            
            // Assert
            Assert.Equal(1, processedCount);
            
            _logger.LogInformation("Processed {Count} scheduled triggers", processedCount);
        }
    }
}
