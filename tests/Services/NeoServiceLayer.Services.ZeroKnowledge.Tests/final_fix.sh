#\!/bin/bash

# Fix ZkCircuitType.Groth16 to valid enum value
sed -i 's/ZkCircuitType.Groth16/ZkCircuitType.Arithmetic/g' ZeroKnowledgeServiceTests.cs

# Fix PublicInputs and PrivateInputs properties
sed -i '93,94s/PublicInputs = new\[\] { "public_input" },/InputSchema = new Dictionary<string, object> { ["public_input"] = "uint256" },/' ZeroKnowledgeServiceTests.cs
sed -i '94s/PrivateInputs = new\[\] { "private_input" }/OutputSchema = new Dictionary<string, object> { ["private_input"] = "uint256" }/' ZeroKnowledgeServiceTests.cs

# Fix the CompileCircuitAsync calls
sed -i '119s/.*_service.CompileCircuitAsync(circuitId, invalidCircuitDefinition));/            _service.CompileCircuitAsync(new ZkCircuitDefinition { Name = circuitId, Type = ZkCircuitType.Arithmetic, Description = "Invalid", Constraints = new[] { invalidCircuitDefinition } }, BlockchainType.NeoN3));/' ZeroKnowledgeServiceTests.cs

# Fix the theory test CompileCircuitAsync calls  
sed -i '133,137s/var result = await _service.CompileCircuitAsync(circuitId, circuitDefinition);/var circuitDef = new ZkCircuitDefinition { Name = circuitId, Type = ZkCircuitType.Arithmetic, Description = "Test", Constraints = new[] { circuitDefinition } };\n        var result = await _service.CompileCircuitAsync(circuitDef, BlockchainType.NeoN3);/' ZeroKnowledgeServiceTests.cs

# Fix GenerateProofAsync calls
sed -i 's/_service.GenerateProofAsync(circuit, inputs, invalidWitnesses)/_service.GenerateProofAsync(new NeoServiceLayer.Core.Models.ProofRequest { CircuitId = circuit.Id, PublicInputs = inputs, PrivateInputs = invalidWitnesses }, BlockchainType.NeoN3)/g' ZeroKnowledgeServiceTests.cs
sed -i 's/_service.GenerateProofAsync(nonExistentCircuit, inputs, witnesses)/_service.GenerateProofAsync(new NeoServiceLayer.Core.Models.ProofRequest { CircuitId = nonExistentCircuit.Id, PublicInputs = inputs, PrivateInputs = witnesses }, BlockchainType.NeoN3)/g' ZeroKnowledgeServiceTests.cs

# Fix the tasks list declaration
sed -i 's/var tasks = new List<Task<byte\[\]>>();/var tasks = new List<Task<NeoServiceLayer.Core.Models.ProofResult>>();/' ZeroKnowledgeServiceTests.cs

# Fix Record.ExceptionAsync
sed -i 's/Exception exception = await Record.ExceptionAsync(() => _service.CompileCircuitAsync(null\!, "test_def"));/Exception exception = await Record.ExceptionAsync(async () => await _service.CompileCircuitAsync(null\!, BlockchainType.NeoN3));/' ZeroKnowledgeServiceTests.cs

