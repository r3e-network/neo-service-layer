# PowerShell script to run enclave tests with the real SDK in simulation mode

# Set environment variables for testing
$env:OCCLUM_SIMULATION = "1"
$env:OCCLUM_ENCLAVE_PATH = "$PSScriptRoot\..\src\NeoServiceLayer.Tee.Enclave\build\lib\libenclave.so"
$env:OCCLUM_INSTANCE_DIR = "$PSScriptRoot\..\occlum_instance"

# Ensure the enclave is built
Write-Host "Building enclave..."
dotnet build ..\src\NeoServiceLayer.Tee.Enclave\NeoServiceLayer.Tee.Enclave.csproj -c Debug

# Run the tests
Write-Host "Running Enclave Tests..."
dotnet test .\NeoServiceLayer.Tee.Enclave.Tests\NeoServiceLayer.Tee.Enclave.Tests.csproj --filter "Category=Occlum|Category=Attestation|Category=Performance|Category=ErrorHandling|Category=Security" --logger "console;verbosity=detailed"

Write-Host "Running Occlum Tests..."
dotnet test .\NeoServiceLayer.Occlum.Tests\NeoServiceLayer.Occlum.Tests.csproj --filter "Category=Occlum" --logger "console;verbosity=detailed"

Write-Host "Running Integration Tests..."
dotnet test .\NeoServiceLayer.IntegrationTests\NeoServiceLayer.IntegrationTests.csproj --filter "Category=Occlum" --logger "console;verbosity=detailed"

Write-Host "All tests completed!"
