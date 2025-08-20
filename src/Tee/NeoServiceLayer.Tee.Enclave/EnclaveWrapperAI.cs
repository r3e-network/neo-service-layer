using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Tee.Enclave;

/// <summary>
/// DEPRECATED: AI and account operations for the legacy enclave wrapper.
/// This partial class is deprecated. Use OcclumEnclaveWrapper instead.
/// </summary>
[Obsolete("This class is deprecated. Use OcclumEnclaveWrapper instead.")]
public partial class EnclaveWrapper
{
    /// <summary>
    /// Trains an AI model in the enclave.
    /// </summary>
    /// <param name="modelId">The unique identifier for the model.</param>
    /// <param name="modelType">The type of model (e.g., "LinearRegression", "NeuralNetwork").</param>
    /// <param name="trainingData">The training data as a flat array of doubles.</param>
    /// <param name="parameters">JSON string containing training parameters.</param>
    /// <returns>JSON string containing the training result and model metadata.</returns>
    public string TrainModel(string modelId, string modelType, double[] trainingData, string parameters)
    {
        EnsureInitialized();

        byte[] modelIdBytes = Encoding.UTF8.GetBytes(modelId);
        byte[] modelTypeBytes = Encoding.UTF8.GetBytes(modelType);
        byte[] parametersBytes = Encoding.UTF8.GetBytes(parameters);
        byte[] resultBytes = new byte[8192]; // 8KB buffer for result
        IntPtr actualResultSizePtr = Marshal.AllocHGlobal(IntPtr.Size);

        try
        {
            int result = NativeOcclumEnclave.occlum_ai_train_model(
                modelIdBytes,
                modelTypeBytes,
                trainingData,
                (UIntPtr)trainingData.Length,
                parametersBytes,
                resultBytes,
                (UIntPtr)resultBytes.Length,
                actualResultSizePtr);

            if (result != 0)
            {
                throw new EnclaveException($"Failed to train model '{modelId}'. Error code: {result}");
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
    /// Makes a prediction using a trained AI model in the enclave.
    /// </summary>
    /// <param name="modelId">The identifier of the trained model.</param>
    /// <param name="inputData">The input data for prediction.</param>
    /// <returns>Tuple containing the prediction output and metadata.</returns>
    public (double[] output, string metadata) Predict(string modelId, double[] inputData)
    {
        EnsureInitialized();

        byte[] modelIdBytes = Encoding.UTF8.GetBytes(modelId);
        double[] outputData = new double[1024]; // Buffer for output
        byte[] metadataBytes = new byte[4096]; // Buffer for metadata
        IntPtr actualOutputSizePtr = Marshal.AllocHGlobal(IntPtr.Size);
        IntPtr actualMetadataSizePtr = Marshal.AllocHGlobal(IntPtr.Size);

        try
        {
            int result = NativeOcclumEnclave.occlum_ai_predict(
                modelIdBytes,
                inputData,
                (UIntPtr)inputData.Length,
                outputData,
                (UIntPtr)outputData.Length,
                actualOutputSizePtr,
                metadataBytes,
                (UIntPtr)metadataBytes.Length,
                actualMetadataSizePtr);

            if (result != 0)
            {
                throw new EnclaveException($"Failed to make prediction with model '{modelId}'. Error code: {result}");
            }

            int actualOutputSize = Marshal.ReadInt32(actualOutputSizePtr);
            int actualMetadataSize = Marshal.ReadInt32(actualMetadataSizePtr);

            double[] actualOutput = new double[actualOutputSize];
            Array.Copy(outputData, actualOutput, actualOutputSize);

            string metadata = Encoding.UTF8.GetString(metadataBytes, 0, actualMetadataSize);

            return (actualOutput, metadata);
        }
        finally
        {
            Marshal.FreeHGlobal(actualOutputSizePtr);
            Marshal.FreeHGlobal(actualMetadataSizePtr);
        }
    }

    /// <summary>
    /// Creates an abstract account in the enclave.
    /// </summary>
    /// <param name="accountId">The unique identifier for the account.</param>
    /// <param name="accountData">JSON string containing account configuration.</param>
    /// <returns>JSON string containing the account creation result.</returns>
    public string CreateAccount(string accountId, string accountData)
    {
        EnsureInitialized();

        byte[] accountIdBytes = Encoding.UTF8.GetBytes(accountId);
        byte[] accountDataBytes = Encoding.UTF8.GetBytes(accountData);
        byte[] resultBytes = new byte[4096]; // 4KB buffer for result
        IntPtr actualResultSizePtr = Marshal.AllocHGlobal(IntPtr.Size);

        try
        {
            int result = NativeOcclumEnclave.occlum_account_create(
                accountIdBytes,
                accountDataBytes,
                resultBytes,
                (UIntPtr)resultBytes.Length,
                actualResultSizePtr);

            if (result != 0)
            {
                throw new EnclaveException($"Failed to create account '{accountId}'. Error code: {result}");
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
    /// Signs a transaction using an abstract account in the enclave.
    /// </summary>
    /// <param name="accountId">The identifier of the account.</param>
    /// <param name="transactionData">The transaction data to sign.</param>
    /// <returns>JSON string containing the signed transaction.</returns>
    public string SignTransaction(string accountId, string transactionData)
    {
        EnsureInitialized();

        byte[] accountIdBytes = Encoding.UTF8.GetBytes(accountId);
        byte[] transactionDataBytes = Encoding.UTF8.GetBytes(transactionData);
        byte[] resultBytes = new byte[8192]; // 8KB buffer for result
        IntPtr actualResultSizePtr = Marshal.AllocHGlobal(IntPtr.Size);

        try
        {
            int result = NativeOcclumEnclave.occlum_account_sign_transaction(
                accountIdBytes,
                transactionDataBytes,
                resultBytes,
                (UIntPtr)resultBytes.Length,
                actualResultSizePtr);

            if (result != 0)
            {
                throw new EnclaveException($"Failed to sign transaction for account '{accountId}'. Error code: {result}");
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
    /// Adds a guardian to an abstract account in the enclave.
    /// </summary>
    /// <param name="accountId">The identifier of the account.</param>
    /// <param name="guardianData">JSON string containing guardian information.</param>
    /// <returns>JSON string containing the result of adding the guardian.</returns>
    public string AddGuardian(string accountId, string guardianData)
    {
        EnsureInitialized();

        byte[] accountIdBytes = Encoding.UTF8.GetBytes(accountId);
        byte[] guardianDataBytes = Encoding.UTF8.GetBytes(guardianData);
        byte[] resultBytes = new byte[4096]; // 4KB buffer for result
        IntPtr actualResultSizePtr = Marshal.AllocHGlobal(IntPtr.Size);

        try
        {
            int result = NativeOcclumEnclave.occlum_account_add_guardian(
                accountIdBytes,
                guardianDataBytes,
                resultBytes,
                (UIntPtr)resultBytes.Length,
                actualResultSizePtr);

            if (result != 0)
            {
                throw new EnclaveException($"Failed to add guardian to account '{accountId}'. Error code: {result}");
            }

            int actualResultSize = Marshal.ReadInt32(actualResultSizePtr);
            return Encoding.UTF8.GetString(resultBytes, 0, actualResultSize);
        }
        finally
        {
            Marshal.FreeHGlobal(actualResultSizePtr);
        }
    }

    // Interface compatibility methods - delegate to internal implementations
    public string TrainAIModel(string modelId, string modelType, double[] trainingData, string parameters = "{}")
    {
        return TrainModel(modelId, modelType, trainingData, parameters);
    }

    public (double[] predictions, string metadata) PredictWithAIModel(string modelId, double[] inputData)
    {
        return Predict(modelId, inputData);
    }

    public string CreateAbstractAccount(string accountId, string accountData)
    {
        return CreateAccount(accountId, accountData);
    }

    public string SignAbstractAccountTransaction(string accountId, string transactionData)
    {
        return SignTransaction(accountId, transactionData);
    }

    public string AddAbstractAccountGuardian(string accountId, string guardianData)
    {
        return AddGuardian(accountId, guardianData);
    }
}
