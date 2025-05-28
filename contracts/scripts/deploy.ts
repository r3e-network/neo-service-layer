import { ethers } from "hardhat";
import { Contract } from "ethers";

interface DeployedContracts {
  serviceRegistry: Contract;
  randomnessConsumer: Contract;
  oracleConsumer: Contract;
  abstractAccountFactory: Contract;
  abstractAccountImplementation: Contract;
}

async function main() {
  console.log("🚀 Starting Neo Service Layer contracts deployment...");
  
  const [deployer] = await ethers.getSigners();
  console.log("📝 Deploying contracts with account:", deployer.address);
  
  const balance = await ethers.provider.getBalance(deployer.address);
  console.log("💰 Account balance:", ethers.formatEther(balance), "ETH");

  // Deploy ServiceRegistry
  console.log("\n📋 Deploying ServiceRegistry...");
  const ServiceRegistry = await ethers.getContractFactory("ServiceRegistry");
  const serviceRegistry = await ServiceRegistry.deploy();
  await serviceRegistry.waitForDeployment();
  const serviceRegistryAddress = await serviceRegistry.getAddress();
  console.log("✅ ServiceRegistry deployed to:", serviceRegistryAddress);

  // Register services in the registry
  console.log("\n🔧 Registering services...");
  
  // Register Randomness Service
  const randomnessServiceTx = await serviceRegistry.registerService(
    "RandomnessService",
    "1.0.0",
    deployer.address, // Placeholder address
    "https://neo-service-layer.com/randomness"
  );
  await randomnessServiceTx.wait();
  const randomnessServiceId = await serviceRegistry.serviceNameToId("RandomnessService");
  console.log("✅ Randomness Service registered with ID:", randomnessServiceId);

  // Register Oracle Service
  const oracleServiceTx = await serviceRegistry.registerService(
    "OracleService",
    "1.0.0",
    deployer.address, // Placeholder address
    "https://neo-service-layer.com/oracle"
  );
  await oracleServiceTx.wait();
  const oracleServiceId = await serviceRegistry.serviceNameToId("OracleService");
  console.log("✅ Oracle Service registered with ID:", oracleServiceId);

  // Register Abstract Account Service
  const abstractAccountServiceTx = await serviceRegistry.registerService(
    "AbstractAccountService",
    "1.0.0",
    deployer.address, // Placeholder address
    "https://neo-service-layer.com/abstract-account"
  );
  await abstractAccountServiceTx.wait();
  const abstractAccountServiceId = await serviceRegistry.serviceNameToId("AbstractAccountService");
  console.log("✅ Abstract Account Service registered with ID:", abstractAccountServiceId);

  // Deploy RandomnessConsumer
  console.log("\n🎲 Deploying RandomnessConsumer...");
  const RandomnessConsumer = await ethers.getContractFactory("RandomnessConsumer");
  const randomnessConsumer = await RandomnessConsumer.deploy(
    serviceRegistryAddress,
    randomnessServiceId
  );
  await randomnessConsumer.waitForDeployment();
  const randomnessConsumerAddress = await randomnessConsumer.getAddress();
  console.log("✅ RandomnessConsumer deployed to:", randomnessConsumerAddress);

  // Deploy OracleConsumer
  console.log("\n🔮 Deploying OracleConsumer...");
  const OracleConsumer = await ethers.getContractFactory("OracleConsumer");
  const oracleConsumer = await OracleConsumer.deploy(
    serviceRegistryAddress,
    oracleServiceId
  );
  await oracleConsumer.waitForDeployment();
  const oracleConsumerAddress = await oracleConsumer.getAddress();
  console.log("✅ OracleConsumer deployed to:", oracleConsumerAddress);

  // Deploy AbstractAccount implementation
  console.log("\n👤 Deploying AbstractAccount implementation...");
  const AbstractAccount = await ethers.getContractFactory("AbstractAccount");
  const abstractAccountImplementation = await AbstractAccount.deploy(
    deployer.address, // owner
    [deployer.address], // guardians
    1, // recovery threshold
    deployer.address // factory (placeholder)
  );
  await abstractAccountImplementation.waitForDeployment();
  const abstractAccountImplementationAddress = await abstractAccountImplementation.getAddress();
  console.log("✅ AbstractAccount implementation deployed to:", abstractAccountImplementationAddress);

  // Deploy AbstractAccountFactory
  console.log("\n🏭 Deploying AbstractAccountFactory...");
  const AbstractAccountFactory = await ethers.getContractFactory("AbstractAccountFactory");
  const abstractAccountFactory = await AbstractAccountFactory.deploy(
    serviceRegistryAddress,
    abstractAccountServiceId,
    abstractAccountImplementationAddress
  );
  await abstractAccountFactory.waitForDeployment();
  const abstractAccountFactoryAddress = await abstractAccountFactory.getAddress();
  console.log("✅ AbstractAccountFactory deployed to:", abstractAccountFactoryAddress);

  // Update service addresses in registry
  console.log("\n🔄 Updating service addresses in registry...");
  
  await serviceRegistry.updateService(
    randomnessServiceId,
    randomnessConsumerAddress,
    "https://neo-service-layer.com/randomness"
  );
  console.log("✅ Randomness Service address updated");

  await serviceRegistry.updateService(
    oracleServiceId,
    oracleConsumerAddress,
    "https://neo-service-layer.com/oracle"
  );
  console.log("✅ Oracle Service address updated");

  await serviceRegistry.updateService(
    abstractAccountServiceId,
    abstractAccountFactoryAddress,
    "https://neo-service-layer.com/abstract-account"
  );
  console.log("✅ Abstract Account Service address updated");

  // Verify deployments
  console.log("\n🔍 Verifying deployments...");
  
  const serviceCount = await serviceRegistry.getServiceCount();
  console.log("📊 Total services registered:", serviceCount.toString());

  const randomnessRequestCount = await randomnessConsumer.getRequestCount();
  console.log("🎲 Randomness requests:", randomnessRequestCount.toString());

  const oracleRequestCount = await oracleConsumer.getRequestCount();
  console.log("🔮 Oracle requests:", oracleRequestCount.toString());

  const accountCount = await abstractAccountFactory.getAccountCount();
  console.log("👤 Abstract accounts created:", accountCount.toString());

  // Print deployment summary
  console.log("\n📋 DEPLOYMENT SUMMARY");
  console.log("=====================");
  console.log("🏛️  ServiceRegistry:", serviceRegistryAddress);
  console.log("🎲 RandomnessConsumer:", randomnessConsumerAddress);
  console.log("🔮 OracleConsumer:", oracleConsumerAddress);
  console.log("👤 AbstractAccount Implementation:", abstractAccountImplementationAddress);
  console.log("🏭 AbstractAccountFactory:", abstractAccountFactoryAddress);
  console.log("\n🔑 Service IDs:");
  console.log("🎲 Randomness Service ID:", randomnessServiceId);
  console.log("🔮 Oracle Service ID:", oracleServiceId);
  console.log("👤 Abstract Account Service ID:", abstractAccountServiceId);

  // Save deployment info to file
  const deploymentInfo = {
    network: await ethers.provider.getNetwork(),
    deployer: deployer.address,
    timestamp: new Date().toISOString(),
    contracts: {
      serviceRegistry: serviceRegistryAddress,
      randomnessConsumer: randomnessConsumerAddress,
      oracleConsumer: oracleConsumerAddress,
      abstractAccountImplementation: abstractAccountImplementationAddress,
      abstractAccountFactory: abstractAccountFactoryAddress,
    },
    serviceIds: {
      randomnessService: randomnessServiceId,
      oracleService: oracleServiceId,
      abstractAccountService: abstractAccountServiceId,
    },
  };

  const fs = require("fs");
  const path = require("path");
  
  const deploymentsDir = path.join(__dirname, "../deployments");
  if (!fs.existsSync(deploymentsDir)) {
    fs.mkdirSync(deploymentsDir, { recursive: true });
  }
  
  const networkName = (await ethers.provider.getNetwork()).name || "unknown";
  const deploymentFile = path.join(deploymentsDir, `${networkName}-deployment.json`);
  
  fs.writeFileSync(deploymentFile, JSON.stringify(deploymentInfo, null, 2));
  console.log("\n💾 Deployment info saved to:", deploymentFile);

  console.log("\n🎉 Deployment completed successfully!");
  console.log("🔗 You can now interact with the Neo Service Layer contracts on NeoX blockchain");
}

main()
  .then(() => process.exit(0))
  .catch((error) => {
    console.error("❌ Deployment failed:", error);
    process.exit(1);
  });
