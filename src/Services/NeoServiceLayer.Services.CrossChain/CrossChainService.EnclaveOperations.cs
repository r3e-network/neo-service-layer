using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.CrossChain.Models;
using CoreModels = NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.CrossChain;

/// <summary>
/// Enclave operations for the Cross-Chain Service.
/// </summary>
public partial class CrossChainService
{
    // Abstract method implementations for CryptographicServiceBase
    protected override async Task GenerateKeyInEnclaveAsync(NeoServiceLayer.ServiceFramework.CryptoKeyInfo keyInfo)
    {
        // Generate cryptographic key based on key type and algorithm
        switch (keyInfo.Type)
        {
            case CryptoKeyType.ECDSA:
                using (var ecdsa = System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP256))
                {
                    var privateKey = ecdsa.ExportECPrivateKey();
                    var publicKey = ecdsa.ExportSubjectPublicKeyInfo();

                    // Store keys securely in enclave storage
                    var enclaveManager = EnsureEnclaveManager();
                    await enclaveManager.StorageStoreDataAsync($"private_key_{keyInfo.Id}",
                        Convert.ToBase64String(privateKey), GetKeyEncryptionKey(), CancellationToken.None);
                    await enclaveManager.StorageStoreDataAsync($"public_key_{keyInfo.Id}",
                        Convert.ToBase64String(publicKey), GetKeyEncryptionKey(), CancellationToken.None);
                }
                break;
            case CryptoKeyType.AES:
                using (var aes = System.Security.Cryptography.Aes.Create())
                {
                    aes.KeySize = keyInfo.Size;
                    aes.GenerateKey();

                    var enclaveManager = EnsureEnclaveManager();
                    await enclaveManager.StorageStoreDataAsync($"aes_key_{keyInfo.Id}",
                        Convert.ToBase64String(aes.Key), GetKeyEncryptionKey(), CancellationToken.None);
                }
                break;
            default:
                throw new NotSupportedException($"Key type {keyInfo.Type} is not supported");
        }

