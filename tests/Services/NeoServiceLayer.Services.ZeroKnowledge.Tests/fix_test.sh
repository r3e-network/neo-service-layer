#\!/bin/bash

# Fix constructor
sed -i 's/_service = new ZeroKnowledgeService(\n            _mockLogger.Object,\n            _mockConfiguration.Object,\n            _mockEnclaveManager.Object,\n            _mockServiceRegistry.Object);/_service = new ZeroKnowledgeService(_mockLogger.Object, null);/g' ZeroKnowledgeServiceTests.cs

# Fix StartAsync calls
sed -i 's/await _service.StartAsync(CancellationToken.None);/await _service.StartAsync();/g' ZeroKnowledgeServiceTests.cs

# Fix StopAsync calls
sed -i 's/await _service.StopAsync(CancellationToken.None);/await _service.StopAsync();/g' ZeroKnowledgeServiceTests.cs

# Fix IConfigurationSection ambiguity
sed -i 's/IConfigurationSection configSection/Microsoft.Extensions.Configuration.IConfigurationSection configSection/g' ZeroKnowledgeServiceTests.cs

# Fix namespace references
sed -i '1i using NeoServiceLayer.Core;' ZeroKnowledgeServiceTests.cs

# Remove mock registry setup
sed -i '/_mockServiceRegistry/d' ZeroKnowledgeServiceTests.cs

