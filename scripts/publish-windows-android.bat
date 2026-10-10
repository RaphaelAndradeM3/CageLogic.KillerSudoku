@echo off
setlocal

rem Publishes the unpackaged Windows app and a locally signed Android APK.
rem The Android signing identity is created once under LOCALAPPDATA and reused.

for %%I in ("%~dp0..") do set "ROOT=%%~fI"
set "PROJECT=%ROOT%\src\CageLogic.Maui\CageLogic.Maui.csproj"
set "PUBLISH_ROOT=%ROOT%\artifacts\publish"
set "WINDOWS_OUT=%PUBLISH_ROOT%\windows"
set "ANDROID_OUT=%PUBLISH_ROOT%\android"
set "SIGNING_DIR=%LOCALAPPDATA%\CageLogic\KillerSudoku\signing"
set "KEYSTORE=%SIGNING_DIR%\cagelogic-local-release.p12"
set "PASSWORD_FILE=%SIGNING_DIR%\keystore-password.txt"
set "CAGELOGIC_SIGNING_PASSWORD_FILE=%PASSWORD_FILE%"

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERRO: .NET SDK nao foi encontrado no PATH.
    exit /b 1
)

if not exist "%PROJECT%" (
    echo ERRO: projeto MAUI nao encontrado: "%PROJECT%"
    exit /b 1
)

if not exist "%WINDOWS_OUT%" (
    mkdir "%WINDOWS_OUT%"
    if errorlevel 1 goto publish_failed
)
if not exist "%ANDROID_OUT%" (
    mkdir "%ANDROID_OUT%"
    if errorlevel 1 goto publish_failed
)

echo.
echo [1/2] Publicando Windows x64...
dotnet publish "%PROJECT%" -f net10.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win-x64 -p:SelfContained=true -p:WindowsAppSDKSelfContained=true "-p:PublishDir=%WINDOWS_OUT%"
if errorlevel 1 goto publish_failed

echo.
call :prepare_android_signing
if errorlevel 1 goto publish_failed

echo.
echo [2/2] Publicando APK Android assinado...
del /q "%ANDROID_OUT%\*.apk" 2>nul
if exist "%ANDROID_OUT%\*.apk" (
    echo ERRO: nao foi possivel remover APKs antigos de "%ANDROID_OUT%".
    exit /b 1
)
dotnet publish "%PROJECT%" -f net10.0-android -c Release -p:AndroidKeyStore=true -p:AndroidPackageFormats=apk "-p:AndroidSigningKeyStore=%KEYSTORE%" -p:AndroidSigningKeyAlias=cagelogic "-p:AndroidSigningStorePass=file:%PASSWORD_FILE%" "-p:AndroidSigningKeyPass=file:%PASSWORD_FILE%" "-p:PublishDir=%ANDROID_OUT%"
if errorlevel 1 goto publish_failed

set "APK_FOUND="
for %%F in ("%ANDROID_OUT%\*.apk") do if exist "%%~fF" set "APK_FOUND=%%~fF"
if not defined APK_FOUND (
    echo ERRO: a publicacao terminou, mas nenhum APK foi encontrado em "%ANDROID_OUT%".
    exit /b 1
)

echo.
echo Publicacao concluida.
echo Windows: "%WINDOWS_OUT%"
echo APK Android assinado: "%APK_FOUND%"
echo Chave de assinatura local mantida em: "%SIGNING_DIR%"
echo Copie o APK para o celular e autorize a instalacao dessa origem, se solicitado.
exit /b 0

:prepare_android_signing
if not defined LOCALAPPDATA (
    echo ERRO: a variavel LOCALAPPDATA nao esta definida.
    exit /b 1
)

if not exist "%SIGNING_DIR%" mkdir "%SIGNING_DIR%"
if errorlevel 1 (
    echo ERRO: nao foi possivel criar a pasta local de assinatura: "%SIGNING_DIR%".
    exit /b 1
)

if exist "%KEYSTORE%" if not exist "%PASSWORD_FILE%" (
    echo ERRO: a chave local existe, mas o arquivo de senha foi removido.
    echo Restaure a pasta de assinatura de um backup; nao sera criada outra chave automaticamente.
    exit /b 1
)

set "KEYTOOL="
for /f "delims=" %%K in ('where.exe keytool.exe 2^>nul') do if not defined KEYTOOL set "KEYTOOL=%%K"
if not defined KEYTOOL if defined JAVA_HOME if exist "%JAVA_HOME%\bin\keytool.exe" set "KEYTOOL=%JAVA_HOME%\bin\keytool.exe"
if not defined KEYTOOL for /d %%J in ("%ProgramFiles(x86)%\Android\openjdk\jdk-*") do if exist "%%~fJ\bin\keytool.exe" set "KEYTOOL=%%~fJ\bin\keytool.exe"
if not defined KEYTOOL (
    echo ERRO: keytool nao encontrado. Instale/configure o JDK usado pelo workload Android.
    exit /b 1
)

if not exist "%PASSWORD_FILE%" (
    echo Criando identidade local de assinatura Android pela primeira vez...
    powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -Command "$bytes = New-Object byte[] 48; $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create(); $rng.GetBytes($bytes); [System.IO.File]::WriteAllText($env:CAGELOGIC_SIGNING_PASSWORD_FILE, [Convert]::ToBase64String($bytes), [System.Text.Encoding]::ASCII)"
    if errorlevel 1 (
        echo ERRO: nao foi possivel gerar a senha local de assinatura.
        exit /b 1
    )
)

if not exist "%KEYSTORE%" (
    "%KEYTOOL%" -genkeypair -noprompt -v -keystore "%KEYSTORE%" -storetype PKCS12 -alias cagelogic -keyalg RSA -keysize 2048 -validity 10000 -dname "CN=CageLogic Local Signing,O=CageLogic,C=BR" -storepass:file "%PASSWORD_FILE%" -keypass:file "%PASSWORD_FILE%"
    if errorlevel 1 (
        echo ERRO: nao foi possivel criar o keystore Android local.
        exit /b 1
    )
)

exit /b 0

:publish_failed
echo.
echo ERRO: uma das etapas de publicacao falhou. Leia as mensagens acima e corrija o erro antes de repetir.
exit /b 1