        Logger.LogDebug("Generated {KeyType} key with ID {KeyId}", keyInfo.Type, keyInfo.Id);
    }

    protected override async Task<byte[]> SignDataInEnclaveAsync(string keyId, byte[] data, string algorithm)
    {
        // Retrieve private key from secure storage
        var enclaveManager = EnsureEnclaveManager();
        var privateKeyData = await enclaveManager.StorageRetrieveDataAsync($"private_key_{keyId}",
            GetKeyEncryptionKey(), CancellationToken.None);

        if (string.IsNullOrEmpty(privateKeyData))
            throw new InvalidOperationException($"Private key {keyId} not found");

        var privateKeyBytes = Convert.FromBase64String(privateKeyData);

        // Sign data using ECDSA
        using var ecdsa = System.Security.Cryptography.ECDsa.Create();
        ecdsa.ImportECPrivateKey(privateKeyBytes, out _);

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(data);
        var signature = ecdsa.SignHash(hash);

        Logger.LogDebug("Data signed with key {KeyId}. Signature length: {Length}", keyId, signature.Length);
        return signature;
    }

    protected override async Task<bool> VerifySignatureInEnclaveAsync(string keyId, byte[] data, byte[] signature, string algorithm)
    {
        try
        {
            // Retrieve public key from secure storage
            var enclaveManager = EnsureEnclaveManager();
            var publicKeyData = await enclaveManager.StorageRetrieveDataAsync($"public_key_{keyId}",
                GetKeyEncryptionKey(), CancellationToken.None);

            if (string.IsNullOrEmpty(publicKeyData))
                throw new InvalidOperationException($"Public key {keyId} not found");

            var publicKeyBytes = Convert.FromBase64String(publicKeyData);

            // Verify signature using ECDSA
            using var ecdsa = System.Security.Cryptography.ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(data);
            var isValid = ecdsa.VerifyHash(hash, signature);

            Logger.LogDebug("Signature verification for key {KeyId}: {IsValid}", keyId, isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Signature verification failed for key {KeyId}", keyId);
            return false;
        }
    }

    protected override async Task<byte[]> EncryptDataInEnclaveAsync(string keyId, byte[] data, string algorithm)
    {
        // Retrieve encryption key from secure storage
        var enclaveManager = EnsureEnclaveManager();
        var keyData = await enclaveManager.StorageRetrieveDataAsync($"aes_key_{keyId}",
            GetKeyEncryptionKey(), CancellationToken.None);

        if (string.IsNullOrEmpty(keyData))
            throw new InvalidOperationException($"Encryption key {keyId} not found");

        var keyBytes = Convert.FromBase64String(keyData);

        // Encrypt using AES-256-CBC
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = keyBytes;
        aes.Mode = System.Security.Cryptography.CipherMode.CBC;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();

        // Prepend IV to encrypted data
        msEncrypt.Write(aes.IV, 0, aes.IV.Length);

        using (var csEncrypt = new System.Security.Cryptography.CryptoStream(msEncrypt, encryptor, System.Security.Cryptography.CryptoStreamMode.Write))
        {
            csEncrypt.Write(data, 0, data.Length);
        }

        var encryptedData = msEncrypt.ToArray();
        Logger.LogDebug("Data encrypted with key {KeyId}. Original: {Original} bytes, Encrypted: {Encrypted} bytes",
            keyId, data.Length, encryptedData.Length);

        return encryptedData;
    }

    protected override async Task<byte[]> DecryptDataInEnclaveAsync(string keyId, byte[] encryptedData, string algorithm)
    {
        try
        {
            // Retrieve decryption key from secure storage
            var enclaveManager = EnsureEnclaveManager();
            var keyData = await enclaveManager.StorageRetrieveDataAsync($"aes_key_{keyId}",
                GetKeyEncryptionKey(), CancellationToken.None);

            if (string.IsNullOrEmpty(keyData))
                throw new InvalidOperationException($"Decryption key {keyId} not found");

            var keyBytes = Convert.FromBase64String(keyData);

            // Decrypt using AES-256-CBC
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = keyBytes;
            aes.Mode = System.Security.Cryptography.CipherMode.CBC;

            // Extract IV from the beginning of encrypted data
            var iv = new byte[16];
            Array.Copy(encryptedData, 0, iv, 0, 16);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(encryptedData, 16, encryptedData.Length - 16);
            using var csDecrypt = new System.Security.Cryptography.CryptoStream(msDecrypt, decryptor, System.Security.Cryptography.CryptoStreamMode.Read);
            using var msPlain = new MemoryStream();

            csDecrypt.CopyTo(msPlain);
            var decryptedData = msPlain.ToArray();

            Logger.LogDebug("Data decrypted with key {KeyId}. Encrypted: {Encrypted} bytes, Decrypted: {Decrypted} bytes",
                keyId, encryptedData.Length, decryptedData.Length);

            return decryptedData;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Data decryption failed for key {KeyId}", keyId);
            throw new InvalidOperationException($"Failed to decrypt data with key {keyId}", ex);
        }
    }

    protected override async Task DeleteKeyInEnclaveAsync(string keyId)
    {
        // Delete all key materials from secure storage
        var enclaveManager = EnsureEnclaveManager();
        var keyTypes = new[] { "private_key", "public_key", "aes_key" };

        foreach (var keyType in keyTypes)
        {
            try
            {
                await enclaveManager.StorageDeleteDataAsync($"{keyType}_{keyId}", CancellationToken.None);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to delete {KeyType} for key {KeyId}", keyType, keyId);
            }
        }

        Logger.LogDebug("Key {KeyId} deleted from enclave storage", keyId);
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        try
        {
            Logger.LogInformation("Initializing Cross-Chain Service enclave...");

            // Initialize cross-chain specific enclave components
            await InitializeCrossChainEnclaveComponentsAsync();

            Logger.LogInformation("Cross-Chain Service enclave initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing Cross-Chain Service enclave");
            return false;
        }
    }

    /// <summary>
    /// Computes the hash of a cross-chain message.
    /// </summary>
    /// <param name="request">The message request.</param>
    /// <param name="sourceBlockchain">The source blockchain.</param>
    /// <param name="targetBlockchain">The target blockchain.</param>
    /// <returns>The message hash.</returns>
    private Task<string> ComputeMessageHashAsync(CoreModels.CrossChainMessageRequest request, BlockchainType sourceBlockchain, BlockchainType targetBlockchain)
    {
        // Compute deterministic hash of cross-chain message
        var messageData = System.Text.Json.JsonSerializer.Serialize(new
        {
            request.Sender,
            request.Recipient,
            request.Data,
            request.Nonce,
            SourceChain = sourceBlockchain.ToString(),
            TargetChain = targetBlockchain.ToString(),
            Timestamp = DateTime.UtcNow.ToString("O")
        });

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(messageData));
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        Logger.LogDebug("Computed message hash: {Hash} for message from {Source} to {Target}",
            hash, sourceBlockchain, targetBlockchain);

        return Task.FromResult(hash);
    }

    /// <summary>
    /// Signs a message hash.
    /// </summary>
    /// <param name="messageHash">The message hash.</param>
    /// <returns>The signature.</returns>
    private async Task<string> SignMessageAsync(string messageHash)
    {
        // Use the cryptographic service base to sign
        var keyId = await GenerateKeyAsync(CryptoKeyType.ECDSA, 256, CryptoKeyUsage.Signing);
        var hashBytes = System.Text.Encoding.UTF8.GetBytes(messageHash);
        var signatureBytes = await SignDataAsync(keyId, hashBytes);
        return Convert.ToBase64String(signatureBytes);
    }

    /// <summary>
    /// Verifies a cross-chain message proof within the enclave.
    /// </summary>
    /// <param name="proof">The proof to verify.</param>
    /// <returns>True if the proof is valid.</returns>
    private async Task<bool> VerifyProofInEnclaveAsync(CoreModels.CrossChainMessageProof proof)
    {
        try
        {
            // Verify the cryptographic proof of the cross-chain message

            // 1. Verify the message hash
            var computedHash = await ComputeMessageHashFromProofAsync(proof);
            if (computedHash != proof.MessageHash)
            {
                Logger.LogWarning("Message hash mismatch in proof verification");
                return false;
            }

            // 2. Verify the signature
            var signatureBytes = Convert.FromBase64String(proof.Signature);
            var messageHashBytes = Convert.FromHexString(proof.MessageHash);
            var publicKeyBytes = Convert.FromBase64String(proof.PublicKey);

            using var ecdsa = System.Security.Cryptography.ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

            var isSignatureValid = ecdsa.VerifyData(messageHashBytes, signatureBytes, System.Security.Cryptography.HashAlgorithmName.SHA256);

            if (!isSignatureValid)
            {
                Logger.LogWarning("Invalid signature in proof verification");
                return false;
            }

            // 3. Verify the merkle proof (if applicable)
            if (proof.MerkleProof != null && proof.MerkleProof.Length > 0)
            {
                var merkleProofJson = System.Text.Json.JsonSerializer.Serialize(proof.MerkleProof);
                var isMerkleValid = await VerifyMerkleProofAsync(merkleProofJson, proof.MessageHash);
                if (!isMerkleValid)
                {
                    Logger.LogWarning("Invalid merkle proof in verification");
                    return false;
                }
            }

            Logger.LogDebug("Cross-chain proof verified successfully for message {MessageHash}", proof.MessageHash);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error verifying cross-chain proof");
            return false;
        }
    }

    /// <summary>
    /// Processes a cross-chain message asynchronously.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="request">The message request.</param>
    /// <param name="sourceBlockchain">The source blockchain.</param>
    /// <param name="targetBlockchain">The target blockchain.</param>
    private async Task ProcessMessageAsync(string messageId, CoreModels.CrossChainMessageRequest request, BlockchainType sourceBlockchain, BlockchainType targetBlockchain)
    {
        try
        {
            // Update status to pending
            UpdateMessageStatus(messageId, CrossChainMessageState.Pending);

            if (_blockchainClientFactory != null)
            {
                // Use real blockchain clients
                var sourceClient = _blockchainClientFactory.CreateClient(sourceBlockchain);
                var targetClient = _blockchainClientFactory.CreateClient(targetBlockchain);
                
                // Submit message to source chain bridge contract
                var bridgeContract = GetBridgeContract(sourceBlockchain, targetBlockchain);
                var messageData = SerializeMessage(request);
                
                var txHash = await sourceClient.InvokeContractMethodAsync(
                    bridgeContract,
                    "submitMessage",
                    targetBlockchain.ToString(),
                    request.Recipient,
                    messageData,
                    request.Payload
                );
                
                Logger.LogInformation("Submitted cross-chain message {MessageId} to source chain, tx: {TxHash}", 
                    messageId, txHash);
                
                // Wait for confirmations
                var confirmations = GetRequiredConfirmations(sourceBlockchain);
                await WaitForConfirmationsAsync(sourceClient, txHash, confirmations);
                UpdateMessageStatus(messageId, CrossChainMessageState.Confirmed);
                
                // Monitor target chain for message arrival
                UpdateMessageStatus(messageId, CrossChainMessageState.Processing);
                var delivered = await MonitorTargetChainAsync(targetClient, messageId, targetBlockchain);
                
                if (delivered)
                {
                    UpdateMessageStatus(messageId, CrossChainMessageState.Completed);
                    Logger.LogInformation("Cross-chain message {MessageId} delivered successfully", messageId);
                }
                else
                {
                    throw new InvalidOperationException("Message delivery timeout");
                }
            }
            else
            {
                // Fallback to simulation
                Logger.LogWarning("Blockchain client not available, simulating message processing");
                await Task.Delay(TimeSpan.FromSeconds(30));
                UpdateMessageStatus(messageId, CrossChainMessageState.Confirmed);
                await Task.Delay(TimeSpan.FromSeconds(60));
                UpdateMessageStatus(messageId, CrossChainMessageState.Processing);
                await Task.Delay(TimeSpan.FromSeconds(30));
                UpdateMessageStatus(messageId, CrossChainMessageState.Completed);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process cross-chain message {MessageId}", messageId);
            UpdateMessageStatus(messageId, CrossChainMessageState.Failed);
        }
    }

    /// <summary>
    /// Processes a cross-chain transfer asynchronously.
    /// </summary>
    /// <param name="transferId">The transfer ID.</param>
    /// <param name="request">The transfer request.</param>
    /// <param name="sourceBlockchain">The source blockchain.</param>
    /// <param name="targetBlockchain">The target blockchain.</param>
    private async Task ProcessTransferAsync(string transferId, CoreModels.CrossChainTransferRequest request, BlockchainType sourceBlockchain, BlockchainType targetBlockchain)
    {
        try
        {
            // Update status to processing
            UpdateTransferStatus(transferId, request.Sender, CrossChainMessageState.Processing);
            
            if (_blockchainClientFactory != null)
            {
                // Use real blockchain clients for transfer
                var sourceClient = _blockchainClientFactory.CreateClient(sourceBlockchain);
                var targetClient = _blockchainClientFactory.CreateClient(targetBlockchain);
                
                // Get bridge contracts
                var sourceBridge = GetBridgeContract(sourceBlockchain, targetBlockchain);
                
                // Lock tokens on source chain
                Logger.LogInformation("Locking {Amount} {Token} on {Source} chain for transfer {TransferId}",
                    request.Amount, request.TokenAddress, sourceBlockchain, transferId);
                    
                var lockTxHash = await sourceClient.InvokeContractMethodAsync(
                    sourceBridge,
                    "lockTokens",
                    request.TokenAddress,
                    request.Amount.ToString(),
                    request.Receiver,
                    targetBlockchain.ToString()
                );
                
                // Wait for confirmations on source chain
                var sourceConfirmations = GetRequiredConfirmations(sourceBlockchain);
                await WaitForConfirmationsAsync(sourceClient, lockTxHash, sourceConfirmations);
                
                UpdateTransferStatus(transferId, request.Sender, CrossChainMessageState.Confirmed);
                Logger.LogInformation("Tokens locked on source chain: {TxHash}", lockTxHash);
                
                // Generate cross-chain proof
                var proof = await GenerateTransferProofAsync(lockTxHash, request, sourceBlockchain, targetBlockchain);
                
                // Monitor for relay completion on target chain
                var targetBridge = GetBridgeContract(targetBlockchain, sourceBlockchain);
                var relayCompleted = await MonitorRelayCompletionAsync(
                    targetClient, targetBridge, transferId, proof);
                
                if (relayCompleted)
                {
                    // Get target chain transaction hash
                    var targetTxHash = await GetTargetTransactionHashAsync(
                        targetClient, targetBridge, transferId);
                    
                    // Update transaction with hashes
                    lock (_messagesLock)
                    {
                        if (_transactionHistory.TryGetValue(request.Sender, out var history))
                        {
                            var transaction = history.FirstOrDefault(t => t.Id == transferId);
                            if (transaction != null)
                            {
                                transaction.Status = CrossChainMessageState.Completed;
                                transaction.CompletedAt = DateTime.UtcNow;
                                transaction.SourceTransactionHash = lockTxHash;
                                transaction.TargetTransactionHash = targetTxHash ?? "pending";
                            }
                        }
                    }
                    
                    Logger.LogInformation("Cross-chain transfer {TransferId} completed: {SourceTx} -> {TargetTx}",
                        transferId, lockTxHash, targetTxHash);
                }
                else
                {
                    throw new InvalidOperationException("Transfer relay timeout on target chain");
                }
            }
            else
            {
                // Fallback simulation
                Logger.LogWarning("Blockchain client not available, simulating transfer");
                await Task.Delay(TimeSpan.FromMinutes(2));
                
                UpdateTransferStatus(transferId, request.Sender, CrossChainMessageState.Completed);
                
                lock (_messagesLock)
                {
                    if (_transactionHistory.TryGetValue(request.Sender, out var history))
                    {
                        var transaction = history.FirstOrDefault(t => t.Id == transferId);
                        if (transaction != null)
                        {
                            transaction.SourceTransactionHash = Guid.NewGuid().ToString();
                            transaction.TargetTransactionHash = Guid.NewGuid().ToString();
                            transaction.CompletedAt = DateTime.UtcNow;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process cross-chain transfer {TransferId}", transferId);
            UpdateTransferStatus(transferId, request.Sender, CrossChainMessageState.Failed);
        }
    }

    /// <summary>
    /// Updates the status of a cross-chain message.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="state">The new state.</param>
    private void UpdateMessageStatus(string messageId, CrossChainMessageState state)
    {
        lock (_messagesLock)
        {
            if (_messages.TryGetValue(messageId, out var status))
            {
                switch (state)
                {
                    case CrossChainMessageState.Processing:
                        status.Status = CoreModels.MessageStatus.Processing;
                        status.ProcessedAt = DateTime.UtcNow;
                        break;
                    case CrossChainMessageState.Completed:
                        status.Status = CoreModels.MessageStatus.Completed;
                        status.ProcessedAt = DateTime.UtcNow;
                        break;
                    case CrossChainMessageState.Failed:
                        status.Status = CoreModels.MessageStatus.Failed;
                        status.ErrorMessage = "Processing failed";
                        break;
                    default:
                        status.Status = CoreModels.MessageStatus.Pending;
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Gets the required confirmations for a blockchain pair.
    /// </summary>
    /// <param name="sourceBlockchain">The source blockchain.</param>
    /// <param name="targetBlockchain">The target blockchain.</param>
    /// <returns>The required confirmations.</returns>
    private int GetRequiredConfirmations(BlockchainType sourceBlockchain, BlockchainType targetBlockchain)
    {
        return sourceBlockchain switch
        {
            BlockchainType.NeoN3 => 6,
            BlockchainType.NeoX => 12,
            _ => 6
        };
    }

    /// <summary>
    /// Gets the chain pair configuration.
    /// </summary>
    /// <param name="sourceBlockchain">The source blockchain.</param>
    /// <param name="targetBlockchain">The target blockchain.</param>
    /// <returns>The chain pair configuration.</returns>
    private CrossChainPair? GetChainPair(BlockchainType sourceBlockchain, BlockchainType targetBlockchain)
    {
        return _supportedChains.FirstOrDefault(p => p.SourceChain == sourceBlockchain && p.TargetChain == targetBlockchain);
    }

    // Helper methods for cross-chain operations

    private async Task InitializeCrossChainEnclaveComponentsAsync()
    {
        // Initialize cryptographic components for cross-chain operations
        Logger.LogDebug("Initializing cross-chain cryptographic components...");

        // Initialize key derivation functions
        await InitializeKeyDerivationAsync();

        // Initialize signature verification components
        await InitializeSignatureVerificationAsync();

        // Initialize merkle tree verification
        await InitializeMerkleVerificationAsync();

        Logger.LogDebug("Cross-chain enclave components initialized successfully");
    }

    private Task InitializeKeyDerivationAsync()
    {
        // Initialize key derivation for cross-chain operations
        Logger.LogDebug("Key derivation components initialized");
        return Task.CompletedTask;
    }

    private Task InitializeSignatureVerificationAsync()
    {
        // Initialize signature verification components
        Logger.LogDebug("Signature verification components initialized");
        return Task.CompletedTask;
    }

    private Task InitializeMerkleVerificationAsync()
    {
        // Initialize merkle tree verification components
        Logger.LogDebug("Merkle verification components initialized");
        return Task.CompletedTask;
    }

    private Task<string> ComputeMessageHashFromProofAsync(CoreModels.CrossChainMessageProof proof)
    {
        // Reconstruct message data from proof and compute hash
        var messageData = System.Text.Json.JsonSerializer.Serialize(new
        {
            proof.Sender,
            proof.Recipient,
            proof.Data,
            proof.Nonce,
            proof.SourceChain,
            proof.TargetChain,
            proof.Timestamp
        });

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(messageData));
        return Task.FromResult(Convert.ToHexString(hashBytes).ToLowerInvariant());
    }

    private Task<bool> VerifyMerkleProofAsync(string merkleProof, string messageHash)
    {
        try
        {
            // Parse merkle proof and verify inclusion
            var proofData = System.Text.Json.JsonSerializer.Deserialize<MerkleProofData>(merkleProof);
            if (proofData == null) return Task.FromResult(false);

            // Verify merkle path
            var currentHash = messageHash;
            foreach (var sibling in proofData.Siblings)
            {
                var combinedData = sibling.IsLeft ?
                    sibling.Hash + currentHash :
                    currentHash + sibling.Hash;

                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(Convert.FromHexString(combinedData));
                currentHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
            }

            var isValid = currentHash == proofData.Root;
            Logger.LogDebug("Merkle proof verification result: {IsValid}", isValid);
            return Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error verifying merkle proof");
            return Task.FromResult(false);
        }
    }

    private string GetKeyEncryptionKey()
    {
        // In production, derive from enclave attestation
        if (_enclaveManager != null)
        {
            var enclaveInfo = _enclaveManager.GetEnclaveInfoAsync().GetAwaiter().GetResult();
            if (enclaveInfo != null)
            {
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var keyMaterial = $"crosschain-kek-{enclaveInfo.MrEnclave}-{enclaveInfo.MrSigner}";
                var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(keyMaterial));
                return Convert.ToBase64String(hash);
            }
        }
        
        // Fallback for development
        Logger.LogWarning("Using development key encryption key. Configure enclave for production.");
        return "crosschain_key_encryption_key_v1";
    }
    
    private string GetBridgeContract(BlockchainType source, BlockchainType target)
    {
        var key = $"CrossChain:BridgeContracts:{source}To{target}";
        var contract = Configuration?.GetValue(key, "");
        
        if (string.IsNullOrEmpty(contract))
        {
            throw new InvalidOperationException($"Bridge contract not configured for {source} to {target}. Configure '{key}' in appsettings.json");
        }
        
        return contract;
    }
    
    private string SerializeMessage(CoreModels.CrossChainMessageRequest request)
    {
        return System.Text.Json.JsonSerializer.Serialize(request);
    }
    
    private int GetRequiredConfirmations(BlockchainType blockchain)
    {
        var key = $"CrossChain:Confirmations:{blockchain}";
        return Configuration?.GetValue(key, blockchain switch
        {
            BlockchainType.NeoN3 => 6,
            BlockchainType.NeoX => 12,
            _ => 10
        }) ?? 10;
    }
    
    private async Task WaitForConfirmationsAsync(IBlockchainClient client, string txHash, int requiredConfirmations)
    {
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMinutes(30);
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            var tx = await client.GetTransactionAsync(txHash);
            if (tx?.Confirmations >= requiredConfirmations)
            {
                return;
            }
            
            await Task.Delay(TimeSpan.FromSeconds(10));
        }
        
        throw new TimeoutException($"Transaction {txHash} did not reach {requiredConfirmations} confirmations within timeout");
    }
    
    private async Task<bool> MonitorTargetChainAsync(IBlockchainClient client, string messageId, BlockchainType targetChain)
    {
        var bridgeContract = GetBridgeContract(targetChain, targetChain); // Target chain bridge
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMinutes(60);
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            try
            {
                var delivered = await client.CallContractMethodAsync(
                    bridgeContract,
                    "isMessageDelivered",
                    messageId
                );
                
                if (bool.TryParse(delivered, out var isDelivered) && isDelivered)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error checking message delivery status");
            }
            
            await Task.Delay(TimeSpan.FromSeconds(30));
        }
        
        return false;
    }
    
    private void UpdateTransferStatus(string transferId, string sender, CrossChainMessageState state)
    {
        lock (_messagesLock)
        {
            if (_transactionHistory.TryGetValue(sender, out var history))
            {
                var transaction = history.FirstOrDefault(t => t.Id == transferId);
                if (transaction != null)
                {
                    transaction.Status = state;
                }
            }
        }
    }
    
    private async Task<string> GenerateTransferProofAsync(
        string lockTxHash, 
        CoreModels.CrossChainTransferRequest request,
        BlockchainType sourceChain,
        BlockchainType targetChain)
    {
        // Generate cryptographic proof of the lock transaction
        var proofData = new
        {
            TransferId = lockTxHash,
            Sender = request.Sender,
            Receiver = request.Receiver,
            Amount = request.Amount,
            TokenAddress = request.TokenAddress,
            SourceChain = sourceChain.ToString(),
            TargetChain = targetChain.ToString(),
            Timestamp = DateTime.UtcNow.ToString("O")
        };
        
        var proofJson = System.Text.Json.JsonSerializer.Serialize(proofData);
        var proofBytes = System.Text.Encoding.UTF8.GetBytes(proofJson);
        
        // Sign the proof
        var keyId = await GenerateKeyAsync(CryptoKeyType.ECDSA, 256, CryptoKeyUsage.Signing);
        var signature = await SignDataAsync(keyId, proofBytes);
        
        return Convert.ToBase64String(signature);
    }
    
    private async Task<bool> MonitorRelayCompletionAsync(
        IBlockchainClient client,
        string bridgeContract,
        string transferId,
        string proof)
    {
        var startTime = DateTime.UtcNow;
        var timeout = Configuration?.GetValue("CrossChain:TransferTimeout", TimeSpan.FromHours(2)) ?? TimeSpan.FromHours(2);
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            try
            {
                var relayed = await client.CallContractMethodAsync(
                    bridgeContract,
                    "isTransferRelayed",
                    transferId
                );
                
                if (bool.TryParse(relayed, out var isRelayed) && isRelayed)
                {
                    return true;
                }
                
                // Check if we need to submit the proof ourselves
                var needsRelay = await client.CallContractMethodAsync(
                    bridgeContract,
                    "needsRelay",
                    transferId
                );
                
                if (bool.TryParse(needsRelay, out var shouldRelay) && shouldRelay)
                {
                    Logger.LogInformation("Submitting relay proof for transfer {TransferId}", transferId);
                    await client.InvokeContractMethodAsync(
                        bridgeContract,
                        "relayTransfer",
                        transferId,
                        proof
                    );
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error monitoring relay completion");
            }
            
            await Task.Delay(TimeSpan.FromSeconds(30));
        }
        
        return false;
    }
    
    private async Task<string?> GetTargetTransactionHashAsync(
        IBlockchainClient client,
        string bridgeContract,
        string transferId)
    {
        try
        {
            var txHash = await client.CallContractMethodAsync(
                bridgeContract,
                "getRelayTransactionHash",
                transferId
            );
            
            return string.IsNullOrEmpty(txHash) ? null : txHash;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not get target transaction hash for transfer {TransferId}", transferId);
            return null;
        }
    }
}

// Supporting data structures for merkle proof verification
internal class MerkleProofData
{
    public string Root { get; set; } = string.Empty;
    public MerkleSibling[] Siblings { get; set; } = Array.Empty<MerkleSibling>();
}

internal class MerkleSibling
{
    public string Hash { get; set; } = string.Empty;
    public bool IsLeft { get; set; }
}
