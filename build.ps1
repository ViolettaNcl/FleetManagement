# build.ps1
Write-Host "Starting build, test, coverage, and documentation process..."

# Переход в папку основного проекта
Set-Location -Path ".\FleetManagment"

# Сборка проекта
Write-Host "Building FleetManagment..."
dotnet build FleetManagment.csproj --configuration Release

# Переход в папку тестов
Set-Location -Path "..\FleetManagement.Tests"

# Запуск тестов с покрытием
Write-Host "Running tests with coverage..."
dotnet test FleetManagement.Tests.csproj --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:ExcludeByAttribute=\"GeneratedCodeAttribute,ObsoleteAttribute\"

# Генерация отчёта о покрытии
Write-Host "Generating coverage report..."
reportgenerator -reports:coverage.opencover.xml -targetdir:..\CoverageReport

# Генерация документации
Write-Host "Generating documentation..."
Set-Location -Path ".."
docfx docfx.json

Write-Host "Automation completed!"
