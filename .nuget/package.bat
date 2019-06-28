
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

pause