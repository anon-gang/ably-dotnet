using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using IO.Ably.Push;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace DotnetPush.ViewModels
{
    /// <summary>
    /// ViewModel class for the About page.
    /// </summary>
    public class SettingsViewModel : BaseViewModel
    {
        private string _clientId;
        private string _authKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="AboutViewModel"/> class.
        /// </summary>
        /// <param name="dialogs">Dialogs.</param>
        public SettingsViewModel()
        {
            Title = "Settings";

            // TODO: FIx dependency

            // Load settings from Preferences
            ClientId = AblySettings.ClientId;
            AblyAuthKey = AblySettings.AblyKey;

            Save = new Command(() =>
            {
                AblySettings.ClientId = ClientId;
                AblySettings.AblyKey = AblyAuthKey;
            });

            InitialiseAbly = new Command(() =>
            {
                if (string.IsNullOrEmpty(AblyAuthKey))
                {
                    Debug.Write("Please fill in the AblyAuthKey", "Invalid configuration");
                }
                else
                {
                    var realtimeClient = AblyFactory.Configure(opts => opts.Key = AblyAuthKey, new PushCallbacks()
                    {
                        ActivatedCallback = error =>
                        {
                            if (error != null)
                            {
                                Debug.Write($"Failed to activate. Message: {error.Message}");
                            }

                            Debug.Write("Successfully activated push notifications.");
                            return Task.CompletedTask;
                        },
                        DeactivatedCallback = error =>
                        {
                            if (error != null)
                            {
                                Debug.Write($"Failed to deactivate push notifications. Message: {error.Message}");
                            }

                            Debug.Write("Successfully deactivated push notifications");

                            return Task.CompletedTask;
                        },
                        SyncRegistrationFailedCallback = error =>
                        {
                            if (error != null)
                            {
                                Debug.Write($"Sync registration failed. Message: {error.Message}");
                            }

                            Debug.Write("Sync registration failed without an error.");

                            return Task.CompletedTask;
                        }
                    });

                    DependencyService.RegisterSingleton(realtimeClient);
                    HasAbly = true;
                }
            });
        }

        /// <summary>
        /// Command which will call AblyRealtime.Push.Activate().
        /// </summary>
        public ICommand Save { get; }

        /// <summary>
        /// Command which will call AblyRealtime.Push.Activate().
        /// </summary>
        public ICommand InitialiseAbly { get; }

        /// <summary>
        /// Displays current clientId that is set in the library.
        /// </summary>
        public string ClientId
        {
            get => _clientId;
            set => SetProperty(ref _clientId, value);
        }

        /// <summary>
        /// Displays the current State of the ActivationStateMachine. It's only updated
        /// and doesn't show the current loaded state.
        /// </summary>
        public string AblyAuthKey
        {
            get => _authKey;
            set => SetProperty(ref _authKey, value);
        }
    }
}
