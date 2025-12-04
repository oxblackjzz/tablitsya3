# Quick Render Redeploy After Database URL Fix

Write-Host "🚀 Deploying fixed version to Render..." -ForegroundColor Green
Write-Host ""
Write-Host "⚠️  IMPORTANT: Before pushing, update your DATABASE_URL in Render Dashboard!" -ForegroundColor Yellow
Write-Host ""
Write-Host "Steps to fix DATABASE_URL:" -ForegroundColor Cyan
Write-Host "1. Go to https://dashboard.render.com" -ForegroundColor White
Write-Host "2. Click your PostgreSQL database" -ForegroundColor White
Write-Host "3. Copy the 'Internal Database URL' (starts with postgres://)" -ForegroundColor White
Write-Host "4. Go to your Web Service > Environment" -ForegroundColor White
Write-Host "5. Update DATABASE_URL with the Internal URL" -ForegroundColor White
Write-Host "6. Save and it will auto-redeploy" -ForegroundColor White
Write-Host ""

$continue = Read-Host "Have you updated the DATABASE_URL in Render? (y/n)"

if ($continue -ne "y") {
    Write-Host "❌ Cancelled. Please update DATABASE_URL first." -ForegroundColor Red
    Write-Host "📖 See RENDER_DATABASE_URL_FIX.md for detailed instructions" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "📦 Committing changes..." -ForegroundColor Cyan
git add .
git commit -m "Fix: Add better DATABASE_URL parsing and fallback to file storage"

Write-Host "📤 Pushing to GitHub..." -ForegroundColor Cyan
git push origin master

Write-Host ""
Write-Host "✅ Pushed to GitHub!" -ForegroundColor Green
Write-Host "🔄 Render will auto-deploy in a few moments..." -ForegroundColor Cyan
Write-Host ""
Write-Host "📊 Monitor deployment:" -ForegroundColor Yellow
Write-Host "   https://dashboard.render.com" -ForegroundColor White
Write-Host ""
Write-Host "✨ Expected log output:" -ForegroundColor Yellow
Write-Host "   ✅ Converted Postgres URL to Npgsql format" -ForegroundColor White
Write-Host "   ✅ PostgreSQL Database configured" -ForegroundColor White
Write-Host "   ✅ Database migrations completed" -ForegroundColor White
Write-Host ""
