$base = "https://nexus-engine-production-60c8.up.railway.app"
$pass = $true

Write-Host "=====================================" -ForegroundColor White
Write-Host "   NEXUS ENGINE -- FULL TEST SUITE   " -ForegroundColor White
Write-Host "=====================================" -ForegroundColor White

# --- SCENARIO 1: Full Match ---
Write-Host "`n[S1] Full Match" -ForegroundColor Cyan
$a1 = (Invoke-RestMethod -Method POST -Uri "$base/api/accounts" -ContentType "application/json" -Body "{}")
Invoke-RestMethod -Method POST -Uri "$base/api/accounts/$($a1.id)/deposit" -ContentType "application/json" -Body '{"amount": 1000}' | Out-Null
Invoke-RestMethod -Method POST -Uri "$base/api/orders" -ContentType "application/json" -Body "{`"accountId`":`"$($a1.id)`",`"symbol`":`"BTC/USD`",`"side`":`"Buy`",`"price`":100,`"quantity`":2}" | Out-Null
$b1 = (Invoke-RestMethod -Method POST -Uri "$base/api/accounts" -ContentType "application/json" -Body "{}")
Invoke-RestMethod -Method POST -Uri "$base/api/accounts/$($b1.id)/deposit" -ContentType "application/json" -Body '{"amount": 1000}' | Out-Null
Invoke-RestMethod -Method POST -Uri "$base/api/orders" -ContentType "application/json" -Body "{`"accountId`":`"$($b1.id)`",`"symbol`":`"BTC/USD`",`"side`":`"Sell`",`"price`":100,`"quantity`":2}" | Out-Null
$accA1 = Invoke-RestMethod -Method GET -Uri "$base/api/accounts/$($a1.id)"
$accB1 = Invoke-RestMethod -Method GET -Uri "$base/api/accounts/$($b1.id)"
$ordA1 = (Invoke-RestMethod -Method GET -Uri "$base/api/orders?accountId=$($a1.id)")[0]
$ordB1 = (Invoke-RestMethod -Method GET -Uri "$base/api/orders?accountId=$($b1.id)")[0]
$s1 = ($accA1.balance -eq 1000 -and $accA1.reservedBalance -eq 0 -and
       $accB1.balance -eq 1200 -and $accB1.reservedBalance -eq 0 -and
       $ordA1.status -eq "Filled" -and $ordB1.status -eq "Filled")
if ($s1) { Write-Host "  PASS" -ForegroundColor Green } else {
    Write-Host "  FAIL" -ForegroundColor Red
    Write-Host "  A: balance=$($accA1.balance) reserved=$($accA1.reservedBalance) order=$($ordA1.status)"
    Write-Host "  B: balance=$($accB1.balance) reserved=$($accB1.reservedBalance) order=$($ordB1.status)"
    $pass = $false
}

# --- SCENARIO 2: Partial Fill ---
Write-Host "`n[S2] Partial Fill (Buy qty 5, Sell qty 2)" -ForegroundColor Cyan
$a2 = (Invoke-RestMethod -Method POST -Uri "$base/api/accounts" -ContentType "application/json" -Body "{}")
Invoke-RestMethod -Method POST -Uri "$base/api/accounts/$($a2.id)/deposit" -ContentType "application/json" -Body '{"amount": 1000}' | Out-Null
Invoke-RestMethod -Method POST -Uri "$base/api/orders" -ContentType "application/json" -Body "{`"accountId`":`"$($a2.id)`",`"symbol`":`"BTC/USD`",`"side`":`"Buy`",`"price`":100,`"quantity`":5}" | Out-Null
$b2 = (Invoke-RestMethod -Method POST -Uri "$base/api/accounts" -ContentType "application/json" -Body "{}")
Invoke-RestMethod -Method POST -Uri "$base/api/accounts/$($b2.id)/deposit" -ContentType "application/json" -Body '{"amount": 1000}' | Out-Null
Invoke-RestMethod -Method POST -Uri "$base/api/orders" -ContentType "application/json" -Body "{`"accountId`":`"$($b2.id)`",`"symbol`":`"BTC/USD`",`"side`":`"Sell`",`"price`":100,`"quantity`":2}" | Out-Null
$accA2 = Invoke-RestMethod -Method GET -Uri "$base/api/accounts/$($a2.id)"
$accB2 = Invoke-RestMethod -Method GET -Uri "$base/api/accounts/$($b2.id)"
$ordA2 = (Invoke-RestMethod -Method GET -Uri "$base/api/orders?accountId=$($a2.id)")[0]
$ordB2 = (Invoke-RestMethod -Method GET -Uri "$base/api/orders?accountId=$($b2.id)")[0]
$s2 = ($accA2.balance -eq 1000 -and $accA2.reservedBalance -eq 300 -and
       $accB2.balance -eq 1200 -and $accB2.reservedBalance -eq 0 -and
       $ordA2.status -eq "PartiallyFilled" -and [decimal]$ordA2.remainingQuantity -eq 3 -and
       $ordB2.status -eq "Filled")
