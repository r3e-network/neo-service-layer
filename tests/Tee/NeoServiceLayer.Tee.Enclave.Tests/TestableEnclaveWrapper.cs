using System.Reflection;
using System.Runtime.InteropServices;

namespace NeoServiceLayer.Tee.Enclave.Tests;

/// <summary>
/// A testable version of the EnclaveWrapper class that doesn't call the native methods.
/// </summary>
public class TestableEnclaveWrapper : IEnclaveWrapper
{
    private bool _initialized;
    private bool _disposed;
    private readonly Dictionary<string, Func<object[], object>> _methodResults;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestableEnclaveWrapper"/> class.
    /// </summary>
    public TestableEnclaveWrapper()
    {
        _initialized = false;
        _disposed = false;
        _methodResults = new Dictionary<string, Func<object[], object>>();
    }

    /// <summary>
    /// Sets up a result for a method call.
    /// </summary>
    /// <param name="methodName">The name of the method.</param>
    /// <param name="result">The result to return.</param>
    public void SetupMethodResult(string methodName, object result)
    {
        _methodResults[methodName] = _ => result;
    }

    /// <summary>
    /// Sets up a result for a method call with parameters.
    /// </summary>
    /// <param name="methodName">The name of the method.</param>
    /// <param name="resultFunc">A function that takes the method parameters and returns a result.</param>
    public void SetupMethodResult(string methodName, Func<object[], object> resultFunc)
    {
        _methodResults[methodName] = resultFunc;
    }

    /// <summary>
    /// Gets the result for a method call.
    /// </summary>
    /// <param name="methodName">The name of the method.</param>
    /// <param name="parameters">The method parameters.</param>
    /// <returns>The result of the method call.</returns>
    private T GetMethodResult<T>(string methodName, params object[] parameters)
    {
        if (!_initialized && methodName != nameof(Initialize))
        {
            throw new EnclaveException("Enclave is not initialized. Call Initialize() first.");
        }

        if (_methodResults.TryGetValue(methodName, out var resultFunc))
        {
            return (T)resultFunc(parameters);
        }

        if (typeof(T) == typeof(string))
        {
            return (T)(object)"";
        }
        else if (typeof(T) == typeof(byte[]))
        {
            return (T)(object)Array.Empty<byte>();
        }
        else if (typeof(T) == typeof(bool))
        {
            return (T)(object)false;
        }
        else if (typeof(T) == typeof(int))
        {
            return (T)(object)0;
        }

        return default!;
    }

    /// <summary>
    /// Initializes the enclave.
    /// </summary>
    /// <returns>True if the enclave was initialized successfully, false otherwise.</returns>
    public bool Initialize()
    {
        bool result = GetMethodResult<bool>(nameof(Initialize));
        _initialized = result;
        return result;
    }

    /// <summary>
    /// Executes a JavaScript function in the enclave.
    /// </summary>
    /// <param name="functionCode">The JavaScript function code to execute.</param>
    /// <param name="args">The arguments to pass to the function.</param>
    /// <returns>The result of the function execution.</returns>
    public string ExecuteJavaScript(string functionCode, string args)
    {
        return GetMethodResult<string>(nameof(ExecuteJavaScript), functionCode, args);
    }

    /// <summary>
    /// Gets data from an external source in the enclave.
    /// </summary>
    /// <param name="dataSource">The data source URL.</param>
    /// <param name="dataPath">The path to the data within the source.</param>
    /// <returns>The data from the external source.</returns>
    public string GetData(string dataSource, string dataPath)
    {
        return GetMethodResult<string>(nameof(GetData), dataSource, dataPath);
    }

    /// <summary>
    /// Generates a random number in the enclave.
    /// </summary>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>A random number between min and max (inclusive).</returns>
    public int GenerateRandom(int min, int max)
    {
        return GetMethodResult<int>(nameof(GenerateRandom), min, max);
    }

    /// <summary>
    /// Encrypts data in the enclave.
    /// </summary>
    /// <param name="data">The data to encrypt.</param>
    /// <param name="key">The encryption key.</param>
    /// <returns>The encrypted data.</returns>
    public byte[] Encrypt(byte[] data, byte[] key)
    {
        return GetMethodResult<byte[]>(nameof(Encrypt), data, key);
    }

    /// <summary>
    /// Decrypts data in the enclave.
    /// </summary>
    /// <param name="data">The data to decrypt.</param>
    /// <param name="key">The decryption key.</param>
    /// <returns>The decrypted data.</returns>
    public byte[] Decrypt(byte[] data, byte[] key)
    {
        return GetMethodResult<byte[]>(nameof(Decrypt), data, key);
    }

    /// <summary>
    /// Signs data in the enclave.
    /// </summary>
    /// <param name="data">The data to sign.</param>
    /// <param name="key">The signing key.</param>
    /// <returns>The signature.</returns>
    public byte[] Sign(byte[] data, byte[] key)
    {
        return GetMethodResult<byte[]>(nameof(Sign), data, key);
    }

    /// <summary>
    /// Verifies a signature in the enclave.
    /// </summary>
    /// <param name="data">The data to verify.</param>
    /// <param name="signature">The signature to verify.</param>
    /// <param name="key">The verification key.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    public bool Verify(byte[] data, byte[] signature, byte[] key)
    {
        return GetMethodResult<bool>(nameof(Verify), data, signature, key);
    }

    /// <summary>
    /// Disposes the enclave wrapper.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_initialized)
        {
            _initialized = false;
        }

        _disposed = true;
    }
}
