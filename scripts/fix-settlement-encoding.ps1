$path = Join-Path $PSScriptRoot '..\V2boardApi\Areas\App\Views\Admin\_GetSettlementSetting.cshtml'
$content = Get-Content -Path $path -Raw -Encoding UTF8
$utf8Bom = New-Object System.Text.UTF8Encoding $true
[System.IO.File]::WriteAllText((Resolve-Path $path), $content, $utf8Bom)
Write-Host "Fixed encoding with UTF-8 BOM: $path"
