using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Tee.Host.Testing
{
    /// <summary>
    /// Represents a test to run on an enclave.
    /// </summary>
    public class EnclaveTest
    {
        /// <summary>
        /// Gets or sets the name of the test.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the test.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the JavaScript code to execute.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the input data for the JavaScript code.
        /// </summary>
        public string Input { get; set; }

        /// <summary>
        /// Gets or sets the secrets for the JavaScript code.
        /// </summary>
        public string Secrets { get; set; }

        /// <summary>
        /// Gets or sets the ID of the function to execute.
        /// </summary>
        public string FunctionId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user executing the function.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the path to the enclave file.
        /// </summary>
        public string EnclavePath { get; set; }

        /// <summary>
        /// Gets or sets whether to create the enclave in simulation mode.
        /// </summary>
        public bool? SimulationMode { get; set; }

        /// <summary>
        /// Gets or sets the assertions to check after executing the JavaScript code.
        /// </summary>
        public List<EnclaveTestAssertion> Assertions { get; set; }

        /// <summary>
        /// Gets or sets the timeout for the test in milliseconds.
        /// </summary>
        public int? TimeoutMs { get; set; }

        /// <summary>
        /// Gets or sets the tags for the test.
        /// </summary>
        public List<string> Tags { get; set; }

        /// <summary>
        /// Gets or sets the dependencies for the test.
        /// </summary>
        public List<string> Dependencies { get; set; }

        /// <summary>
        /// Gets or sets whether to skip the test.
        /// </summary>
        public bool Skip { get; set; }

        /// <summary>
        /// Gets or sets the reason for skipping the test.
        /// </summary>
        public string SkipReason { get; set; }
    }

    /// <summary>
    /// Represents an assertion to check after executing JavaScript code.
    /// </summary>
    public class EnclaveTestAssertion
    {
        /// <summary>
        /// Gets or sets the name of the assertion.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the assertion.
        /// </summary>
        public AssertionType Type { get; set; }

        /// <summary>
        /// Gets or sets the expected value for the assertion.
        /// </summary>
        public string ExpectedValue { get; set; }

        /// <summary>
        /// Gets or sets the custom code for the assertion.
        /// </summary>
        public string CustomCode { get; set; }
    }

    /// <summary>
    /// Represents the result of running a test on an enclave.
    /// </summary>
    public class EnclaveTestResult
    {
        /// <summary>
        /// Gets or sets the name of the test.
        /// </summary>
        public string TestName { get; set; }

        /// <summary>
        /// Gets or sets whether the test was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the output of the JavaScript execution.
        /// </summary>
        public string Output { get; set; }

        /// <summary>
        /// Gets or sets the error message if the JavaScript execution failed.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the gas used by the JavaScript execution.
        /// </summary>
        public long GasUsed { get; set; }

        /// <summary>
        /// Gets or sets the start time of the test.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time of the test.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the total time of the test in milliseconds.
        /// </summary>
        public long TotalTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the results of the assertions.
        /// </summary>
        public List<AssertionResult> AssertionResults { get; set; }

        /// <summary>
        /// Gets or sets additional metadata about the test.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; }
    }

    /// <summary>
    /// Represents the result of an assertion.
    /// </summary>
    public class AssertionResult
    {
        /// <summary>
        /// Gets or sets the name of the assertion.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets whether the assertion was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the message for the assertion result.
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// Represents the type of an assertion.
    /// </summary>
    public enum AssertionType
    {
        /// <summary>
        /// Asserts that the output contains the expected value.
        /// </summary>
        OutputContains,

        /// <summary>
        /// Asserts that the output equals the expected value.
        /// </summary>
        OutputEquals,

        /// <summary>
        /// Asserts that the output starts with the expected value.
        /// </summary>
        OutputStartsWith,

        /// <summary>
        /// Asserts that the output ends with the expected value.
        /// </summary>
        OutputEndsWith,

        /// <summary>
        /// Asserts that the output matches the expected regular expression.
        /// </summary>
        OutputMatches,

        /// <summary>
        /// Asserts that the error contains the expected value.
        /// </summary>
        ErrorContains,

        /// <summary>
        /// Asserts that the error equals the expected value.
        /// </summary>
        ErrorEquals,

        /// <summary>
        /// Asserts that the success flag equals the expected value.
        /// </summary>
        Success,

        /// <summary>
        /// Asserts that the execution time is less than or equal to the expected value.
        /// </summary>
        ExecutionTimeMs,

        /// <summary>
        /// Asserts that the gas used is less than or equal to the expected value.
        /// </summary>
        GasUsed,

        /// <summary>
        /// Asserts using custom code.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Options for the enclave test runner.
    /// </summary>
    public class EnclaveTestOptions
    {
        /// <summary>
        /// Gets or sets the default path to the enclave file.
        /// </summary>
        public string DefaultEnclavePath { get; set; }

        /// <summary>
        /// Gets or sets whether to create enclaves in simulation mode by default.
        /// </summary>
        public bool DefaultSimulationMode { get; set; } = true;

        /// <summary>
        /// Gets or sets the default timeout for tests in milliseconds.
        /// </summary>
        public int DefaultTimeoutMs { get; set; } = 30000;

        /// <summary>
        /// Gets or sets whether to continue running tests after a failure.
        /// </summary>
        public bool ContinueOnFailure { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to run tests in parallel.
        /// </summary>
        public bool ParallelExecution { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum number of parallel test executions.
        /// </summary>
        public int MaxParallelExecutions { get; set; } = 4;

        /// <summary>
        /// Gets or sets whether to log detailed information about test execution.
        /// </summary>
        public bool VerboseLogging { get; set; } = false;
    }
}
