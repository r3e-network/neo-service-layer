using System;

namespace NeoServiceLayer.RPC.Server.Attributes
{
    /// <summary>
    /// Attribute to mark methods as JSON-RPC endpoints.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class JsonRpcMethodAttribute : Attribute
    {
        /// <summary>
        /// Gets the JSON-RPC method name.
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        /// Gets or sets the method description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonRpcMethodAttribute"/> class.
        /// </summary>
        /// <param name="methodName">The JSON-RPC method name.</param>
        public JsonRpcMethodAttribute(string methodName)
        {
            MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        }
    }
}
