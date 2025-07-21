# Setup git hooks for the project on Windows

$HooksDir = "scripts\hooks"
$GitHooksDir = ".git\hooks"

Write-Host "Setting up git hooks..." -ForegroundColor Green

# Check if hooks directory exists
if (-not (Test-Path $HooksDir)) {
    Write-Host "❌ Hooks directory not found at $HooksDir" -ForegroundColor Red
    exit 1
}

# Install each hook
Get-ChildItem -Path $HooksDir -File | ForEach-Object {
    $hookName = $_.Name
    Write-Host "Installing $hookName..."
    Copy-Item $_.FullName "$GitHooksDir\$hookName" -Force
}

Write-Host "✅ Git hooks installed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Branch naming convention enforced:" -ForegroundColor Yellow
Write-Host "  - feature/* : New features"
Write-Host "  - fix/*     : Bug fixes"
Write-Host "  - hotfix/*  : Critical fixes"
Write-Host "  - release/* : Release branches"
Write-Host "  - chore/*   : Maintenance tasks"