set -e
driver=$1
branch=$2
framework=$3
clone=$4
export BuildCoreOnly=True
echo Driver: $driver
echo Branch/Tag: $branch
echo Framework: $framework
echo Clone: $clone

if [ $clone = 'true' ]; then

	if [ ! -d ./csharp-dse-driver ]; then
     	git clone git@github.com:riptano/csharp-dse-driver.git
	fi
	
	if [ ! -d ./csharp-driver ]; then
     	git clone git@github.com:datastax/csharp-driver.git
	fi

	if [ $driver = 'dse' ]; then
		cd csharp-dse-driver
		git fetch origin $branch
		git reset --hard origin/$branch
	elif [ $driver = 'cassandra' ]; then
		cd csharp-driver
		git fetch origin $branch
		git reset --hard origin/$branch
	elif [ $driver = 'cassandra-private' ]; then
		cd csharp-driver
		git remote add private git@github.com:riptano/csharp-driver-private.git
		git fetch private $branch
		git reset --hard private/$branch
	fi
	cd ..
fi

dotnet restore src/DataStax.Driver.Benchmarks/DataStax.Driver.Benchmarks.sln -v minimal

dotnet clean src/DataStax.Driver.Benchmarks/DataStax.Driver.Benchmarks.sln -v minimal

dotnet build src/DataStax.Driver.Benchmarks/DataStax.Driver.Benchmarks.sln -c Release -v minimal

dotnet run --project src/DataStax.Driver.Benchmarks/DataStax.Driver.Benchmarks.csproj -c Release -f $framework -- $*

set +e
