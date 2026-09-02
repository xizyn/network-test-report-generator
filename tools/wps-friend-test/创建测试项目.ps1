$packageRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $PSScriptRoot 'WPS集成测试项目.gridreport.json'
$outputFolder = Join-Path $packageRoot '05-Test-Output'
New-Item -ItemType Directory -Path $outputFolder -Force | Out-Null
$project = [ordered]@{
    Name = 'WPS集成测试光伏项目'
    CustomerName = '测试新能源有限公司'
    ProjectNumber = 'WPS-TEST-20260902'
    StationName = 'WPS集成测试电站'
    TestDate = '2026-09-02T00:00:00'
    TemplatePath = Join-Path $packageRoot '03-Test-Data\test-template.docx'
    SourceFolder = Join-Path $packageRoot '03-Test-Data'
    OutputFolder = $outputFolder
    Values = [ordered]@{}
    AuditEntries = @()
}
$project | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $projectPath -Encoding UTF8
Write-Host "测试项目已创建：$projectPath" -ForegroundColor Green
Write-Host '请回到 GridReport，点击“打开项目”，选择这个 .gridreport.json 文件。' -ForegroundColor Yellow
