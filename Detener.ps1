$ErrorActionPreference='Stop'
Set-Location $PSScriptRoot
$localDir=Join-Path $PSScriptRoot '.local'
$appPidFile=Join-Path $localDir 'app.pid'
$startFile=Join-Path $localDir 'app-start.ticks'
if((Test-Path $appPidFile) -and (Test-Path $startFile)) {
    $appProcessId=[int](Get-Content $appPidFile)
    $process=Get-Process -Id $appProcessId -ErrorAction SilentlyContinue
    $expectedTicks=[long](Get-Content $startFile)
    if($process -and $process.ProcessName -eq 'dotnet' -and $process.StartTime.ToUniversalTime().Ticks -eq $expectedTicks) {
        Stop-Process -Id $appProcessId
        Write-Host 'Aplicacion detenida.'
    }
}
$configFile=Join-Path $PSScriptRoot 'appsettings.Local.json'
$adminFile=Join-Path $localDir 'mysql-admin.cnf'
if((Test-Path $configFile) -and (Test-Path $adminFile)) {
    $config=Get-Content $configFile -Raw | ConvertFrom-Json
    if($config.LocalDemo -eq $true) {
        & 'C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqladmin.exe' "--defaults-extra-file=$adminFile" --get-server-public-key --ssl-mode=DISABLED shutdown
        if($LASTEXITCODE -ne 0){throw 'MySQL no pudo detenerse o ya estaba detenido. Consulte el mensaje anterior.'}
        Write-Host 'MySQL local detenido. Los datos permanecen en .local/mysql.'
    }
}
