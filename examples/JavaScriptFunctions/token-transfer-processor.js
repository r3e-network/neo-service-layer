/**
 * Token Transfer Processor
 *
 * This function processes token transfer events from the Neo N3 blockchain.
 * It demonstrates how to use user secrets, SGX secure functions, and GAS accounting.
 *
 * Required secrets:
 * - API_KEY: API key for an external service
 * - WEBHOOK_URL: URL to send notifications
 */

function main(input) {
    // Log the start of execution (uses SGX secure logging)
    log("Processing token transfer event");

    // Track GAS usage for this operation
    gasAccounting.useGas(10);

    // Validate input
    if (!input || !input.from || !input.to || !input.amount || !input.asset) {
        throw new Error("Invalid input: missing required fields");
    }

    // Get user secrets
    const apiKey = getSecret("API_KEY");
    const webhookUrl = getSecret("WEBHOOK_URL");

    if (!apiKey || !webhookUrl) {
        throw new Error("Missing required secrets: API_KEY and WEBHOOK_URL");
    }

    // Process the token transfer
    const result = processTransfer(input, apiKey, webhookUrl);

    // Generate a signature for the result using SGX
    const resultJson = JSON.stringify(result);
    const signature = sgx.signData(resultJson);

    // Return the result with the signature
    return {
        result: result,
        signature: signature,
        gas_used: gasAccounting.getGasUsed(),
        timestamp: new Date().toISOString(),
        mrenclave: sgx.getMrEnclave()
    };
}

/**
 * Process a token transfer event
 * @param {Object} transfer - The transfer event data
 * @param {string} apiKey - API key for external service
 * @param {string} webhookUrl - Webhook URL for notifications
 * @returns {Object} The processing result
 */
function processTransfer(transfer, apiKey, webhookUrl) {
    // Track GAS usage for this operation
    gasAccounting.useGas(50);

    // Calculate the USD value of the transfer (example)
    const usdValue = calculateUsdValue(transfer.asset, transfer.amount);

    // Generate a unique transaction ID using SGX random bytes
    const randomBytes = sgx.getRandomBytes(16);
    const transactionId = `tx_${randomBytes}`;

    // Create a notification payload
    const notification = {
        type: "token_transfer",
        id: transactionId,
        from: transfer.from,
        to: transfer.to,
        asset: transfer.asset,
        amount: transfer.amount,
        usd_value: usdValue,
        timestamp: new Date().toISOString()
    };

    // Send the notification to the webhook URL using the API key for authentication
    try {
        // Use the SGX secure HTTP client to make the request
        // This ensures the API key remains protected within the enclave
        const response = sgx.httpRequest({
            method: 'POST',
            url: webhookUrl,
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${apiKey}`,
                'X-Transaction-ID': transactionId
            },
            body: JSON.stringify(notification)
        });

        log(`Notification sent to ${webhookUrl}, status: ${response.status}`);

        if (response.status < 200 || response.status >= 300) {
            log(`Warning: Webhook returned non-success status: ${response.status}`);
        }
    } catch (error) {
        log(`Error sending notification: ${error.message}`);
        throw new Error(`Failed to send notification: ${error.message}`);
    }

    // Return the processing result
    return {
        transaction_id: transactionId,
        status: "processed",
        usd_value: usdValue,
        notification_sent: true
    };
}

/**
 * Calculate the USD value of a token amount
 * @param {string} asset - The asset hash
 * @param {string} amount - The token amount
 * @returns {number} The USD value
 */
function calculateUsdValue(asset, amount) {
    // Track GAS usage for this operation
    gasAccounting.useGas(30);

    // Fetch the current price from the oracle
    try {
        // Use the SGX secure HTTP client to fetch price data
        const response = sgx.httpRequest({
            method: 'GET',
            url: 'https://api.neooracle.io/v1/prices',
            headers: {
                'Authorization': `Bearer ${apiKey}`
            }
        });

        if (response.status !== 200) {
            throw new Error(`Oracle API returned status: ${response.status}`);
        }

        // Parse the price data
        const priceData = JSON.parse(response.body);

        // Create a map of asset hash to price
        const prices = {};
        for (const item of priceData.prices) {
            prices[item.asset_hash.toLowerCase()] = item.usd_price;
        }

        // If the asset isn't found in the oracle data, use fallback prices
        if (!prices[asset.toLowerCase()]) {
            log(`Warning: Price not found for asset ${asset}, using fallback prices`);

            // Fallback prices for common assets
            const fallbackPrices = {
                "0xd2a4cff31913016155e38e474a2c06d08be276cf": 50.0, // NEO
                "0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5": 0.5,  // GAS
                "0x8c23f196d8a1bfd103a9dcb1f9ccf0c611377d3b": 1.0   // USDT
            };

            // Use the fallback price if available, otherwise default to 0
            return parseFloat(amount) * (fallbackPrices[asset.toLowerCase()] || 0);
        }

        // Get the price for the asset
        const price = prices[asset.toLowerCase()] || 0;

        // Calculate the USD value
        return parseFloat(amount) * price;
    } catch (error) {
        log(`Error fetching price data: ${error.message}`);

        // Fallback prices for common assets
        const fallbackPrices = {
            "0xd2a4cff31913016155e38e474a2c06d08be276cf": 50.0, // NEO
            "0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5": 0.5,  // GAS
            "0x8c23f196d8a1bfd103a9dcb1f9ccf0c611377d3b": 1.0   // USDT
        };

        // Use the fallback price if available, otherwise default to 0
        const price = fallbackPrices[asset.toLowerCase()] || 0;

        // Calculate the USD value
        return parseFloat(amount) * price;
    }


}
