# build.ps1
Write-Host "Starting build, test, coverage, and documentation process..."

# ереход в папку основного проекта
Set-Location -Path ".\FleetManagment"

# Сборка проекта
Write-Host "Building FleetManagment..."
dotnet build FleetManagement.csproj --configuration Release

# ереход в папку тестов
Set-Location -Path "..\FleetManagement.Tests"

# апуск тестов с покрытием
Write-Host "Running tests with coverage..."
dotnet test FleetManagement.Tests.csproj --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:ExcludeByAttribute=\"GeneratedCodeAttribute,ObsoleteAttribute\"

# енерация отчёта о покрытии
Write-Host "Generating coverage report..."
reportgenerator -reports:coverage.opencover.xml -targetdir:..\CoverageReport

# енерация документации
Write-Host "Generating documentation..."
Set-Location -Path ".."
docfx docfx.json

Write-Host "Automation completed!"
