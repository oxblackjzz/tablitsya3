# Швидкий force redeploy з покращеною міграцією
# PowerShell

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "EMERGENCY FIX: Database Migration Failed" -ForegroundColor Red
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Changes made:" -ForegroundColor Yellow
Write-Host "  * Improved DatabaseMigrationService with better error handling" -ForegroundColor Green
Write-Host "  * Added detailed logging for migration process" -ForegroundColor Green
Write-Host "  * Added timeout protection (60 seconds)" -ForegroundColor Green
Write-Host "  * Added fallback to EnsureCreated if migration fails" -ForegroundColor Green
Write-Host ""

# 1. Commit changes
Write-Host "Committing emergency fix..." -ForegroundColor Yellow
git add .
git commit -m "EMERGENCY FIX: Improve database migration with better error handling and logging"

if ($LASTEXITCODE -eq 0) {
    Write-Host "Changes committed" -ForegroundColor Green
} else {
    Write-Host "Nothing to commit (changes already committed)" -ForegroundColor Yellow
}

# 2. Push to GitHub
Write-Host ""
Write-Host "Pushing to GitHub..." -ForegroundColor Yellow
git push origin master

if ($LASTEXITCODE -eq 0) {
    Write-Host "Pushed to GitHub!" -ForegroundColor Green
} else {
    Write-Host "Push failed! Check your internet connection." -ForegroundColor Red
  exit 1
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Code Updated on GitHub!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "NEXT STEPS:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Go to Render Dashboard:" -ForegroundColor White
Write-Host "   https://dashboard.render.com" -ForegroundColor Cyan
Write-Host ""
Write-Host "2. Open your Web Service" -ForegroundColor White
Write-Host ""
Write-Host "3. Click 'Manual Deploy' button" -ForegroundColor White
Write-Host ""
Write-Host "4. WARNING: SELECT 'Clear build cache & deploy'" -ForegroundColor Red
Write-Host "   (This is IMPORTANT to force recompilation!)" -ForegroundColor Red
Write-Host ""
Write-Host "5. Wait 3-5 minutes for deploy to complete" -ForegroundColor White
Write-Host ""
Write-Host "6. Check the Logs tab - you should see:" -ForegroundColor White
Write-Host ""
Write-Host "   ============================================" -ForegroundColor Gray
Write-Host "   STARTING DATABASE INITIALIZATION" -ForegroundColor Gray
Write-Host "   ============================================" -ForegroundColor Gray
Write-Host "   Connected to database" -ForegroundColor Gray
Write-Host "   TABLES NOT FOUND - STARTING MIGRATION" -ForegroundColor Gray
Write-Host "   Database tables created successfully" -ForegroundColor Gray
Write-Host "   DATABASE MIGRATION COMPLETED!" -ForegroundColor Gray
Write-Host "   ============================================" -ForegroundColor Gray
Write-Host ""

$openDashboard = Read-Host "Open Render Dashboard in browser? (y/n)"
if ($openDashboard -eq 'y' -or $openDashboard -eq 'Y') {
    Start-Process "https://dashboard.render.com"
    Write-Host ""
    Write-Host "Browser opened!" -ForegroundColor Green
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Detailed troubleshooting guide:" -ForegroundColor Yellow
Write-Host "   EMERGENCY-FIX-MIGRATION-FAILED.md" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "TIP: Watch the Logs tab during deploy!" -ForegroundColor Yellow
Write-Host " New logs are much more detailed." -ForegroundColor Yellow
Write-Host ""
