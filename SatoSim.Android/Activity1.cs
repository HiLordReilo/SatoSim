using System.IO;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using FmodForFoxes;
using Microsoft.Xna.Framework;
using SatoSim.Core;
using static SatoSim.Core.Managers.GameManager;



namespace SatoSim.Android
{
    [Activity(
        Label = "@string/app_name",
        MainLauncher = true,
        Icon = "@drawable/icon",
        AlwaysRetainTaskState = true,
        LaunchMode = LaunchMode.SingleInstance,
        ScreenOrientation = ScreenOrientation.SensorLandscape,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize,
        Immersive = true
    )]
    public class Activity1 : AndroidGameActivity
    {
        private Game1 _game;
        private View _view;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Setup game directory
            GameDirectory = Path.Combine("/storage/emulated/0/", "Directory");
            //if (!Directory.Exists(GameDirectory)) Directory.CreateDirectory(GameDirectory);
            
            _game = new Game1(new AndroidNativeFmodLibrary());
            _view = _game.Services.GetService(typeof(View)) as View;

            SetContentView(_view);
            _game.Run();
        }
        
        protected override void OnResume()
        {
            base.OnResume();

            // When we resume (which also seems to happen on startup), hide the system UI to go to full screen mode.
            HideSystemUI();
        }

        private void HideSystemUI()
        {
            // Apparently for Android OS Kitkat and higher, you can set a full screen mode. Why this isn't on by default, or some kind
            // of simple switch, is beyond me.
            // Got this from the following forum post: http://community.monogame.net/t/blocking-the-menu-bar-from-appearing/1021/2
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
            {
                View decorView = Window.DecorView;
                var uiOptions = (int)decorView.SystemUiVisibility;
                var newUiOptions = (int)uiOptions;

                newUiOptions |= (int)SystemUiFlags.LowProfile;
                newUiOptions |= (int)SystemUiFlags.Fullscreen;
                newUiOptions |= (int)SystemUiFlags.HideNavigation;
                newUiOptions |= (int)SystemUiFlags.ImmersiveSticky;
                newUiOptions |= (int)SystemUiFlags.Immersive;

                decorView.SystemUiVisibility = (StatusBarVisibility)newUiOptions;

                //this.Immersive = true;
            }
        }
    }
}