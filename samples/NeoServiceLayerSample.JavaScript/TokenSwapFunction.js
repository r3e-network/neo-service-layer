// Token Swap Function for Neo Service Layer

// Order book - will be kept confidential within the enclave
let orderBook = {
  buy: [],  // Buy orders
  sell: [] // Sell orders
};

// Function to add a new order to the order book
function addOrder(input) {
  // Access user secrets
  const apiKey = SECRETS.api_key || "default-api-key";
  console.log(`Using API key: ${apiKey}`);
  
  // Create a new order
  const order = {
    id: generateOrderId(),
    tokenA: input.tokenA,
    tokenB: input.tokenB,
    amountA: input.amountA,
    amountB: input.amountB,
    sender: input.sender,
    timestamp: Date.now()
  };
  
  // Calculate the price
  const price = order.amountB / order.amountA;
  order.price = price;
  
  // Add the order to the order book
  if (order.tokenA < order.tokenB) {
    // This is a buy order
    order.type = "buy";
    orderBook.buy.push(order);
    // Sort buy orders by price (highest first)
    orderBook.buy.sort((a, b) => b.price - a.price);
  } else {
    // This is a sell order
    order.type = "sell";
    orderBook.sell.push(order);
    // Sort sell orders by price (lowest first)
    orderBook.sell.sort((a, b) => a.price - b.price);
  }
  
  // Try to match orders
  const matches = matchOrders();
  
  // Send the result back to the blockchain
  if (matches.length > 0) {
    // Send a callback transaction to the blockchain with the match results
    blockchain.callback(
      "0x1234567890abcdef1234567890abcdef12345678", // Contract hash
      "ReceiveSwapMatches", // Method name
      { 
        matches: matches,
        orderId: order.id
      }
    );
  }
  
  return { 
    success: true, 
    orderId: order.id,
    matches: matches
  };
}

// Function to match orders
function matchOrders() {
  let matches = [];
  
  // Continue matching as long as there are matching orders
  while (orderBook.buy.length > 0 && orderBook.sell.length > 0) {
    const buyOrder = orderBook.buy[0];
    const sellOrder = orderBook.sell[0];
    
    // Check if orders match (buy price >= sell price)
    if (buyOrder.price >= sellOrder.price) {
      // Calculate the quantity to swap
      const amountA = Math.min(buyOrder.amountA, sellOrder.amountA);
      const amountB = Math.min(buyOrder.amountB, sellOrder.amountB);
      
      // Execute the swap
      const match = {
        buyOrderId: buyOrder.id,
        sellOrderId: sellOrder.id,
        tokenA: buyOrder.tokenA,
        tokenB: buyOrder.tokenB,
        amountA: amountA,
        amountB: amountB,
        price: sellOrder.price,
        buyer: buyOrder.sender,
        seller: sellOrder.sender,
        timestamp: Date.now()
      };
      
      matches.push(match);
      
      // Update order quantities
      buyOrder.amountA -= amountA;
      buyOrder.amountB -= amountB;
      sellOrder.amountA -= amountA;
      sellOrder.amountB -= amountB;
      
      // Remove fulfilled orders
      if (buyOrder.amountA === 0 || buyOrder.amountB === 0) {
        orderBook.buy.shift();
      }
      if (sellOrder.amountA === 0 || sellOrder.amountB === 0) {
        orderBook.sell.shift();
      }
    } else {
      // No more matches possible
      break;
    }
  }
  
  return matches;
}

// Function to get the order book (only for the owner)
function getOrderBook(input) {
  // Check if the user is the owner
  if (input.sender !== "owner") {
    return { error: "Unauthorized" };
  }
  
  return orderBook;
}

// Function to get user orders
function getUserOrders(input) {
  const userOrders = {
    buy: orderBook.buy.filter(order => order.sender === input.sender),
    sell: orderBook.sell.filter(order => order.sender === input.sender)
  };
  
  return userOrders;
}

// Helper function to generate a unique order ID
function generateOrderId() {
  return "order_" + Date.now() + "_" + Math.random().toString(36).substring(2, 15);
}

// Main function that handles all contract operations
function main(input) {
  console.log(`Executing token swap function with input: ${JSON.stringify(input)}`);
  
  // Access user information
  console.log(`Function ID: ${functionId}`);
  console.log(`User ID: ${userId}`);
  
  // Check if the user has secrets
  if (SECRETS) {
    console.log(`User has ${Object.keys(SECRETS).length} secrets`);
  } else {
    console.log("User has no secrets");
  }
  
  switch (input.action) {
    case "addOrder":
      return addOrder(input);
    
    case "getOrderBook":
      return getOrderBook(input);
    
    case "getUserOrders":
      return getUserOrders(input);
    
    default:
      return { error: "Invalid action" };
  }
}
