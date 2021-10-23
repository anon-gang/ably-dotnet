using IO.Ably;
using Xamarin.Forms;

namespace DotnetPush
{
    /// <summary>
    /// Xamarin Application entry point.
    /// </summary>
    public partial class App
    {
        /// <summary>
        /// Ably Factory.
        /// </summary>
        public AblyFactory AblyFactory { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        /// <param name="ablyFactory">Factory.</param>
        /// <param name="appLoggerSink">Instance of the AppLoggerSink so we can display and analyze logs inside the app.</param>
        public App(AblyFactory ablyFactory, AppLoggerSink appLoggerSink)
        {
            InitializeComponent();
            AblyFactory = ablyFactory;

            DependencyService.RegisterSingleton(ablyFactory);

            DependencyService.RegisterSingleton(appLoggerSink);
            MainPage = new AppShell();
        }
    }
}
