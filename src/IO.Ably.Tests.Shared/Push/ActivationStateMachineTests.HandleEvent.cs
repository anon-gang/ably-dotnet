using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using IO.Ably.Push;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.DotNetCore20.Push
{
    public partial class ActivationStateMachineTests
    {
        public class HandleEventTests : MockHttpRestSpecs
        {
            private class FakeEvent : ActivationStateMachine.Event { }

            private class FakeState : ActivationStateMachine.State
            {
                public string Name { get; }

                private bool _persist;

                public FakeState(ActivationStateMachine machine, string name)
                    : base(machine)
                {
                    Name = name;
                }

                public override bool Persist => _persist;

                public void SetPersist(bool value) => _persist = value;

                public Func<ActivationStateMachine.Event, bool> CanHandleEventHandler = (@event) => @event is FakeEvent;

                public override bool CanHandleEvent(ActivationStateMachine.Event @event)
                {
                    return CanHandleEventHandler(@event);
                }

                public Func<ActivationStateMachine.Event, Task<(ActivationStateMachine.State, Func<Task<ActivationStateMachine.Event>>)>> TransitionHandler = async e => (null, ActivationStateMachine.EmptyNextEventFunc);

                public override Task<(ActivationStateMachine.State, Func<Task<ActivationStateMachine.Event>>)> Transition(ActivationStateMachine.Event @event) => TransitionHandler(@event);
            }

            public IMobileDevice MobileDevice { get; }

            private ActivationStateMachine GetStateMachine()
            {
                var rest = GetRestClient();
                return new ActivationStateMachine(rest, MobileDevice, Logger);
            }

            [Fact]
            public async Task WhenStateCannotHandleEvent_ShouldQueueCurrentEventInPendingQueue()
            {
                var stateMachine = GetStateMachine();

                var state = new FakeState(stateMachine, "Can't handle anything");
                state.CanHandleEventHandler = _ => false;
                stateMachine.SetState(state);
                stateMachine.StateChangeHandler = (current, newState) =>
                    throw new AssertionFailedException("There should be no state transition");

                var eventThatCantBeHandled = new ActivationStateMachine.CalledActivate();

                await stateMachine.HandleEvent(eventThatCantBeHandled);

                stateMachine.PendingEvents.Count.Should().Be(1);
                var firstEvent = stateMachine.PendingEvents.Dequeue();
                firstEvent.Should().BeSameAs(eventThatCantBeHandled);
            }

            // Test Persist
            // Test that second event is handled after first transition.
            // Test that pending events are handled when something happens with the state.
            // Test that if a pending event can't be handled then it's kept in the queue

            public HandleEventTests(ITestOutputHelper output)
                : base(output)
            {
                MobileDevice = new FakeMobileDevice();
            }
        }
    }
}
