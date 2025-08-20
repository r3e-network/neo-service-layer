using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Integration.Tests.Transactions
{
    #region Distributed Transaction Models

    public class DistributedTransactionScenario
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Services { get; set; } = new();
        public List<OperationDefinition> Operations { get; set; } = new();
        public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public List<ConsistencyRule> ConsistencyRules { get; set; } = new();
    }

    public class DistributedTransactionTestResult
    {
        public string TransactionId { get; set; } = string.Empty;
        public string ScenarioName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public List<string> ParticipatingServices { get; set; } = new();
        public List<TransactionOperation> Operations { get; set; } = new();
        public bool PreparePhaseSuccess { get; set; }
        public bool AllOperationsSucceeded { get; set; }
        public string? FailedOperation { get; set; }
        public bool CommitSuccess { get; set; }
        public bool RollbackSuccess { get; set; }
        public ConsistencyCheckResult ConsistencyCheck { get; set; } = new();
        public TimeSpan AverageOperationTime { get; set; }
        public bool Success { get; set; }
        public string? FailureReason { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class TransactionOperation
    {
        public string Name { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public object? Result { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class OperationDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public TimeSpan? EstimatedDuration { get; set; }
    }

    public class TransactionContext
    {
        public string TransactionId { get; set; } = string.Empty;
        public IsolationLevel IsolationLevel { get; set; }
        public TimeSpan Timeout { get; set; }
        public List<string> Services { get; set; } = new();
        public DateTime StartTime { get; set; }
        public TransactionState State { get; set; }
    }

    public enum TransactionState
    {
        Active,
        Preparing,
        Prepared,
        Committing,
        Committed,
        Aborting,
        Aborted
    }

    public class PrepareResult
    {
        public string Service { get; set; } = string.Empty;
        public bool Success { get; set; }
        public DateTime PreparedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    #endregion

    #region SAGA Pattern Models

    public class SagaScenario
    {
        public string Name { get; set; } = string.Empty;
        public List<SagaStep> Steps { get; set; } = new();
        public CompensationStrategy CompensationStrategy { get; set; } = CompensationStrategy.Sequential;
        public bool TestIdempotency { get; set; }
        public bool TestEventualConsistency { get; set; }
    }

    public class SagaTestResult
    {
        public string SagaId { get; set; } = string.Empty;
        public string ScenarioName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public List<SagaStepResult> Steps { get; set; } = new();
        public bool AllStepsCompleted { get; set; }
        public string? FailedStep { get; set; }
        public bool CompensationTriggered { get; set; }
        public bool CompensationSuccess { get; set; }
        public List<CompensationStepResult> CompensationSteps { get; set; } = new();
        public IdempotencyTestResult? IdempotencyTest { get; set; }
        public EventualConsistencyTestResult? EventualConsistencyTest { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class SagaStep
    {
        public string Name { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public CompensationDefinition? Compensation { get; set; }
    }

    public class SagaStepResult
    {
        public string StepName { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public object? Output { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class CompensationDefinition
    {
        public string Operation { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public enum CompensationStrategy
    {
        Sequential,
        Parallel,
        Selective
    }

    public class SagaOrchestrator
    {
        public string SagaId { get; set; } = string.Empty;
        public List<SagaStep> Steps { get; set; } = new();
        public CompensationStrategy CompensationStrategy { get; set; }
        public int CurrentStep { get; set; }
        public SagaState State { get; set; }
    }

    public enum SagaState
    {
        Running,
        Compensating,
        Completed,
        Failed,
        Compensated
    }

    public class CompensationResult
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<CompensationStepResult> Steps { get; set; } = new();
        public bool Success { get; set; }
    }

    public class CompensationStepResult
    {
        public string OriginalStep { get; set; } = string.Empty;
        public bool Success { get; set; }
        public DateTime CompensatedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    #endregion

    #region Two-Phase Commit Models

    public class TwoPhaseCommitScenario
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Participants { get; set; } = new();
        public bool TestCoordinatorFailure { get; set; }
        public bool TestParticipantFailure { get; set; }
        public string? FailureParticipant { get; set; }
    }

    public class TwoPhaseCommitTestResult
    {
        public string TransactionId { get; set; } = string.Empty;
        public string ScenarioName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public List<string> Participants { get; set; } = new();
        public List<ParticipantVote> VoteResults { get; set; } = new();
        public bool AllVotedCommit { get; set; }
        public bool VotingPhaseAborted { get; set; }
        public string? AbortingParticipant { get; set; }
        public bool CommitPhaseStarted { get; set; }
        public bool AllCommitted { get; set; }
        public bool PartialCommitFailure { get; set; }
        public bool AbortSent { get; set; }
        public CoordinatorFailureTestResult? CoordinatorFailureTest { get; set; }
        public ParticipantFailureTestResult? ParticipantFailureTest { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class ParticipantVote
    {
        public string Participant { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public VoteType Vote { get; set; }
        public DateTime VotedAt { get; set; }
    }

    public enum VoteType
    {
        Commit,
        Abort
    }

    public class CoordinatorFailureTestResult
    {
        public bool RecoverySuccessful { get; set; }
        public TimeSpan RecoveryTime { get; set; }
        public bool DataConsistent { get; set; }
    }

    public class ParticipantFailureTestResult
    {
        public bool ParticipantRecovered { get; set; }
        public TimeSpan RecoveryTime { get; set; }
        public bool TransactionCompleted { get; set; }
    }

    #endregion

    #region Isolation Level Testing Models

    public class IsolationLevelScenario
    {
        public string Name { get; set; } = string.Empty;
        public IsolationLevel IsolationLevel { get; set; }
        public bool TestDirtyReads { get; set; }
        public bool TestNonRepeatableReads { get; set; }
        public bool TestPhantomReads { get; set; }
        public bool TestDeadlocks { get; set; }
    }

    public class IsolationLevelTestResult
    {
        public string ScenarioName { get; set; } = string.Empty;
        public IsolationLevel IsolationLevel { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public IsolationTestResult? DirtyReadTest { get; set; }
        public IsolationTestResult? NonRepeatableReadTest { get; set; }
        public IsolationTestResult? PhantomReadTest { get; set; }
        public DeadlockTestResult? DeadlockTest { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class IsolationTestResult
    {
        public string TestName { get; set; } = string.Empty;
        public bool Prevented { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class DeadlockTestResult
    {
        public bool DeadlockDetected { get; set; }
        public TimeSpan DetectionTime { get; set; }
        public string ResolutionStrategy { get; set; } = string.Empty;
    }

    #endregion

    #region Compensation Testing Models

    public class CompensationScenario
    {
        public string Name { get; set; } = string.Empty;
        public bool TriggerCompensation { get; set; }
        public List<CompensationActionDefinition> CompensationActions { get; set; } = new();
        public object? ExpectedState { get; set; }
        public bool TestIdempotency { get; set; }
    }

    public class CompensationTestResult
    {
        public string ScenarioName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool TransactionExecuted { get; set; }
        public List<CompensationAction> CompensationActions { get; set; } = new();
        public bool AllCompensationsSucceeded { get; set; }
        public bool StateRestored { get; set; }
        public bool IdempotencyVerified { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class CompensationActionDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class CompensationAction
    {
        public string Name { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public bool Success { get; set; }
        public DateTime ExecutedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    #endregion

    #region Consistency Models

    public class ConsistencyRule
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public List<string> Services { get; set; } = new();
        public Dictionary<string, object> Criteria { get; set; } = new();
    }

    public class ConsistencyCheckResult
    {
        public DateTime CheckedAt { get; set; }
        public bool IsConsistent { get; set; }
        public List<ConsistencyRuleResult> RuleResults { get; set; } = new();
    }

    public class ConsistencyRuleResult
    {
        public string RuleName { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> Details { get; set; } = new();
    }

    public class IdempotencyTestResult
    {
        public bool IsIdempotent { get; set; }
        public bool TestPassed { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class EventualConsistencyTestResult
    {
        public bool IsEventuallyConsistent { get; set; }
        public TimeSpan ConsistencyWindow { get; set; }
        public bool TestPassed { get; set; }
    }

    public class TransactionResult
    {
        public string TransactionId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    #endregion
}
