using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;
using System.Numerics;

namespace NeoServiceLayerSample.SmartContract
{
    [DisplayName("NeoServiceLayerContract")]
    [ManifestExtra("Author", "Neo")]
    [ManifestExtra("Email", "dev@neo.org")]
    [ManifestExtra("Description", "Neo Service Layer Sample Contract")]
    public class NeoServiceLayerContract : SmartContract
    {
        // Events
        [DisplayName("ExecuteJavaScript")]
        public static event Action<string, string, string, string> OnExecuteJavaScript;
        
        [DisplayName("ExecutionResult")]
        public static event Action<string, string> OnExecutionResult;
        
        // Storage keys
        private static readonly byte[] RequestPrefix = new byte[] { 0x01 };
        private static readonly byte[] ResultPrefix = new byte[] { 0x02 };
        
        // Request JavaScript execution in the Neo Service Layer
        public static void ExecuteJavaScript(string functionId, string input, string userId)
        {
            // Get the caller's script hash
            UInt160 caller = Runtime.CallingScriptHash;
            
            // Ensure the caller is a valid address
            if (!caller.IsValid)
                throw new Exception("Invalid caller");
            
            // Create a unique request ID
            string requestId = $"{caller}_{Runtime.Time}";
            
            // Store the request
            StorageMap requests = new StorageMap(Storage.CurrentContext, RequestPrefix);
            requests.Put(requestId, input);
            
            // Emit an event to notify the Neo Service Layer
            // Parameters:
            // 1. functionId: The ID of the JavaScript function to execute
            // 2. input: The input data for the function
            // 3. userId: The ID of the user who owns the function
            // 4. requestId: The unique ID of this request
            OnExecuteJavaScript(functionId, input, userId, requestId);
            
            Runtime.Log($"JavaScript execution requested: {requestId}");
        }
        
        // Callback method for the Neo Service Layer to return the execution result
        public static void ReceiveExecutionResult(string requestId, string result)
        {
            // Ensure the caller is authorized (in a real implementation, this would check a whitelist)
            // For simplicity, we're allowing any caller in this example
            
            // Get the original request
            StorageMap requests = new StorageMap(Storage.CurrentContext, RequestPrefix);
            ByteString requestData = requests.Get(requestId);
            
            if (requestData is null)
                throw new Exception("Request not found");
            
            // Store the result
            StorageMap results = new StorageMap(Storage.CurrentContext, ResultPrefix);
            results.Put(requestId, result);
            
            // Emit an event to notify the requester
            OnExecutionResult(requestId, result);
            
            Runtime.Log($"JavaScript execution completed: {requestId}");
        }
        
        // Get the result of a JavaScript execution
        public static string GetExecutionResult(string requestId)
        {
            StorageMap results = new StorageMap(Storage.CurrentContext, ResultPrefix);
            ByteString result = results.Get(requestId);
            
            if (result is null)
                return "Pending";
            
            return result;
        }
        
        // Store a JavaScript function in the Neo Service Layer
        public static void StoreJavaScriptFunction(string functionId, string code)
        {
            // Get the caller's script hash
            UInt160 caller = Runtime.CallingScriptHash;
            
            // Ensure the caller is a valid address
            if (!caller.IsValid)
                throw new Exception("Invalid caller");
            
            // Emit an event to notify the Neo Service Layer
            // This is a custom event that the Neo Service Layer would need to listen for
            // In a real implementation, this would be handled by a separate event handler
            Runtime.Log($"JavaScript function stored: {functionId}");
        }
        
        // Example of a confidential token swap
        public static void RequestTokenSwap(string tokenA, string tokenB, BigInteger amountA, BigInteger amountB)
        {
            // Get the caller's script hash
            UInt160 caller = Runtime.CallingScriptHash;
            
            // Ensure the caller is a valid address
            if (!caller.IsValid)
                throw new Exception("Invalid caller");
            
            // Create the input data for the JavaScript function
            string input = $@"{{
                ""action"": ""addOrder"",
                ""tokenA"": ""{tokenA}"",
                ""tokenB"": ""{tokenB}"",
                ""amountA"": {amountA},
                ""amountB"": {amountB},
                ""sender"": ""{caller}""
            }}";
            
            // Request JavaScript execution
            ExecuteJavaScript("token-swap", input, caller.ToString());
        }
        
        // Example of a confidential voting
        public static void CastVote(string candidateId)
        {
            // Get the caller's script hash
            UInt160 caller = Runtime.CallingScriptHash;
            
            // Ensure the caller is a valid address
            if (!caller.IsValid)
                throw new Exception("Invalid caller");
            
            // Create the input data for the JavaScript function
            string input = $@"{{
                ""action"": ""castVote"",
                ""candidateId"": ""{candidateId}"",
                ""voter"": ""{caller}""
            }}";
            
            // Request JavaScript execution
            ExecuteJavaScript("confidential-voting", input, caller.ToString());
        }
        
        // Example of a provably fair random number generation
        public static void RequestRandomNumber(BigInteger min, BigInteger max)
        {
            // Get the caller's script hash
            UInt160 caller = Runtime.CallingScriptHash;
            
            // Ensure the caller is a valid address
            if (!caller.IsValid)
                throw new Exception("Invalid caller");
            
            // Create the input data for the JavaScript function
            string input = $@"{{
                ""action"": ""generateRandomNumber"",
                ""min"": {min},
                ""max"": {max},
                ""requester"": ""{caller}""
            }}";
            
            // Request JavaScript execution
            ExecuteJavaScript("random-number-generator", input, caller.ToString());
        }
    }
}
