function Find-WpsWriter {
    $command = Get-Command wps.exe -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Source
    if ($command -and (Test-Path -LiteralPath $command)) { return $command }
    $roots = @('C:\Program Files\Kingsoft','C:\Program Files (x86)\Kingsoft',(Join-Path $env:LOCALAPPDATA 'Kingsoft')) | Where-Object { Test-Path -LiteralPath $_ }
    foreach ($root in $roots) {
        $writer = Get-ChildItem -LiteralPath $root -Filter 'wps.exe' -File -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($writer) { return $writer.FullName }
    }
    return $null
}
function Test-Http([string]$url) { try { (Invoke-WebRequest -UseBasicParsing -TimeoutSec 3 $url).StatusCode -eq 200 } catch { $false } }

$wpsPath = Find-WpsWriter
Write-Host '=== WPS 测试环境检查 ===' -ForegroundColor Cyan
if ($wpsPath) {
    $version = (Get-Item -LiteralPath $wpsPath).VersionInfo.ProductVersion
    Write-Host "WPS Writer：已找到`n路径：$wpsPath`n版本：$version" -ForegroundColor Green
} else { Write-Host 'WPS Writer：未找到（请确认已安装 WPS 文字）' -ForegroundColor Red }
Write-Host ("WPS 加载项服务（127.0.0.1:58890）：" + $(if (Test-Http 'http://127.0.0.1:58890/version') {'正常'} else {'未响应'}))
Write-Host ("GridReport Bridge（127.0.0.1:43801）：" + $(if (Test-Http 'http://127.0.0.1:43801/health') {'已连接'} else {'未响应，请先启动 GridReport'}))
$publish = Join-Path $env:APPDATA 'kingsoft\wps\jsaddons\publish.xml'
if (Test-Path -LiteralPath $publish) {
    $installed = (Get-Content -Raw -LiteralPath $publish) -match 'gridreportwps'
    Write-Host ("加载项配置：存在；涉网试验=" + $(if ($installed) {'已安装'} else {'未安装'}))
} else { Write-Host '加载项配置：未找到 publish.xml' }
