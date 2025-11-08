using System;
using Android;
using Android.App;
using Android.Content.PM;
using Android.Hardware.Usb;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.Core.View;
using Google.Android.Material.BottomNavigation;
using Xamarin.Essentials;

#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member

namespace MSS6xTool
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    [IntentFilter(new[] { UsbManager.ActionUsbDeviceAttached })]
    [MetaData(UsbManager.ActionUsbDeviceAttached, Resource = "@xml/device_filter")]

    // ReSharper disable once UnusedMember.Global
#pragma warning disable CS0618
    internal class MainActivity : AppCompatActivity, BottomNavigationView.IOnNavigationItemSelectedListener
#pragma warning restore CS0618
    {
        [Obsolete]
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Global.Activity = this;

            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            RequestPermissions(new[]
            {
                Manifest.Permission.WriteExternalStorage,
                Manifest.Permission.ReadExternalStorage,
                Manifest.Permission.BatteryStats,
                Manifest.Permission.WakeLock,
                Manifest.Permission.Internet
            }, 0);

            var navigationView = FindViewById<BottomNavigationView>(Resource.Id.navigation);
            navigationView?.SetOnNavigationItemSelectedListener(this);

            using (var pm = (PowerManager)GetSystemService(PowerService))
            {
                Global.WakeLock = pm?.NewWakeLock(WakeLockFlags.ScreenDim, PackageName);
            }

            Global.UsbManager = (UsbManager)GetSystemService(UsbService);
            if (Global.UsbManager?.DeviceList != null)
                foreach (var dev in Global.UsbManager?.DeviceList!)
                {
                    if (dev.Value.VendorId == 1027)
                    {
                        Global.UsbDevice = dev.Value;
                    }
                }

            FileManagement.AssetPrepare(Assets);
            Ui.UiLink();
            Tweaks.UiLink();
            AdvancedMenu.RestoreSettings();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            MenuCompat.SetGroupDividerEnabled(menu, true);
            AdvancedMenu.Menu = menu;
            AdvancedMenu.BatteryOverride(true);
            AdvancedMenu.AirplaneOverride(true);
            MSS6x.IdentifyDme();

            return true;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.viewSwitch:
                    Ui.ViewSwitch();
                    return true;
            }
            return false;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            AdvancedMenu.MenuSelection(item);
            return base.OnOptionsItemSelected(item);
        }
    }
}
