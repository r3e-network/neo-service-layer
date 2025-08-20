using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.CQRS;
using NeoServiceLayer.Infrastructure.CQRS.Repositories;
using NeoServiceLayer.Services.KeyManagement.Commands;
using NeoServiceLayer.Services.KeyManagement.Domain;
using NeoServiceLayer.Services.KeyManagement.Infrastructure;
using NeoServiceLayer.ServiceFramework;
using System.Collections.Generic;
using System.Linq;


namespace NeoServiceLayer.Services.KeyManagement.CommandHandlers
{
    public class GenerateKeyCommandHandler : ICommandHandler<GenerateKeyCommand, string>
    {
        private readonly IAggregateRepository<CryptographicKey> _repository;
        private readonly ICryptographicService _cryptoService;
        private readonly ILogger<GenerateKeyCommandHandler> Logger;

        public GenerateKeyCommandHandler(
            IAggregateRepository<CryptographicKey> repository,
            ICryptographicService cryptoService,
            ILogger<GenerateKeyCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> HandleAsync(
            GenerateKeyCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogInformation(
                    "Generating {Algorithm} key of size {KeySize} for {KeyType}",
                    command.Algorithm, command.KeySize, command.KeyType);

                // Generate the actual cryptographic key
                var keyData = await _cryptoService.GenerateKeyAsync(
                    command.Algorithm,
                    command.KeySize,
                    cancellationToken);

                var keyId = Guid.NewGuid().ToString();

                // Create the aggregate with the generated key
                var key = new CryptographicKey(
                    keyId,
                    command.KeyType,
                    command.Algorithm,
                    keyData.PublicKey,
                    keyData.EncryptedPrivateKey,
                    command.InitiatedBy,
                    command.ExpiresAt,
                    command.Metadata);

                // Save the aggregate
                await _repository.SaveAsync(key, null, cancellationToken);

                Logger.LogInformation(
                    "Generated key {KeyId} of type {KeyType} with algorithm {Algorithm}",
                    keyId, command.KeyType, command.Algorithm);

                return keyId;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    "Failed to generate key of type {KeyType} with algorithm {Algorithm}",
                    command.KeyType, command.Algorithm);
                throw;
            }
        }
    }

    public class ActivateKeyCommandHandler : ICommandHandler<ActivateKeyCommand>
    {
        private readonly IAggregateRepository<CryptographicKey> _repository;
        private readonly ILogger<ActivateKeyCommandHandler> Logger;

        public ActivateKeyCommandHandler(
            IAggregateRepository<CryptographicKey> repository,
            ILogger<ActivateKeyCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleAsync(
            ActivateKeyCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var key = await _repository.GetByIdAsync(command.KeyId, cancellationToken);
                if (key == null)
                    throw new KeyNotFoundException($"Key {command.KeyId} not found");

                key.Activate(command.InitiatedBy);

                await _repository.SaveAsync(key, cancellationToken: cancellationToken);

                Logger.LogInformation(
                    "Activated key {KeyId} by {InitiatedBy}",
                    command.KeyId, command.InitiatedBy);
            }
            catch (Exception ex) when (ex is not KeyNotFoundException)
            {
                Logger.LogError(ex, "Failed to activate key {KeyId}", command.KeyId);
                throw;
            }
        }
    }

    public class RevokeKeyCommandHandler : ICommandHandler<RevokeKeyCommand>
    {
        private readonly IAggregateRepository<CryptographicKey> _repository;
        private readonly ILogger<RevokeKeyCommandHandler> Logger;

        public RevokeKeyCommandHandler(
            IAggregateRepository<CryptographicKey> repository,
            ILogger<RevokeKeyCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleAsync(
            RevokeKeyCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var key = await _repository.GetByIdAsync(command.KeyId, cancellationToken);
                if (key == null)
                    throw new KeyNotFoundException($"Key {command.KeyId} not found");

                key.Revoke(command.InitiatedBy, command.Reason);

                await _repository.SaveAsync(key, cancellationToken: cancellationToken);

                Logger.LogInformation(
                    "Revoked key {KeyId} by {InitiatedBy} with reason: {Reason}",
                    command.KeyId, command.InitiatedBy, command.Reason);
            }
            catch (Exception ex) when (ex is not KeyNotFoundException)
            {
                Logger.LogError(ex, "Failed to revoke key {KeyId}", command.KeyId);
                throw;
            }
        }
    }

    public class RotateKeyCommandHandler : ICommandHandler<RotateKeyCommand, string>
    {
        private readonly IAggregateRepository<CryptographicKey> _repository;
        private readonly ICryptographicService _cryptoService;
        private readonly ILogger<RotateKeyCommandHandler> Logger;

        public RotateKeyCommandHandler(
            IAggregateRepository<CryptographicKey> repository,
            ICryptographicService cryptoService,
            ILogger<RotateKeyCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> HandleAsync(
            RotateKeyCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var key = await _repository.GetByIdAsync(command.KeyId, cancellationToken);
                if (key == null)
                    throw new KeyNotFoundException($"Key {command.KeyId} not found");

                // Generate new key material
                var newKeyData = await _cryptoService.GenerateKeyAsync(
                    key.Algorithm,
                    2048, // Default key size for rotation
                    cancellationToken);

                key.Rotate(
                    newKeyData.PublicKey,
                    newKeyData.EncryptedPrivateKey,
                    command.InitiatedBy);

                await _repository.SaveAsync(key, cancellationToken: cancellationToken);

                Logger.LogInformation(
                    "Rotated key {KeyId} by {InitiatedBy}",
                    command.KeyId, command.InitiatedBy);

                return command.KeyId;
            }
            catch (Exception ex) when (ex is not KeyNotFoundException)
            {
                Logger.LogError(ex, "Failed to rotate key {KeyId}", command.KeyId);
                throw;
            }
        }
    }

    public class SignDataCommandHandler : ICommandHandler<SignDataCommand, string>
    {
        private readonly IAggregateRepository<CryptographicKey> _repository;
        private readonly ICryptographicService _cryptoService;
        private readonly ILogger<SignDataCommandHandler> Logger;

        public SignDataCommandHandler(
            IAggregateRepository<CryptographicKey> repository,
            ICryptographicService cryptoService,
            ILogger<SignDataCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> HandleAsync(
            SignDataCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var key = await _repository.GetByIdAsync(command.KeyId, cancellationToken);
                if (key == null)
                    throw new KeyNotFoundException($"Key {command.KeyId} not found");

                // Record usage
                key.RecordUsage(command.InitiatedBy, "Sign");

                // Perform the actual signing
                var signature = await _cryptoService.SignDataAsync(
                    command.KeyId,
                    command.Data,
                    cancellationToken);

                await _repository.SaveAsync(key, cancellationToken: cancellationToken);

                Logger.LogInformation(
                    "Signed data with key {KeyId} by {InitiatedBy}",
                    command.KeyId, command.InitiatedBy);

                return signature;
            }
            catch (Exception ex) when (ex is not KeyNotFoundException)
            {
                Logger.LogError(ex, "Failed to sign data with key {KeyId}", command.KeyId);
                throw;
            }
        }
    }

    public class VerifySignatureCommandHandler : ICommandHandler<VerifySignatureCommand, bool>
    {
        private readonly IAggregateRepository<CryptographicKey> _repository;
        private readonly ICryptographicService _cryptoService;
        private readonly ILogger<VerifySignatureCommandHandler> Logger;

        public VerifySignatureCommandHandler(
            IAggregateRepository<CryptographicKey> repository,
            ICryptographicService cryptoService,
            ILogger<VerifySignatureCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> HandleAsync(
            VerifySignatureCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var key = await _repository.GetByIdAsync(command.KeyId, cancellationToken);
                if (key == null)
                    throw new KeyNotFoundException($"Key {command.KeyId} not found");

                // Record usage
                key.RecordUsage(command.InitiatedBy, "Verify");

                // Perform the actual verification
                var isValid = await _cryptoService.VerifySignatureAsync(
                    command.KeyId,
                    command.Data,
                    command.Signature,
                    cancellationToken);

                await _repository.SaveAsync(key, cancellationToken: cancellationToken);

                Logger.LogInformation(
                    "Verified signature with key {KeyId} by {InitiatedBy}, result: {IsValid}",
                    command.KeyId, command.InitiatedBy, isValid);

                return isValid;
            }
            catch (Exception ex) when (ex is not KeyNotFoundException)
            {
                Logger.LogError(ex, "Failed to verify signature with key {KeyId}", command.KeyId);
                throw;
            }
        }
    }

    public class GrantKeyAccessCommandHandler : ICommandHandler<GrantKeyAccessCommand>
    {
        private readonly IAggregateRepository<CryptographicKey> _repository;
        private readonly ILogger<GrantKeyAccessCommandHandler> Logger;

        public GrantKeyAccessCommandHandler(
            IAggregateRepository<CryptographicKey> repository,
            ILogger<GrantKeyAccessCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleAsync(
            GrantKeyAccessCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var key = await _repository.GetByIdAsync(command.KeyId, cancellationToken);
                if (key == null)
                    throw new KeyNotFoundException($"Key {command.KeyId} not found");

                key.GrantAccess(command.UserId, command.InitiatedBy);

                await _repository.SaveAsync(key, cancellationToken: cancellationToken);

                Logger.LogInformation(
                    "Granted access to key {KeyId} for user {UserId} by {InitiatedBy}",
                    command.KeyId, command.UserId, command.InitiatedBy);
            }
            catch (Exception ex) when (ex is not KeyNotFoundException)
            {
                Logger.LogError(ex, "Failed to grant access to key {KeyId}", command.KeyId);
                throw;
            }
        }
    }

    public class RevokeKeyAccessCommandHandler : ICommandHandler<RevokeKeyAccessCommand>
    {
        private readonly IAggregateRepository<CryptographicKey> _repository;
        private readonly ILogger<RevokeKeyAccessCommandHandler> Logger;

        public RevokeKeyAccessCommandHandler(
            IAggregateRepository<CryptographicKey> repository,
            ILogger<RevokeKeyAccessCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleAsync(
            RevokeKeyAccessCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var key = await _repository.GetByIdAsync(command.KeyId, cancellationToken);
                if (key == null)
                    throw new KeyNotFoundException($"Key {command.KeyId} not found");

                key.RevokeAccess(command.UserId, command.InitiatedBy);

                await _repository.SaveAsync(key, cancellationToken: cancellationToken);

                Logger.LogInformation(
                    "Revoked access to key {KeyId} for user {UserId} by {InitiatedBy}",
                    command.KeyId, command.UserId, command.InitiatedBy);
            }
            catch (Exception ex) when (ex is not KeyNotFoundException)
            {
                Logger.LogError(ex, "Failed to revoke access to key {KeyId}", command.KeyId);
                throw;
            }
        }
    }

    // Custom exceptions
    public class KeyNotFoundException : Exception
    {
        public KeyNotFoundException(string message) : base(message) { }
    }
}