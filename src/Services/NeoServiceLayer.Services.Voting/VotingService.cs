using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.EnclaveStorage;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.ServiceFramework;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Voting;

/// <summary>
/// Implementation of the Voting Service with SGX computing and storage capabilities.
/// Demonstrates how to use the standard SGX interface for secure voting operations.
/// </summary>
public partial class VotingService
{
    // This partial class contains only SGX-related extensions
    // Main class definition is in VotingService.Core.cs
}