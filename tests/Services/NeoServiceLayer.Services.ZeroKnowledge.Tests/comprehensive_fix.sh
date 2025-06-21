#\!/bin/bash

# First, fix the line that was cut off
sed -i '150s/$/\n        var originalInputs = new Dictionary<string, object> { ["public_input"] = 25 };/' ZeroKnowledgeServiceTests.cs

# Replace all CompileCircuitAsync calls with ZkCircuitDefinition
sed -i '85,86s/var result = await _service.CompileCircuitAsync(circuitId, circuitDefinition);/var circuitDef = new NeoServiceLayer.Services.ZeroKnowledge.Models.ZkCircuitDefinition\n        {\n            Name = circuitId,\n            Type = NeoServiceLayer.Services.ZeroKnowledge.Models.ZkCircuitType.Groth16,\n            Description = "Test circuit",\n            Constraints = new[] { circuitDefinition },\n            PublicInputs = new[] { "public_input" },\n            PrivateInputs = new[] { "private_input" }\n        };\n        var result = await _service.CompileCircuitAsync(circuitDef, BlockchainType.NeoN3);/' ZeroKnowledgeServiceTests.cs

sed -i '122s/var result = await _service.CompileCircuitAsync(circuitId, circuitDefinition);/var circuitDef = new NeoServiceLayer.Services.ZeroKnowledge.Models.ZkCircuitDefinition\n        {\n            Name = circuitId,\n            Type = NeoServiceLayer.Services.ZeroKnowledge.Models.ZkCircuitType.Groth16,\n            Description = "Test circuit",\n            Constraints = new[] { circuitDefinition },\n            PublicInputs = new[] { "public_input" },\n            PrivateInputs = new[] { "private_input" }\n        };\n        var result = await _service.CompileCircuitAsync(circuitDef, BlockchainType.NeoN3);/' ZeroKnowledgeServiceTests.cs

# Replace GenerateProofAsync calls
sed -i 's/await _service.GenerateProofAsync(circuit, inputs, witnesses)/await _service.GenerateProofAsync(new NeoServiceLayer.Core.Models.ProofRequest { CircuitId = circuit.Id, PublicInputs = inputs, PrivateInputs = witnesses }, BlockchainType.NeoN3)/g' ZeroKnowledgeServiceTests.cs

sed -i 's/_service.GenerateProofAsync(circuit, inputs, witnesses)/_service.GenerateProofAsync(new NeoServiceLayer.Core.Models.ProofRequest { CircuitId = circuit.Id, PublicInputs = inputs, PrivateInputs = witnesses }, BlockchainType.NeoN3)/g' ZeroKnowledgeServiceTests.cs

sed -i 's/_service.GenerateProofAsync(circuit, originalInputs, witnesses)/_service.GenerateProofAsync(new NeoServiceLayer.Core.Models.ProofRequest { CircuitId = circuit.Id, PublicInputs = originalInputs, PrivateInputs = witnesses }, BlockchainType.NeoN3)/g' ZeroKnowledgeServiceTests.cs

# Replace VerifyProofAsync calls
sed -i 's/await _service.VerifyProofAsync(circuit, proof, inputs)/await _service.VerifyProofAsync(new NeoServiceLayer.Core.Models.ProofVerification { CircuitId = circuit.Id, ProofData = proof.ProofData, PublicInputs = inputs }, BlockchainType.NeoN3)/g' ZeroKnowledgeServiceTests.cs

sed -i 's/await _service.VerifyProofAsync(circuit, invalidProof, inputs)/await _service.VerifyProofAsync(new NeoServiceLayer.Core.Models.ProofVerification { CircuitId = circuit.Id, ProofData = invalidProof, PublicInputs = inputs }, BlockchainType.NeoN3)/g' ZeroKnowledgeServiceTests.cs

sed -i 's/await _service.VerifyProofAsync(circuit, proof, tamperedInputs)/await _service.VerifyProofAsync(new NeoServiceLayer.Core.Models.ProofVerification { CircuitId = circuit.Id, ProofData = proof.ProofData, PublicInputs = tamperedInputs }, BlockchainType.NeoN3)/g' ZeroKnowledgeServiceTests.cs

# Fix the IConfigurationSection mock setup
sed -i 's/var configSection = new Mock<IConfigurationSection>();/var configSection = new Mock<Microsoft.Extensions.Configuration.IConfigurationSection>();/' ZeroKnowledgeServiceTests.cs

# Fix the Record.ExceptionAsync 
sed -i 's/Exception exception = await Record.ExceptionAsync(() => _service.CompileCircuitAsync(null\!, "test_def"));/Exception exception = await Record.ExceptionAsync(async () => await _service.CompileCircuitAsync(null\!, BlockchainType.NeoN3));/' ZeroKnowledgeServiceTests.cs

# Add missing using statements if not already present
if \! grep -q "using NeoServiceLayer.Services.ZeroKnowledge.Models;" ZeroKnowledgeServiceTests.cs; then
    sed -i '4a using NeoServiceLayer.Services.ZeroKnowledge.Models;' ZeroKnowledgeServiceTests.cs
fi

