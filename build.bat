@echo off
where /q msbuild || ECHO Cound not find msbuild in PATH. && EXIT /B
set branch=%1
if "%branch%"=="" set branch="master"
if "%branch%"=="--help" set branch="master"
if not exist csharp-driver git clone https://github.com/datastax/csharp-driver.git
cd csharp-driver
git fetch origin
git checkout %branch%
git reset --hard origin/%branch%
cd ..
rem Required for package restore
msbuild /v:q /nologo /property:Configuration=Release csharp-driver\src\Cassandra.sln
msbuild /v:q /nologo /p:Configuration=Release /t:clean src/DataStax.Driver.Benchmarks/DataStax.Driver.Benchmarks.sln
msbuild /v:q /nologo /p:Configuration=Release src/DataStax.Driver.Benchmarks/DataStax.Driver.Benchmarks.sln
if %errorlevel% neq 0 exit /b %errorlevel%
src\DataStax.Driver.Benchmarks\bin\Release\DataStax.Driver.Benchmarks.exe %*