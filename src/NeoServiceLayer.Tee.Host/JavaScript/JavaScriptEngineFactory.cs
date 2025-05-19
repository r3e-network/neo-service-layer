using System;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Shared.JavaScript;

namespace NeoServiceLayer.Tee.Host.JavaScript
{
    /// <summary>
    /// Factory for creating JavaScript engines.
    /// </summary>
    public class JavaScriptEngineFactory : IJavaScriptEngineFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IGasAccounting _gasAccounting;

        /// <summary>
        /// Initializes a new instance of the JavaScriptEngineFactory class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="gasAccounting">The gas accounting.</param>
        public JavaScriptEngineFactory(ILoggerFactory loggerFactory, IGasAccounting gasAccounting)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _gasAccounting = gasAccounting ?? throw new ArgumentNullException(nameof(gasAccounting));
        }

        /// <inheritdoc/>
        public IJavaScriptEngine CreateEngine(JavaScriptEngineType engineType)
        {
            return CreateEngine(engineType, new JavaScriptEngineOptions());
        }

        /// <inheritdoc/>
        public IJavaScriptEngine CreateEngine(JavaScriptEngineType engineType, JavaScriptEngineOptions options)
        {
            switch (engineType)
            {
                case JavaScriptEngineType.QuickJS:
                    return new QuickJsEngine(
                        _loggerFactory.CreateLogger<QuickJsEngine>(),
                        _gasAccounting,
                        options);

                case JavaScriptEngineType.V8:
                    return new V8Engine(
                        _loggerFactory.CreateLogger<V8Engine>(),
                        _gasAccounting,
                        options);

                case JavaScriptEngineType.ClearScript:
                    return new ClearScriptEngine(
                        _loggerFactory.CreateLogger<ClearScriptEngine>(),
                        _gasAccounting,
                        options);

                case JavaScriptEngineType.Simple:
                    return new SimpleJavaScriptEngine(
                        _loggerFactory.CreateLogger<SimpleJavaScriptEngine>(),
                        _gasAccounting,
                        options);

                default:
                    throw new ArgumentException($"Unsupported JavaScript engine type: {engineType}", nameof(engineType));
            }
        }

        /// <inheritdoc/>
        public JavaScriptEngineType GetDefaultEngineType()
        {
            // Check if QuickJS is available
            try
            {
                using (IJavaScriptEngine engine = CreateEngine(JavaScriptEngineType.QuickJS))
                {
                    if (engine.InitializeAsync().Result)
                    {
                        return JavaScriptEngineType.QuickJS;
                    }
                }
            }
            catch
            {
                // QuickJS is not available
            }

            // Check if V8 is available
            try
            {
                using (IJavaScriptEngine engine = CreateEngine(JavaScriptEngineType.V8))
                {
                    if (engine.InitializeAsync().Result)
                    {
                        return JavaScriptEngineType.V8;
                    }
                }
            }
            catch
            {
                // V8 is not available
            }

            // Check if ClearScript is available
            try
            {
                using (IJavaScriptEngine engine = CreateEngine(JavaScriptEngineType.ClearScript))
                {
                    if (engine.InitializeAsync().Result)
                    {
                        return JavaScriptEngineType.ClearScript;
                    }
                }
            }
            catch
            {
                // ClearScript is not available
            }

            // Fall back to simple engine
            return JavaScriptEngineType.Simple;
        }

        /// <inheritdoc/>
        public JavaScriptEngineType[] GetAvailableEngineTypes()
        {
            var availableEngineTypes = new List<JavaScriptEngineType>();

            // Check if QuickJS is available
            try
            {
                using (IJavaScriptEngine engine = CreateEngine(JavaScriptEngineType.QuickJS))
                {
                    if (engine.InitializeAsync().Result)
                    {
                        availableEngineTypes.Add(JavaScriptEngineType.QuickJS);
                    }
                }
            }
            catch
            {
                // QuickJS is not available
            }

            // Check if V8 is available
            try
            {
                using (IJavaScriptEngine engine = CreateEngine(JavaScriptEngineType.V8))
                {
                    if (engine.InitializeAsync().Result)
                    {
                        availableEngineTypes.Add(JavaScriptEngineType.V8);
                    }
                }
            }
            catch
            {
                // V8 is not available
            }

            // Check if ClearScript is available
            try
            {
                using (IJavaScriptEngine engine = CreateEngine(JavaScriptEngineType.ClearScript))
                {
                    if (engine.InitializeAsync().Result)
                    {
                        availableEngineTypes.Add(JavaScriptEngineType.ClearScript);
                    }
                }
            }
            catch
            {
                // ClearScript is not available
            }

            // Simple engine is always available
            availableEngineTypes.Add(JavaScriptEngineType.Simple);

            return availableEngineTypes.ToArray();
        }
    }
}
