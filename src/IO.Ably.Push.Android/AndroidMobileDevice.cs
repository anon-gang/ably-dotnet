using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Android.App;
using Android.Content;
using Android.Gms.Tasks;
using Android.Support.V4.Content;
using Firebase.Messaging;
using IO.Ably.Infrastructure;
using Xamarin.Essentials;

namespace IO.Ably.Push.Android
{
    /// <summary>
    /// Android implementation for IMobile device.
    /// </summary>
    public class AndroidMobileDevice : IMobileDevice
    {
        private const string TokenType = "fcm";
        private static AblyRealtime _realtimeInstance;

        /// <summary>
        /// Initialises the Android MobileDevice implementation with the IoC dependency.
        /// </summary>
        /// <param name="configureCallbacks">Option action which can be used to configure callbacks. It's especially useful to subscribe/unsubscribe to push channels when a device is activated / deactivated.</param>
        public static void Initialise(Action<PushCallbacks> configureCallbacks = null)
        {
            var callbacks = new PushCallbacks();
            configureCallbacks?.Invoke(callbacks);
            IoC.MobileDevice = new AndroidMobileDevice(callbacks, DefaultLogger.LoggerInstance);
        }

        /// <summary>
        /// Registers the AblyRealtime instance used for push notifications.
        /// </summary>
        /// <param name="realtime">Instance of AblyRealtime.</param>
        public static void RegisterRealtimeInstanceForPush(AblyRealtime realtime) => _realtimeInstance = realtime;

        /// <summary>
        /// Method to be called by OnNewToken which is part of a class inheriting from FirebaseMessagingService.
        /// </summary>
        /// <param name="token">The passed deviceRegistration token.</param>
        public static void OnNewRegistrationToken(string token)
        {
            if (_realtimeInstance is null)
            {
                throw new AblyException(
                    "No realtime instance was registered. Please call AndroidMobileDevice.RegisterRealtimeInstanceForPush(realtime) and pass your AblyRealtime instance.");
            }

            var logger = DefaultLogger.LoggerInstance;
            logger.Debug($"Received OnNewRegistrationToken with token {token}");

            var pushRealtime = _realtimeInstance.GetPushRealtime();
            var registrationToken = new RegistrationToken(TokenType, token);
            pushRealtime.StateMachine.UpdateRegistrationToken(Result.Ok(registrationToken));
        }

        private readonly ILogger _logger;

        private AndroidMobileDevice(PushCallbacks callbacks, ILogger logger)
        {
            Callbacks = callbacks;
            _logger = logger;
        }

        private Context Context => Application.Context;

        /// <inheritdoc/>
        public string DevicePlatform => "android";

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

        /// <summary>
        /// Used to setup callbacks for various places in the push notifications process.
        /// </summary>
        public PushCallbacks Callbacks { get; }

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

        /// <inheritdoc/>
        public void RequestRegistrationToken(Action<Result<RegistrationToken>> callback)
        {
            try
            {
                _logger.Debug("Requesting a new Registration token");
                var messagingInstance = FirebaseMessaging.Instance;
                var resultTask = messagingInstance.GetToken();

                resultTask.AddOnCompleteListener(new RequestTokenCompleteListener(callback, _logger));
            }
            catch (Exception e)
            {
                _logger.Error("Error while requesting a new Registration token.", e);
                var errorInfo = new ErrorInfo($"Failed to request AndroidToken. Error: {e?.Message}.", 50000, HttpStatusCode.InternalServerError, e);
                callback(Result.Fail<RegistrationToken>(errorInfo));
            }
        }

        private class RequestTokenCompleteListener : Java.Lang.Object, IOnCompleteListener
        {
            private readonly Action<Result<RegistrationToken>> _callback;
            private readonly ILogger _logger;

            public RequestTokenCompleteListener(Action<Result<RegistrationToken>> callback, ILogger logger)
            {
                _callback = callback;
                _logger = logger;
            }

            public void OnComplete(Task task)
            {
                if (task.IsSuccessful)
                {
                    _logger.Debug($"RequestTaken completed successfully. Result: {task.Result}");
                    var registrationToken = new RegistrationToken(TokenType, (string)task.Result);
                    _callback(Result.Ok(registrationToken));
                }
                else
                {
                    _logger.Error("RequestToken failed.", task.Exception);
                    var exception = task.Exception;
                    var errorInfo = new ErrorInfo(
                        $"Failed to return valid AndroidToken. Error: {exception?.Message}.",
                        50000,
                        HttpStatusCode.InternalServerError,
                        exception);
                    _callback(Result.Fail<RegistrationToken>(errorInfo));
                }
            }
        }
    }
}
