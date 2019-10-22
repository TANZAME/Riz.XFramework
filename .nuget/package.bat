:: 1. 编译release版本
:: 2. 填充api_key
:: 3. 修改nuspec文件依赖项（TZM.XFramework）的版本
@echo off
set api_key=
set source_api_uri=https://api.nuget.org/v3/index.json
set startup_dir=%~dp0
cd ..\
set startup_dir=%cd%
cd .nuget

:: 打包 TZM.XFramework -Build
echo pack TZM.XFramework
copy TZM.XFramework.nuspec %startup_dir%\net45\TZM.XFramework
nuget pack %startup_dir%\net45\TZM.XFramework\TZM.XFramework.csproj -Properties Configuration=Release
del %startup_dir%\net45\TZM.XFramework\TZM.XFramework.nuspec
echo=

:: 打包 TZM.XFramework.MySql
echo pack TZM.XFramework.MySql
copy TZM.XFramework.MySql.nuspec %startup_dir%\net45\TZM.XFramework.MySql
nuget pack %startup_dir%\net45\TZM.XFramework.MySql\TZM.XFramework.MySql.csproj -Properties Configuration=Release
del %startup_dir%\net45\TZM.XFramework.MySql\TZM.XFramework.MySql.nuspec
echo=

:: 打包 TZM.XFramework.Oracle
echo pack TZM.XFramework.Oracle
copy TZM.XFramework.Oracle.nuspec %startup_dir%\net45\TZM.XFramework.Oracle
nuget pack %startup_dir%\net45\TZM.XFramework.Oracle\TZM.XFramework.Oracle.csproj -Properties Configuration=Release
del %startup_dir%\net45\TZM.XFramework.Oracle\TZM.XFramework.Oracle.nuspec
echo=

:: 打包 TZM.XFramework.Postgre
echo pack TZM.XFramework.Postgre
copy TZM.XFramework.Postgre.nuspec %startup_dir%\net45\TZM.XFramework.Postgre
nuget pack %startup_dir%\net45\TZM.XFramework.Postgre\TZM.XFramework.Postgre.csproj -Properties Configuration=Release
del %startup_dir%\net45\TZM.XFramework.Postgre\TZM.XFramework.Postgre.nuspec
echo=

:: 打包 TZM.XFramework.SQLite
echo pack TZM.XFramework.SQLite
copy TZM.XFramework.SQLite.nuspec %startup_dir%\net45\TZM.XFramework.SQLite
nuget pack %startup_dir%\net45\TZM.XFramework.SQLite\TZM.XFramework.SQLite.csproj -Properties Configuration=Release
del %startup_dir%\net45\TZM.XFramework.SQLite\TZM.XFramework.SQLite.nuspec
echo=
pause

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

:: 批量推送包

for /R %cd% %%f in (*.nupkg) do ( 
echo=
::dotnet nuget push %%f -k %api_key% -s %source_api_uri%
pause
)

echo=
pause