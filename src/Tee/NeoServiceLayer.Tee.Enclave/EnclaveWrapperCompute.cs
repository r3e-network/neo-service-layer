using System.Runtime.InteropServices;
using System.Text;

namespace NeoServiceLayer.Tee.Enclave;

/// <summary>
/// JavaScript and compute operations for the enclave wrapper.
/// </summary>
public partial class EnclaveWrapper
{
    /// <summary>
    /// Executes a JavaScript function in the enclave.
    /// </summary>
    /// <param name="functionCode">The JavaScript function code to execute.</param>
    /// <param name="args">The arguments to pass to the function.</param>
    /// <returns>The result of the function execution.</returns>
    public string ExecuteJavaScript(string functionCode, string args)
    {
        EnsureInitialized();

        byte[] functionCodeBytes = Encoding.UTF8.GetBytes(functionCode);
        byte[] argsBytes = Encoding.UTF8.GetBytes(args);
        byte[] resultBytes = new byte[4096]; // Adjust buffer size as needed
        IntPtr resultSizePtr = Marshal.AllocHGlobal(IntPtr.Size);

        try
        {
            int result = NativeOcclumEnclave.occlum_execute_js(
                functionCodeBytes, (UIntPtr)functionCodeBytes.Length,
                argsBytes, (UIntPtr)argsBytes.Length,
                resultBytes, (UIntPtr)resultBytes.Length,
                resultSizePtr);

            if (result != 0)
            {
                throw new EnclaveException($"Failed to execute JavaScript function. Error code: {result}");
            }

            int resultSize = Marshal.ReadInt32(resultSizePtr);
            return Encoding.UTF8.GetString(resultBytes, 0, resultSize);
        }
        finally
        {
            Marshal.FreeHGlobal(resultSizePtr);
        }
    }

    /// <summary>
    /// Gets data from an external source in the enclave.
    /// </summary>
    /// <param name="dataSource">The data source URL.</param>
    /// <param name="dataPath">The path to the data within the source.</param>
    /// <returns>The data from the external source.</returns>
    public string GetData(string dataSource, string dataPath)
    {
        EnsureInitialized();

        byte[] dataSourceBytes = Encoding.UTF8.GetBytes(dataSource);
        byte[] dataPathBytes = Encoding.UTF8.GetBytes(dataPath);
        byte[] resultBytes = new byte[4096]; // Adjust buffer size as needed
        IntPtr resultSizePtr = Marshal.AllocHGlobal(IntPtr.Size);

        try
        {
            int result = NativeOcclumEnclave.occlum_get_data(
                dataSourceBytes, (UIntPtr)dataSourceBytes.Length,
                dataPathBytes, (UIntPtr)dataPathBytes.Length,
                resultBytes, (UIntPtr)resultBytes.Length,
                resultSizePtr);

            if (result != 0)
            {
                throw new EnclaveException($"Failed to get data. Error code: {result}");
            }

            int resultSize = Marshal.ReadInt32(resultSizePtr);
            return Encoding.UTF8.GetString(resultBytes, 0, resultSize);
        }
        finally
        {
            Marshal.FreeHGlobal(resultSizePtr);
        }
    }

    /// <summary>
    /// Executes a computation in the enclave with enhanced environment and error handling.
    /// </summary>
    /// <param name="computationId">The unique identifier for the computation.</param>
    /// <param name="computationCode">The JavaScript code to execute.</param>
    /// <param name="parameters">JSON string containing computation parameters.</param>
    /// <returns>JSON string containing the computation result and metadata.</returns>
    public string ExecuteComputation(string computationId, string computationCode, string parameters)
    {
        EnsureInitialized();

        byte[] idBytes = Encoding.UTF8.GetBytes(computationId);
        byte[] codeBytes = Encoding.UTF8.GetBytes(computationCode);
        byte[] paramBytes = Encoding.UTF8.GetBytes(parameters);
        byte[] resultBytes = new byte[8192]; // 8KB buffer for result
        IntPtr actualResultSizePtr = Marshal.AllocHGlobal(IntPtr.Size);

        try
        {
            int result = NativeOcclumEnclave.occlum_compute_execute(
                idBytes,
                codeBytes,
                paramBytes,
                resultBytes,
                (UIntPtr)resultBytes.Length,
                actualResultSizePtr);

            if (result != 0)
            {
                throw new EnclaveException($"Failed to execute computation '{computationId}'. Error code: {result}");
            }

            int actualResultSize = Marshal.ReadInt32(actualResultSizePtr);
            return Encoding.UTF8.GetString(resultBytes, 0, actualResultSize);
        }
        finally
        {
            Marshal.FreeHGlobal(actualResultSizePtr);
        }
    }

    /// <summary>
    /// Fetches data from an external URL using the Oracle service in the enclave.
    /// </summary>
    /// <param name="url">The URL to fetch data from.</param>
    /// <param name="headers">Optional HTTP headers.</param>
    /// <param name="processingScript">Optional JavaScript for data processing.</param>
    /// <param name="outputFormat">Desired output format (e.g., "json", "raw").</param>
    /// <returns>JSON string containing the fetched data and metadata.</returns>
    public string FetchOracleData(string url, string? headers = null, string? processingScript = null, string? outputFormat = "json")
    {
        EnsureInitialized();

        byte[] urlBytes = Encoding.UTF8.GetBytes(url);
        byte[] headersBytes = string.IsNullOrEmpty(headers) ? new byte[1] : Encoding.UTF8.GetBytes(headers);
        byte[] scriptBytes = string.IsNullOrEmpty(processingScript) ? new byte[1] : Encoding.UTF8.GetBytes(processingScript);
        byte[] formatBytes = string.IsNullOrEmpty(outputFormat) ? Encoding.UTF8.GetBytes("json") : Encoding.UTF8.GetBytes(outputFormat);
        byte[] resultBytes = new byte[16384]; // 16KB buffer for result
        IntPtr actualResultSizePtr = Marshal.AllocHGlobal(IntPtr.Size);

        try
        {
            int result = NativeOcclumEnclave.occlum_oracle_fetch_data(
                urlBytes,
                headersBytes,
                scriptBytes,
                formatBytes,
                resultBytes,
                (UIntPtr)resultBytes.Length,
                actualResultSizePtr);

            if (result != 0)
            {
                throw new EnclaveException($"Failed to fetch Oracle data from {url}. Error code: {result}");
            }

            int actualResultSize = Marshal.ReadInt32(actualResultSizePtr);
            return Encoding.UTF8.GetString(resultBytes, 0, actualResultSize);
        }
        finally
        {
            Marshal.FreeHGlobal(actualResultSizePtr);
        }
    }
}
