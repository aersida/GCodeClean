@ECHO OFF
dotnet restore

@IF """%~1"""=="""""" (set pver=1.0.0) ELSE (set pver=%~1)
@ECHO version set to %pver%

@ECHO Publishing CLI %1
@DEL bin\release\publish\cli-%1\*.* /F /Q /S >NUL
@DEL bin\release\gcodeclean-cli-%1-linux-arm.zip >NUL
@DEL bin\release\gcodeclean-cli-%1-linux-x64.zip >NUL
@DEL bin\release\gcodeclean-cli-%1-win-x64.zip >NUL

@set outDir=cli/bin/release/net9.0/publish/
@set destZip=bin/release/gcodeclean-cli-%1-

@ECHO @dotnet publish --configuration Release /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary --property:PublishDir=%outDir% -p:Version=%pver% --runtime %1 --self-contained

@dotnet publish cli/CLI.csproj -p:PublishProfile=linux-arm -p:Version=%pver%
@dotnet publish cli/CLI.csproj -p:PublishProfile=linux-x64 -p:Version=%pver%
@dotnet publish cli/CLI.csproj -p:PublishProfile=win-x64 -p:Version=%pver%
@SET psCmdA="Add-Type -A 'System.IO.Compression.FileSystem'; [IO.Compression.ZipFile]::CreateFromDirectory("
@SET psCmd=%psCmdA%'%outDir%linux-arm', '%destZip%linux-arm.zip');
powershell.exe -nologo -noprofile -command %psCmd%
@SET psCmd=%psCmdA%'%outDir%linux-x64', '%destZip%linux-x64.zip');
powershell.exe -nologo -noprofile -command %psCmd%
@SET psCmd=%psCmdA%'%outDir%win-x64', '%destZip%win-x64.zip');
powershell.exe -nologo -noprofile -command %psCmd%

@ECHO Compress-Archive -LiteralPath '%outDir%' -DestinationPath '%destZip%'
GOTO :EOF
