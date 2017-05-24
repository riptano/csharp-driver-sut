# C# Driver SUT

HTTP wrapper / console client for the DataStax C# driver for Apache Cassandra, suitable for benchmarking.

It uses the [killrvideo schema](https://github.com/pmcfadin/cassandra-videodb-sample-schema/blob/master/videodb-schema.cql).

## Prerequisites

### Windows

- .NET Framework 4.5+
- msbuild in PATH
- gnuplot (choco install -y gnuplot)

## Usage

### .NET

The console interface executes insert and select bound statements `n` times and displays the operations per second.

```bash
> build <driver> <branch_name> [options]
```

Use `build --help` to display the options.

Options:

* driver : driver and branch used by benchmark test. e.g. cassandra
* branch : driver and branch used by benchmark test. e.g. origin/master
* -s : Number of series of tests for each outstanding setup. (default: 5)
* -o : Number of oustanding requests. 
    * Use 0 (zero) to run against all the configurations : 10 20 30 40 50 70 90 110 130 150 180 210 240 270 300 340 380 420 460 500
* -r : Number of requests to be made. (default: 10000)
* -c : Cassandra/DSE contact point
* -w : Workload profile: standard (default), mapper or minimal
* -p : Amount of connections per host. (default: 1)

Example:

```bash
> build <driver> <branch> -s 5 -o 0 -r 1000000 -c 192.168.1.100 -w standard -p 1
```

## Usage samples

#### OSS driver master branch:

Use oss driver branch master, execute 1,000,000 requests with 512 as maximum outstanding requests (in-flight), using 192.168.1.100 as Cassandra cluster contact point.

```bash
> build cassandra origin/master -s 5 -o 512 -r 1000000 -c 192.168.1.100
```

#### DSE driver dse branch:

Use dse driver branch dse, execute 1,000,000 requests with 512 as maximum outstanding requests (in-flight), and 3 connections, using 192.168.1.100 as Cassandra cluster contact point.

```bash
> build dse origin/dse -s 5 -o 512 -r 1000000 -c 192.168.1.100 -p 3
```

### Generate comparison charts

To generate comparison charts run the following command with parameters:

* old : old branch/tag to compare. eg: 3.2.1
* current : current branch. eg: master
* profile : Profile used. eg: standard
* type : type of operation to compare results: read or write
* output : name of result png file

```bash
> gnuplot -e "compare='<old>'" -e "current='<master>'" -e "profile='<profile>'" -e "type='<type>'" -e "outputfile='<output>'" compare.gnuplot
```

#### Example:

```bash
> gnuplot -e "compare='3.2.1'" -e "current='master'" -e "profile='standard'" -e "type='read'" -e "outputfile='read.png'" compare.gnuplot
```

### Full test cycle sample

This benchmark test will compare the results of oss driver version "3.2.1" with "master", using a local cassandra server (for official results, create a cluster at proper servers).

```bash
> build cassandra tags/3.2.1 -s 5 -o 0 -r 1000000 -c 127.0.0.1
> build cassandra origin/master -s 5 -o 0 -r 1000000 -c 127.0.0.1
> gnuplot -e "compare='3.2.1'" -e "current='master'" -e "profile='standard'" -e "type='read'" -e "outputfile='read.png'" compare.gnuplot
> gnuplot -e "compare='3.2.1'" -e "current='master'" -e "profile='standard'" -e "type='write'" -e "outputfile='write.png'" compare.gnuplot
```
