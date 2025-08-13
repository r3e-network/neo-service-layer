using System;
using System.Runtime.InteropServices;

namespace NeoServiceLayer.Tee.Enclave.Native;

/// <summary>
/// Native API bindings for Intel SGX operations.
/// This provides the actual interface to SGX SDK functions.
/// </summary>
public static class SgxNativeApi
{
    private const string SGX_URTS_LIB = "sgx_urts";
    private const string SGX_UKEY_EXCHANGE_LIB = "sgx_ukey_exchange";

    #region SGX Status Codes

    public enum SgxStatus : uint
    {
        Success = 0x00000000,
        Unexpected = 0x00000001,
        InvalidParameter = 0x00000002,
        OutOfMemory = 0x00000003,
        EnclaveCreationError = 0x00000004,
        EnclaveDestroyed = 0x00000005,
        InvalidEnclave = 0x00000006,
        InvalidSig = 0x00000007,
        OutOfEPC = 0x00000008,
        NoDevice = 0x00000009,
        MemoryMapConflict = 0x0000000A,
        InvalidMetadata = 0x0000000B,
        DeviceBusy = 0x0000000C,
        InvalidVersion = 0x0000000D,
        ModeIncompatible = 0x0000000E,
        EnclaveFileAccess = 0x0000000F,
        InvalidMisc = 0x00000010,
        InvalidLaunchToken = 0x00000011
    }

    public enum SealingPolicy : uint
    {
        MrSigner = 0x01,
        MrEnclave = 0x02
    }

    #endregion

    #region P/Invoke Declarations

    [DllImport(SGX_URTS_LIB, CallingConvention = CallingConvention.Cdecl)]
    private static extern SgxStatus sgx_create_enclave(
        [In, MarshalAs(UnmanagedType.LPStr)] string file_name,
        int debug,
        IntPtr launch_token,
        IntPtr launch_token_updated,
        out ulong enclave_id,
        IntPtr misc_attr);

        #region SGX Error Codes
        public const uint SGX_SUCCESS = 0x00000000;
        public const uint SGX_ERROR_UNEXPECTED = 0x00000001;
        public const uint SGX_ERROR_INVALID_PARAMETER = 0x00000002;
        public const uint SGX_ERROR_OUT_OF_MEMORY = 0x00000003;
        public const uint SGX_ERROR_ENCLAVE_LOST = 0x00000004;
        public const uint SGX_ERROR_INVALID_ENCLAVE = 0x00000005;
        public const uint SGX_ERROR_INVALID_ENCLAVE_ID = 0x00000006;
        public const uint SGX_ERROR_INVALID_SIGNATURE = 0x00000007;
        public const uint SGX_ERROR_OUT_OF_EPC = 0x00000008;
        public const uint SGX_ERROR_NO_DEVICE = 0x00000009;
        public const uint SGX_ERROR_MEMORY_MAP_CONFLICT = 0x0000000A;
        public const uint SGX_ERROR_INVALID_METADATA = 0x0000000B;
        public const uint SGX_ERROR_DEVICE_BUSY = 0x0000000C;
        public const uint SGX_ERROR_INVALID_VERSION = 0x0000000D;
        public const uint SGX_ERROR_INVALID_ATTRIBUTE = 0x0000000E;
        public const uint SGX_ERROR_ENCLAVE_FILE_ACCESS = 0x0000000F;
        public const uint SGX_ERROR_NDEBUG_ENCLAVE = 0x00000010;
        public const uint SGX_ERROR_UNDEFINED_SYMBOL = 0x00002000;
        public const uint SGX_ERROR_INVALID_ENCLAVE_IMAGE = 0x00002001;
        public const uint SGX_ERROR_SERVICE_UNAVAILABLE = 0x00003001;
        public const uint SGX_ERROR_SERVICE_TIMEOUT = 0x00003002;
        public const uint SGX_ERROR_AE_INVALID_EPIDBLOB = 0x00003003;
        public const uint SGX_ERROR_SERVICE_INVALID_PRIVILEGE = 0x00003004;
        public const uint SGX_ERROR_EPID_MEMBER_REVOKED = 0x00003005;
        public const uint SGX_ERROR_UPDATE_NEEDED = 0x00003006;
        public const uint SGX_ERROR_NETWORK_FAILURE = 0x00003007;
        public const uint SGX_ERROR_AE_SESSION_INVALID = 0x00003008;
        public const uint SGX_ERROR_BUSY = 0x0000300A;
        public const uint SGX_ERROR_MC_NOT_FOUND = 0x0000300C;
        public const uint SGX_ERROR_MC_NO_ACCESS_RIGHT = 0x0000300D;
        public const uint SGX_ERROR_MC_USED_UP = 0x0000300E;
        public const uint SGX_ERROR_MC_OVER_QUOTA = 0x0000300F;
        public const uint SGX_ERROR_KDF_MISMATCH = 0x00003011;
        public const uint SGX_ERROR_UNRECOGNIZED_PLATFORM = 0x00003012;
        public const uint SGX_ERROR_UNSUPPORTED_CONFIG = 0x00003013;
        #endregion

