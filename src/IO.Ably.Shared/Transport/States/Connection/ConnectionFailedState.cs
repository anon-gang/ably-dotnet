﻿using System.Threading.Tasks;
using IO.Ably;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    using IO.Ably.Realtime;

    internal class ConnectionFailedState : ConnectionStateBase
    {
        public new ErrorInfo DefaultErrorInfo => ErrorInfo.ReasonFailed;

        public ConnectionFailedState(IConnectionContext context, ErrorInfo error, ILogger logger)
            : base(context, logger)
        {
            Error = error ?? ErrorInfo.ReasonFailed;
        }

        public override ConnectionState State => Realtime.ConnectionState.Failed;

        public override void Connect()
        {
            Context.ExecuteCommand(SetConnectingStateCommand.Create());
        }

        public override void BeforeTransition()
        {
            Context.DestroyTransport();
            Context.Connection.Key = null;
            Context.Connection.Id = null;
        }

        public override Task OnAttachToContext()
        {
            // This is a terminal state. Clear the transport.
            Context.ClearAckQueueAndFailMessages(ErrorInfo.ReasonFailed);

            return TaskConstants.BooleanTrue;
        }
    }
}
