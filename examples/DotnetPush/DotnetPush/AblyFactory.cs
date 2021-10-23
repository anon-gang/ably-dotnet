using System;
using IO.Ably;
using IO.Ably.Push;

namespace DotnetPush
{
    /// <summary>
    /// Creates the ably client.
    /// </summary>
    public class AblyFactory
    {
        /// <summary>
        /// Init Function.
        /// </summary>
        private readonly Func<ClientOptions, PushCallbacks, IRealtimeClient> _initFunc;

        private readonly ILoggerSink _loggerSink;

        /// <summary>
        /// Options.
        /// </summary>
        public ClientOptions Options { get; private set; }

        /// <summary>
        /// Callbacks.
        /// </summary>
        public PushCallbacks Callbacks { get; private set; }

        /// <summary>
        /// Current instance.
        /// </summary>
        public IRealtimeClient RealtimeInstance { get; private set; }

        /// <summary>
        /// Create instance.
        /// </summary>
        /// <param name="initFunc">Function that executes it on the device itself.</param>
        /// <param name="loggerSink">TODO.</param>
        public AblyFactory(Func<ClientOptions,  PushCallbacks, IRealtimeClient> initFunc, ILoggerSink loggerSink)
        {
            _initFunc = initFunc;
            _loggerSink = loggerSink;
        }

        /// <summary>
        /// Configure function.
        /// </summary>
        /// <param name="optionsAction">options.</param>
        /// <param name="callbacks">callbacks.</param>
        /// <returns>Realtime client.</returns>
        public IRealtimeClient Configure(Action<ClientOptions> optionsAction, PushCallbacks callbacks)
        {
            var options = new ClientOptions();
            options.LogHandler = _loggerSink;
            options.LogLevel = LogLevel.Debug;
            optionsAction(options);

            // This is just to make testing easier.
            var savedClientId = AblySettings.ClientId;
            if (string.IsNullOrWhiteSpace(savedClientId) == false)
            {
                options.ClientId = savedClientId;
            }
            else
            {
                options.ClientId = Guid.NewGuid().ToString("D");
                AblySettings.ClientId = options.ClientId; // Save it for later use.
            }

            return _initFunc(options, callbacks);
        }
    }
}
