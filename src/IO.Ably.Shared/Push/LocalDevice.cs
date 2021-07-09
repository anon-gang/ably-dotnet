using System;
using IO.Ably.Encryption;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IO.Ably.Push
{
    /// <summary>
    /// LocalDevice represents the current state of the device in respect of it being a target for push notifications.
    /// </summary>
    public class LocalDevice : DeviceDetails
    {
        /// <summary>
        /// Devices that have completed registration have an identity token assigned to them by the push service. TODO: Check how accurate this is.
        /// </summary>
        [JsonIgnore]
        public string DeviceIdentityToken { get; set; }

        internal bool IsRegistered => DeviceIdentityToken.IsNotEmpty();

        internal bool IsCreated => Id.IsNotEmpty();

        internal RegistrationToken RegistrationToken
        {
            get => RegistrationToken.FromRecipientJson(Push?.Recipient, DefaultLogger.LoggerInstance);
            set => Push.Recipient = RegistrationToken.ToRecipientJson(value, DefaultLogger.LoggerInstance);
        }

        /// <summary>
        /// Create a new instance of localDevice with a random Id and secret.
        /// </summary>
        /// <param name="clientId">The clientId which is set on the device. Can be null.</param>
        /// <param name="mobileDevice">MobileDevice interface.</param>
        /// <returns>Instance of LocalDevice.</returns>
        public static LocalDevice Create(string clientId, IMobileDevice mobileDevice)
        {
            return new LocalDevice
            {
                Id = Guid.NewGuid().ToString("D"),
                DeviceSecret = Crypto.GenerateSecret(),
                ClientId = clientId,

                // TODO: Pass mobile device in constructor instead of using static dependencies.
                Platform = mobileDevice.DevicePlatform,
                FormFactor = mobileDevice.FormFactor,
            };
        }
    }
}
