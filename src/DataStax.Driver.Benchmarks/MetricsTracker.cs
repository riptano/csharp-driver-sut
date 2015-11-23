using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Metrics;
using Metrics.Core;
using Timer = System.Threading.Timer;

namespace DataStax.Driver.Benchmarks
{
    internal class MetricsTracker: IMetricsTracker
    {
        private readonly string _baseKey;
        private readonly Socket _socket;
        private readonly Timer _timer;
        //There is no ConcurrentSet in .NET
        private readonly ConcurrentDictionary<string, bool> _keys = new ConcurrentDictionary<string, bool>();
        private const int ExportThroughputPeriod = 5000;
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public MetricsTracker(IPEndPoint serverAddress, Version driverVersion)
        {
            _baseKey = "sut.csharp-driver." + 
                driverVersion.Major + "_" + driverVersion.Minor + "_" + driverVersion.Build + ".";
            _socket = new Socket(serverAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            _socket.Connect(serverAddress);
            _timer = new Timer(TimerTick, null, ExportThroughputPeriod, ExportThroughputPeriod);
        }

        public void Update(string key, long elapsed)
        {
            var throughputMeter = Metric.Meter(key, Unit.Requests, TimeUnit.Seconds);
            _keys.GetOrAdd(key, k => true);
            throughputMeter.Mark();

        }

        private void TimerTick(object state)
        {
            //timestamp in seconds
            var timestamp = Convert.ToInt32((DateTime.UtcNow - UnixEpoch).TotalSeconds);
            var message = "";
            foreach (var key in _keys.Keys)
            {
                var throughput = (MeterMetric)Metric.Meter(key, Unit.Requests, TimeUnit.Seconds);
                message += _baseKey + key + ".throughput " + (int)throughput.GetValue(true).MeanRate + " " + timestamp + "\n";
            }
            if (!string.IsNullOrEmpty(message))
            {
                Write(message);
            }
        }

        private void Write(string message)
        {
            try
            {
                var buffer = Encoding.ASCII.GetBytes(message);
                _socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, WriteCallback, null);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Metric could not be sent: {0}", ex);
            }
        }

        private void WriteCallback(IAsyncResult ar)
        {
            try
            {
                _socket.EndSend(ar);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Metric send operation could not be completed: {0}", ex);
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Dispose();
        }
    }

    public interface IMetricsTracker: IDisposable
    {
        void Update(string key, long elapsed);
    }

    internal class EmptyMetricsTracker : IMetricsTracker
    {
        public void Dispose() { }

        public void Update(string key, long elapsed) { }
    }
}
