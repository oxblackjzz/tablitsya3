# Скрипт для розгортання на Render з автоматичною ініціалізацією БД
# PowerShell

Write-Host "🚀 Deploying to Render with Database Initialization..." -ForegroundColor Cyan

# 1. Commit changes
Write-Host "`n📝 Committing changes..." -ForegroundColor Yellow
git add .
$commitMessage = Read-Host "Enter commit message (or press Enter for default)"
if ([string]::IsNullOrWhiteSpace($commitMessage)) {
    $commitMessage = "Database schema initialization fix"
}
git commit -m $commitMessage

# 2. Push to GitHub
Write-Host "`n📤 Pushing to GitHub..." -ForegroundColor Yellow
git push origin master

Write-Host "`n✅ Code pushed to GitHub!" -ForegroundColor Green
Write-Host ""
Write-Host "⚠️  IMPORTANT: Database Initialization Required" -ForegroundColor Red
Write-Host "=============================================" -ForegroundColor Red
Write-Host ""
Write-Host "Before the app can work, you MUST create the database tables:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Go to Render Dashboard: https://dashboard.render.com" -ForegroundColor White
Write-Host "2. Select your PostgreSQL database" -ForegroundColor White
Write-Host "3. Click 'Connect' → 'Web Shell'" -ForegroundColor White
Write-Host "4. Copy the contents of 'Database/create-database.sql' file" -ForegroundColor White
Write-Host "5. Paste into the Web Shell and press Enter" -ForegroundColor White
Write-Host ""
Write-Host "📋 SQL files are now in the Database/ folder:" -ForegroundColor Cyan
Write-Host "   - Database/create-database.sql (create tables)" -ForegroundColor Gray
Write-Host "   - Database/drop-database.sql (delete all data)" -ForegroundColor Gray
Write-Host ""
Write-Host "⚠️  NOTE: Visual Studio shows SQL80001 errors - IGNORE THEM!" -ForegroundColor Yellow
Write-Host "   These are PostgreSQL scripts, VS only understands SQL Server" -ForegroundColor Gray
Write-Host "   See Database/README-SQL-ERRORS.md for details" -ForegroundColor Gray
Write-Host ""
Write-Host "After running the SQL script:" -ForegroundColor Yellow
Write-Host "1. Wait for Render to finish deploying (check deploy logs)" -ForegroundColor White
Write-Host "2. The app will automatically seed initial data" -ForegroundColor White
Write-Host "3. Your app will be ready to use!" -ForegroundColor White
Write-Host ""
Write-Host "🔍 Monitor deployment:" -ForegroundColor Cyan
Write-Host " https://dashboard.render.com → Your Service → Logs" -ForegroundColor Gray
Write-Host ""
Write-Host "Look for these messages in logs:" -ForegroundColor Yellow
Write-Host "   ✅ Connected to database" -ForegroundColor Green
Write-Host "   ✅ Database schema created/verified" -ForegroundColor Green
Write-Host "   ✅ Initial data seeded" -ForegroundColor Green
Write-Host ""

# Ask if user wants to open Render dashboard
$openDashboard = Read-Host "Open Render Dashboard in browser? (y/n)"
if ($openDashboard -eq 'y' -or $openDashboard -eq 'Y') {
    Start-Process "https://dashboard.render.com"
}

Write-Host "`n✨ Deployment initiated! Follow the steps above to complete setup." -ForegroundColor Cyan
