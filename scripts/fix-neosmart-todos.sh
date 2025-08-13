#!/bin/bash

# Script to fix TODO items in NeoN3SmartContractManager.cs
# This script replaces commented-out RpcClient calls with active implementations

echo "Fixing TODO items in NeoN3SmartContractManager.cs..."

FILE="/home/ubuntu/neo-service-layer/src/Services/NeoServiceLayer.Services.SmartContracts.NeoN3/NeoN3SmartContractManager.cs"

# Backup the file
cp "$FILE" "$FILE.bak"

# Fix 1: Enable SendRawTransactionAsync calls
sed -i 's|// var txHash = await _rpcClient.SendRawTransactionAsync(signedTx); // TODO: Enable when RpcClient is available\n                var txHash = signedTx.Hash;|var txHash = await _rpcClient.SendRawTransactionAsync(signedTx).ConfigureAwait(false);|g' "$FILE"

# Fix 2: Enable GetBlockAsync calls for block number
sed -i 's|0 : 0; // TODO: Enable when RpcClient is available: (await _rpcClient.GetBlockAsync(applicationLog.BlockHash.ToString())).Index|(await _rpcClient.GetBlockAsync(applicationLog.BlockHash.ToString()).ConfigureAwait(false)).Index : 0|g' "$FILE"

# Fix 3: Enable InvokeScriptAsync
sed -i 's|// TODO: Enable when RPC client is available\n            // var result = await _rpcClient.InvokeScriptAsync(script);|var result = await _rpcClient.InvokeScriptAsync(script).ConfigureAwait(false);|g' "$FILE"

# Fix 4: Enable GetContractStateAsync
sed -i 's|// TODO: Enable when RPC client is available\n            // var contractState = await _rpcClient.GetContractStateAsync(contractHash);|var contractState = await _rpcClient.GetContractStateAsync(contractHash).ConfigureAwait(false);|g' "$FILE"

# Fix 5: Enable GetBlockCountAsync
sed -i 's|// TODO: Enable when RPC client is available\n                // var blockCount = await _rpcClient.GetBlockCountAsync();|var blockCount = await _rpcClient.GetBlockCountAsync().ConfigureAwait(false);|g' "$FILE"

# Fix 6: Enable GetBlockAsync in event monitoring
sed -i 's|// TODO: Enable when RPC client is available\n                    // var block = await _rpcClient.GetBlockAsync(blockIndex.ToString());|var block = await _rpcClient.GetBlockAsync(blockIndex.ToString()).ConfigureAwait(false);|g' "$FILE"

# Fix 7: Enable GetApplicationLogAsync
sed -i 's|// TODO: Enable when RPC client is available\n                            // var appLog = await _rpcClient.GetApplicationLogAsync(tx.Hash.ToString());|var appLog = await _rpcClient.GetApplicationLogAsync(tx.Hash.ToString()).ConfigureAwait(false);|g' "$FILE"

# Fix 8: Update ValidUntilBlock calculation
sed -i 's|ValidUntilBlock = 1000000 + 86400, // TODO: await _rpcClient.GetBlockCountAsync() + 86400 when RPC is available|ValidUntilBlock = await _rpcClient.GetBlockCountAsync().ConfigureAwait(false) + 86400,|g' "$FILE"

echo "TODO fixes completed. Backup saved as $FILE.bak"