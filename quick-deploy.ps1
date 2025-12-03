# ?? Автоматичний деплой на GitHub
# Запустіть цей скрипт і все буде готове для Render/Railway/Vercel

param(
    [string]$GitHubUsername = "",
    [string]$RepoName = "tablitsya3"
)

Write-Host "?????????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?   ?? АВТОМАТИЧНЕ РОЗГОРТАННЯ BLAZOR ПРОЕКТУ ??      ?" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????????????????????" -ForegroundColor Cyan

# Перевірка Git
Write-Host "`n1?? Перевірка Git..." -ForegroundColor Yellow
if (!(Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Host "? Git не встановлено!" -ForegroundColor Red
    Write-Host "?? Завантажте з: https://git-scm.com/download/win" -ForegroundColor Yellow
    exit 1
}
Write-Host "? Git встановлено" -ForegroundColor Green

# GitHub username
if ($GitHubUsername -eq "") {
 Write-Host "`n?? Введіть ваш GitHub username:" -ForegroundColor Cyan
    $GitHubUsername = Read-Host
}

# Ініціалізація Git (якщо не ініціалізовано)
Write-Host "`n2?? Ініціалізація Git репозиторію..." -ForegroundColor Yellow
if (!(Test-Path ".git")) {
    git init
    Write-Host "? Git ініціалізовано" -ForegroundColor Green
} else {
    Write-Host "? Git вже ініціалізовано" -ForegroundColor Green
}

# Перевірка .gitignore
Write-Host "`n3?? Перевірка .gitignore..." -ForegroundColor Yellow
if (Test-Path ".gitignore") {
    Write-Host "? .gitignore існує" -ForegroundColor Green
} else {
    Write-Host "?? .gitignore не знайдено, створюємо..." -ForegroundColor Yellow
    # Створення базового .gitignore буде автоматично
}

# Додавання файлів
Write-Host "`n4?? Додавання файлів до Git..." -ForegroundColor Yellow
git add .
Write-Host "? Файли додано" -ForegroundColor Green

# Commit
Write-Host "`n5?? Створення commit..." -ForegroundColor Yellow
$commitMessage = "Beta version ready for deployment"
git commit -m "$commitMessage"
if ($LASTEXITCODE -eq 0) {
    Write-Host "? Commit створено" -ForegroundColor Green
} else {
    Write-Host "?? Нічого не змінилось або вже закоммічено" -ForegroundColor Yellow
}

# Перейменування гілки на main
Write-Host "`n6?? Налаштування гілки main..." -ForegroundColor Yellow
$currentBranch = git branch --show-current
if ($currentBranch -ne "main") {
    git branch -M main
    Write-Host "? Гілка перейменована на main" -ForegroundColor Green
} else {
    Write-Host "? Гілка вже main" -ForegroundColor Green
}

# Додавання remote
Write-Host "`n7?? Підключення до GitHub..." -ForegroundColor Yellow
$remoteUrl = "https://github.com/$GitHubUsername/$RepoName.git"

# Перевірка чи існує remote
$existingRemote = git remote get-url origin 2>$null
if ($LASTEXITCODE -ne 0) {
    git remote add origin $remoteUrl
    Write-Host "? Remote додано: $remoteUrl" -ForegroundColor Green
} else {
    Write-Host "? Remote вже налаштовано: $existingRemote" -ForegroundColor Green
  
    # Якщо remote відрізняється, оновлюємо
    if ($existingRemote -ne $remoteUrl) {
        git remote set-url origin $remoteUrl
        Write-Host "? Remote оновлено на: $remoteUrl" -ForegroundColor Green
    }
}

# Push
Write-Host "`n8?? Завантаження на GitHub..." -ForegroundColor Yellow
Write-Host "? Це може зайняти кілька хвилин..." -ForegroundColor Cyan

git push -u origin main

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Код успішно завантажено на GitHub!" -ForegroundColor Green
} else {
    Write-Host "? Помилка при завантаженні!" -ForegroundColor Red
    Write-Host "Можливо репозиторій не існує. Створіть його на GitHub:" -ForegroundColor Yellow
    Write-Host "https://github.com/new" -ForegroundColor Cyan
    Write-Host "Назва репозиторію: $RepoName" -ForegroundColor Cyan
    exit 1
}

# Підсумок
Write-Host "`n?????????????????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host "?     ?? ГОТОВО! КОД НА GITHUB! ??           ?" -ForegroundColor Green
Write-Host "?????????????????????????????????????????????????????????????????" -ForegroundColor Green

Write-Host "`n?? Ваш репозиторій:" -ForegroundColor Cyan
Write-Host "   https://github.com/$GitHubUsername/$RepoName" -ForegroundColor Yellow

Write-Host "`n?? НАСТУПНІ КРОКИ:" -ForegroundColor Cyan
Write-Host ""
Write-Host "Оберіть один з варіантів розгортання:" -ForegroundColor White
Write-Host ""

Write-Host "1?? Render.com (Безкоштовно, найпростіше для .NET):" -ForegroundColor Yellow
Write-Host "   • Відкрийте: https://render.com" -ForegroundColor White
Write-Host "   • Sign up with GitHub" -ForegroundColor White
Write-Host "   • New ? Web Service" -ForegroundColor White
Write-Host "   • Connect репозиторій: $RepoName" -ForegroundColor White
Write-Host "   • Deploy!" -ForegroundColor White
Write-Host "   ?? Час: 5 хвилин" -ForegroundColor Cyan
Write-Host ""

Write-Host "2?? Railway.app (5/міс, найстабільніше):" -ForegroundColor Yellow
Write-Host "   • Відкрийте: https://railway.app" -ForegroundColor White
Write-Host "   • Sign up with GitHub" -ForegroundColor White
Write-Host "   • New Project ? Deploy from GitHub" -ForegroundColor White
Write-Host "   • Виберіть: $RepoName" -ForegroundColor White
Write-Host "   • Deploy!" -ForegroundColor White
Write-Host "   ?? Час: 5 хвилин" -ForegroundColor Cyan
Write-Host ""

Write-Host "3?? Azure App Service (безкоштовний F1 план):" -ForegroundColor Yellow
Write-Host "   • Запустіть: .\deploy-to-azure.ps1" -ForegroundColor White
Write-Host "   ?? Час: 15 хвилин" -ForegroundColor Cyan
Write-Host ""

Write-Host "?? Детальні інструкції:" -ForegroundColor Cyan
Write-Host "   • ULTRA_SIMPLE_DEPLOY.md - найпростіше" -ForegroundColor White
Write-Host " • DEPLOYMENT_GUIDE.md - детально для Azure" -ForegroundColor White
Write-Host "   • DEPLOYMENT_ALTERNATIVES.md - всі варіанти" -ForegroundColor White

Write-Host "`n?? РЕКОМЕНДАЦІЯ: Render.com" -ForegroundColor Magenta
Write-Host "   ? Безкоштовно" -ForegroundColor Green
Write-Host "? 0 налаштувань" -ForegroundColor Green
Write-Host "   ? Підтримка .NET 9" -ForegroundColor Green
Write-Host "   ? Dockerfile вже готовий!" -ForegroundColor Green

Write-Host "`n? Успіхів з запуском! ??`n" -ForegroundColor Green
