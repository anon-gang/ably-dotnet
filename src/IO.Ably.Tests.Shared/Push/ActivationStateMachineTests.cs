using System;
using System.Collections.Generic;
using FluentAssertions;
using IO.Ably.Push;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.DotNetCore20.Push
{
    public class FakeMobileDevice : IMobileDevice
    {
        public Func<Result<string>> GetRegistrationToken => () => Result.Ok(Guid.NewGuid().ToString());
        public Dictionary<string, string> Settings { get; } = new Dictionary<string, string>();

        public void SendIntent(string name, Dictionary<string, object> extraParameters)
        {
            throw new NotImplementedException();
        }

        public void SetPreference(string key, string value, string groupName)
        {
            Settings[$"{groupName}:{key}"] = value;
        }

        public string GetPreference(string key, string groupName)
        {
            return Settings[$"{groupName}:{key}"];
        }

        public void RemovePreference(string key, string groupName)
        {
            Settings.Remove($"{groupName}:{key}");
        }

        public void ClearPreferences(string groupName)
        {
            var keysToRemove = new List<string>();
            foreach (var key in Settings.Keys)
            {
                if (key.StartsWith(groupName))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                Settings.Remove(key);
            }
        }

        public void RequestRegistrationToken(Action<Result<string>> callback)
        {
            callback(GetRegistrationToken());
        }

        public string DevicePlatform => "test";

        public string FormFactor => "phone";
    }

    public partial class ActivationStateMachineTests
    {
        [Trait("spec", "TI4")]
        public class NotActivatedStateTests : MockHttpRestSpecs
        {
            public IMobileDevice MobileDevice { get; }

            [Fact]
            public void NotActivated_ShouldBeTheInitialState_WhenConstructed()
            {
                var rest = GetRestClient();
                var stateMachine = new ActivationStateMachine(rest, MobileDevice, Logger);

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