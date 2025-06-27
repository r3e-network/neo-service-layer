using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Testing;
using NeoServiceLayer.Contracts.Services;
using System;
using System.Numerics;

namespace NeoServiceLayer.Tests.Services
{
    [TestClass]
    public class AutomationContractTests : ContractTestFramework
    {
        private AutomationContract automationContract;
        private UInt160 testOwner;

        [TestInitialize]
        public void Setup()
        {
            automationContract = new AutomationContract();
            testOwner = UInt160.Parse("0x1234567890123456789012345678901234567890");
            
            // Deploy contract
            automationContract._deploy(null, false);
        }

        [TestMethod]
        public void TestCreateWorkflow_ValidParameters_Success()
        {
            // Arrange
            var name = "Test Workflow";
            var description = "A test workflow for automation";
            var steps = new AutomationContract.WorkflowStep[]
            {
                new AutomationContract.WorkflowStep
                {
                    Name = "Step 1",
                    Description = "First step",
                    Type = AutomationContract.StepType.ContractCall,
                    Parameters = "{\"contract\":\"0x1234\",\"method\":\"test\"}",
                    Condition = "",
                    ContinueOnFailure = false,
                    TimeoutSeconds = 30
                },
                new AutomationContract.WorkflowStep
                {
                    Name = "Step 2",
                    Description = "Second step",
                    Type = AutomationContract.StepType.Notification,
                    Parameters = "{\"message\":\"Workflow completed\"}",
                    Condition = "",
                    ContinueOnFailure = true,
                    TimeoutSeconds = 10
                }
            };
            var config = new AutomationContract.WorkflowConfig
            {
                MaxExecutionTime = 300,
                EnableLogging = true,
                RequireApproval = false,
                NotificationEndpoint = "https://api.example.com/notify"
            };

            // Act
            var workflowId = automationContract.CreateWorkflow(name, description, steps, config);

            // Assert
            Assert.IsNotNull(workflowId);
            Assert.IsTrue(workflowId.Length > 0);

            var workflow = automationContract.GetWorkflow(workflowId);
            Assert.IsNotNull(workflow);
            Assert.AreEqual(name, workflow.Name);
            Assert.AreEqual(description, workflow.Description);
            Assert.AreEqual(steps.Length, workflow.Steps.Length);
            Assert.AreEqual(AutomationContract.WorkflowStatus.Active, workflow.Status);
            Assert.AreEqual(1, workflow.Version);
            Assert.AreEqual(0, workflow.ExecutionCount);
        }

        [TestMethod]
        public void TestCreateWorkflow_TooManySteps_ThrowsException()
        {
            // Arrange
            var steps = new AutomationContract.WorkflowStep[51]; // Exceeds MAX_WORKFLOW_STEPS
            for (int i = 0; i < steps.Length; i++)
            {
                steps[i] = new AutomationContract.WorkflowStep
                {
                    Name = $"Step {i}",
                    Type = AutomationContract.StepType.ContractCall,
                    Parameters = "{}"
                };
            }
            var config = new AutomationContract.WorkflowConfig();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                automationContract.CreateWorkflow("Test", "Description", steps, config));
        }

        [TestMethod]
        public void TestUpdateWorkflow_ValidWorkflow_Success()
        {
            // Arrange
            var workflowId = CreateTestWorkflow();
            var newSteps = new AutomationContract.WorkflowStep[]
            {
                new AutomationContract.WorkflowStep
                {
                    Name = "Updated Step",
                    Type = AutomationContract.StepType.DataTransfer,
                    Parameters = "{\"updated\":true}"
                }
            };
            var newConfig = new AutomationContract.WorkflowConfig
            {
                MaxExecutionTime = 600,
                EnableLogging = false
            };

            // Act
            var result = automationContract.UpdateWorkflow(workflowId, newSteps, newConfig);

            // Assert
            Assert.IsTrue(result);

            var workflow = automationContract.GetWorkflow(workflowId);
            Assert.AreEqual(2, workflow.Version);
            Assert.AreEqual(newSteps.Length, workflow.Steps.Length);
            Assert.AreEqual("Updated Step", workflow.Steps[0].Name);
        }

