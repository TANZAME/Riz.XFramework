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
dotnet pack --configuration Release --output %startup_dir%\.nuget\ %startup_dir%\Riz.XFramework.sln

:: 批量推送包 
for /R %cd% %%f in (*.nupkg) do ( 
::dotnet nuget push %%f -k %api_key% -s %source_api_uri%
echo=
)

echo=
pause