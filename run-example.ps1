# Start the Neo Service Layer API
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project src/NeoServiceLayer.Api" -PassThru -NoNewWindow

# Wait for the API to start
Write-Host "Waiting for the API to start..."
Start-Sleep -Seconds 10

# Run the example
Write-Host "Running the example..."
dotnet run --project examples/CompleteWorkflow

# Stop the API
Write-Host "Stopping the API..."
Stop-Process -Id $apiProcess.Id
