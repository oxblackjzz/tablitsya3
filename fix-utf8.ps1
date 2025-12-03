# Fix UTF-8 Encoding Script
Write-Host "=== Fixing UTF-8 encoding ===" -ForegroundColor Green

$projectRoot = "Tablitsya3"
$fixedCount = 0

# Process .razor files
Get-ChildItem -Path $projectRoot -Filter "*.razor" -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Raw -Encoding UTF8
  [System.IO.File]::WriteAllText($_.FullName, $content, [System.Text.UTF8Encoding]::new($true))
    Write-Host "Fixed: $($_.Name)" -ForegroundColor Yellow
    $fixedCount++
}

# Process .cs files
Get-ChildItem -Path $projectRoot -Filter "*.cs" -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Raw -Encoding UTF8
    [System.IO.File]::WriteAllText($_.FullName, $content, [System.Text.UTF8Encoding]::new($true))
    Write-Host "Fixed: $($_.Name)" -ForegroundColor Yellow
    $fixedCount++
}

Write-Host "`nTotal fixed: $fixedCount files" -ForegroundColor Green
