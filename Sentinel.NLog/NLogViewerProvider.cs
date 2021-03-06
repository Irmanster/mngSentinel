﻿namespace Sentinel.NLog
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    using Common.Logging;

    using Interfaces;
    using Interfaces.Providers;

    public class NLogViewerProvider : INetworkProvider
    {
        private const int PumpFrequency = 100;

        public static readonly IProviderRegistrationRecord ProviderRegistrationInformation =
            new ProviderRegistrationInformation(new ProviderInfo());

        private static readonly DateTime Log4JDateBase = new DateTime(1970, 1, 1);

        private static readonly ILog Log = LogManager.GetLogger<NLogViewerProvider>();

        private readonly Queue<string> pendingQueue = new Queue<string>();

        private readonly INLogAppenderSettings networkSettings;

        private CancellationTokenSource cancellationTokenSource;

        private Task listenerTask;

        public NLogViewerProvider(IProviderSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            networkSettings = settings as INLogAppenderSettings;
            if (networkSettings == null)
            {
                throw new ArgumentException("settings should be assignable to INLogAppenderSettings", nameof(settings));
            }

            Information = ProviderRegistrationInformation.Info;
            ProviderSettings = networkSettings;
        }

        public IProviderInfo Information { get; private set; }

        public IProviderSettings ProviderSettings { get; private set; }

        public ILogger Logger { get; set; }

        public string Name { get; set; }

        public bool IsActive => listenerTask != null && listenerTask.Status == TaskStatus.Running;

        public int Port { get; private set; }

        public void Start()
        {
            Log.Debug("Start requested");

            if (listenerTask == null || listenerTask.IsCompleted)
            {
                cancellationTokenSource = new CancellationTokenSource();
                var token = cancellationTokenSource.Token;

                listenerTask = Task.Factory.StartNew(SocketListener, token);
                Task.Factory.StartNew(MessagePump, token);
            }
            else
            {
                Log.WarnFormat("{0} listener task is already active and can not be started again.", networkSettings.Protocol);
            }
        }

        public void Pause()
        {
            Log.Debug("Pause requested");
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
            {
                Log.Debug("Cancellation token triggered");
                cancellationTokenSource.Cancel();
            }
        }

        public void Close()
        {
            Log.Debug("Close requested");
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
            {
                Log.Debug("Cancellation token triggered");
                cancellationTokenSource.Cancel();
            }
        }

        private void SocketListener()
        {
            Log.Debug("SocketListener started");

            if (networkSettings == null)
            {
                Log.Error("Network settings has not been initialised");
                throw new NullReferenceException();
            }

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                var endPoint = new IPEndPoint(IPAddress.Any, networkSettings.Port);

                using (var listener = new NetworkClientWrapper(networkSettings.Protocol, endPoint))
                {
                    while (!cancellationTokenSource.IsCancellationRequested)
                    {
                        var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

                        listener.SetReceiveTimeout(1000);

                        try
                        {
                            var bytes = listener.Receive(ref remoteEndPoint);

                            Log.DebugFormat("Received {0} bytes from {1} ({2})", bytes.Length, remoteEndPoint.Address, networkSettings.Protocol);

                            var message = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                            lock (pendingQueue)
                            {
                                pendingQueue.Enqueue(message);
                            }
                        }
                        catch (SocketException socketException)
                        {
                            if (socketException.SocketErrorCode != SocketError.TimedOut)
                            {
                                Log.Debug("SocketException", socketException);
                                Log.DebugFormat(
                                    "SocketException.SocketErrorCode = {0}",
                                    socketException.SocketErrorCode);

                                // Break out of the 'using socket' loop and try to establish a new socket.
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            Log.DebugFormat("Exception: {0}", e.Message);
                        }
                    }
                }
            }

            Log.Debug("SocketListener completed");
        }

        private void MessagePump()
        {
            Log.Debug("MessagePump started");

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                Thread.Sleep(PumpFrequency);

                try
                {
                    if (Logger != null)
                    {
                        lock (pendingQueue)
                        {
                            var processedQueue = new Queue<ILogEntry>();

                            while (pendingQueue.Count > 0)
                            {
                                var message = pendingQueue.Dequeue();

                                if (IsValidEntry(message))
                                {
                                    var deserializeMessage = DecodeEntry(message);

                                    if (deserializeMessage != null)
                                    {
                                        processedQueue.Enqueue(deserializeMessage);
                                    }
                                }
                            }

                            if (processedQueue.Any())
                            {
                                Logger.AddBatch(processedQueue);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            Log.Debug("MessagePump completed");
        }

        private bool IsValidEntry(string logEntry)
        {
            return logEntry.StartsWith("<log4j");
        }

        private LogEntry DecodeEntry(string m)
        {
            XNamespace log4J = "unique";
            string message = $@"<entry xmlns:log4j=""{log4J}"">{m}</entry>";

            XElement element = XElement.Parse(message);
            XElement record = element.Element(log4J + "event");

            // Establish whether a sub-system seems to be defined.
            string description = record.Element(log4J + "message").Value;

            string classification = string.Empty;
            string system = record.Attribute("logger").Value;
            string type = record.Attribute("level").Value;
            string host = "???";

            foreach (XElement propertyElement in record.Element(log4J + "properties").Elements())
            {
                if (propertyElement.Name == log4J + "data" && propertyElement.Attribute("name") != null
                    && propertyElement.Attribute("name").Value == "log4jmachinename")
                {
                    host = propertyElement.Attribute("value").Value;
                }
            }

            var date = Log4JDateBase + TimeSpan.FromMilliseconds(double.Parse(record.Attribute("timestamp").Value));

            var entry = new LogEntry
                            {
                                DateTime = date,
                                System = system,
                                Thread = record.Attribute("thread").Value,
                                Description = description,
                                Type = type,
                                MetaData =
                                    new Dictionary<string, object>
                                        {
                                            { "Classification", classification },
                                            { "Host", host }
                                        }
                            };
            if (entry.Description.ToUpper().Contains("EXCEPTION"))
            {
                entry.MetaData.Add("Exception", true);
            }

            return entry;
        }
    }
}