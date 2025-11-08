using System.IO;
using Android.App;
using Android.Hardware.Usb;
using Android.OS;
using Environment = System.Environment;

// ReSharper disable IdentifierTypo

namespace MSS6xTool
{
    internal static class Global
    {
        public static byte[] BinaryFile = null;
        public static bool BatteryOverride;
        public static bool AirplaneOverride;
        public static bool IsFlashing;
        public static bool SuccessfulIdentify;
        public static bool IncompatibleZif;
        public static bool FullBinaryLoaded;
        public static string Vin;
        public static string HwRef;
        public static string Zif;
        public static string FileName;
        public const string DateFormat = "yyyy-MM-dd_HHmm_ss";
        public const string SgbdReading = "ms_s65.prg";
        public const string SgbdFlashing = "10flash.prg";
        public const string SavePath = @"/storage/emulated/0/Download/MSS6x/";
        public static readonly string EcuPath = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData) + @"/ecu/";

        public static Activity Activity;
        public static UsbManager UsbManager;
        public static UsbDevice UsbDevice;
        public static PowerManager.WakeLock WakeLock;
    }
}