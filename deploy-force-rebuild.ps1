# CRITICAL FIX - DATABASE URL

Write-Host "IMPORTANT! Check DATABASE_URL in Render!" -ForegroundColor Red
Write-Host ""
Write-Host "Checklist:" -ForegroundColor Yellow
Write-Host "- DATABASE_URL must start with postgresql://" -ForegroundColor White
Write-Host "- Use Internal Database URL from Render PostgreSQL Dashboard" -ForegroundColor White
Write-Host ""

$continue = Read-Host "DATABASE_URL configured correctly? (y/n)"

if ($continue -ne "y") {
    Write-Host ""
    Write-Host "STOP! Configure DATABASE_URL first:" -ForegroundColor Red
    Write-Host ""
    Write-Host "1. Render Dashboard -> PostgreSQL Database" -ForegroundColor Cyan
    Write-Host "2. Copy 'Internal Database URL'" -ForegroundColor Cyan
    Write-Host "3. Web Service -> Environment -> DATABASE_URL" -ForegroundColor Cyan
    Write-Host "4. Paste Internal URL" -ForegroundColor Cyan
    Write-Host "5. Save Changes" -ForegroundColor Cyan
    Write-Host ""
 exit 1
}

Write-Host ""
Write-Host "Committing changes..." -ForegroundColor Cyan
git add -A
git commit -m "Fix: Force rebuild with DATABASE_URL debugging (v2.5)"

Write-Host "Pushing to GitHub..." -ForegroundColor Cyan
git push origin master

Write-Host ""
Write-Host "Code pushed!" -ForegroundColor Green
Write-Host ""
Write-Host "Wait 2-3 minutes for deployment..." -ForegroundColor Yellow
Write-Host ""
Write-Host "After deployment, look for in logs:" -ForegroundColor Cyan
Write-Host "   DATABASE_URL present: True" -ForegroundColor White
Write-Host "   DATABASE_URL length: 147" -ForegroundColor White
Write-Host "   First 20 chars: postgresql://..." -ForegroundColor White
Write-Host "   Converted Postgres URL to Npgsql format" -ForegroundColor Green
Write-Host "   Database migrations completed" -ForegroundColor Green
Write-Host ""
Write-Host "Check logs: https://dashboard.render.com" -ForegroundColor Yellow
Write-Host ""