        #region SGX Structures
        [StructLayout(LayoutKind.Sequential)]
        public struct SgxLaunchToken
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
            public byte[] Token;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SgxMiscAttribute
        {
            public uint Sflags;
            public ulong XfrmSet;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SgxAttributes
        {
            public ulong Flags;
            public ulong Xfrm;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SgxMeasurement
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] M;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SgxReport
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] CpuSvn;
            public uint MiscSelect;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)]
            public byte[] Reserved1;
            public SgxAttributes Attributes;
            public SgxMeasurement MrEnclave;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] Reserved2;
            public SgxMeasurement MrSigner;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] Reserved3;
            public ushort ConfigSvn;
            public ushort ProdId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] Reserved4;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] FamilyId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] ImageId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] ReportData;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SgxTargetInfo
        {
            public SgxMeasurement MrEnclave;
            public SgxAttributes Attributes;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] Reserved1;
            public SgxMiscAttribute MiscSelect;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 456)]
            public byte[] Reserved2;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SgxReportData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] D;
        }
        #endregion

        #region Enclave Management
        /// <summary>
        /// Creates an SGX enclave.
        /// </summary>
        /// <param name="fileName">Path to the enclave library.</param>
        /// <param name="debug">Debug flag (1 for debug, 0 for release).</param>
        /// <param name="token">Launch token (can be null for simulation mode).</param>
        /// <param name="tokenUpdated">Output parameter indicating if token was updated.</param>
        /// <param name="enclaveId">Output parameter for the created enclave ID.</param>
        /// <param name="miscAttr">Output parameter for miscellaneous attributes.</param>
        /// <returns>SGX status code.</returns>
        [DllImport(SgxUrtsDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern uint sgx_create_enclave(
            [MarshalAs(UnmanagedType.LPStr)] string fileName,
            int debug,
            ref SgxLaunchToken token,
            ref int tokenUpdated,
            ref ulong enclaveId,
            ref SgxMiscAttribute miscAttr);

        /// <summary>
        /// Creates an SGX enclave (simplified version for simulation mode).
        /// </summary>
        /// <param name="fileName">Path to the enclave library.</param>
        /// <param name="debug">Debug flag (1 for debug, 0 for release).</param>
        /// <param name="token">Launch token (can be IntPtr.Zero for simulation mode).</param>
        /// <param name="tokenUpdated">Output parameter indicating if token was updated.</param>
        /// <param name="enclaveId">Output parameter for the created enclave ID.</param>
        /// <param name="miscAttr">Output parameter for miscellaneous attributes.</param>
        /// <returns>SGX status code.</returns>
        [DllImport(SgxUrtsDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern uint sgx_create_enclave_ex(
            [MarshalAs(UnmanagedType.LPStr)] string fileName,
            int debug,
            IntPtr token,
            ref int tokenUpdated,
            ref ulong enclaveId,
            IntPtr miscAttr);

        /// <summary>
        /// Destroys an SGX enclave.
        /// </summary>
        /// <param name="enclaveId">The enclave ID to destroy.</param>
        /// <returns>SGX status code.</returns>
        [DllImport(SgxUrtsDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint sgx_destroy_enclave(ulong enclaveId);

        /// <summary>
        /// Gets the target info for an enclave.
        /// </summary>
        /// <param name="enclaveId">The enclave ID.</param>
        /// <param name="targetInfo">Output parameter for target info.</param>
        /// <returns>SGX status code.</returns>
        [DllImport(SgxUrtsDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint sgx_get_target_info(
            ulong enclaveId,
            ref SgxTargetInfo targetInfo);
        #endregion

        #region Attestation
        /// <summary>
        /// Creates a report for attestation.
        /// </summary>
        /// <param name="enclaveId">The enclave ID.</param>
        /// <param name="reportData">Report data to include.</param>
        /// <param name="targetInfo">Target info for the report.</param>
        /// <param name="report">Output parameter for the generated report.</param>
        /// <returns>SGX status code.</returns>
        [DllImport(SgxUrtsDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint sgx_create_report(
            ulong enclaveId,
            ref SgxReportData reportData,
            ref SgxTargetInfo targetInfo,
            ref SgxReport report);

        /// <summary>
        /// Verifies a report.
        /// </summary>
        /// <param name="report">The report to verify.</param>
        /// <returns>SGX status code.</returns>
        [DllImport(SgxUrtsDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint sgx_verify_report(ref SgxReport report);
        #endregion

        #region Sealing
        /// <summary>
        /// Gets the sealed data size.
        /// </summary>
        /// <param name="additionalMacTxtLen">Additional MAC text length.</param>
        /// <param name="txtToEncryptLen">Text to encrypt length.</param>
        /// <returns>Required sealed data size.</returns>
        [DllImport(SgxUtilsDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint sgx_calc_sealed_data_size(
            uint additionalMacTxtLen,
            uint txtToEncryptLen);

        /// <summary>
        /// Gets the encrypted text size from sealed data.
        /// </summary>
        /// <param name="sealedData">Pointer to sealed data.</param>
        /// <returns>Encrypted text size.</returns>
        [DllImport(SgxUtilsDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint sgx_get_encrypt_txt_len(IntPtr sealedData);

        /// <summary>
        /// Gets the additional MAC text size from sealed data.
        /// </summary>
        /// <param name="sealedData">Pointer to sealed data.</param>
        /// <returns>Additional MAC text size.</returns>
        [DllImport(SgxUtilsDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint sgx_get_add_mac_txt_len(IntPtr sealedData);
        #endregion

        #region Utility Functions
        /// <summary>
        /// Gets the extended error information.
        /// </summary>
        /// <param name="enclaveId">The enclave ID.</param>
        /// <returns>Extended error information.</returns>
        [DllImport(SgxUrtsDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint sgx_get_extended_error_info(ulong enclaveId);

        /// <summary>
        /// Registers an exception handler for the enclave.
        /// </summary>
        /// <param name="enclaveId">The enclave ID.</param>
        /// <param name="handler">Exception handler function pointer.</param>
        /// <returns>SGX status code.</returns>
        [DllImport(SgxUrtsDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint sgx_register_exception_handler(
            ulong enclaveId,
            IntPtr handler);

        /// <summary>
        /// Unregisters an exception handler for the enclave.
        /// </summary>
        /// <param name="enclaveId">The enclave ID.</param>
        /// <param name="handler">Exception handler function pointer.</param>
        /// <returns>SGX status code.</returns>
        [DllImport(SgxUrtsDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint sgx_unregister_exception_handler(
            ulong enclaveId,
            IntPtr handler);
        #endregion

        #region Error Handling
        /// <summary>
        /// Converts SGX error code to human-readable string.
        /// </summary>
        /// <param name="errorCode">SGX error code.</param>
        /// <returns>Error description string.</returns>
        public static string GetErrorDescription(uint errorCode)
        {
            return errorCode switch
            {
                SGX_SUCCESS => "Success",
                SGX_ERROR_UNEXPECTED => "Unexpected error",
                SGX_ERROR_INVALID_PARAMETER => "Invalid parameter",
                SGX_ERROR_OUT_OF_MEMORY => "Out of memory",
                SGX_ERROR_ENCLAVE_LOST => "Enclave lost",
                SGX_ERROR_INVALID_ENCLAVE => "Invalid enclave",
                SGX_ERROR_INVALID_ENCLAVE_ID => "Invalid enclave ID",
                SGX_ERROR_INVALID_SIGNATURE => "Invalid signature",
                SGX_ERROR_OUT_OF_EPC => "Out of EPC memory",
                SGX_ERROR_NO_DEVICE => "No SGX device",
                SGX_ERROR_MEMORY_MAP_CONFLICT => "Memory map conflict",
                SGX_ERROR_INVALID_METADATA => "Invalid metadata",
                SGX_ERROR_DEVICE_BUSY => "SGX device busy",
                SGX_ERROR_INVALID_VERSION => "Invalid version",
                SGX_ERROR_INVALID_ATTRIBUTE => "Invalid attribute",
                SGX_ERROR_ENCLAVE_FILE_ACCESS => "Enclave file access error",
                SGX_ERROR_NDEBUG_ENCLAVE => "Non-debug enclave",
                SGX_ERROR_UNDEFINED_SYMBOL => "Undefined symbol",
                SGX_ERROR_INVALID_ENCLAVE_IMAGE => "Invalid enclave image",
                SGX_ERROR_SERVICE_UNAVAILABLE => "Service unavailable",
                SGX_ERROR_SERVICE_TIMEOUT => "Service timeout",
                SGX_ERROR_NETWORK_FAILURE => "Network failure",
                SGX_ERROR_BUSY => "System busy",
                SGX_ERROR_UNRECOGNIZED_PLATFORM => "Unrecognized platform",
                SGX_ERROR_UNSUPPORTED_CONFIG => "Unsupported configuration",
                _ => $"Unknown error code: 0x{errorCode:X8}"
            };
        }

        /// <summary>
        /// Checks if an SGX operation was successful.
        /// </summary>
        /// <param name="status">SGX status code.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public static bool IsSuccess(uint status) => status == SGX_SUCCESS;

        /// <summary>
        /// Throws an exception if the SGX operation failed.
        /// </summary>
        /// <param name="status">SGX status code.</param>
        /// <param name="operation">Operation description for error message.</param>
        /// <exception cref="SgxException">Thrown when operation fails.</exception>
        public static void ThrowIfError(uint status, string operation)
        {
            if (status != SGX_SUCCESS)
            {
                throw new SgxException($"{operation} failed: {GetErrorDescription(status)}", status);
            }
        }
        #endregion
    }

    /// <summary>
    /// Exception thrown when an SGX operation fails.
    /// </summary>
    public class SgxException : Exception
    {
        /// <summary>
        /// Gets the SGX error code.
        /// </summary>
        public uint ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SgxException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="errorCode">The SGX error code.</param>
        public SgxException(string message, uint errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SgxException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="errorCode">The SGX error code.</param>
        /// <param name="innerException">The inner exception.</param>
        public SgxException(string message, uint errorCode, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
