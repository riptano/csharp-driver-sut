nuget restore src/DataStax.Driver.Benchmarks/DataStax.Driver.Benchmarks.sln
xbuild /v:m /p:Configuration=Release /p:restorepackages=false src/DataStax.Driver.Benchmarks/DataStax.Driver.Benchmarks.sln
mono ./src/DataStax.Driver.Benchmarks/bin/Release/DataStax.Driver.Benchmarks.exe "$@"