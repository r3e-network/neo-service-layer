import { expect } from "chai";
import { ethers } from "hardhat";
import { Contract, Signer } from "ethers";
import { time } from "@nomicfoundation/hardhat-network-helpers";

describe("Neo Service Layer Contracts", function () {
  let serviceRegistry: Contract;
  let randomnessConsumer: Contract;
  let oracleConsumer: Contract;
  let abstractAccountFactory: Contract;
  let abstractAccountImplementation: Contract;
  
  let owner: Signer;
  let user1: Signer;
  let user2: Signer;
  let guardian1: Signer;
  let guardian2: Signer;
  
  let randomnessServiceId: string;
  let oracleServiceId: string;
  let abstractAccountServiceId: string;

  beforeEach(async function () {
    [owner, user1, user2, guardian1, guardian2] = await ethers.getSigners();

    // Deploy ServiceRegistry
    const ServiceRegistry = await ethers.getContractFactory("ServiceRegistry");
    serviceRegistry = await ServiceRegistry.deploy();
    await serviceRegistry.waitForDeployment();

    // Register services
    await serviceRegistry.registerService(
      "RandomnessService",
      "1.0.0",
      await owner.getAddress(),
      "https://neo-service-layer.com/randomness"
    );
    randomnessServiceId = await serviceRegistry.serviceNameToId("RandomnessService");

    await serviceRegistry.registerService(
      "OracleService",
      "1.0.0",
      await owner.getAddress(),
      "https://neo-service-layer.com/oracle"
    );
    oracleServiceId = await serviceRegistry.serviceNameToId("OracleService");

    await serviceRegistry.registerService(
      "AbstractAccountService",
      "1.0.0",
      await owner.getAddress(),
      "https://neo-service-layer.com/abstract-account"
    );
    abstractAccountServiceId = await serviceRegistry.serviceNameToId("AbstractAccountService");

    // Deploy consumer contracts
    const RandomnessConsumer = await ethers.getContractFactory("RandomnessConsumer");
    randomnessConsumer = await RandomnessConsumer.deploy(
      await serviceRegistry.getAddress(),
      randomnessServiceId
    );
    await randomnessConsumer.waitForDeployment();

    const OracleConsumer = await ethers.getContractFactory("OracleConsumer");
    oracleConsumer = await OracleConsumer.deploy(
      await serviceRegistry.getAddress(),
      oracleServiceId
    );
    await oracleConsumer.waitForDeployment();

    // Deploy AbstractAccount implementation
    const AbstractAccount = await ethers.getContractFactory("AbstractAccount");
    abstractAccountImplementation = await AbstractAccount.deploy(
      await owner.getAddress(),
      [await guardian1.getAddress()],
      1,
      await owner.getAddress()
    );
    await abstractAccountImplementation.waitForDeployment();

    // Deploy AbstractAccountFactory
    const AbstractAccountFactory = await ethers.getContractFactory("AbstractAccountFactory");
    abstractAccountFactory = await AbstractAccountFactory.deploy(
      await serviceRegistry.getAddress(),
      abstractAccountServiceId,
      await abstractAccountImplementation.getAddress()
    );
    await abstractAccountFactory.waitForDeployment();
  });

  describe("ServiceRegistry", function () {
    it("Should register and retrieve services", async function () {
      const service = await serviceRegistry.getServiceByName("RandomnessService");
      expect(service.serviceName).to.equal("RandomnessService");
      expect(service.serviceVersion).to.equal("1.0.0");
      expect(service.isActive).to.be.true;
    });

    it("Should track service metrics", async function () {
      await serviceRegistry.logServiceRequest(randomnessServiceId, true);
      await serviceRegistry.logServiceRequest(randomnessServiceId, false);

      const metrics = await serviceRegistry.getServiceMetrics(randomnessServiceId);
      expect(metrics.totalRequests).to.equal(2);
      expect(metrics.successfulRequests).to.equal(1);
      expect(metrics.failedRequests).to.equal(1);
    });

    it("Should deactivate and activate services", async function () {
      await serviceRegistry.deactivateService(randomnessServiceId);
      expect(await serviceRegistry.isServiceActive(randomnessServiceId)).to.be.false;

      await serviceRegistry.activateService(randomnessServiceId);
      expect(await serviceRegistry.isServiceActive(randomnessServiceId)).to.be.true;
    });
  });

  describe("RandomnessConsumer", function () {
    it("Should request randomness", async function () {
      const tx = await randomnessConsumer.connect(user1).requestRandomness(1, 100);
      const receipt = await tx.wait();
      
      const event = receipt.logs.find((log: any) => 
        log.fragment && log.fragment.name === "RandomnessRequested"
      );
      expect(event).to.not.be.undefined;

      const requestCount = await randomnessConsumer.getRequestCount();
      expect(requestCount).to.equal(1);
    });

    it("Should fulfill randomness request", async function () {
      const tx = await randomnessConsumer.connect(user1).requestRandomness(1, 100);
      const receipt = await tx.wait();
      
      const event = receipt.logs.find((log: any) => 
        log.fragment && log.fragment.name === "RandomnessRequested"
      );
      const requestId = event.args[0];

      await randomnessConsumer.fulfillRandomness(requestId, 42);

      const [fulfilled, randomValue] = await randomnessConsumer.getRandomnessResult(requestId);
      expect(fulfilled).to.be.true;
      expect(randomValue).to.equal(42);
    });

    it("Should batch request randomness", async function () {
      const mins = [1, 10, 100];
      const maxs = [10, 100, 1000];

      const tx = await randomnessConsumer.connect(user1).batchRequestRandomness(mins, maxs);
      await tx.wait();

      const requestCount = await randomnessConsumer.getRequestCount();
      expect(requestCount).to.equal(3);
    });

    it("Should reject invalid ranges", async function () {
      await expect(
        randomnessConsumer.connect(user1).requestRandomness(100, 1)
      ).to.be.revertedWith("Invalid range: min must be less than max");

      await expect(
        randomnessConsumer.connect(user1).requestRandomness(1, 1000002)
      ).to.be.revertedWith("Range too large");
    });
  });

  describe("OracleConsumer", function () {
    it("Should request oracle data", async function () {
      const tx = await oracleConsumer.connect(user1).requestOracleData("coinmarketcap", "bitcoin/price");
      const receipt = await tx.wait();
      
      const event = receipt.logs.find((log: any) => 
        log.fragment && log.fragment.name === "OracleDataRequested"
      );
      expect(event).to.not.be.undefined;

      const requestCount = await oracleConsumer.getRequestCount();
      expect(requestCount).to.equal(1);
    });

    it("Should fulfill oracle request", async function () {
      const tx = await oracleConsumer.connect(user1).requestOracleData("coinmarketcap", "bitcoin/price");
      const receipt = await tx.wait();
      
      const event = receipt.logs.find((log: any) => 
        log.fragment && log.fragment.name === "OracleDataRequested"
      );
      const requestId = event.args[0];

      const priceData = ethers.AbiCoder.defaultAbiCoder().encode(["uint256"], [50000]);
      await oracleConsumer.fulfillOracleRequest(requestId, true, priceData);

      const [fulfilled, success, data] = await oracleConsumer.getOracleResult(requestId);
      expect(fulfilled).to.be.true;
      expect(success).to.be.true;
      expect(data).to.equal(priceData);
    });

    it("Should manage data sources", async function () {
      expect(await oracleConsumer.isDataSourceSupported("coinmarketcap")).to.be.true;
      
      await oracleConsumer.addDataSource("custom-api");
      expect(await oracleConsumer.isDataSourceSupported("custom-api")).to.be.true;
      
      await oracleConsumer.removeDataSource("custom-api");
      expect(await oracleConsumer.isDataSourceSupported("custom-api")).to.be.false;
    });

    it("Should reject unsupported data sources", async function () {
      await expect(
        oracleConsumer.connect(user1).requestOracleData("unsupported", "data/path")
      ).to.be.revertedWith("Data source not supported");
    });
  });

  describe("AbstractAccountFactory", function () {
    it("Should create abstract account", async function () {
      const guardians = [await guardian1.getAddress(), await guardian2.getAddress()];
      const salt = ethers.randomBytes(32);

      const tx = await abstractAccountFactory.connect(user1).createAccount(
        await user1.getAddress(),
        guardians,
        2,
        salt
      );
      const receipt = await tx.wait();
      
      const event = receipt.logs.find((log: any) => 
        log.fragment && log.fragment.name === "AbstractAccountCreated"
      );
      expect(event).to.not.be.undefined;

      const accountCount = await abstractAccountFactory.getAccountCount();
      expect(accountCount).to.equal(1);
    });

    it("Should predict account address", async function () {
      const guardians = [await guardian1.getAddress()];
      const salt = ethers.randomBytes(32);

      const predictedAddress = await abstractAccountFactory.predictAccountAddress(
        await user1.getAddress(),
        guardians,
        1,
        salt
      );

      const tx = await abstractAccountFactory.connect(user1).createAccount(
        await user1.getAddress(),
        guardians,
        1,
        salt
      );
      const receipt = await tx.wait();
      
      const event = receipt.logs.find((log: any) => 
        log.fragment && log.fragment.name === "AbstractAccountCreated"
      );
      const actualAddress = event.args[1];

      expect(actualAddress).to.equal(predictedAddress);
    });

    it("Should batch create accounts", async function () {
      const guardiansArray = [
        [await guardian1.getAddress()],
        [await guardian2.getAddress()],
      ];
      const thresholds = [1, 1];
      const salts = [ethers.randomBytes(32), ethers.randomBytes(32)];

      const tx = await abstractAccountFactory.connect(user1).batchCreateAccounts(
        await user1.getAddress(),
        guardiansArray,
        thresholds,
        salts
      );
      await tx.wait();

      const accountCount = await abstractAccountFactory.getAccountCount();
      expect(accountCount).to.equal(2);
    });
  });

  describe("Integration Tests", function () {
    it("Should demonstrate full workflow", async function () {
      // 1. Create abstract account
      const guardians = [await guardian1.getAddress(), await guardian2.getAddress()];
      const salt = ethers.randomBytes(32);

      const createTx = await abstractAccountFactory.connect(user1).createAccount(
        await user1.getAddress(),
        guardians,
        2,
        salt
      );
      const createReceipt = await createTx.wait();
      
      const createEvent = createReceipt.logs.find((log: any) => 
        log.fragment && log.fragment.name === "AbstractAccountCreated"
      );
      const accountAddress = createEvent.args[1];

      // 2. Request randomness
      const randomTx = await randomnessConsumer.connect(user1).requestRandomness(1, 1000);
      const randomReceipt = await randomTx.wait();
      
      const randomEvent = randomReceipt.logs.find((log: any) => 
        log.fragment && log.fragment.name === "RandomnessRequested"
      );
      const randomRequestId = randomEvent.args[0];

      // 3. Request oracle data
      const oracleTx = await oracleConsumer.connect(user1).requestOracleData("coinmarketcap", "bitcoin/price");
      const oracleReceipt = await oracleTx.wait();
      
      const oracleEvent = oracleReceipt.logs.find((log: any) => 
        log.fragment && log.fragment.name === "OracleDataRequested"
      );
      const oracleRequestId = oracleEvent.args[0];

      // 4. Fulfill requests (simulating Neo Service Layer)
      await randomnessConsumer.fulfillRandomness(randomRequestId, 777);
      
      const priceData = ethers.AbiCoder.defaultAbiCoder().encode(["uint256"], [65000]);
      await oracleConsumer.fulfillOracleRequest(oracleRequestId, true, priceData);

      // 5. Verify results
      const [randomFulfilled, randomValue] = await randomnessConsumer.getRandomnessResult(randomRequestId);
      expect(randomFulfilled).to.be.true;
      expect(randomValue).to.equal(777);

      const [oracleFulfilled, oracleSuccess, oracleData] = await oracleConsumer.getOracleResult(oracleRequestId);
      expect(oracleFulfilled).to.be.true;
      expect(oracleSuccess).to.be.true;
      expect(oracleData).to.equal(priceData);

      // 6. Verify account creation
      const accountId = await abstractAccountFactory.getAccountId(accountAddress);
      const accountInfo = await abstractAccountFactory.getAccount(accountId);
      expect(accountInfo.owner).to.equal(await user1.getAddress());
      expect(accountInfo.isActive).to.be.true;
    });
  });
});
