using IO.Ably.Infrastructure;
using Newtonsoft.Json.Linq;

namespace IO.Ably.Push
{
    /// <summary>
    /// Class used to hold registration tokens.
    /// </summary>
    public class RegistrationToken
    {
        /// <summary>
        /// FGM or GCM for Google and APNS for Apple.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Token value.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Constructs a new registration token instance.
        /// </summary>
        /// <param name="type">Token type.</param>
        /// <param name="token">Token value.</param>
        public RegistrationToken(string type, string token)
        {
            Type = type;
            Token = token;
        }

        /// <summary>
        /// Overrides to string to display Type and Token.
        /// </summary>
        /// <returns>Returns a string including Type and Token Value.</returns>
        public override string ToString()
        {
            return $"RegistrationToken: Type = {Type}, Token = {Token}";
        }

        internal static JObject ToRecipientJson(RegistrationToken token, ILogger logger)
        {
            switch (token.Type)
            {
                case "apns":
                    JObject appleJson = new JObject();
                    appleJson.Add("transportType", token.Type);
                    appleJson.Add("deviceToken", token.Token);
                    return appleJson;
                case "fcm":
                    JObject androidJson = new JObject();
                    androidJson.Add("transportType", token.Type);
                    androidJson.Add("registrationToken", token.Token);
                    return androidJson;
                default:
                    logger.Warning($"ToRecipientJson: Invalid token type {token.Type}");
                    return null;
            }
        }

        internal static RegistrationToken FromRecipientJson(JObject recipientJson, ILogger logger)
        {
            if (recipientJson != null)
            {
                var transportType = (string)recipientJson.GetValue("transportType");
                switch (transportType)
                {
                    case "fcm":
                        return new RegistrationToken(transportType, (string)recipientJson.GetValue("registrationToken"));
                    case "apns":
                        return new RegistrationToken(transportType, (string)recipientJson.GetValue("deviceToken"));
                    default:
                        logger.Warning($"FromRecipientJson: Invalid transportType {transportType}");
                        return null;
                }
            }

            return null;
        }
    }
}
