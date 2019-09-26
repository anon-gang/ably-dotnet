﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;

namespace IO.Ably.Realtime
{
    public enum NetworkState
    {
        Online,
        Offline
    }

    public sealed class Connection : EventEmitter<ConnectionEvent, ConnectionStateChange>, IDisposable
    {
        internal event EventHandler BeginConnect;

        private readonly Guid ObjectId = Guid.NewGuid(); //Used to identify the connection object for OsEventSubscribers
        private static readonly ConcurrentDictionary<Guid, Action<NetworkState>> OsEventSubscribers =
            new ConcurrentDictionary<Guid, Action<NetworkState>>();

        protected override Action<Action> NotifyClient => RealtimeClient.NotifyExternalClients;

        internal static void NotifyOperatingSystemNetworkState(NetworkState state, ILogger logger = null)
        {
            if (logger == null)
            {
                logger = DefaultLogger.LoggerInstance;
            }

            if (logger.IsDebug)
            {
                logger.Debug("OS Network connection state: " + state);
            }

            foreach (var subscriber in OsEventSubscribers.ToArray())
            {
                try
                {
                    if (logger.IsDebug)
                    {
                        logger.Debug("Calling network state handler for connection with id: " + subscriber.Key.ToString("D"));
                    }

                    subscriber.Value?.Invoke(state);
                }
                catch (Exception e)
                {
                    logger.Error($"Error notifying connectionId {subscriber.Key:D} about network events", e);
                }
            }
        }

        private void RegisterWithOsNetworkStateEvents(Action<NetworkState> stateAction)
        {
            if (Logger.IsDebug)
            {
                Logger.Debug("Registering OS network state handler for Connection with id: " + ObjectId.ToString("D"));
            }

            OsEventSubscribers.AddOrUpdate(ObjectId, stateAction, (_, __) => stateAction);
        }

        private void CleanUpNetworkStateEvents()
        {
            try
            {
                var result = OsEventSubscribers.TryRemove(ObjectId, out _);
                if (Logger.IsDebug)
                {
                    Logger.Debug("Os network listener removed result: " + result);
                }

            }
            catch (Exception e)
            {
                Logger.Warning("Error cleaning up networking events hook");
            }
        }

        internal void SetConfirmedAlive()
        {
            ConfirmedAliveAt = DateTimeOffset.UtcNow;
        }

        internal DateTimeOffset? ConfirmedAliveAt { get; set; }

        internal AblyRest RestClient => RealtimeClient.RestClient;

        internal AblyRealtime RealtimeClient { get; }

        internal ConnectionManager ConnectionManager { get; set; }

        internal List<string> FallbackHosts;

        private string _host;

        internal Func<DateTimeOffset> Now { get; set; }

        internal bool CanPublishMessages =>
            State == Realtime.ConnectionState.Connected
            || ((State == Realtime.ConnectionState.Initialized
                 || State == Realtime.ConnectionState.Connecting
                 || State == Realtime.ConnectionState.Disconnected)
                && RealtimeClient.Options.QueueMessages);

        internal Connection(AblyRealtime realtimeClient, Func<DateTimeOffset> nowFunc, ILogger logger = null)
            : base(logger)
        {
            Now = nowFunc;
            FallbackHosts = realtimeClient?.Options?.FallbackHosts.Shuffle().ToList();
            RealtimeClient = realtimeClient;

            RegisterWithOsNetworkStateEvents(HandleNetworkStateChange);

            var recover = realtimeClient?.Options?.Recover;
            if (recover.IsNotEmpty())
            {
                ParseRecoveryKey(recover);
            }
        }

        private void ParseRecoveryKey(string recover)
        {
            var match = TransportParams.RecoveryKeyRegex.Match(recover);
            if (match.Success)
            {
                MessageSerial = long.Parse(match.Groups[3].Value);
            }
            else
            {
                Logger.Error($"Recovery Key '{recover}' could not be parsed.");
            }
        }

        internal void Initialise()
        {
            ConnectionManager = new ConnectionManager(this, Now, Logger);
            ConnectionState = new ConnectionInitializedState(ConnectionManager, Logger);
        }

        /// <summary>
        ///     Indicates the current state of this connection.
        /// </summary>
        public ConnectionState State => ConnectionState.State;

        internal NetworkState NetworkState { get; set; } = NetworkState.Online;

        private void HandleNetworkStateChange(NetworkState state)
        {
            NetworkState = state;
            ConnectionManager.HandleNetworkStateChange(state);
        }

