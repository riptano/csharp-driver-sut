#! /bin/bash

if [ ! -d csharp-driver ]; then
    git clone https://github.com/datastax/csharp-driver.git;
fi;
branch=${1:-master}
if [ ${branch} = "--help" ]; then  
    branch="master" 
fi
cd csharp-driver
git fetch origin && git checkout ${branch} && git reset --hard origin/${branch}
cd ..
nuget restore src/DataStax.Driver.Benchmarks/DataStax.Driver.Benchmarks.sln
xbuild /v:m /p:Configuration=Release /p:restorepackages=false src/DataStax.Driver.Benchmarks/DataStax.Driver.Benchmarks.sln
mono ./src/DataStax.Driver.Benchmarks/bin/Release/DataStax.Driver.Benchmarks.exe "$@"