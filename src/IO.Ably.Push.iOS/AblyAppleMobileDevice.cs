using System;
using System.Collections.Generic;
using IO.Ably.Infrastructure;
using UIKit;
using Xamarin.Essentials;

namespace IO.Ably.Push.iOS
{
    public class AblyAppleMobileDevice : IMobileDevice
    {
        private readonly ILogger _logger;

        private AblyAppleMobileDevice(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Initialises the Android MobileDevice implementation with the IoC dependency.
        /// </summary>
        public static void Initialise()
        {
            IoC.MobileDevice = new AblyAppleMobileDevice(DefaultLogger.LoggerInstance);
        }

        public static void OnNewRegistrationToken(string token, AblyRealtime realtime)
        {
            // Call the state machine to register the new token
            var realtimePush = realtime.GetPushRealtime();
            realtimePush.StateMachine.UpdateRegistrationToken(Result.Ok(token));
        }

        public static void OnRegistrationTokenFailed(ErrorInfo error, AblyRealtime realtime)
        {
            // Call the state machine to register the new token
            var realtimePush = realtime.GetPushRealtime();
            realtimePush.StateMachine.UpdateRegistrationToken(Result.Fail<string>(error));
        }

        public void SendIntent(string name, Dictionary<string, object> extraParameters)
        {
            // TODO: Remove and replace with delegates
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

        public void RequestRegistrationToken(Action<Result<string>> callback)
        {
            UIApplication.SharedApplication.RegisterForRemoteNotifications();

        }

        public string DevicePlatform => "apple"; // TODO: See if there is a way to distinguish between IOS, MacOs, WatchOs
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
    }
}