$packageRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$diagnostics = Join-Path $packageRoot 'diagnostics'
New-Item -ItemType Directory -Path $diagnostics -Force | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$report = Join-Path $diagnostics "环境诊断-$stamp.txt"
"时间：$(Get-Date -Format o)" | Set-Content -LiteralPath $report -Encoding UTF8
Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -match 'wps|kingsoft|kwps' } | Select-Object ProcessName,Id,Path,ProductVersion,StartTime | Format-List | Out-File -LiteralPath $report -Append -Encoding UTF8
try { "Bridge health：$((Invoke-WebRequest -UseBasicParsing -TimeoutSec 3 'http://127.0.0.1:43801/health').Content)" | Out-File -LiteralPath $report -Append -Encoding UTF8 } catch { 'Bridge health：未响应' | Out-File -LiteralPath $report -Append -Encoding UTF8 }
try { "WPS service：$((Invoke-WebRequest -UseBasicParsing -TimeoutSec 3 'http://127.0.0.1:58890/version').Content)" | Out-File -LiteralPath $report -Append -Encoding UTF8 } catch { 'WPS service：未响应' | Out-File -LiteralPath $report -Append -Encoding UTF8 }
$publish = Join-Path $env:APPDATA 'kingsoft\wps\jsaddons\publish.xml'
"publish.xml：$(if (Test-Path -LiteralPath $publish) {'存在；涉网试验=' + ((Get-Content -Raw $publish) -match 'gridreportwps')} else {'不存在'})" | Out-File -LiteralPath $report -Append -Encoding UTF8
$logRoot = Join-Path $env:LOCALAPPDATA 'GridReport\logs'
if (Test-Path -LiteralPath $logRoot) { Get-ChildItem -LiteralPath $logRoot -Filter '*.log' -File | Where-Object { $_.LastWriteTime -gt (Get-Date).AddDays(-2) } | Select-Object -First 5 | Copy-Item -Destination $diagnostics -Force }
$zip = Join-Path $diagnostics 'WPS测试诊断包.zip'
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
Compress-Archive -Path (Join-Path $diagnostics '*') -DestinationPath $zip -Force
Write-Host "已生成：$zip" -ForegroundColor Green
