#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$DatabasePath,
    [ValidateRange(1024,65535)][int]$Port = 5095,
    [string]$AdminEmail = "admin@example.test"
)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw "Install .NET 10 SDK first." }
if ([string]::IsNullOrWhiteSpace($DatabasePath)) {
    $folder = Join-Path $root ".fleet-local"
    [void](New-Item -ItemType Directory -Path $folder -Force)
    $DatabasePath = Join-Path $folder "fleet-v3.db"
}
$DatabasePath = [IO.Path]::GetFullPath($DatabasePath)
if ($DatabasePath -match '[;"\r\n]') { throw "Choose a database path without connection-string delimiters." }
if (-not (Test-Path -LiteralPath (Split-Path -Parent $DatabasePath) -PathType Container)) { throw "Database parent directory must exist." }
if ((Test-Path -LiteralPath $DatabasePath) -and -not (Test-Path -LiteralPath $DatabasePath -PathType Leaf)) { throw "Database target must be a file." }
$fresh = -not (Test-Path -LiteralPath $DatabasePath)
if ($fresh) {
    foreach ($suffix in @("-wal","-shm","-journal")) {
        if (Test-Path -LiteralPath ($DatabasePath+$suffix)) { throw "Unexpected existing SQLite sidecar. Choose a new database path." }
    }
}
$names = @("Fleet__ApiKey","Fleet__InitializeDatabase","Fleet__SeedDemo","Fleet__Provider","Fleet__UpgradeV2Schema","Fleet__RequireShifts","Fleet__BootstrapAdminEmail","Fleet__BootstrapAdminPassword","ConnectionStrings__Fleet")
$previous = @{}
foreach ($name in $names) { $previous[$name] = [Environment]::GetEnvironmentVariable($name,"Process") }
try {
    $env:Fleet__ApiKey = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
    $env:Fleet__InitializeDatabase = "true"
    $env:Fleet__Provider = "Sqlite"
    $env:Fleet__SeedDemo = if ($fresh) { "true" } else { "false" }
    $env:Fleet__UpgradeV2Schema = "false"
    $env:Fleet__RequireShifts = "false"
    $env:ConnectionStrings__Fleet = "Data Source=$DatabasePath"
    Remove-Item Env:Fleet__BootstrapAdminEmail,Env:Fleet__BootstrapAdminPassword -ErrorAction SilentlyContinue
    if ($fresh) {
        if ($AdminEmail -notmatch '^[^@\s]+@[^@\s]+\.[^@\s]+$') { throw "Enter a valid administrator email." }
        $password = Read-Host "Choose a unique admin password (12+ characters, uppercase, lowercase, number, symbol)" -AsSecureString
        try {
            $plain = ConvertFrom-SecureString $password -AsPlainText
            if ($plain.Length -lt 12 -or $plain.Length -gt 512 -or $plain -cnotmatch '[A-Z]' -or $plain -cnotmatch '[a-z]' -or $plain -notmatch '\d' -or $plain -notmatch '[^a-zA-Z0-9]') {
                throw "Password does not meet the required format."
            }
            $env:Fleet__BootstrapAdminEmail = $AdminEmail
            $env:Fleet__BootstrapAdminPassword = $plain
        } finally {
            $plain = $null
            $password.Dispose()
        }
        Write-Host "New local synthetic database will be initialized: $DatabasePath"
        Write-Host "Administrator: $AdminEmail"
    } else {
        Write-Host "Existing database will be checked without upgrade or demo seeding: $DatabasePath"
        Write-Host "Use its existing Identity credentials."
    }
    Write-Host "Connected workspace: http://127.0.0.1:$Port/console"
    Write-Host "Stop the local process with Ctrl+C. Do not publish this service or database."
    & dotnet run --project (Join-Path $root "src/FleetManagement.Api") --no-launch-profile --urls "http://127.0.0.1:$Port"
    if ($LASTEXITCODE -ne 0) { throw "API process exited with code $LASTEXITCODE. See the console log; no database deletion was attempted." }
} finally {
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name,$previous[$name],"Process") }
}
