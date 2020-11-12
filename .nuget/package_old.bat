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

:: 打包 Riz.XFramework -Build
echo pack Riz.XFramework
copy Riz.XFramework.nuspec %startup_dir%\net45\Riz.XFramework
nuget pack %startup_dir%\net45\Riz.XFramework\Riz.XFramework.csproj -Properties Configuration=Release
del %startup_dir%\net45\Riz.XFramework\Riz.XFramework.nuspec
echo=

:: 打包 Riz.XFramework.MySql
echo pack Riz.XFramework.MySql
copy Riz.XFramework.MySql.nuspec %startup_dir%\net45\Riz.XFramework.MySql
nuget pack %startup_dir%\net45\Riz.XFramework.MySql\Riz.XFramework.MySql.csproj -Properties Configuration=Release
del %startup_dir%\net45\Riz.XFramework.MySql\Riz.XFramework.MySql.nuspec
echo=

:: 打包 Riz.XFramework.Oracle
echo pack Riz.XFramework.Oracle
copy Riz.XFramework.Oracle.nuspec %startup_dir%\net45\Riz.XFramework.Oracle
nuget pack %startup_dir%\net45\Riz.XFramework.Oracle\Riz.XFramework.Oracle.csproj -Properties Configuration=Release
del %startup_dir%\net45\Riz.XFramework.Oracle\Riz.XFramework.Oracle.nuspec
echo=

:: 打包 Riz.XFramework.Postgre
echo pack Riz.XFramework.Postgre
copy Riz.XFramework.Postgre.nuspec %startup_dir%\net45\Riz.XFramework.Postgre
nuget pack %startup_dir%\net45\Riz.XFramework.Postgre\Riz.XFramework.Postgre.csproj -Properties Configuration=Release
del %startup_dir%\net45\Riz.XFramework.Postgre\Riz.XFramework.Postgre.nuspec
echo=

:: 打包 Riz.XFramework.SQLite
echo pack Riz.XFramework.SQLite
copy Riz.XFramework.SQLite.nuspec %startup_dir%\net45\Riz.XFramework.SQLite
nuget pack %startup_dir%\net45\Riz.XFramework.SQLite\Riz.XFramework.SQLite.csproj -Properties Configuration=Release
del %startup_dir%\net45\Riz.XFramework.SQLite\Riz.XFramework.SQLite.nuspec
echo=

:: 打包 Riz.XFrameworkCore
echo pack Riz.XFrameworkCore
dotnet pack --no-build --configuration Release --output %startup_dir%\.nuget\ %startup_dir%\netcore\Riz.XFrameworkCore\Riz.XFrameworkCore.csproj

:: 打包 Riz.XFrameworkCore.MySql
echo pack Riz.XFrameworkCore.MySql
dotnet pack --no-build --configuration Release --output %startup_dir%\.nuget\ %startup_dir%\netcore\Riz.XFrameworkCore.MySql\Riz.XFrameworkCore.MySql.csproj

:: 打包 Riz.XFrameworkCore
echo pack Riz.XFrameworkCore.Oracle
dotnet pack --no-build --configuration Release --output %startup_dir%\.nuget\ %startup_dir%\netcore\Riz.XFrameworkCore.Oracle\Riz.XFrameworkCore.Oracle.csproj

:: 打包 Riz.XFrameworkCore.Postgre
echo pack Riz.XFrameworkCore.Postgre
dotnet pack --no-build --configuration Release --output %startup_dir%\.nuget\ %startup_dir%\netcore\Riz.XFrameworkCore.Postgre\Riz.XFrameworkCore.Postgre.csproj

:: 打包 Riz.XFrameworkCore.SQLite
echo pack Riz.XFrameworkCore.SQLite
dotnet pack --no-build --configuration Release --output %startup_dir%\.nuget\ %startup_dir%\netcore\Riz.XFrameworkCore.SQLite\Riz.XFrameworkCore.SQLite.csproj

:: 批量推送包 
for /R %cd% %%f in (*.nupkg) do ( 
::echo=
::dotnet nuget push %%f -k %api_key% -s %source_api_uri%
echo=
)

echo=
pause