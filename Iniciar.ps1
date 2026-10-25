param([switch]$SinAbrirNavegador)
$ErrorActionPreference='Stop'
Set-Location $PSScriptRoot
$localDir=Join-Path $PSScriptRoot '.local'
New-Item -ItemType Directory -Force $localDir | Out-Null
$mysqlHome='C:\Program Files\MySQL\MySQL Server 8.0'
$mysqlServer=Join-Path $mysqlHome 'bin\mysqld.exe'
$mysqlClient=Join-Path $mysqlHome 'bin\mysql.exe'
$configPath=Join-Path $PSScriptRoot 'appsettings.Local.json'
$dataPath=Join-Path $localDir 'mysql'
function New-LocalSecret { return [Guid]::NewGuid().ToString('N')+[Guid]::NewGuid().ToString('N') }
function Test-Port([int]$Port) {
    $client=New-Object Net.Sockets.TcpClient
    try { $task=$client.ConnectAsync('127.0.0.1',$Port);return $task.Wait(500) -and $client.Connected } catch{return $false}finally{$client.Dispose()}
}
if(!(Test-Path $configPath)) {
    if(!(Test-Path $mysqlServer)){throw "No se encontro MySQL Server 8.0 en $mysqlHome. Configure appsettings.Local.json para su servidor."}
    if(Test-Port 3307){throw 'El puerto 3307 esta ocupado. Cierre la instancia anterior o configure otro puerto.'}
    if(Test-Path (Join-Path $dataPath 'mysql')){throw 'Ya hay datos locales sin archivo de configuracion. Recupere appsettings.Local.json antes de continuar.'}
    New-Item -ItemType Directory -Force $dataPath | Out-Null
    Write-Host 'Preparando MySQL local para la demostracion...'
    $initArgs="--no-defaults --initialize-insecure --basedir=`"$mysqlHome`" --datadir=`"$dataPath`" --console"
    $initialized=Start-Process $mysqlServer -ArgumentList $initArgs -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput (Join-Path $localDir 'mysql-initialize-out.log') -RedirectStandardError (Join-Path $localDir 'mysql-initialize.log')
    if($initialized.ExitCode -ne 0){throw 'No se pudo inicializar MySQL. Consulte .local/mysql-initialize.log.'}
    $serverArgs="--no-defaults --basedir=`"$mysqlHome`" --datadir=`"$dataPath`" --port=3307 --bind-address=127.0.0.1 --mysqlx=OFF --console"
    $mysqlProcess=Start-Process $mysqlServer -ArgumentList $serverArgs -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $localDir 'mysql-out.log') -RedirectStandardError (Join-Path $localDir 'mysql-error.log')
    $mysqlProcess.Id | Set-Content (Join-Path $localDir 'mysql.pid')
    for($i=0;$i -lt 30;$i++){if(Test-Port 3307){break};Start-Sleep -Milliseconds 500}
    if(!(Test-Port 3307)){throw 'MySQL no pudo iniciar. Revise .local/mysql-error.log.'}
    $appSecret=New-LocalSecret
    $rootSecret=New-LocalSecret
    $sql="CREATE DATABASE punto_venta_demo CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci; CREATE USER 'pv_app'@'localhost' IDENTIFIED BY '$appSecret'; GRANT ALL PRIVILEGES ON punto_venta_demo.* TO 'pv_app'@'localhost'; ALTER USER 'root'@'localhost' IDENTIFIED BY '$rootSecret';"
    $sql | & $mysqlClient --no-defaults --host=127.0.0.1 --port=3307 --user=root
    if($LASTEXITCODE -ne 0){throw 'No se pudo configurar el usuario de MySQL.'}
    @('[client]','host=127.0.0.1','port=3307','user=root',"password=$rootSecret") | Set-Content (Join-Path $localDir 'mysql-admin.cnf') -Encoding ASCII
    $config=@{ConnectionStrings=@{MySql="Server=127.0.0.1;Port=3307;Database=punto_venta_demo;User ID=pv_app;Password=$appSecret;SslMode=Disabled;AllowPublicKeyRetrieval=True;"};Jwt=@{Key=(New-LocalSecret)};Seed=@{AdminPassword=(New-LocalSecret);EmployeePassword=(New-LocalSecret);DemoData=$true};Database=@{Initialize=$true};ApiBaseUrl='http://127.0.0.1:5080/';LocalDemo=$true}
    $config | ConvertTo-Json -Depth 5 | Set-Content $configPath -Encoding UTF8
}
$config=Get-Content $configPath -Raw | ConvertFrom-Json
if($config.LocalDemo -eq $true -and !(Test-Port 3307)) {
    $serverArgs="--no-defaults --basedir=`"$mysqlHome`" --datadir=`"$dataPath`" --port=3307 --bind-address=127.0.0.1 --mysqlx=OFF --console"
    $mysqlProcess=Start-Process $mysqlServer -ArgumentList $serverArgs -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $localDir 'mysql-out.log') -RedirectStandardError (Join-Path $localDir 'mysql-error.log')
    $mysqlProcess.Id | Set-Content (Join-Path $localDir 'mysql.pid')
    for($i=0;$i -lt 30;$i++){if(Test-Port 3307){break};Start-Sleep -Milliseconds 500}
    if(!(Test-Port 3307)){throw 'MySQL no pudo iniciar. Revise .local/mysql-error.log.'}
}
if(!(Test-Port 5080)) {
    Write-Host 'Compilando Punto de Venta...'
    dotnet build --nologo -p:NuGetAudit=false
    if($LASTEXITCODE -ne 0){throw 'La compilacion fallo. Revise los mensajes anteriores.'}
    $appProcess=Start-Process 'dotnet' -ArgumentList 'bin/Debug/net10.0/PuntoVenta.dll --urls http://127.0.0.1:5080' -WorkingDirectory $PSScriptRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $localDir 'app-out.log') -RedirectStandardError (Join-Path $localDir 'app-error.log')
    $appProcess.Id | Set-Content (Join-Path $localDir 'app.pid')
    $appProcess.StartTime.ToUniversalTime().Ticks | Set-Content (Join-Path $localDir 'app-start.ticks')
    for($i=0;$i -lt 40;$i++){if(Test-Port 5080){break};Start-Sleep -Milliseconds 500}
    if(!(Test-Port 5080)){throw 'La aplicacion no pudo iniciar. Revise .local/app-out.log y .local/app-error.log.'}
}
Write-Host ''
Write-Host 'PUNTO DE VENTA LISTO: http://127.0.0.1:5080' -ForegroundColor Green
Write-Host 'Usuario de demostracion: admin'
Write-Host ('Clave inicial admin (privada): '+$config.Seed.AdminPassword)
Write-Host ('Clave inicial EMPLEADO (privada): '+$config.Seed.EmployeePassword)
Write-Host 'Los datos se conservan al cerrar y volver a iniciar.'
if(!$SinAbrirNavegador){Start-Process 'http://127.0.0.1:5080'}
