using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using IO.Ably.Push.Android;
using Firebase;
using Xamarin.Essentials;

namespace DotnetPush.Droid
{
    [Activity(Label = "DotnetPush", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private AppLoggerSink _loggerSink;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _loggerSink = new AppLoggerSink();
            Platform.Init(this, savedInstanceState);
            Xamarin.Forms.Forms.Init(this, savedInstanceState);

            // Initialise the Firebase application
            FirebaseApp.InitializeApp(this);
            var factory = new AblyFactory(AndroidMobileDevice.Initialise, _loggerSink);
            LoadApplication(new App(factory, _loggerSink));
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}