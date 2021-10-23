using System.ComponentModel;
using System.Windows.Input;
using IO.Ably.Push;
using Xamarin.Forms;

namespace DotnetPush.ViewModels
{
    /// <summary>
    /// ViewModel class for the About page.
    /// </summary>
    public class AboutViewModel : BaseViewModel
    {
        private string _clientId;
        private string _currentState;

        /// <summary>
        /// Initializes a new instance of the <see cref="AboutViewModel"/> class.
        /// </summary>
        public AboutViewModel()
        {
            Title = "About";

            // pushRealtime.OnActivationSta teMachineChangeState((current, next) =>
            // {
            //     CurrentState = next;
            // });
            ActivatePush = new Command(() => Ably.Push.Activate());
            DeactivatePush = new Command(() => Ably.Push.Deactivate());
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HasAbly))
            {
                ShowAblyMessage = !HasAbly;

                if (HasAbly)
                {
                    ClientId = Ably.Auth.ClientId;
                }
            }
        }

        /// <summary>
        /// Command which will call AblyRealtime.Push.Activate().
        /// </summary>
        public ICommand ActivatePush { get; }

        /// <summary>
        /// Command which will call AblyRealtime.Push.Deactivate().
        /// </summary>
        public ICommand DeactivatePush { get; }

        private bool _showAblyMessage = true;

        /// <summary>
        /// Displays current clientId that is set in the library.
        /// </summary>
        public bool ShowAblyMessage
        {
            get => _showAblyMessage;
            set => SetProperty(ref _showAblyMessage, value);
        }

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
        public string CurrentState
        {
            get => _currentState;
            set => SetProperty(ref _currentState, value);
        }
    }
}
