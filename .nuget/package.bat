:: 运行之前需要 填充api_key，修改spec文件的引用版本
@echo off
set api_key=oy2kk2ngquhrxcavn5zq5kowltsoisye7cbs23wd3bmtdq
set source_api_uri=https://api.nuget.org/v3/index.json
set startup_dir=%~dp0
cd ..\
set startup_dir=%cd%
cd .nuget

:: 打包 TZM.XFramework -Build
echo pack TZM.XFramework
nuget pack %startup_dir%\net45\TZM.XFramework\TZM.XFramework.csproj
echo=

:: 打包 TZM.XFramework.MySql
echo pack TZM.XFramework.MySql
nuget pack %startup_dir%\net45\TZM.XFramework.MySql\TZM.XFramework.MySql.csproj
echo=

:: 打包 TZM.XFramework.Oracle
echo pack TZM.XFramework.Oracle
nuget pack %startup_dir%\net45\TZM.XFramework.Oracle\TZM.XFramework.Oracle.csproj
echo=

:: 打包 TZM.XFramework.Postgre
echo pack TZM.XFramework.Postgre
nuget pack %startup_dir%\net45\TZM.XFramework.Postgre\TZM.XFramework.Postgre.csproj
echo=

:: 打包 TZM.XFramework.SQLite
echo pack TZM.XFramework.SQLite
nuget pack %startup_dir%\net45\TZM.XFramework.SQLite\TZM.XFramework.SQLite.csproj
echo=

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

:: 打包 TZM.XFrameworkCore.SQLite
echo pack TZM.XFrameworkCore.SQLite
dotnet pack --no-build --output %startup_dir%\.nuget\ %startup_dir%\netcore\TZM.XFrameworkCore.SQLite\TZM.XFrameworkCore.SQLite.csproj

:: 批量推送包

for /R %cd% %%f in (*.nupkg) do ( 
echo=
dotnet nuget push %%f -k %api_key% -s %source_api_uri%
)

echo=
pause