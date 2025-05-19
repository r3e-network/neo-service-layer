using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Tee.Host.Testing
{
    /// <summary>
    /// Interface for running tests on enclaves.
    /// </summary>
    public interface IEnclaveTestRunner
    {
        /// <summary>
        /// Runs a test on an enclave.
        /// </summary>
        /// <param name="test">The test to run.</param>
        /// <returns>The test result.</returns>
        Task<EnclaveTestResult> RunTestAsync(EnclaveTest test);

        /// <summary>
        /// Runs multiple tests on enclaves.
        /// </summary>
        /// <param name="tests">The tests to run.</param>
        /// <returns>The test results.</returns>
        Task<IReadOnlyList<EnclaveTestResult>> RunTestsAsync(IEnumerable<EnclaveTest> tests);
    }
}
