# C# Driver SUT

HTTP wrapper / console client for the DataStax C# driver for Apache Cassandra, suitable for benchmarking.

It uses the [killrvideo schema](https://github.com/pmcfadin/cassandra-videodb-sample-schema/blob/master/videodb-schema.cql).

## Prerequisites

### Windows

- .NET Framework 4.5+
- msbuild in PATH

### Linux / OS X

- Mono 3.12+

## Building

### .NET

```bash
> build <branch_name> [options]
```

Use `build --help` to display the options.

### Mono

```bash
$ source build.sh <branch_name> [options]
```

Use `--help` to display the options.

## Usage samples

### Using console interface

The console interface executes insert and select bound statements `n` times and displays the operations per second.

Example:

Use driver branch master, execute 1,000,000 requests with 512 as maximum outstanding requests (in-flight), using 192.168.1.100 as Cassandra cluster contact point.

```bash
> build master -s N -o 512 -r 1000000 -c 192.168.1.100
```

### Using web interface

The web interface executes a insert and select bound statements `n` times per each HTTP request and exports throughput and latency metrics to a Graphite server.

Example:

Use driver branch master, execute 500 requests per http request with 256 as maximum outstanding requests (in-flight) per host, using 192.168.1.100 as Cassandra cluster contact point and 127.0.0.1:2003 as metrics server endpoint.

```bash
> build master -s Y -o 256 -r 500 -c 192.168.1.100 -m 127.0.0.1:2003
```