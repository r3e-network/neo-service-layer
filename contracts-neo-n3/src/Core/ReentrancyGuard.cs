using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;

namespace NeoServiceLayer.Contracts.Core
{
    /// <summary>
    /// Provides protection against reentrancy attacks for Neo N3 smart contracts.
    /// This abstract contract should be inherited by contracts that make external calls.
    /// </summary>
    public abstract class ReentrancyGuard : SmartContract
    {
        // Storage key prefix for reentrancy status
        private const byte REENTRANCY_PREFIX = 0x20;
        
        // Reentrancy status constants
        private const byte NOT_ENTERED = 0;
        private const byte ENTERED = 1;

        /// <summary>
        /// Gets the current reentrancy status from storage
        /// </summary>
        protected static bool IsReentrant()
        {
            var key = new byte[] { REENTRANCY_PREFIX };
            var status = Storage.Get(Storage.CurrentContext, key);
            return status != null && status.Length > 0 && status[0] == ENTERED;
        }

        /// <summary>
        /// Sets the reentrancy status in storage
        /// </summary>
        protected static void SetReentrancyStatus(byte status)
        {
            var key = new byte[] { REENTRANCY_PREFIX };
            Storage.Put(Storage.CurrentContext, key, new byte[] { status });
        }

        /// <summary>
        /// Modifier to prevent reentrancy. Call this at the beginning of functions that make external calls.
        /// </summary>
        protected static void NonReentrantBegin()
        {
            if (IsReentrant())
            {
                throw new InvalidOperationException("ReentrancyGuard: reentrant call detected");
            }
            SetReentrancyStatus(ENTERED);
        }

        /// <summary>
        /// Call this at the end of functions that make external calls to reset the reentrancy guard.
        /// </summary>
        protected static void NonReentrantEnd()
        {
            SetReentrancyStatus(NOT_ENTERED);
        }

        /// <summary>
        /// Wrapper method for executing non-reentrant operations
        /// </summary>
        protected static T ExecuteNonReentrant<T>(Func<T> operation)
        {
            NonReentrantBegin();
            try
            {
                return operation();
            }
            finally
            {
                NonReentrantEnd();
            }
        }

        /// <summary>
        /// Wrapper method for executing non-reentrant operations without return value
        /// </summary>
        protected static void ExecuteNonReentrant(Action operation)
        {
            NonReentrantBegin();
            try
            {
                operation();
            }
            finally
            {
                NonReentrantEnd();
            }
        }
    }
}