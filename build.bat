@echo off
set branch=%1
if "%branch%"=="" set branch="master"
if not exist csharp-driver git clone https://github.com/datastax/csharp-driver.git
cd csharp-driver
git pull origin
git checkout %branch%
cd ..
msbuild /v:q /nologo /property:Configuration=Release csharp-driver\src\Cassandra.sln
msbuild /v:m /p:Configuration=Release /t:clean src/DataStax.Driver.Benchmarks/DataStax.Driver.Benchmarks.sln
msbuild /v:m /p:Configuration=Release src/DataStax.Driver.Benchmarks/DataStax.Driver.Benchmarks.sln
src\DataStax.Driver.Benchmarks\bin\Release\DataStax.Driver.Benchmarks.exe %*