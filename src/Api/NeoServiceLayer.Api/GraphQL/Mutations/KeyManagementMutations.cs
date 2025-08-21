using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.KeyManagement;

namespace NeoServiceLayer.Api.GraphQL.Mutations;

/// <summary>
/// GraphQL mutations for key management operations.
/// </summary>
[ExtendObjectType(typeof(Mutation))]
public class KeyManagementMutations
{
    /// <summary>
    /// Generates a new cryptographic key.
    /// </summary>
    /// <param name="input">The key generation input.</param>
    /// <param name="keyManagementService">The key management service.</param>
    /// <returns>The generated key metadata.</returns>
    [Authorize(Roles = ["Admin", "KeyManager"])]
    [GraphQLDescription("Generates a new cryptographic key")]
    public async Task<KeyMetadata> GenerateKey(
        GenerateKeyInput input,
        [Service] IKeyManagementService keyManagementService)
    {
        var request = new GenerateKeyRequest
        {
            KeyId = input.KeyId,
            KeyType = input.KeyType,
            KeyUsage = input.KeyUsage,
            BlockchainType = input.BlockchainType,
            Exportable = input.Exportable,
            AttestationChallenge = input.AttestationChallenge,
            Tags = input.Tags
        };

        return await keyManagementService.GenerateKeyAsync(request, CancellationToken.None);
    }

    /// <summary>
    /// Signs data using a specific key.
    /// </summary>
    /// <param name="input">The signing input.</param>
    /// <param name="keyManagementService">The key management service.</param>
    /// <returns>The signature result.</returns>
    [Authorize(Roles = ["Admin", "KeyManager", "User"])]
    [GraphQLDescription("Signs data using a specific key")]
    public async Task<SignatureResult> SignData(
        SignDataInput input,
        [Service] IKeyManagementService keyManagementService)
    {
        var request = new SignDataRequest
        {
            KeyId = input.KeyId,
            DataHex = input.DataHex,
            BlockchainType = input.BlockchainType,
            SignatureFormat = input.SignatureFormat
        };

        return await keyManagementService.SignDataAsync(request, CancellationToken.None);
    }

    /// <summary>
    /// Verifies a signature.
    /// </summary>
    /// <param name="input">The verification input.</param>
    /// <param name="keyManagementService">The key management service.</param>
    /// <returns>The verification result.</returns>
    [Authorize(Roles = ["Admin", "KeyManager", "User"])]
    [GraphQLDescription("Verifies a signature")]
    public async Task<VerificationResult> VerifySignature(
        VerifySignatureInput input,
        [Service] IKeyManagementService keyManagementService)
    {
        var request = new VerifySignatureRequest
        {
            KeyId = input.KeyId,
            DataHex = input.DataHex,
            SignatureHex = input.SignatureHex,
            BlockchainType = input.BlockchainType
        };

        return await keyManagementService.VerifySignatureAsync(request, CancellationToken.None);
    }

    /// <summary>
    /// Rotates a key.
    /// </summary>
    /// <param name="input">The rotation input.</param>
    /// <param name="keyManagementService">The key management service.</param>
    /// <returns>The new key metadata.</returns>
    [Authorize(Roles = ["Admin", "KeyManager"])]
    [GraphQLDescription("Rotates a key to a new version")]
    public async Task<KeyMetadata> RotateKey(
        RotateKeyInput input,
        [Service] IKeyManagementService keyManagementService)
    {
        var request = new RotateKeyRequest
        {
            KeyId = input.KeyId,
            BlockchainType = input.BlockchainType,
            RetainOldKey = input.RetainOldKey
        };

        return await keyManagementService.RotateKeyAsync(request, CancellationToken.None);
    }

    /// <summary>
    /// Deletes a key.
    /// </summary>
    /// <param name="keyId">The key identifier.</param>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <param name="keyManagementService">The key management service.</param>
    /// <returns>Success status.</returns>
    [Authorize(Roles = ["Admin"])]
    [GraphQLDescription("Deletes a key permanently")]
    public async Task<bool> DeleteKey(
        string keyId,
        BlockchainType blockchainType,
        [Service] IKeyManagementService keyManagementService)
    {
        await keyManagementService.DeleteKeyAsync(keyId, blockchainType, CancellationToken.None);
        return true;
    }
}

/// <summary>
/// Input for key generation.
/// </summary>
public class GenerateKeyInput
{
    /// <summary>
    /// Gets or sets the key identifier.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the key type.
    /// </summary>
    public string KeyType { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the key usage.
    /// </summary>
    public string KeyUsage { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }
    
    /// <summary>
    /// Gets or sets whether the key is exportable.
    /// </summary>
    public bool Exportable { get; set; }
    
    /// <summary>
    /// Gets or sets the attestation challenge.
    /// </summary>
    public string? AttestationChallenge { get; set; }
    
    /// <summary>
    /// Gets or sets the tags.
    /// </summary>
    public Dictionary<string, string>? Tags { get; set; }
}

/// <summary>
/// Input for signing data.
/// </summary>
public class SignDataInput
{
    /// <summary>
    /// Gets or sets the key identifier.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the data to sign (hex).
    /// </summary>
    public string DataHex { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }
    
    /// <summary>
    /// Gets or sets the signature format.
    /// </summary>
    public string? SignatureFormat { get; set; }
}

/// <summary>
/// Input for signature verification.
/// </summary>
public class VerifySignatureInput
{
    /// <summary>
    /// Gets or sets the key identifier.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the data (hex).
    /// </summary>
    public string DataHex { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the signature (hex).
    /// </summary>
    public string SignatureHex { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }
}

/// <summary>
/// Input for key rotation.
/// </summary>
public class RotateKeyInput
{
    /// <summary>
    /// Gets or sets the key identifier.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }
    
    /// <summary>
    /// Gets or sets whether to retain the old key.
    /// </summary>
    public bool RetainOldKey { get; set; }
}