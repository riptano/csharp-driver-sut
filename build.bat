@echo off
where /q dotnet || ECHO Cound not find dotnet in PATH. && EXIT /B
set driver=%1
set branch=%2
set framework=%3
set clone=%4
echo.Driver: %driver%
echo.Branch/Tag: %branch%
echo.Framework: %framework%
echo.Clone: %clone%

if "%clone%"=="true" (

	if not exist csharp-dse-driver/ git clone git@github.com:riptano/csharp-dse-driver.git
	if %errorlevel% neq 0 exit /b %errorlevel%
	
	if not exist csharp-driver/ git clone git@github.com:datastax/csharp-driver.git
	if %errorlevel% neq 0 exit /b %errorlevel%
	
	if "%driver%"=="dse" (
		cd csharp-dse-driver
		git fetch origin %branch%
		git reset --hard %branch%
		if %errorlevel% neq 0 exit /b %errorlevel%
	)
	if "%driver%"=="cassandra" (
		cd csharp-driver
		git fetch origin %branch%
		git reset --hard %branch%
		if %errorlevel% neq 0 exit /b %errorlevel%
	)
	if "%driver%"=="cassandra-private" (
		cd csharp-driver
		git remote add private git@github.com:riptano/csharp-driver-private.git
		git fetch private %branch%
		git reset --hard private/%branch%
		if %errorlevel% neq 0 exit /b %errorlevel%
	)
	cd ..
)

dotnet restore "src\DataStax.Driver.Benchmarks\DataStax.Driver.Benchmarks.sln" -v minimal
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet clean "src\DataStax.Driver.Benchmarks\DataStax.Driver.Benchmarks.sln" -v minimal
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet build "src\DataStax.Driver.Benchmarks\DataStax.Driver.Benchmarks.sln" -c Release -v minimal
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet run --project "src\DataStax.Driver.Benchmarks\DataStax.Driver.Benchmarks.csproj" -c Release -f "%framework%" -- %*
if %errorlevel% neq 0 exit /b %errorlevel%