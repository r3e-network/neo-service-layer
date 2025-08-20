using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Core;

/// <summary>
/// Represents the status of a service.
/// </summary>
public enum ServiceStatus
{
    /// <summary>
    /// The service has not been initialized.
    /// </summary>
    NotInitialized = 0,

    /// <summary>
    /// The service is initializing.
    /// </summary>
    Initializing = 1,

    /// <summary>
    /// The service has been initialized but not started.
    /// </summary>
    Initialized = 2,

    /// <summary>
    /// The service is starting.
    /// </summary>
    Starting = 3,

    /// <summary>
    /// The service is running.
    /// </summary>
    Running = 4,

    /// <summary>
    /// The service is stopping.
    /// </summary>
    Stopping = 5,

    /// <summary>
    /// The service has been stopped.
    /// </summary>
    Stopped = 6,

    /// <summary>
    /// The service has encountered an error.
    /// </summary>
    Error = 7,

    /// <summary>
    /// The service is in maintenance mode.
    /// </summary>
    Maintenance = 8,

    /// <summary>
    /// The service has failed.
    /// </summary>
    Failed = 9,

    /// <summary>
    /// The service has been disposed.
    /// </summary>
    Disposed = 10
}