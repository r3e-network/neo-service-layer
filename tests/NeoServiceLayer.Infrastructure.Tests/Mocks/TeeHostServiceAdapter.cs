using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Shared.Models;
using CoreTask = NeoServiceLayer.Core.Models.Task;

namespace NeoServiceLayer.Infrastructure.Tests.Mocks
{
    /// <summary>
    /// Adapter class that implements Core.Interfaces.ITeeHostService and delegates to Tee.Host.Services.ITeeHostService.
    /// </summary>
    public class TeeHostServiceAdapter : ITeeHostService
    {
        private readonly NeoServiceLayer.Tee.Host.Services.ITeeHostService _teeHostService;

        /// <summary>
        /// Initializes a new instance of the TeeHostServiceAdapter class.
        /// </summary>
        /// <param name="teeHostService">The Tee.Host.Services.ITeeHostService implementation.</param>
        public TeeHostServiceAdapter(NeoServiceLayer.Tee.Host.Services.ITeeHostService teeHostService)
        {
            _teeHostService = teeHostService ?? throw new ArgumentNullException(nameof(teeHostService));
        }

        /// <inheritdoc/>
        public System.Threading.Tasks.Task<bool> InitializeTeeAsync()
        {
            return _teeHostService.InitializeTeeAsync();
        }

        /// <inheritdoc/>
        public System.Threading.Tasks.Task<Dictionary<string, object>> ExecuteTaskAsync(CoreTask task)
        {
            return _teeHostService.ExecuteTaskAsync(task);
        }

        /// <inheritdoc/>
        public System.Threading.Tasks.Task<TeeMessage> SendMessageAsync(TeeMessage message)
        {
            return _teeHostService.SendMessageAsync(message);
        }

        /// <inheritdoc/>
        public System.Threading.Tasks.Task<AttestationProof> GetAttestationProofAsync()
        {
            return _teeHostService.GetAttestationProofAsync();
        }

        /// <inheritdoc/>
        public System.Threading.Tasks.Task<bool> VerifyAttestationProofAsync(AttestationProof attestationProof)
        {
            return _teeHostService.VerifyAttestationProofAsync(attestationProof);
        }

        /// <inheritdoc/>
        public System.Threading.Tasks.Task<TeeStatus> GetStatusAsync()
        {
            return _teeHostService.GetStatusAsync();
        }
    }
}
