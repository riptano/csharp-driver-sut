@echo off
where /q dotnet || ECHO Cound not find dotnet in PATH. && EXIT /B
set driver=%1
set branch=%2
set framework=%3
echo.Driver: %driver%
echo.Branch/Tag: %branch%
rem Required for package restore
dotnet restore "src\DataStax.Driver.Benchmarks\DataStax.Driver.Benchmarks.sln" -v minimal
dotnet clean "src\DataStax.Driver.Benchmarks\DataStax.Driver.Benchmarks.sln" -v minimal
dotnet build "src\DataStax.Driver.Benchmarks\DataStax.Driver.Benchmarks.sln" -c Release -v minimal
if %errorlevel% neq 0 exit /b %errorlevel%
dotnet run --project "src\DataStax.Driver.Benchmarks\DataStax.Driver.Benchmarks.csproj" -c Release -f "%framework%" %*