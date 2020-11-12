:: 1. 编译release版本
:: 2. 填充api_key
:: 3. 批量推送包里面的注释去掉
:: 4. 

@echo off
set api_key=
set source_api_uri=https://api.nuget.org/v3/index.json
set startup_dir=%~dp0
cd ..\
set startup_dir=%cd%
cd .nuget

:: 打包 Riz.XFramework
echo pack Riz.XFramework
dotnet pack --configuration Release --output %startup_dir%\.nuget\ %startup_dir%\src\Riz.XFramework\Riz.XFramework.csproj

:: 打包 Riz.XFramework.MySql
echo pack Riz.XFramework.MySql
dotnet pack --configuration Release --output %startup_dir%\.nuget\ %startup_dir%\src\Riz.XFramework.MySql\Riz.XFramework.MySql.csproj

:: 打包 Riz.XFramework
echo pack Riz.XFramework.Oracle
dotnet pack --configuration Release --output %startup_dir%\.nuget\ %startup_dir%\src\Riz.XFramework.Oracle\Riz.XFramework.Oracle.csproj

:: 打包 Riz.XFramework.Postgre
echo pack Riz.XFramework.Postgre
dotnet pack --configuration Release --output %startup_dir%\.nuget\ %startup_dir%\src\Riz.XFramework.Postgre\Riz.XFramework.Postgre.csproj

:: 打包 Riz.XFramework.SQLite
echo pack Riz.XFramework.SQLite
dotnet pack --configuration Release --output %startup_dir%\.nuget\ %startup_dir%\src\Riz.XFramework.SQLite\Riz.XFramework.SQLite.csproj

:: 批量推送包 
for /R %cd% %%f in (*.nupkg) do ( 
::echo=
::dotnet nuget push %%f -k %api_key% -s %source_api_uri%
echo=
)

echo=
pause