using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Shared.Models.Rpc;

namespace NeoServiceLayer.Tee.Host.Rpc
{
    /// <summary>
    /// Interface for a secure remote procedure call (RPC) system.
    /// </summary>
    public interface ISecureRpcSystem : IDisposable
    {
        /// <summary>
        /// Calls a remote procedure.
        /// </summary>
        /// <param name="method">The name of the method to call.</param>
        /// <param name="parameters">The parameters for the method.</param>
        /// <param name="timeout">The timeout for the call in milliseconds.</param>
        /// <returns>The response from the remote procedure.</returns>
        Task<RpcResponse> CallAsync(string method, object parameters, int? timeout = null);

        /// <summary>
        /// Registers a method handler.
        /// </summary>
        /// <param name="method">The name of the method to register.</param>
        /// <param name="handler">The handler for the method.</param>
        void RegisterMethod(string method, Func<RpcRequest, Task<RpcResponse>> handler);

        /// <summary>
        /// Unregisters a method handler.
        /// </summary>
        /// <param name="method">The name of the method to unregister.</param>
        /// <returns>True if the method was unregistered, false otherwise.</returns>
        bool UnregisterMethod(string method);

        /// <summary>
        /// Gets all registered methods.
        /// </summary>
        /// <returns>A list of registered methods.</returns>
        IReadOnlyList<string> GetRegisteredMethods();
    }
}
