using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace IO.Ably.Push
{
    internal partial class ActivationStateMachine
    {
        internal static readonly Func<Task<Event>> EmptyNextEventFunc =
            () => Task.FromResult((Event)null);

        internal static Func<Task<Event>> ToNextEventFunc(Func<Task<Event>> singleEventFunc)
        {
            return async () => await singleEventFunc();
        }

        internal static Func<Task<Event>> ToNextEventFunc(Event nextEvent)
        {
            if (nextEvent is null)
            {
                return EmptyNextEventFunc;
            }

            return async () => nextEvent;
        }

        public abstract class State
        {
            protected State(ActivationStateMachine machine)
            {
                Machine = machine;
            }

            protected ActivationStateMachine Machine { get; }

            public abstract bool Persist { get; }

            public abstract bool CanHandleEvent(Event @event);

            public abstract Task<(State, Func<Task<Event>>)> Transition(Event @event);

            public override string ToString()
            {
                return GetType().Name;
            }
        }

        public sealed class NotActivated : State
        {
            public NotActivated(ActivationStateMachine machine)
                : base(machine)
            {
            }

            public override bool Persist => true;

            public override bool CanHandleEvent(Event @event)
            {
                return @event is CalledActivate || @event is CalledDeactivate || @event is GotPushDeviceDetails;
            }

            public override async Task<(State, Func<Task<Event>>)> Transition(Event @event)
            {
                switch (@event)
                {
                    case CalledDeactivate _:
                        Machine.CallDeactivatedCallback(null);
                        return (this, EmptyNextEventFunc);
                    case CalledActivate _:
                        // TODO: Logging
                        var device = Machine.LocalDevice;

                        if (device.IsRegistered)
                        {
                            var newState = new WaitingForRegistrationSync(Machine, @event);
                            return (newState, ToNextEventFunc(Machine.ValidateRegistration));
                        }

                        Event nextEvent = null;
                        if (device.RegistrationToken != null)
                        {
                            nextEvent = new GotPushDeviceDetails();
                        }
                        else
                        {
                            Machine.GetRegistrationToken();
                        }

                        if (device.IsCreated == false)
                        {
                            var newLocalDevice = LocalDevice.Create(Machine.ClientId, Machine._mobileDevice);
                            Machine.PersistLocalDevice(newLocalDevice);
                            Machine.LocalDevice = newLocalDevice;
                        }

                        return (new WaitingForPushDeviceDetails(Machine), ToNextEventFunc(nextEvent));
                    case GotPushDeviceDetails _:
                        return (this, EmptyNextEventFunc);
                    default:
                        return (null, EmptyNextEventFunc);
                }
            }
        }

        // Stub for now
        public sealed class WaitingForPushDeviceDetails : State
        {
            public WaitingForPushDeviceDetails(ActivationStateMachine machine)
                : base(machine)
            {
            }

            public override bool Persist => true;

            public override bool CanHandleEvent(Event @event)
            {
                return @event is CalledActivate
                       || @event is CalledDeactivate
                       || @event is GettingPushDeviceDetailsFailed
                       || @event is GotPushDeviceDetails;
            }

            public override async Task<(State, Func<Task<Event>>)> Transition(Event @event)
            {
                switch (@event)
                {
                    case CalledActivate _:
                        return (this, EmptyNextEventFunc);
                    case CalledDeactivate _:
                        Machine.CallDeactivatedCallback(null);
                        return (new NotActivated(Machine), EmptyNextEventFunc);
                    case GettingPushDeviceDetailsFailed failedEvent:
                        Machine.CallDeactivatedCallback(failedEvent.Reason);
                        return (new NotActivated(Machine), EmptyNextEventFunc);
                    case GotPushDeviceDetails _:
                        return (new WaitingForDeviceRegistration(Machine), ToNextEventFunc(RegisterDevice));
                    default:
                        return (null, EmptyNextEventFunc);
                }

                async Task<Event> RegisterDevice()
                {
                    DeviceDetails device = Machine.LocalDevice;

                    var ably = Machine._restClient; // TODO: Check if there is an instance when Ably is not set. In which case Java set queues GettingDeviceRegistrationFailed

                    try
                    {
                        var registeredDevice = await ably.Push.Admin.RegisterDevice(device);
                        var deviceIdentityToken = registeredDevice.DeviceIdentityToken;
                        if (deviceIdentityToken.IsEmpty())
                        {
                            // TODO: Log
                            return new GettingDeviceRegistrationFailed(new ErrorInfo(
                                "Invalid deviceIdentityToken in response", 40000, HttpStatusCode.BadRequest));
                        }

                        // TODO: When integration testing this will most likely fail. I suspect deviceIdentityToken is not a plain string.
                        return new GotDeviceRegistration(deviceIdentityToken);

                        // TODO: RSH8f. Leaving commented out code as a reminder.
                        // I still haven't figured out how clientId in the state machine could be different from the one stored in the client
                        // JsonPrimitive responseClientIdJson = response.getAsJsonPrimitive("clientId");
                        // if (responseClientIdJson != null)
                        // {
                        //     String responseClientId = responseClientIdJson.getAsString();
                        //     if (device.clientId == null)
                        //     {
                        //         /* Spec RSH8f: there is an implied clientId in our credentials that we didn't know about */
                        //         activationContext.setClientId(responseClientId, false);
                        //     }
                        // }
                    }
                    catch (AblyException e)
                    {
                        // Log
                        return new GettingDeviceRegistrationFailed(e.ErrorInfo);
                    }
                }
            }
        }

        public sealed class WaitingForDeviceRegistration : State
        {
            public WaitingForDeviceRegistration(ActivationStateMachine machine)
                : base(machine)
            {
            }

            public override bool Persist => false;

            public override bool CanHandleEvent(Event @event)
            {
                return @event is CalledActivate || @event is GotDeviceRegistration ||
                       @event is GettingDeviceRegistrationFailed;
            }

            public override async Task<(State, Func<Task<Event>>)> Transition(Event @event)
            {
                switch (@event)
                {
                    case CalledActivate _:
                        return (this, EmptyNextEventFunc);
                    case GotDeviceRegistration registrationEvent:
                        Machine.SetDeviceIdentityToken(registrationEvent.DeviceIdentityToken);
                        Machine.CallActivatedCallback(null);
                        return (new WaitingForNewPushDeviceDetails(Machine), EmptyNextEventFunc);
                    case GettingDeviceRegistrationFailed failedEvent:
                        Machine.CallActivatedCallback(failedEvent.Reason);
                        return (new NotActivated(Machine), EmptyNextEventFunc);
                    default:
                        return (null, EmptyNextEventFunc);
                }
            }
        }

        public sealed class WaitingForNewPushDeviceDetails : State
        {
            public WaitingForNewPushDeviceDetails(ActivationStateMachine machine)
                : base(machine)
            {
            }

            public override bool Persist => true;

            public override bool CanHandleEvent(Event @event)
            {
                return @event is CalledActivate || @event is CalledDeactivate || @event is GotPushDeviceDetails;
            }

            public override async Task<(State, Func<Task<Event>>)> Transition(Event @event)
            {
                switch (@event)
                {
                    case CalledActivate _:
                        Machine.CallActivatedCallback(null);
                        return (this, EmptyNextEventFunc);
                    case CalledDeactivate _:
                        return (new WaitingForDeregistration(Machine, this), ToNextEventFunc(Machine.Deregister));
                    case GotPushDeviceDetails _:
                        // Note: I don't fully understand why we do this.
                        var device = Machine.EnsureLocalDeviceIsLoaded();

                        var nextEvent = await Machine.UpdateRegistration(device);

                        return (new WaitingForRegistrationSync(Machine, @event), ToNextEventFunc(nextEvent));
                    default:
                        return (null, EmptyNextEventFunc);
                }
            }
        }

        public sealed class WaitingForDeregistration : State
        {
            private readonly State _previousState;

            public WaitingForDeregistration(ActivationStateMachine machine, State previousState)
                : base(machine)
            {
                _previousState = previousState;
            }

            public override bool Persist => false;

            public override bool CanHandleEvent(Event @event)
            {
                return @event is CalledDeactivate || @event is Deregistered || @event is DeregistrationFailed;
            }

            public override async Task<(State, Func<Task<Event>>)> Transition(Event @event)
            {
                switch (@event)
                {
                    case CalledDeactivate _:
                        return (this, EmptyNextEventFunc);
                    case Deregistered _:
                        Machine.ResetDevice();
                        Machine.CallDeactivatedCallback(null);
                        return (new NotActivated(Machine), EmptyNextEventFunc);
                    case DeregistrationFailed failed:
                        Machine.CallDeactivatedCallback(failed.Reason);
                        return (_previousState, EmptyNextEventFunc);
                    default:
                        return (null, EmptyNextEventFunc);
                }
            }
        }

        // Stub for now
        public sealed class WaitingForRegistrationSync : State
        {
            private readonly Event _fromEvent;

            public WaitingForRegistrationSync(ActivationStateMachine machine, Event fromEvent)
                : base(machine)
            {
                _fromEvent = fromEvent;
            }

            public override bool Persist => false;

            public override bool CanHandleEvent(Event @event)
            {
                return (@event is CalledActivate && !(_fromEvent is CalledActivate))
                        || @event is RegistrationSynced
                        || @event is SyncRegistrationFailed;
            }

            public override async Task<(State, Func<Task<Event>>)> Transition(Event @event)
            {
                switch (@event)
                {
                    case CalledActivate _ when _fromEvent is CalledActivate:
                        // Don't handle; there's a CalledActivate ongoing already, so this one should
                        // be enqueued for when that one finishes.
                        return (null, EmptyNextEventFunc);

                    case CalledActivate _:
                        Machine.CallActivatedCallback(null);
                        return (this, EmptyNextEventFunc);

                    case RegistrationSynced _:
                        if (_fromEvent is CalledActivate)
                        {
                            Machine.CallActivatedCallback(null);
                        }

                        return (new WaitingForNewPushDeviceDetails(Machine), EmptyNextEventFunc);

                    case SyncRegistrationFailed failed:
                        // TODO: Here we could try to recover ourselves if the error is e.g.
                        // a networking error. Just notify the user for now.
                        ErrorInfo reason = failed.Reason;
                        if (_fromEvent is CalledActivate)
                        {
                            Machine.CallActivatedCallback(reason);
                        }
                        else
                        {
                            Machine.CallSyncRegistrationFailedCallback(reason);
                        }

                        return (new AfterRegistrationSyncFailed(Machine), EmptyNextEventFunc);

                    default:
                        return (null, EmptyNextEventFunc);
                }
            }
        }

        public sealed class AfterRegistrationSyncFailed : State
        {
            public AfterRegistrationSyncFailed(ActivationStateMachine machine)
                : base(machine)
            {
            }

            public override bool Persist => true;

            public override bool CanHandleEvent(Event @event)
            {
                return @event is CalledActivate || @event is GotPushDeviceDetails || @event is CalledDeactivate;
            }

            public override async Task<(State, Func<Task<Event>>)> Transition(Event @event)
            {
                switch (@event)
                {
                    case CalledActivate _:
                    case GotPushDeviceDetails _:
                        return (new WaitingForRegistrationSync(Machine, @event), ToNextEventFunc(Machine.ValidateRegistration));
                    case CalledDeactivate _:
                        return (new WaitingForDeregistration(Machine, this), ToNextEventFunc(Machine.Deregister));
                    default:
                        return (null, EmptyNextEventFunc);
                }
            }
        }
    }
}
