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
        [Trait("spec", "RSH3a")]
        public class NotActivatedStateTests : MockHttpRestSpecs
        {
            public IMobileDevice MobileDevice { get; }

            private ActivationStateMachine GetStateMachine()
            {
                var rest = GetRestClient();
                return new ActivationStateMachine(rest, MobileDevice, Logger);
            }

            private (AblyRest, ActivationStateMachine) GetStateMachineAndRestClient()
            {
                var rest = GetRestClient();
                return (rest, new ActivationStateMachine(rest, MobileDevice, Logger));
            }

            [Fact]
            public void NotActivated_ShouldBeTheInitialState_WhenConstructed()
            {
                var stateMachine = GetStateMachine();
                stateMachine.CurrentState.Should().BeOfType<ActivationStateMachine.NotActivated>();
            }

            [Fact]
            [Trait("spec", "TI4")]
            public async Task WithCalledDeactivateEvent_ShouldRemainInState_NotActivated()
            {
                var stateMachine = GetStateMachine();
                stateMachine.StateChangeHandler = (currentState, newState)
                    => throw new AssertionFailedException("There should be no state transition");

                await stateMachine.HandleEvent(new ActivationStateMachine.CalledDeactivate());

                stateMachine.CurrentState.Should().BeOfType<ActivationStateMachine.NotActivated>();
            }

            public NotActivatedStateTests(ITestOutputHelper output)
                : base(output)
            {
                MobileDevice = new FakeMobileDevice();
            }
        }
    }
}
