using System;
using System.Collections.Generic;
using System.Text;

namespace NeoServiceLayer.Tee.Host
{
    /// <summary>
    /// Settings for the TEE enclave.
    /// </summary>
    public class TeeEnclaveSettings
    {
        /// <summary>
        /// Gets or sets the type of TEE to use.
        /// </summary>
        public string Type { get; set; } = "Occlum";

        /// <summary>
        /// Gets or sets the path to the enclave.
        /// </summary>
        public string EnclavePath { get; set; } = "bin/libenclave.so";

        /// <summary>
        /// Gets or sets a value indicating whether simulation mode is enabled.
        /// </summary>
        public bool SimulationMode { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether debug mode is enabled.
        /// </summary>
        public bool Debug { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether Occlum support is enabled.
        /// </summary>
        public bool OcclumSupport { get; set; } = true;

        /// <summary>
        /// Gets or sets the Occlum instance directory.
        /// </summary>
        public string OcclumInstanceDir { get; set; } = "/occlum_instance";

        /// <summary>
        /// Gets or sets the Occlum log level.
        /// </summary>
        public string OcclumLogLevel { get; set; } = "info";

        /// <summary>
        /// Gets or sets the JavaScript engine settings.
        /// </summary>
        public JavaScriptEngineSettings JavaScriptEngine { get; set; } = new JavaScriptEngineSettings();

        /// <summary>
        /// Gets or sets the user secrets settings.
        /// </summary>
        public UserSecretsSettings UserSecrets { get; set; } = new UserSecretsSettings();

        /// <summary>
        /// Gets or sets the gas accounting settings.
        /// </summary>
        public GasAccountingSettings GasAccounting { get; set; } = new GasAccountingSettings();
    }

    /// <summary>
    /// Settings for the JavaScript engine.
    /// </summary>
    public class JavaScriptEngineSettings
    {
        /// <summary>
        /// Gets or sets the maximum memory in MB.
        /// </summary>
        public int MaxMemoryMB { get; set; } = 512;

        /// <summary>
        /// Gets or sets the maximum execution time in milliseconds.
        /// </summary>
        public int MaxExecutionTimeMs { get; set; } = 5000;

        /// <summary>
        /// Gets or sets a value indicating whether the debugger is enabled.
        /// </summary>
        public bool EnableDebugger { get; set; } = false;
    }

    /// <summary>
    /// Settings for user secrets.
    /// </summary>
    public class UserSecretsSettings
    {
        /// <summary>
        /// Gets or sets the maximum number of secrets per user.
        /// </summary>
        public int MaxSecretsPerUser { get; set; } = 100;

        /// <summary>
        /// Gets or sets the maximum secret size in bytes.
        /// </summary>
        public int MaxSecretSizeBytes { get; set; } = 4096;
    }

    /// <summary>
    /// Settings for gas accounting.
    /// </summary>
    public class GasAccountingSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether gas accounting is enabled.
        /// </summary>
        public bool EnableGasAccounting { get; set; } = true;

        /// <summary>
        /// Gets or sets the gas limit per execution.
        /// </summary>
        public long GasLimitPerExecution { get; set; } = 1000000;

        /// <summary>
        /// Gets or sets the gas price multiplier.
        /// </summary>
        public double GasPriceMultiplier { get; set; } = 1.0;
    }
}