        [TestMethod]
        public void TestScheduleTask_ValidWorkflow_Success()
        {
            // Arrange
            var workflowId = CreateTestWorkflow();
            var taskName = "Test Task";
            var trigger = new AutomationContract.TaskTrigger
            {
                Type = AutomationContract.TriggerType.Time,
                ScheduledTime = (ulong)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600),
                IntervalSeconds = 0,
                EventName = "",
                ConditionExpression = "",
                TriggerData = ""
            };
            var config = new AutomationContract.TaskConfig
            {
                MaxRetries = 3,
                RetryDelay = 60,
                TimeoutSeconds = 300,
                EnableNotifications = true,
                NotificationEndpoint = "https://api.example.com/notify"
            };

            // Act
            var taskId = automationContract.ScheduleTask(workflowId, taskName, trigger, config);

            // Assert
            Assert.IsNotNull(taskId);
            Assert.IsTrue(taskId.Length > 0);

            var task = automationContract.GetScheduledTask(taskId);
            Assert.IsNotNull(task);
            Assert.AreEqual(workflowId, task.WorkflowId);
            Assert.AreEqual(taskName, task.Name);
            Assert.AreEqual(AutomationContract.TaskStatus.Scheduled, task.Status);
            Assert.AreEqual(trigger.Type, task.Trigger.Type);
            Assert.AreEqual(0, task.ExecutionCount);
        }

        [TestMethod]
        public void TestScheduleTask_NonExistentWorkflow_ThrowsException()
        {
            // Arrange
            var nonExistentWorkflowId = new byte[] { 0x01, 0x02, 0x03 };
            var trigger = new AutomationContract.TaskTrigger
            {
                Type = AutomationContract.TriggerType.Manual
            };
            var config = new AutomationContract.TaskConfig();

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() =>
                automationContract.ScheduleTask(nonExistentWorkflowId, "Test", trigger, config));
        }

        [TestMethod]
        public void TestExecuteTask_ValidTask_Success()
        {
            // Arrange
            var workflowId = CreateTestWorkflow();
            var taskId = CreateTestTask(workflowId);
            var context = "Manual execution test";

            // Act
            var executionId = automationContract.ExecuteTask(taskId, context);

            // Assert
            Assert.IsNotNull(executionId);
            Assert.IsTrue(executionId.Length > 0);

            var execution = automationContract.GetTaskExecution(executionId);
            Assert.IsNotNull(execution);
            Assert.AreEqual(taskId, execution.TaskId);
            Assert.AreEqual(workflowId, execution.WorkflowId);
            Assert.AreEqual(context, execution.Context);
            Assert.IsTrue(execution.Status == AutomationContract.ExecutionStatus.Completed || 
                         execution.Status == AutomationContract.ExecutionStatus.Failed);

            var task = automationContract.GetScheduledTask(taskId);
            Assert.AreEqual(1, task.ExecutionCount);
            Assert.IsTrue(task.LastExecution > 0);
        }

        [TestMethod]
        public void TestExecuteTask_NonExistentTask_ThrowsException()
        {
            // Arrange
            var nonExistentTaskId = new byte[] { 0x01, 0x02, 0x03 };

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() =>
                automationContract.ExecuteTask(nonExistentTaskId, "test"));
        }

        [TestMethod]
        public void TestActivateTrigger_ValidTrigger_Success()
        {
            // Arrange
            var triggerType = AutomationContract.TriggerType.Event;
            var triggerData = "test_event";
            var context = "Event triggered execution";

            // Act
            var result = automationContract.ActivateTrigger(triggerType, triggerData, context);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestProcessScheduledTasks_NoTasks_ReturnsZero()
        {
            // Act
            var processedCount = automationContract.ProcessScheduledTasks();

            // Assert
            Assert.AreEqual(0, processedCount);
        }

        [TestMethod]
        public void TestWorkflowExecution_MultipleSteps_Success()
        {
            // Arrange
            var workflowId = CreateComplexTestWorkflow();
            var taskId = CreateTestTask(workflowId);

            // Act
            var executionId = automationContract.ExecuteTask(taskId, "Complex workflow test");

            // Assert
            var execution = automationContract.GetTaskExecution(executionId);
            Assert.IsNotNull(execution);
            Assert.IsTrue(execution.StepsExecuted > 0);
            Assert.IsNotNull(execution.StepResults);
        }

        [TestMethod]
        public void TestTaskRetryMechanism_FailedTask_RetriesCorrectly()
        {
            // Arrange
            var workflowId = CreateFailingWorkflow();
            var trigger = new AutomationContract.TaskTrigger
            {
                Type = AutomationContract.TriggerType.Manual
            };
            var config = new AutomationContract.TaskConfig
            {
                MaxRetries = 2,
                RetryDelay = 10,
                TimeoutSeconds = 30
            };
            var taskId = automationContract.ScheduleTask(workflowId, "Retry Test", trigger, config);

            // Act - Execute multiple times to test retry logic
            var execution1Id = automationContract.ExecuteTask(taskId, "First attempt");
            var execution2Id = automationContract.ExecuteTask(taskId, "Second attempt");

            // Assert
            var task = automationContract.GetScheduledTask(taskId);
            Assert.IsTrue(task.ExecutionCount >= 2);
            Assert.IsTrue(task.FailureCount > 0 || task.SuccessCount > 0);
        }

        [TestMethod]
        public void TestWorkflowVersioning_UpdateWorkflow_VersionIncremented()
        {
            // Arrange
            var workflowId = CreateTestWorkflow();
            var originalWorkflow = automationContract.GetWorkflow(workflowId);
            var originalVersion = originalWorkflow.Version;

            var newSteps = new AutomationContract.WorkflowStep[]
            {
                new AutomationContract.WorkflowStep
                {
                    Name = "Version Test Step",
                    Type = AutomationContract.StepType.Condition,
                    Parameters = "{\"condition\":\"true\"}"
                }
            };
            var newConfig = new AutomationContract.WorkflowConfig();

            // Act
            automationContract.UpdateWorkflow(workflowId, newSteps, newConfig);

            // Assert
            var updatedWorkflow = automationContract.GetWorkflow(workflowId);
            Assert.AreEqual(originalVersion + 1, updatedWorkflow.Version);
            Assert.IsTrue(updatedWorkflow.UpdatedAt > originalWorkflow.CreatedAt);
        }

        [TestMethod]
        public void TestGetCounts_AfterCreatingWorkflowsAndTasks_ReturnsCorrectCounts()
        {
            // Arrange
            var initialWorkflowCount = automationContract.GetWorkflowCount();
            var initialTaskCount = automationContract.GetTaskCount();
            var initialExecutionCount = automationContract.GetExecutionCount();

            // Act
            var workflowId1 = CreateTestWorkflow();
            var workflowId2 = CreateTestWorkflow();
            var taskId1 = CreateTestTask(workflowId1);
            var taskId2 = CreateTestTask(workflowId2);
            var executionId1 = automationContract.ExecuteTask(taskId1, "Test execution 1");
            var executionId2 = automationContract.ExecuteTask(taskId2, "Test execution 2");

            // Assert
            Assert.AreEqual(initialWorkflowCount + 2, automationContract.GetWorkflowCount());
            Assert.AreEqual(initialTaskCount + 2, automationContract.GetTaskCount());
            Assert.AreEqual(initialExecutionCount + 2, automationContract.GetExecutionCount());
        }

        [TestMethod]
        public void TestDifferentTriggerTypes_CreateTasks_Success()
        {
            // Arrange
            var workflowId = CreateTestWorkflow();

            // Act & Assert - Time trigger
            var timeTrigger = new AutomationContract.TaskTrigger
            {
                Type = AutomationContract.TriggerType.Time,
                ScheduledTime = (ulong)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600)
            };
            var timeTaskId = automationContract.ScheduleTask(workflowId, "Time Task", timeTrigger, new AutomationContract.TaskConfig());
            Assert.IsNotNull(timeTaskId);

            // Interval trigger
            var intervalTrigger = new AutomationContract.TaskTrigger
            {
                Type = AutomationContract.TriggerType.Interval,
                IntervalSeconds = 3600
            };
            var intervalTaskId = automationContract.ScheduleTask(workflowId, "Interval Task", intervalTrigger, new AutomationContract.TaskConfig());
            Assert.IsNotNull(intervalTaskId);

            // Event trigger
            var eventTrigger = new AutomationContract.TaskTrigger
            {
                Type = AutomationContract.TriggerType.Event,
                EventName = "test_event"
            };
            var eventTaskId = automationContract.ScheduleTask(workflowId, "Event Task", eventTrigger, new AutomationContract.TaskConfig());
            Assert.IsNotNull(eventTaskId);

            // Condition trigger
            var conditionTrigger = new AutomationContract.TaskTrigger
            {
                Type = AutomationContract.TriggerType.Condition,
                ConditionExpression = "balance > 1000"
            };
            var conditionTaskId = automationContract.ScheduleTask(workflowId, "Condition Task", conditionTrigger, new AutomationContract.TaskConfig());
            Assert.IsNotNull(conditionTaskId);
        }

        [TestMethod]
        public void TestDifferentStepTypes_ExecuteWorkflow_Success()
        {
            // Arrange
            var steps = new AutomationContract.WorkflowStep[]
            {
                new AutomationContract.WorkflowStep
                {
                    Name = "Contract Call Step",
                    Type = AutomationContract.StepType.ContractCall,
                    Parameters = "{\"contract\":\"0x1234\",\"method\":\"test\"}"
                },
                new AutomationContract.WorkflowStep
                {
                    Name = "Data Transfer Step",
                    Type = AutomationContract.StepType.DataTransfer,
                    Parameters = "{\"from\":\"source\",\"to\":\"destination\"}"
                },
                new AutomationContract.WorkflowStep
                {
                    Name = "Condition Step",
                    Type = AutomationContract.StepType.Condition,
                    Parameters = "{\"condition\":\"value > 100\"}"
                },
                new AutomationContract.WorkflowStep
                {
                    Name = "Delay Step",
                    Type = AutomationContract.StepType.Delay,
                    Parameters = "{\"seconds\":10}"
                },
                new AutomationContract.WorkflowStep
                {
                    Name = "Notification Step",
                    Type = AutomationContract.StepType.Notification,
                    Parameters = "{\"message\":\"Workflow completed\"}"
                }
            };

            var workflowId = automationContract.CreateWorkflow("Multi-Step Workflow", "Testing all step types", 
                steps, new AutomationContract.WorkflowConfig());
            var taskId = CreateTestTask(workflowId);

            // Act
            var executionId = automationContract.ExecuteTask(taskId, "Multi-step test");

            // Assert
            var execution = automationContract.GetTaskExecution(executionId);
            Assert.IsNotNull(execution);
            Assert.AreEqual(steps.Length, execution.StepResults.Length);
            Assert.IsTrue(execution.StepsExecuted > 0);
        }

        [TestMethod]
        public void TestWorkflowWithContinueOnFailure_PartialFailure_ContinuesExecution()
        {
            // Arrange
            var steps = new AutomationContract.WorkflowStep[]
            {
                new AutomationContract.WorkflowStep
                {
                    Name = "Success Step",
                    Type = AutomationContract.StepType.Notification,
                    Parameters = "{\"message\":\"Success\"}",
                    ContinueOnFailure = false
                },
                new AutomationContract.WorkflowStep
                {
                    Name = "Failing Step",
                    Type = AutomationContract.StepType.ContractCall,
                    Parameters = "{\"invalid\":\"parameters\"}",
                    ContinueOnFailure = true
                },
                new AutomationContract.WorkflowStep
                {
                    Name = "Final Step",
                    Type = AutomationContract.StepType.Notification,
                    Parameters = "{\"message\":\"Final\"}",
                    ContinueOnFailure = false
                }
            };

            var workflowId = automationContract.CreateWorkflow("Continue on Failure Test", "Testing failure handling", 
                steps, new AutomationContract.WorkflowConfig());
            var taskId = CreateTestTask(workflowId);

            // Act
            var executionId = automationContract.ExecuteTask(taskId, "Failure handling test");

            // Assert
            var execution = automationContract.GetTaskExecution(executionId);
            Assert.IsNotNull(execution);
            // Should execute all steps despite middle step failure
            Assert.IsTrue(execution.StepsExecuted >= 2);
        }

        // Helper methods
        private ByteString CreateTestWorkflow()
        {
            var steps = new AutomationContract.WorkflowStep[]
            {
                new AutomationContract.WorkflowStep
                {
                    Name = "Test Step",
                    Type = AutomationContract.StepType.Notification,
                    Parameters = "{\"message\":\"Test notification\"}"
                }
            };
            var config = new AutomationContract.WorkflowConfig
            {
                MaxExecutionTime = 300,
                EnableLogging = true
            };

            return automationContract.CreateWorkflow("Test Workflow", "Test Description", steps, config);
        }

        private ByteString CreateComplexTestWorkflow()
        {
            var steps = new AutomationContract.WorkflowStep[]
            {
                new AutomationContract.WorkflowStep
                {
                    Name = "Initialize",
                    Type = AutomationContract.StepType.ContractCall,
                    Parameters = "{\"action\":\"initialize\"}"
                },
                new AutomationContract.WorkflowStep
                {
                    Name = "Process Data",
                    Type = AutomationContract.StepType.DataTransfer,
                    Parameters = "{\"operation\":\"process\"}"
                },
                new AutomationContract.WorkflowStep
                {
                    Name = "Validate",
                    Type = AutomationContract.StepType.Condition,
                    Parameters = "{\"validation\":\"check_result\"}"
                },
                new AutomationContract.WorkflowStep
                {
                    Name = "Notify",
                    Type = AutomationContract.StepType.Notification,
                    Parameters = "{\"message\":\"Process completed\"}"
                }
            };
            var config = new AutomationContract.WorkflowConfig
            {
                MaxExecutionTime = 600,
                EnableLogging = true
            };

            return automationContract.CreateWorkflow("Complex Workflow", "Multi-step workflow", steps, config);
        }

        private ByteString CreateFailingWorkflow()
        {
            var steps = new AutomationContract.WorkflowStep[]
            {
                new AutomationContract.WorkflowStep
                {
                    Name = "Failing Step",
                    Type = AutomationContract.StepType.ContractCall,
                    Parameters = "{\"invalid\":\"call\"}",
                    ContinueOnFailure = false
                }
            };
            var config = new AutomationContract.WorkflowConfig();

            return automationContract.CreateWorkflow("Failing Workflow", "Workflow that fails", steps, config);
        }

        private ByteString CreateTestTask(ByteString workflowId)
        {
            var trigger = new AutomationContract.TaskTrigger
            {
                Type = AutomationContract.TriggerType.Manual
            };
            var config = new AutomationContract.TaskConfig
            {
                MaxRetries = 1,
                RetryDelay = 10,
                TimeoutSeconds = 60
            };

            return automationContract.ScheduleTask(workflowId, "Test Task", trigger, config);
        }
    }
}