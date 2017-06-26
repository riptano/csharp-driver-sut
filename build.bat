@echo off
where /q msbuild || ECHO Cound not find msbuild in PATH. && EXIT /B
set driver=%1
set branch=%2
echo.Driver: %driver%
echo.Branch/Tag: %branch%
if not exist csharp-dse-driver git clone https://github.com/riptano/csharp-dse-driver.git
if not exist csharp-driver git clone https://github.com/datastax/csharp-driver.git
if "%driver%"=="dse" (
	cd csharp-dse-driver
	git fetch origin %branch%
	git reset --hard %branch%
	msbuild /v:q /nologo /property:Configuration=Release src\Dse.sln
)
git fetch origin
if "%driver%"=="cassandra" (
	cd csharp-driver
	git fetch origin %branch%
	git reset --hard %branch%
	msbuild /v:q /nologo /property:Configuration=Release src\Cassandra.sln
)
cd ..
rem Required for package restore
msbuild /v:q /nologo /p:Configuration=Release /t:clean src/DataStax.Driver.Benchmarks/DataStax.Driver.Benchmarks.sln
msbuild /v:q /nologo /p:Configuration=Release src/DataStax.Driver.Benchmarks/DataStax.Driver.Benchmarks.sln
if %errorlevel% neq 0 exit /b %errorlevel%
src\DataStax.Driver.Benchmarks\bin\Release\DataStax.Driver.Benchmarks.exe %*