if ($s2) { Write-Host "  PASS" -ForegroundColor Green } else {
    Write-Host "  FAIL" -ForegroundColor Red
    Write-Host "  A: balance=$($accA2.balance) reserved=$($accA2.reservedBalance) order=$($ordA2.status) remaining=$($ordA2.remainingQuantity)"
    Write-Host "  B: balance=$($accB2.balance) reserved=$($accB2.reservedBalance) order=$($ordB2.status)"
    $pass = $false
}

# --- SCENARIO 3: Insufficient Balance ---
Write-Host "`n[S3] Insufficient Balance" -ForegroundColor Cyan
$a3 = (Invoke-RestMethod -Method POST -Uri "$base/api/accounts" -ContentType "application/json" -Body "{}")
Invoke-RestMethod -Method POST -Uri "$base/api/accounts/$($a3.id)/deposit" -ContentType "application/json" -Body '{"amount": 100}' | Out-Null
$s3 = $false
try {
    Invoke-RestMethod -Method POST -Uri "$base/api/orders" -ContentType "application/json" -Body "{`"accountId`":`"$($a3.id)`",`"symbol`":`"BTC/USD`",`"side`":`"Buy`",`"price`":200,`"quantity`":1}" | Out-Null
    Write-Host "  FAIL -- order should have been rejected" -ForegroundColor Red
    $pass = $false
} catch {
    $accA3 = Invoke-RestMethod -Method GET -Uri "$base/api/accounts/$($a3.id)"
    $s3 = ($accA3.balance -eq 100 -and $accA3.reservedBalance -eq 0)
    if ($s3) { Write-Host "  PASS" -ForegroundColor Green } else {
        Write-Host "  FAIL -- balance or reserved incorrect after rejection" -ForegroundColor Red
        $pass = $false
    }
}

# --- SCENARIO 4: Cancel Order ---
Write-Host "`n[S4] Cancel Order -- reserved released" -ForegroundColor Cyan
$a4 = (Invoke-RestMethod -Method POST -Uri "$base/api/accounts" -ContentType "application/json" -Body "{}")
Invoke-RestMethod -Method POST -Uri "$base/api/accounts/$($a4.id)/deposit" -ContentType "application/json" -Body '{"amount": 1000}' | Out-Null
$ord4 = Invoke-RestMethod -Method POST -Uri "$base/api/orders" -ContentType "application/json" -Body "{`"accountId`":`"$($a4.id)`",`"symbol`":`"BTC/USD`",`"side`":`"Buy`",`"price`":100,`"quantity`":3}"
Invoke-RestMethod -Method DELETE -Uri "$base/api/orders/$($ord4.id)?accountId=$($a4.id)" | Out-Null
$accA4 = Invoke-RestMethod -Method GET -Uri "$base/api/accounts/$($a4.id)"
$s4 = ($accA4.balance -eq 1000 -and $accA4.reservedBalance -eq 0)
if ($s4) { Write-Host "  PASS" -ForegroundColor Green } else {
    Write-Host "  FAIL" -ForegroundColor Red
    Write-Host "  A: balance=$($accA4.balance) reserved=$($accA4.reservedBalance)"
    $pass = $false
}

# --- FINAL RESULT ---
Write-Host "`n=====================================" -ForegroundColor White
if ($pass) {
    Write-Host "   ALL TESTS PASSED" -ForegroundColor Green
} else {
    Write-Host "   SOME TESTS FAILED -- see above" -ForegroundColor Red
}
Write-Host "=====================================" -ForegroundColor White