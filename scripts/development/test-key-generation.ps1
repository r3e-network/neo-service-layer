# Neo Service Layer Key Generation Test Script
Write-Host "🔑 Testing Neo Service Layer Key Generation" -ForegroundColor Green

# Step 1: Get Demo Token
Write-Host "`n1. Getting demo token..." -ForegroundColor Yellow
try {
    $tokenResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/auth/demo-token" -Method POST -UseBasicParsing
    $tokenData = $tokenResponse.Content | ConvertFrom-Json
    $token = $tokenData.token
    Write-Host "✅ Token acquired successfully" -ForegroundColor Green
    Write-Host "   Expires: $($tokenData.expires)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Failed to get token: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Test Key Generation
Write-Host "`n2. Testing key generation..." -ForegroundColor Yellow
$keyId = "test-key-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
$requestBody = @{
    keyId = $keyId
    keyType = "Secp256k1"
    keyUsage = "Sign,Verify"
    exportable = $false
    description = "Test key generated from Docker container"
} | ConvertTo-Json

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

try {
    $keyResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/v1/keymanagement/generate/NeoN3" -Method POST -Headers $headers -Body $requestBody -UseBasicParsing
    Write-Host "✅ Key generation successful!" -ForegroundColor Green
    Write-Host "   Status: $($keyResponse.StatusCode)" -ForegroundColor Gray
    Write-Host "   Response: $($keyResponse.Content)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Key generation failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $errorResponse = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorResponse)
        $errorContent = $reader.ReadToEnd()
        Write-Host "   Error details: $errorContent" -ForegroundColor Red
    }
    exit 1
}

# Step 3: Test Key Listing
Write-Host "`n3. Testing key listing..." -ForegroundColor Yellow
try {
    $listResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/v1/keymanagement/list/NeoN3" -Method GET -Headers @{"Authorization" = "Bearer $token"} -UseBasicParsing
    Write-Host "✅ Key listing successful!" -ForegroundColor Green
    Write-Host "   Status: $($listResponse.StatusCode)" -ForegroundColor Gray
    Write-Host "   Response: $($listResponse.Content)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Key listing failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🎉 Key generation test completed!" -ForegroundColor Green
Write-Host "The demo token button should now work correctly in the web interface." -ForegroundColor Cyan 