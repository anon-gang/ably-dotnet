using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DotnetPush;
using Foundation;
using IO.Ably.Infrastructure;
using UIKit;
using Xamarin.Essentials;

namespace IO.Ably.Push.iOS
{
    public class AblyAppleMobileDevice : IMobileDevice
    {
        private const string TokenType = "apns";

        private readonly ILogger _logger;

        private AblyAppleMobileDevice(PushCallbacks callbacks, ILogger logger)
        {
            Callbacks = callbacks;
            _logger = logger;
        }

        /// <summary>
        /// Initialises the Android MobileDevice implementation with the IoC dependency.
        /// </summary>
        /// <param name="configureCallbacks">Action to configure callbacks.</param>
        public static void Initialise(Action<PushCallbacks> configureCallbacks = null)
        {
            var callbacks = new PushCallbacks();
            configureCallbacks?.Invoke(callbacks);
            IoC.MobileDevice = new AblyAppleMobileDevice(callbacks, DefaultLogger.LoggerInstance);
        }

        public static void OnNewRegistrationToken(NSData tokenData, AblyRealtime realtime)
        {
            if (tokenData != null)
            {
                try
                {
                    var token = ConvertTokenToString(tokenData);
                    // Call the state machine to register the new token
                    var realtimePush = realtime.GetPushRealtime();
                    var tokenResult = Result.Ok(new RegistrationToken(TokenType, token));
                    realtimePush.StateMachine.UpdateRegistrationToken(tokenResult);
                }
                catch (Exception e)
                {
                    realtime?.Logger.Error($"Error setting new token. Token: {tokenData}", e);
                }
            }


            string ConvertTokenToString(NSData deviceToken)
            {
                // TODO: Validate with Toni if that is correct
                if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
                {
                    return BitConverter.ToString(deviceToken.ToArray()).Replace("-", string.Empty);
                }

                return Regex.Replace(deviceToken.ToString(), "[^0-9a-zA-Z]+", string.Empty);
            }

        }

        public static void OnRegistrationTokenFailed(ErrorInfo error, AblyRealtime realtime)
        {
            // Call the state machine to register the new token
            var realtimePush = realtime.GetPushRealtime();
            realtimePush.StateMachine.UpdateRegistrationToken(Result.Fail<RegistrationToken>(error));
        }

        /// <inheritdoc/>
        public void SetPreference(string key, string value, string groupName)
        {
            _logger.Debug($"Setting preferences: {groupName}:{key} with value {value}");
            Preferences.Set(key, value, groupName);
        }

        /// <inheritdoc/>
        public string GetPreference(string key, string groupName)
        {
            return Preferences.Get(key, string.Empty, groupName);
        }

        /// <inheritdoc/>
        public void RemovePreference(string key, string groupName)
        {
            _logger.Debug($"Removing preference: {groupName}:{key}");
            Preferences.Remove(key, groupName);
        }

        /// <inheritdoc/>
        public void ClearPreferences(string groupName)
        {
            _logger.Debug($"Clearing preferences group: {groupName}");
            Preferences.Clear(groupName);
        }

        public void RequestRegistrationToken(Action<Result<RegistrationToken>> _) // For IOS integration the callback is not used
        {
            // Register for push notifications.
            var settings = UIUserNotificationSettings.GetSettingsForTypes(
                UIUserNotificationType.Alert
                | UIUserNotificationType.Badge
                | UIUserNotificationType.Sound,
                new NSSet());
            UIApplication.SharedApplication.RegisterUserNotificationSettings(settings);
            UIApplication.SharedApplication.RegisterForRemoteNotifications();

        }

        public string DevicePlatform => "ios"; // TODO: See if there is a way to distinguish between IOS, MacOs, WatchOs
        /// <inheritdoc/>
        public string FormFactor
        {
            get
            {
                var idiom = DeviceInfo.Idiom;
                if (idiom == DeviceIdiom.Watch)
                {
                    return DeviceFormFactor.Watch;
                }

                if (idiom == DeviceIdiom.TV)
                {
                    return DeviceFormFactor.Tv;
                }

                if (idiom == DeviceIdiom.Tablet)
                {
                    return DeviceFormFactor.Tablet;
                }

                if (idiom == DeviceIdiom.Phone)
                {
                    return DeviceFormFactor.Phone;
                }

                if (idiom == DeviceIdiom.Desktop)
                {
                    return DeviceFormFactor.Desktop;
                }

                return DeviceFormFactor.Other;
            }
        }

        public PushCallbacks Callbacks { get; }
    }
}