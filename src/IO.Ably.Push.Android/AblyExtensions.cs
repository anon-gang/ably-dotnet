﻿using System.Reflection;

namespace IO.Ably.Push.Android
{
    /// <summary>
    /// Temporary helper extension until we are ready to make AblyRealtime.Push public.
    /// </summary>
    internal static class AblyExtensions
    {
        /// <summary>
        /// Uses reflection to get an instance of RealtimePush from an instance of the Realtime library.
        /// </summary>
        /// <param name="realtime">Instance of AblyRealtime.</param>
        /// <returns>An instance of PushRealtime.</returns>
        public static PushRealtime GetPushRealtime(this AblyRealtime realtime)
        {
            var pushProperty = realtime.GetType().GetProperty("Push", BindingFlags.Instance | BindingFlags.NonPublic);
            return (PushRealtime)pushProperty.GetValue(realtime);
        }
    }
}
