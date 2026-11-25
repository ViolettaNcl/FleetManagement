#Requires -Version 7.0
$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$launcher = Join-Path $root "tools/start-connected.ps1"
$tokens = $null
$errors = $null
[void][Management.Automation.Language.Parser]::ParseFile($launcher,[ref]$tokens,[ref]$errors)
if ($errors.Count -ne 0) { throw "PowerShell launcher has syntax errors." }
$temp = Join-Path $env:RUNNER_TEMP ("fleet-windows-" + [Guid]::NewGuid().ToString("N"))
[void](New-Item -ItemType Directory -Path $temp)
$db = Join-Path $temp "existing database.db"
$password = "Qa!aA1" + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
$key = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
Write-Host "::add-mask::$password"
Write-Host "::add-mask::$key"
$env:Fleet__ApiKey = $key
$env:Fleet__Provider = "Sqlite"
$env:Fleet__InitializeDatabase = "true"
$env:Fleet__SeedDemo = "true"
$env:Fleet__UpgradeV2Schema = "false"
$env:Fleet__BootstrapAdminEmail = "windows-admin@example.test"
$env:Fleet__BootstrapAdminPassword = $password
$env:ConnectionStrings__Fleet = "Data Source=$db"
function Start-Child([string]$exe, [string[]]$argv) {
    $psi = [Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = $exe
    $psi.WorkingDirectory = $root
    $psi.UseShellExecute = $false
    foreach ($arg in $argv) { $psi.ArgumentList.Add($arg) }
    return [Diagnostics.Process]::Start($psi)
}
function Stop-Child($process) {
    if ($null -ne $process -and -not $process.HasExited) {
        $process.Kill($true)
        [void]$process.WaitForExit(10000)
    }
    if ($null -ne $process) { $process.Dispose() }
}
function Wait-Api($process,[int]$port) {
    for ($i=0;$i -lt 120;$i++) {
        if ($process.HasExited) { throw "API child exited before health check." }
        try {
            $health = Invoke-RestMethod -Uri "http://127.0.0.1:$port/health" -TimeoutSec 2
            if ($health.status -eq "ok") { return }
        } catch {}
        Start-Sleep -Seconds 1
    }
    throw "API did not start."
}
function Read-Fleet([int]$port) {
    $body = @{email="windows-admin@example.test";password=$password} | ConvertTo-Json
    $login = Invoke-RestMethod -Method Post -Uri "http://127.0.0.1:$port/api/auth/login" -ContentType "application/json" -Body $body
    return Invoke-RestMethod -Uri "http://127.0.0.1:$port/api/fleet" -Headers @{Authorization="Bearer "+$login.accessToken}
}
$process=$null
try {
    & dotnet build (Join-Path $root "src/FleetManagement.Api") -c Release
    if ($LASTEXITCODE -ne 0) { throw "API build failed." }
    $dll=Join-Path $root "src/FleetManagement.Api/bin/Release/net10.0/FleetManagement.Api.dll"
    $process=Start-Child "dotnet" @($dll,"--urls","http://127.0.0.1:5095")
    Wait-Api $process 5095
    $before=Read-Fleet 5095
    if ($before.vehicles.Count -ne 4) { throw "Unexpected seed records." }
    Stop-Child $process;$process=$null
    # Run the actual launcher non-interactively against the existing DB.
    # First-launch initialization is tested below using a SecureString; the actual terminal prompt is manual.
    $process=Start-Child "pwsh" @("-NoProfile","-NonInteractive","-File",$launcher,"-DatabasePath",$db,"-Port","5096")
    Wait-Api $process 5096
    $after=Read-Fleet 5096
    if (($before|ConvertTo-Json -Depth 20 -Compress) -ne ($after|ConvertTo-Json -Depth 20 -Compress)) { throw "Existing fleet changed." }
    $html=Invoke-WebRequest -Uri "http://127.0.0.1:5096/console" -TimeoutSec 10
    if ($html.StatusCode -ne 200 -or $html.Content -notmatch "Connected operations workspace") { throw "Connected workspace unavailable." }
    Write-Host "PASS: Windows launcher syntax, existing SQLite startup, account login and fleet preservation."
    Stop-Child $process;$process=$null
    $freshDb=Join-Path $temp "fresh database.db"
    $wrapper=Join-Path $temp "first-run.ps1"
    $env:FleetLauncherPath=$launcher
    $env:FleetFreshDb=$freshDb
    @'
$ErrorActionPreference="Stop"
$secret=ConvertTo-SecureString $env:Fleet__BootstrapAdminPassword -AsPlainText -Force
try { & $env:FleetLauncherPath -DatabasePath $env:FleetFreshDb -Port 5097 -AdminEmail "windows-admin@example.test" -AdminPassword $secret }
finally { $secret.Dispose() }
'@ | Set-Content -LiteralPath $wrapper -Encoding utf8
    $process=Start-Child "pwsh" @("-NoProfile","-NonInteractive","-File",$wrapper)
    Wait-Api $process 5097
    $freshFleet=Read-Fleet 5097
    if ($freshFleet.vehicles.Count -ne 4 -or $freshFleet.drivers.Count -ne 4 -or $freshFleet.trips.Count -ne 4) { throw "Fresh launcher did not seed expected records." }
    if (-not (Test-Path -LiteralPath $freshDb -PathType Leaf)) { throw "Fresh database missing." }
    Stop-Child $process;$process=$null
    $process=Start-Child "pwsh" @("-NoProfile","-NonInteractive","-File",$launcher,"-DatabasePath",$freshDb,"-Port","5098")
    Wait-Api $process 5098
    $reopened=Read-Fleet 5098
    if (($freshFleet|ConvertTo-Json -Depth 20 -Compress) -ne ($reopened|ConvertTo-Json -Depth 20 -Compress)) { throw "Fresh DB changed during reopen." }
    Write-Host "PASS: Windows launcher fresh SecureString initialization, first login, reopen and preservation."
} finally {
    Stop-Child $process
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
    foreach ($name in @("Fleet__ApiKey","Fleet__BootstrapAdminPassword","Fleet__BootstrapAdminEmail","ConnectionStrings__Fleet","FleetLauncherPath","FleetFreshDb")) { Remove-Item "Env:$name" -ErrorAction SilentlyContinue }
}
