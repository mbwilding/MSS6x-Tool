using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Android.Views;
using Xamarin.Essentials;

namespace MSS6x_Tool
{
    internal static class AdvancedMenu
    {
        private static readonly Activity Activity = Global.Activity;
        public static IMenu Menu;

        public static void RestoreSettings()
        {
            Global.BatteryOverride = Preferences.Get("BatteryOverride", false);
            Global.AirplaneOverride = Preferences.Get("AirplaneOverride", false);
        }

        public static async void UsbCheck(UsbManager usbManager, UsbDevice usbDevice)
        {
            string msg = null;

            if (usbManager.DeviceList != null && usbManager.DeviceList.Count > 0)
            {
                var manufacturerName = usbDevice.ManufacturerName;
                var productName = usbDevice.ProductName;
                var productId = usbDevice.ProductId;
                var vendorId = usbDevice.VendorId;
                var deviceId = usbDevice.DeviceId;
                var version = usbDevice.Version;
                var serialNum = usbDevice.SerialNumber;
                var deviceProtocol = usbDevice.DeviceProtocol;
                var interfaceCount = usbDevice.InterfaceCount;
                var configCount = usbDevice.ConfigurationCount;

                msg += "Manufacturer Name: " + manufacturerName + "\n";
                msg += "Product Name: " + productName + "\n";
                msg += "Product ID: " + productId + "\n";
                msg += "Vendor ID: " + vendorId + "\n";
                msg += "Device ID: " + deviceId + "\n";
                msg += "Version: " + version + "\n";
                msg += "Serial: " + serialNum + "\n";
                msg += "----------------------\n";
                msg += "Device Protocol: " + deviceProtocol + "\n";
                msg += "Interface Count: " + interfaceCount + "\n";
                msg += "Config Count: " + configCount;
            }
            else
            {
                msg = "No device connected.";
            }

            await Ui.Message("USB Device Info", msg);
        }

        public static bool BatteryCheck(int percent)
        {
            if (Global.BatteryOverride)
            {
                return false;
            }
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (Battery.ChargeLevel * 100 >= percent || Battery.ChargeLevel == -1)
            {
                return false;
            }
            _ = Ui.Message("Battery Warning", "Charge your battery to at least " + percent + "%.");
            return true;
        }

        public static async void BatteryOverride(bool startCheck = false)
        {
            if (!startCheck)
            {
                if (!Global.BatteryOverride)
                {
                    var confirm = await Ui.Message("Warning",
                        "Flashing with a low battery level puts your vehicle at serious risk.\n" +
                        "You can brick your DME if your device turns off during a flash cycle."
                        , "OK", "Cancel", true);

                    if (!confirm) return;
                }
                Global.BatteryOverride = !Global.BatteryOverride;
            }
            var state = Global.BatteryOverride ? "No" : "Yes";
            var title = "Battery Check: " + state;
            Preferences.Set("BatteryOverride", Global.BatteryOverride);
            Menu.FindItem(Resource.Id.Battery_Override)!.SetTitle(title);
        }

        public static bool IsAirplaneMode(Context context)
        {
            if (Global.AirplaneOverride)
            {
                return true;
            }

            var isAirplaneModeOn = Android.Provider.Settings.Global.GetInt(context.ContentResolver,
                Android.Provider.Settings.Global.AirplaneModeOn);

            if (isAirplaneModeOn != 0) return true;

            _ = Ui.Message("Airplane Mode", "Please enable airplane mode first.");
            return false;
        }

        public static async void AirplaneOverride(bool startCheck = false)
        {
            if (!startCheck)
            {
                if (!Global.AirplaneOverride)
                {
                    var confirm = await Ui.Message("Warning",
                        "Flashing without airplane mode enabled puts your vehicle at serious risk.\n" +
                        "You can brick your DME if your device gets distracted by other applications."
                        , "OK", "Cancel", true);

                    if (!confirm) { return; }
                }
                Global.AirplaneOverride = !Global.AirplaneOverride;
            }
            var state = Global.AirplaneOverride ? "No" : "Yes";
            var title = "Airplane Check: " + state;
            Preferences.Set("AirplaneOverride", Global.AirplaneOverride);
            Menu.FindItem(Resource.Id.Airplane_Override)!.SetTitle(title);
        }

        public static void MenuSelection(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.Read_Full_Long:
                    if (BatteryCheck(20)) break;
                    if (!IsAirplaneMode(Activity)) break;
                    _ = MSS6x.ReadFull(false);
                    break;

                case Resource.Id.Read_ISN_SK:
                    if (BatteryCheck(10)) break;
                    if (!IsAirplaneMode(Activity)) break;
                    _ = MSS6x.ReadISN_SK();
                    break;

                case Resource.Id.Read_RAM:
                    if (BatteryCheck(10)) break;
                    if (!IsAirplaneMode(Activity)) break;
                    _ = MSS6x.ReadRam();
                    break;

                case Resource.Id.RSA_Bypass_Fast:
                    if (BatteryCheck(15)) break;
                    if (!IsAirplaneMode(Activity)) break;
                    _ = MSS6x.RsaBypassTasks();
                    break;

                case Resource.Id.Read_Codes:
                    _ = Ui.Message("Pro Feature", Ui.Pro);
                    break;

                case Resource.Id.Reset_Codes:
                    _ = Ui.Message("Pro Feature", Ui.Pro);
                    break;

                case Resource.Id.Register_Battery:
                    _ = Ui.Message("Pro Feature", Ui.Pro);
                    break;

                case Resource.Id.Battery_Override:
                    BatteryOverride();
                    break;

                case Resource.Id.Airplane_Override:
                    AirplaneOverride();
                    break;

                case Resource.Id.Connected_Devices:
                    UsbCheck(Global.UsbManager, Global.UsbDevice);
                    break;

                case Resource.Id.About:
                    _ = Ui.Message("About MSS6x Tool", Ui.About);
                    break;

            }
        }
    }
}