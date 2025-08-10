using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.Numerics;

namespace NeoServiceLayer.Contracts.Core
{
    /// <summary>
    /// Provides comprehensive input validation for smart contracts
    /// Helps prevent common attack vectors through proper parameter validation
    /// </summary>
    public static class InputValidation
    {
        #region Address Validation
        
        /// <summary>
        /// Validates that an address is not null or zero
        /// </summary>
        public static void ValidateAddress(UInt160 address, string parameterName)
        {
            if (address == null || address.IsZero)
                throw new ArgumentException($"Invalid {parameterName}: address cannot be null or zero");
        }

        /// <summary>
        /// Validates multiple addresses at once
        /// </summary>
        public static void ValidateAddresses(params (UInt160 address, string name)[] addresses)
        {
            foreach (var (address, name) in addresses)
            {
                ValidateAddress(address, name);
            }
        }

        #endregion

        #region Numeric Validation

        /// <summary>
        /// Validates that a BigInteger is positive (greater than zero)
        /// </summary>
        public static void ValidatePositive(BigInteger value, string parameterName)
        {
            if (value <= 0)
                throw new ArgumentException($"Invalid {parameterName}: value must be positive");
        }

        /// <summary>
        /// Validates that a BigInteger is non-negative (zero or greater)
        /// </summary>
        public static void ValidateNonNegative(BigInteger value, string parameterName)
        {
            if (value < 0)
                throw new ArgumentException($"Invalid {parameterName}: value cannot be negative");
        }

        /// <summary>
        /// Validates that a value is within a specific range
        /// </summary>
        public static void ValidateRange(BigInteger value, BigInteger min, BigInteger max, string parameterName)
        {
            if (value < min || value > max)
                throw new ArgumentException($"Invalid {parameterName}: value must be between {min} and {max}");
        }

        /// <summary>
        /// Validates that a value does not exceed a maximum
        /// </summary>
        public static void ValidateMax(BigInteger value, BigInteger max, string parameterName)
        {
            if (value > max)
                throw new ArgumentException($"Invalid {parameterName}: value cannot exceed {max}");
        }

        #endregion

        #region String Validation

        /// <summary>
        /// Validates that a string is not null or empty
        /// </summary>
        public static void ValidateNotEmpty(string value, string parameterName)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException($"Invalid {parameterName}: cannot be null or empty");
        }

        /// <summary>
        /// Validates string length constraints
        /// </summary>
        public static void ValidateStringLength(string value, int maxLength, string parameterName)
        {
            if (value == null)
                throw new ArgumentException($"Invalid {parameterName}: cannot be null");
            
            if (value.Length > maxLength)
                throw new ArgumentException($"Invalid {parameterName}: length cannot exceed {maxLength} characters");
        }

        /// <summary>
        /// Validates string length with minimum and maximum
        /// </summary>
        public static void ValidateStringLengthRange(string value, int minLength, int maxLength, string parameterName)
        {
            if (value == null)
                throw new ArgumentException($"Invalid {parameterName}: cannot be null");
            
            if (value.Length < minLength || value.Length > maxLength)
                throw new ArgumentException($"Invalid {parameterName}: length must be between {minLength} and {maxLength} characters");
        }

        #endregion

        #region ByteString Validation

        /// <summary>
        /// Validates that a ByteString is not null or empty
        /// </summary>
        public static void ValidateByteString(ByteString value, string parameterName)
        {
            if (value == null || value.Length == 0)
                throw new ArgumentException($"Invalid {parameterName}: cannot be null or empty");
        }

        /// <summary>
        /// Validates ByteString length constraints
        /// </summary>
        public static void ValidateByteStringLength(ByteString value, int maxLength, string parameterName)
        {
            if (value == null)
                throw new ArgumentException($"Invalid {parameterName}: cannot be null");
            
            if (value.Length > maxLength)
                throw new ArgumentException($"Invalid {parameterName}: length cannot exceed {maxLength} bytes");
        }

        #endregion

        #region Array Validation

        /// <summary>
        /// Validates that an array is not null or empty
        /// </summary>
        public static void ValidateArrayNotEmpty<T>(T[] array, string parameterName)
        {
            if (array == null || array.Length == 0)
                throw new ArgumentException($"Invalid {parameterName}: array cannot be null or empty");
        }

        /// <summary>
        /// Validates array length constraints
        /// </summary>
        public static void ValidateArrayLength<T>(T[] array, int maxLength, string parameterName)
        {
            if (array == null)
                throw new ArgumentException($"Invalid {parameterName}: array cannot be null");
            
            if (array.Length > maxLength)
                throw new ArgumentException($"Invalid {parameterName}: array length cannot exceed {maxLength}");
        }

        #endregion

        #region Permission Validation

        /// <summary>
        /// Validates that the caller has required permissions
        /// </summary>
        public static void ValidatePermission(byte userPermissions, byte requiredPermissions, string action)
        {
            if ((userPermissions & requiredPermissions) != requiredPermissions)
                throw new InvalidOperationException($"Insufficient permissions for {action}");
        }

        #endregion

        #region State Validation

        /// <summary>
        /// Validates that the contract is not paused
        /// </summary>
        public static void ValidateNotPaused(bool isPaused)
        {
            if (isPaused)
                throw new InvalidOperationException("Contract is paused");
        }

        /// <summary>
        /// Validates contract state
        /// </summary>
        public static void ValidateState(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        #endregion

        #region Witness Validation

        /// <summary>
        /// Validates that the caller is authorized
        /// </summary>
        public static void ValidateWitness(UInt160 address)
        {
            if (!Runtime.CheckWitness(address))
                throw new InvalidOperationException("Unauthorized: invalid witness");
        }

        /// <summary>
        /// Validates that one of the addresses is authorized
        /// </summary>
        public static bool ValidateAnyWitness(params UInt160[] addresses)
        {
            foreach (var address in addresses)
            {
                if (Runtime.CheckWitness(address))
                    return true;
            }
            throw new InvalidOperationException("Unauthorized: no valid witness");
        }

        #endregion

        #region Time Validation

        /// <summary>
        /// Validates that a timestamp is in the future
        /// </summary>
        public static void ValidateFutureTime(ulong timestamp, string parameterName)
        {
            var currentTime = Runtime.Time;
            if (timestamp <= currentTime)
                throw new ArgumentException($"Invalid {parameterName}: timestamp must be in the future");
        }

        /// <summary>
        /// Validates that a timestamp is within a valid range
        /// </summary>
        public static void ValidateTimeRange(ulong timestamp, ulong minTime, ulong maxTime, string parameterName)
        {
            if (timestamp < minTime || timestamp > maxTime)
                throw new ArgumentException($"Invalid {parameterName}: timestamp must be between {minTime} and {maxTime}");
        }

        #endregion
    }
}