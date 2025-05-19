using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Tee.Shared.JavaScript
{
    /// <summary>
    /// Interface for a JavaScript API that provides functionality to JavaScript code.
    /// </summary>
    public interface IJavaScriptApi
    {
        /// <summary>
        /// Gets the name of the API.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the description of the API.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the version of the API.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Gets the functions provided by the API.
        /// </summary>
        IReadOnlyList<JavaScriptApiFunction> Functions { get; }

        /// <summary>
        /// Gets the gas cost for a function.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <param name="args">The arguments to the function.</param>
        /// <returns>The gas cost.</returns>
        ulong GetGasCost(string functionName, params object[] args);

        /// <summary>
        /// Registers the API with a JavaScript engine.
        /// </summary>
        /// <param name="engine">The JavaScript engine.</param>
        void Register(IJavaScriptEngine engine);

        /// <summary>
        /// Unregisters the API from a JavaScript engine.
        /// </summary>
        /// <param name="engine">The JavaScript engine.</param>
        void Unregister(IJavaScriptEngine engine);
    }

    /// <summary>
    /// Function provided by a JavaScript API.
    /// </summary>
    public class JavaScriptApiFunction
    {
        /// <summary>
        /// Gets or sets the name of the function.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the function.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the parameters of the function.
        /// </summary>
        public IReadOnlyList<JavaScriptApiFunctionParameter> Parameters { get; set; }

        /// <summary>
        /// Gets or sets the return type of the function.
        /// </summary>
        public string ReturnType { get; set; }

        /// <summary>
        /// Gets or sets the base gas cost of the function.
        /// </summary>
        public ulong BaseGasCost { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the function is available in secure mode.
        /// </summary>
        public bool AvailableInSecureMode { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the function is available in non-secure mode.
        /// </summary>
        public bool AvailableInNonSecureMode { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the function requires authentication.
        /// </summary>
        public bool RequiresAuthentication { get; set; } = false;

        /// <summary>
        /// Gets or sets the required permissions for the function.
        /// </summary>
        public IReadOnlyList<string> RequiredPermissions { get; set; }
    }

    /// <summary>
    /// Parameter of a JavaScript API function.
    /// </summary>
    public class JavaScriptApiFunctionParameter
    {
        /// <summary>
        /// Gets or sets the name of the parameter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the parameter.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the description of the parameter.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter is optional.
        /// </summary>
        public bool IsOptional { get; set; } = false;

        /// <summary>
        /// Gets or sets the default value of the parameter.
        /// </summary>
        public object DefaultValue { get; set; }
    }
}
