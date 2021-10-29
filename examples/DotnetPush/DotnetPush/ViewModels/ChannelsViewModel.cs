using DotnetPush.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using DotnetPush.Models;
using IO.Ably;
using IO.Ably.Push;
using Xamarin.Forms;

namespace DotnetPush.ViewModels
{
    /// <summary>
    /// Describes a channel in the UI.
    /// </summary>
    public class AblyChannel
    {
        /// <summary>
        /// Name of the ably channel.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of the channel.</param>
        public AblyChannel(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// View model for the Log page.
    /// </summary>
    public class ChannelsViewModel : BaseViewModel
    {
        private string _channelName;
        private string _message;

        /// <summary>
        /// Command to Load log entries.
        /// </summary>
        public Command LoadChannelsCommand { get; }

        /// <summary>
        /// Subscribes current device to channel.
        /// </summary>
        public Command SubscribeToChannel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogViewModel"/> class.
        /// </summary>
        public ChannelsViewModel()
        {
            ChannelsCollection = new ObservableCollection<AblyChannel>();
            LoadChannelsCommand = new Command(async () => await ExecuteLoadItemsCommand());
            SubscribeToChannel = new Command(async () =>
            {
                if (string.IsNullOrEmpty(ChannelName))
                {
                    Message = "Please enter a channel name";
                }

                try
                {
                    await Ably.Channels.Get(ChannelName).Push.SubscribeDevice();
                    Message = "Device successfully subscribed to channel";
                }
                catch (AblyException e)
                {
                    Message = $"Error subscribing device to channel. Messages: {e.Message}. Code: {e.ErrorInfo.Code}";
                }

                ChannelName = string.Empty;
            });
        }

        /// <summary>
        /// Observable collection of LogEntries.
        /// </summary>
        public ObservableCollection<AblyChannel> ChannelsCollection { get; set; }

        /// <summary>
        /// Channel name.
        /// </summary>
        public string ChannelName
        {
            get => _channelName;
            set => SetProperty(ref _channelName, value);
        }

        /// <summary>
        /// Message.
        /// </summary>
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        private async Task ExecuteLoadItemsCommand()
        {
            IsBusy = true;

            try
            {
                ChannelsCollection.Clear();
                var device = Ably.Device;

                if (device.IsRegistered == false)
                {
                    Message = "Cannot get subscriptions when the local device is not registered";
                    return;
                }

                var subscriptions = await Ably.Push.Admin.ChannelSubscriptions.ListAsync(ListSubscriptionsRequest.WithDeviceId(device.Id));
                foreach (var subscription in subscriptions.Items)
                {
                    ChannelsCollection.Add(new AblyChannel(subscription.Channel));
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Executed before the View is displayed.
        /// </summary>
        public void OnAppearing()
        {
            IsBusy = true;
        }
    }
}
