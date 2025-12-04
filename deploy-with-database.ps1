# 🚀 Швидкий Deploy з PostgreSQL

Write-Host "🗄️  Deploying with PostgreSQL Database Support" -ForegroundColor Cyan
Write-Host "=" * 60

# Перевірка чи є зміни
Write-Host "`n📝 Checking for changes..." -ForegroundColor Yellow
$status = git status --porcelain
if (-not $status) {
 Write-Host "✅ No changes to commit" -ForegroundColor Green
} else {
    Write-Host "📦 Changes detected, committing..." -ForegroundColor Yellow
    
    # Add all changes
    git add -A
    
    # Commit with timestamp
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $commitMessage = "Deploy: Add PostgreSQL database support - $timestamp"
    
    Write-Host "💾 Committing: $commitMessage" -ForegroundColor Cyan
    git commit -m $commitMessage
}

# Push to GitHub
Write-Host "`n🚀 Pushing to GitHub..." -ForegroundColor Yellow
try {
    git push origin master
    Write-Host "✅ Successfully pushed to GitHub!" -ForegroundColor Green
} catch {
 Write-Host "❌ Failed to push to GitHub" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host "`n" + ("=" * 60)
Write-Host "✅ DEPLOY STARTED!" -ForegroundColor Green
Write-Host ("=" * 60)

Write-Host "`n📊 What's happening on Render:" -ForegroundColor Cyan
Write-Host "  1️⃣  Creating PostgreSQL database (tablitsya3-db)" -ForegroundColor White
Write-Host "  2️⃣  Building Docker container" -ForegroundColor White
Write-Host "  3️⃣  Connecting web service to database" -ForegroundColor White
Write-Host "  4️⃣  Running database migrations" -ForegroundColor White
Write-Host "  5️⃣  Seeding initial data" -ForegroundColor White
Write-Host "  6️⃣  Deploying application" -ForegroundColor White

Write-Host "`n⏱️  Expected time: 5-7 minutes for first deploy (database creation)" -ForegroundColor Yellow
Write-Host "⏱️  Next deploys: 2-3 minutes" -ForegroundColor Yellow

Write-Host "`n🌐 Live URL:" -ForegroundColor Cyan
Write-Host "https://tablitsya3.onrender.com" -ForegroundColor White

Write-Host "`n📊 Database Info:" -ForegroundColor Cyan
Write-Host "   Name: tablitsya3-db" -ForegroundColor White
Write-Host "   Type: PostgreSQL (Free tier)" -ForegroundColor White
Write-Host "   Region: Frankfurt" -ForegroundColor White
Write-Host "   Retention: 90 days" -ForegroundColor White

Write-Host "`n🔍 Monitor deployment:" -ForegroundColor Cyan
Write-Host "   https://dashboard.render.com" -ForegroundColor White

Write-Host "`n💡 Tips:" -ForegroundColor Yellow
Write-Host "   • Your data is now persistent! ✅" -ForegroundColor Green
Write-Host "   • Updates won't delete orders anymore ✅" -ForegroundColor Green
Write-Host "   • Database backups available in Render dashboard ✅" -ForegroundColor Green
Write-Host "   • Check logs: 'View Logs' in Render dashboard" -ForegroundColor White

Write-Host "`n✨ Database features:" -ForegroundColor Magenta
Write-Host "   • Automatic migrations on deploy" -ForegroundColor White
Write-Host "   • Seed data loaded on first run" -ForegroundColor White
Write-Host "   • Connection string auto-configured" -ForegroundColor White
Write-Host "   • Fallback to file storage if DB unavailable" -ForegroundColor White

Write-Host "`n📖 For more info, read: DATABASE_SETUP.md" -ForegroundColor Cyan
Write-Host ("=" * 60)

# Wait a bit and open browser
Start-Sleep -Seconds 2
Write-Host "`n🌐 Opening Render dashboard..." -ForegroundColor Cyan
Start-Process "https://dashboard.render.com"