        internal ConnectionStateBase ConnectionState { get; set; }

        /// <summary>
        ///     The id of the current connection. This string may be
        ///     used when recovering connection state.
        /// </summary>
        public string Id { get; internal set; }

        /// <summary>
        ///     The serial number of the last message received on this connection.
        ///     The serial number may be used when recovering connection state.
        /// </summary>
        public long? Serial { get; internal set; }

        internal long MessageSerial { get; set; } = 0;

        /// <summary>
        /// </summary>
        public string Key { get; internal set; }

        public bool ConnectionResumable => Key.IsNotEmpty() && Serial.HasValue;

        /// <summary>
        /// - (RTN16b) Connection#recoveryKey is an attribute composed of the connectionKey, and the latest connectionSerial received on the connection, and the current msgSerial
        /// </summary>
        public string RecoveryKey => ConnectionResumable ? $"{Key}:{Serial.Value}:{MessageSerial}" : string.Empty;

        public TimeSpan ConnectionStateTtl { get; internal set; } = Defaults.ConnectionStateTtl;

        /// <summary>
        ///     Information relating to the transition to the current state,
        ///     as an Ably ErrorInfo object. This contains an error code and
        ///     message and, in the failed state in particular, provides diagnostic
        ///     error information.
        /// </summary>
        public ErrorInfo ErrorReason { get; private set; }

        public string Host
        {
            get => _host;

            internal set
            {
                _host = value;
                RestClient.CustomHost = FallbackHosts.Contains(_host) ? _host : string.Empty;
            }
        }

        public void Dispose()
        {
            Close();
            ClearAllDelegatesOfStateChangeEventHandler();
            CleanUpNetworkStateEvents();
            Off();
        }

        private void ClearAllDelegatesOfStateChangeEventHandler()
        {
            foreach (var handler in ConnectionStateChanged.GetInvocationList())
            {
                ConnectionStateChanged -= (EventHandler<ConnectionStateChange>)handler;
            }
        }

        public event EventHandler<ConnectionStateChange> ConnectionStateChanged = delegate { };

        public void Connect()
        {
            ExecuteCommand(ConnectCommand.Create());
        }

        public Task<Result<TimeSpan?>> PingAsync()
        {
            return TaskWrapper.Wrap<TimeSpan?>(Ping);
        }

        public void Ping(Action<TimeSpan?, ErrorInfo> callback)
        {
            ExecuteCommand(new PingCommand(new PingRequest(callback, Now)));
        }

        private void ExecuteCommand(RealtimeCommand cmd)
        {
            if (RealtimeClient.Disposed)
            {
                throw new ObjectDisposedException("This instance has been disposed. Please create a new one.");
            }

            // Find a better way to reference the workflow
            RealtimeClient.Workflow.QueueCommand(cmd);
        }

        /// <summary>
        ///     Causes the connection to close, entering the <see cref="Realtime.ConnectionState.Closed" /> state. Once closed,
        ///     the library will not attempt to re-establish the connection without a call
        ///     to <see cref="Connect()" />.
        /// </summary>
        public void Close()
        {
            ExecuteCommand(CloseConnectionCommand.Create());
        }

        internal void UpdateState(ConnectionStateBase state)
        {
            if (!state.IsUpdate && state.State == State)
            {
                return;
            }

            if (Logger.IsDebug)
            {
                Logger.Debug($"Connection notifying subscribers for state change `{state.State}`");
            }

            var oldState = ConnectionState.State;
            var newState = state.State;
            ConnectionState = state;
            ErrorReason = state.Error;
            var connectionEvent = oldState == newState ? ConnectionEvent.Update : newState.ToConnectionEvent();
            var stateChange = new ConnectionStateChange(connectionEvent, oldState, newState, state.RetryIn, ErrorReason);

            var externalHandlers =
                Volatile.Read(ref ConnectionStateChanged); // Make sure we get all the subscribers on all threads

            RealtimeClient.NotifyExternalClients(
                () => {
                        Emit(connectionEvent, stateChange);
                        try
                        {
                            externalHandlers(this, stateChange);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Error notifying Connection state changed handlers", ex);
                        }});
        }

        public void UpdateSerial(ProtocolMessage message)
        {
            if (message.ConnectionSerial.HasValue)
            {
                Serial = message.ConnectionSerial.Value;
            }
        }
    }
}
