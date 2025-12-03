# ?? Скрипт автоматичного розгортання на Azure
# Запустіть в PowerShell з папки проекту

param(
    [string]$ResourceGroup = "tablitsya3-rg",
    [string]$AppName = "tablitsya3-beta",
    [string]$Location = "westeurope",
[string]$Plan = "tablitsya3-plan"
)

Write-Host "?? Початок розгортання Tablitsya3 на Azure..." -ForegroundColor Green

# Перевірка наявності Azure CLI
Write-Host "`n1?? Перевірка Azure CLI..." -ForegroundColor Cyan
if (!(Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Host "? Azure CLI не встановлено!" -ForegroundColor Red
    Write-Host "?? Завантажте з: https://aka.ms/installazurecliwindows" -ForegroundColor Yellow
    exit 1
}
Write-Host "? Azure CLI встановлено" -ForegroundColor Green

# Вхід в Azure
Write-Host "`n2?? Вхід в Azure..." -ForegroundColor Cyan
az account show 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "?? Необхідна авторизація..." -ForegroundColor Yellow
    az login
}
Write-Host "? Авторизовано" -ForegroundColor Green

# Створення групи ресурсів
Write-Host "`n3?? Створення групи ресурсів..." -ForegroundColor Cyan
$rgExists = az group exists --name $ResourceGroup
if ($rgExists -eq "false") {
    Write-Host "?? Створюємо групу ресурсів $ResourceGroup..." -ForegroundColor Yellow
    az group create --name $ResourceGroup --location $Location
    Write-Host "? Групу ресурсів створено" -ForegroundColor Green
} else {
    Write-Host "? Група ресурсів вже існує" -ForegroundColor Green
}

# Створення App Service Plan
Write-Host "`n4?? Створення App Service Plan..." -ForegroundColor Cyan
$planExists = az appservice plan show --name $Plan --resource-group $ResourceGroup 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "?? Створюємо план $Plan (безкоштовний F1)..." -ForegroundColor Yellow
    az appservice plan create `
    --name $Plan `
   --resource-group $ResourceGroup `
        --sku F1 `
        --is-linux false
    Write-Host "? План створено" -ForegroundColor Green
} else {
    Write-Host "? План вже існує" -ForegroundColor Green
}

# Створення Web App
Write-Host "`n5?? Створення Web App..." -ForegroundColor Cyan
$appExists = az webapp show --name $AppName --resource-group $ResourceGroup 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "?? Створюємо додаток $AppName..." -ForegroundColor Yellow
    az webapp create `
        --name $AppName `
   --resource-group $ResourceGroup `
        --plan $Plan `
 --runtime "DOTNET:9.0"
    Write-Host "? Додаток створено" -ForegroundColor Green
} else {
    Write-Host "? Додаток вже існує" -ForegroundColor Green
}

# Налаштування Application Settings
Write-Host "`n6?? Налаштування Application Settings..." -ForegroundColor Cyan
az webapp config appsettings set `
    --name $AppName `
    --resource-group $ResourceGroup `
    --settings `
  ASPNETCORE_ENVIRONMENT=Production `
        WEBSITE_HTTPLOGGING_RETENTION_DAYS=7
Write-Host "? Налаштування збережено" -ForegroundColor Green

# Збірка проекту
Write-Host "`n7?? Збірка проекту..." -ForegroundColor Cyan
$projectPath = ".\таблиця3"
if (Test-Path $projectPath) {
    Push-Location $projectPath
    
    Write-Host "?? Компіляція..." -ForegroundColor Yellow
    dotnet build --configuration Release
    
    if ($LASTEXITCODE -eq 0) {
  Write-Host "? Збірка успішна" -ForegroundColor Green
    } else {
        Write-Host "? Помилка збірки!" -ForegroundColor Red
        Pop-Location
        exit 1
    }

    Pop-Location
} else {
    Write-Host "? Папка проекту не знайдена!" -ForegroundColor Red
    exit 1
}

# Публікація проекту
Write-Host "`n8?? Публікація проекту..." -ForegroundColor Cyan
Push-Location $projectPath

Write-Host "?? Створення пакету публікації..." -ForegroundColor Yellow
if (Test-Path ".\publish") {
    Remove-Item ".\publish" -Recurse -Force
}
dotnet publish -c Release -o .\publish

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Публікація створена" -ForegroundColor Green
} else {
    Write-Host "? Помилка публікації!" -ForegroundColor Red
    Pop-Location
    exit 1
}

# Створення ZIP архіву
Write-Host "`n9?? Створення архіву..." -ForegroundColor Cyan
if (Test-Path ".\publish.zip") {
    Remove-Item ".\publish.zip" -Force
}
Compress-Archive -Path ".\publish\*" -DestinationPath ".\publish.zip" -Force
Write-Host "? Архів створено" -ForegroundColor Green

# Розгортання на Azure
Write-Host "`n?? Розгортання на Azure..." -ForegroundColor Cyan
Write-Host "? Це може зайняти 2-5 хвилин..." -ForegroundColor Yellow

az webapp deploy `
    --resource-group $ResourceGroup `
 --name $AppName `
    --src-path ".\publish.zip" `
    --type zip

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Розгортання успішне!" -ForegroundColor Green
} else {
    Write-Host "? Помилка розгортання!" -ForegroundColor Red
    Pop-Location
    exit 1
}

Pop-Location

# Очищення
Write-Host "`n?? Очищення..." -ForegroundColor Cyan
Remove-Item "$projectPath\publish" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "$projectPath\publish.zip" -Force -ErrorAction SilentlyContinue
Write-Host "? Очищено" -ForegroundColor Green

# Отримання URL сайту
Write-Host "`n?? Отримання інформації про додаток..." -ForegroundColor Cyan
$appUrl = "https://$AppName.azurewebsites.net"

Write-Host "`n?????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host "?           ?? РОЗГОРТАННЯ ЗАВЕРШЕНО! ??   ?" -ForegroundColor Green
Write-Host "?????????????????????????????????????????????????????" -ForegroundColor Green

Write-Host "`n?? Ваш сайт доступний за адресою:" -ForegroundColor Cyan
Write-Host "   $appUrl" -ForegroundColor Yellow -NoNewline
Write-Host ""

Write-Host "`n?? Корисні команди:" -ForegroundColor Cyan
Write-Host "   Логи:      az webapp log tail --name $AppName --resource-group $ResourceGroup" -ForegroundColor White
Write-Host "   Перезапуск: az webapp restart --name $AppName --resource-group $ResourceGroup" -ForegroundColor White
Write-Host "   Видалення:  az group delete --name $ResourceGroup --yes" -ForegroundColor White

Write-Host "`n?? Підказка: Зачекайте 1-2 хвилини перед першим відкриттям сайту" -ForegroundColor Yellow

# Пропонуємо відкрити сайт
Write-Host "`n?? Відкрити сайт зараз? (Y/N): " -ForegroundColor Green -NoNewline
$answer = Read-Host

if ($answer -eq "Y" -or $answer -eq "y") {
    Start-Process $appUrl
}

Write-Host "`n? Дякуємо що використовуєте наш скрипт розгортання!" -ForegroundColor Magenta
