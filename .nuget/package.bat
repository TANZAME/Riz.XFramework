
@echo off
set startup_dir=%~dp0
cd ..\
set startup_dir=%cd%
cd .nuget

:: 打包 TZM.XFramework -Build
echo pack TZM.XFramework
nuget pack %startup_dir%\net45\TZM.XFramework\TZM.XFramework.csproj

:: 打包 TZM.XFramework.MySql
echo pack TZM.XFramework.MySql
nuget pack %startup_dir%\net45\TZM.XFramework.MySql\TZM.XFramework.MySql.csproj

:: 打包 TZM.XFramework.Oracle
echo pack TZM.XFramework.Oracle
nuget pack %startup_dir%\net45\TZM.XFramework.Oracle\TZM.XFramework.Oracle.csproj

:: 打包 TZM.XFramework.Postgre
echo pack TZM.XFramework.Postgre
nuget pack %startup_dir%\net45\TZM.XFramework.Postgre\TZM.XFramework.Postgre.csproj

:: 打包 TZM.XFrameworkCore
echo pack TZM.XFrameworkCore
dotnet pack --no-build --output %startup_dir%\.nuget\ %startup_dir%\netcore\TZM.XFrameworkCore\TZM.XFrameworkCore.csproj

:: 打包 TZM.XFrameworkCore.MySql
echo pack TZM.XFrameworkCore.MySql
dotnet pack --no-build --output %startup_dir%\.nuget\ %startup_dir%\netcore\TZM.XFrameworkCore.MySql\TZM.XFrameworkCore.MySql.csproj

:: 打包 TZM.XFrameworkCore
echo pack TZM.XFrameworkCore.Oracle
dotnet pack --no-build --output %startup_dir%\.nuget\ %startup_dir%\netcore\TZM.XFrameworkCore.Oracle\TZM.XFrameworkCore.Oracle.csproj

:: 打包 TZM.XFrameworkCore.Postgre
echo pack TZM.XFrameworkCore.Postgre
dotnet pack --no-build --output %startup_dir%\.nuget\ %startup_dir%\netcore\TZM.XFrameworkCore.Postgre\TZM.XFrameworkCore.Postgre.csproj

pause