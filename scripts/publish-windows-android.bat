@echo off
setlocal

rem Publishes the unpackaged Windows app and a signed Android APK.
rem Keep the Android keystore and password files outside the repository.

for %%I in ("%~dp0..") do set "ROOT=%%~fI"
set "PROJECT=%ROOT%\src\CageLogic.Maui\CageLogic.Maui.csproj"
set "PUBLISH_ROOT=%ROOT%\artifacts\publish"
set "WINDOWS_OUT=%PUBLISH_ROOT%\windows"
set "ANDROID_OUT=%PUBLISH_ROOT%\android"

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
echo Assinatura do APK Android
echo Informe os caminhos para o keystore e para arquivos de texto com as senhas.
echo Digite os caminhos sem aspas; o script adiciona as aspas automaticamente.
echo Os arquivos de senha devem conter somente a senha e ficar fora do repositorio.
echo Se a senha da chave e a senha do keystore forem iguais, informe o mesmo arquivo.
echo.

set "KEYSTORE="
set /p "KEYSTORE=Caminho do keystore (.jks/.keystore): "
if not defined KEYSTORE (
    echo ERRO: o caminho do keystore e obrigatorio.
    exit /b 1
)
if not exist "%KEYSTORE%" (
    echo ERRO: keystore nao encontrado: "%KEYSTORE%"
    echo Crie e guarde um keystore persistente; nao gere outro a cada publicacao.
    echo Exemplo: keytool -genkeypair -v -keystore "%%USERPROFILE%%\CageLogicSigning\cagelogic.keystore" -alias cagelogic -keyalg RSA -keysize 2048 -validity 10000
    exit /b 1
)

set "KEY_ALIAS="
set /p "KEY_ALIAS=Alias da chave: "
if not defined KEY_ALIAS (
    echo ERRO: o alias da chave e obrigatorio.
    exit /b 1
)

set "STORE_PASS_FILE="
set /p "STORE_PASS_FILE=Arquivo com a senha do keystore: "
if not defined STORE_PASS_FILE (
    echo ERRO: informe o arquivo da senha do keystore.
    exit /b 1
)
if not exist "%STORE_PASS_FILE%" (
    echo ERRO: arquivo de senha nao encontrado: "%STORE_PASS_FILE%"
    exit /b 1
)

set "KEY_PASS_FILE="
set /p "KEY_PASS_FILE=Arquivo com a senha da chave: "
if not defined KEY_PASS_FILE (
    echo ERRO: informe o arquivo da senha da chave.
    exit /b 1
)
if not exist "%KEY_PASS_FILE%" (
    echo ERRO: arquivo de senha nao encontrado: "%KEY_PASS_FILE%"
    exit /b 1
)

echo.
echo [2/2] Publicando APK Android assinado...
del /q "%ANDROID_OUT%\*.apk" 2>nul
if exist "%ANDROID_OUT%\*.apk" (
    echo ERRO: nao foi possivel remover APKs antigos de "%ANDROID_OUT%".
    exit /b 1
)
dotnet publish "%PROJECT%" -f net10.0-android -c Release -p:AndroidKeyStore=true -p:AndroidPackageFormats=apk "-p:AndroidSigningKeyStore=%KEYSTORE%" "-p:AndroidSigningKeyAlias=%KEY_ALIAS%" "-p:AndroidSigningStorePass=file:%STORE_PASS_FILE%" "-p:AndroidSigningKeyPass=file:%KEY_PASS_FILE%" "-p:PublishDir=%ANDROID_OUT%"
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
echo Copie o APK para o celular e autorize a instalacao dessa origem, se solicitado.
exit /b 0

:publish_failed
echo.
echo ERRO: uma das etapas de publicacao falhou. Leia as mensagens acima e corrija o erro antes de repetir.
exit /b 1
