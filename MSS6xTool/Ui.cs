using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Widget;
using Xamarin.Essentials;

namespace MSS6xTool
{
    internal static class Ui
    {
        private static readonly Activity Activity = Global.Activity;

        public static ViewSwitcher ViewSwitcher;
        public static Button IdentifyDme;
        public static Button LoadFile;
        public static Button SaveFile;
        public static Button ReadTune;
        public static Button ReadFull;
        public static Button FlashTune;
        public static Button FlashFull;
        public static GridLayout FunctionStack;
        public static GridLayout VehicleInfo;
        public static TextView StatusTextBlock;
        public static ProgressBar ProgressDme;
        public static TextView ModelBox;
        public static TextView EngineBox;
        public static TextView DmeTypeBox;
        public static TextView VinBox;
        public static TextView HwRefBox;
        public static TextView ZifBox;
        public static TextView SwRefBox;
        public static TextView ProgramStatusBox;

        public static void UiLink()
        {
            ViewSwitcher = Activity.FindViewById<ViewSwitcher>(Resource.Id.ViewSwitcher);
            ProgressDme = Activity.FindViewById<ProgressBar>(Resource.Id.ProgressDME);
            FunctionStack = Activity.FindViewById<GridLayout>(Resource.Id.FunctionStack);
            VehicleInfo = Activity.FindViewById<GridLayout>(Resource.Id.VehicleInfo);
            IdentifyDme = Activity.FindViewById<Button>(Resource.Id.IdentifyDME);
            LoadFile = Activity.FindViewById<Button>(Resource.Id.LoadFile);
            SaveFile = Activity.FindViewById<Button>(Resource.Id.SaveFile);
            ReadTune = Activity.FindViewById<Button>(Resource.Id.ReadTune);
            ReadFull = Activity.FindViewById<Button>(Resource.Id.ReadFull);
            FlashTune = Activity.FindViewById<Button>(Resource.Id.FlashTune);
            FlashFull = Activity.FindViewById<Button>(Resource.Id.FlashFull);
            StatusTextBlock = Activity.FindViewById<TextView>(Resource.Id.statusTextBlock);
            IdentifyDme!.Click += IdentifyDME_Click;
            LoadFile!.Click += LoadFile_Click;
            SaveFile!.Click += SaveFile_Click;
            ReadTune!.Click += ReadTune_Click;
            ReadFull!.Click += ReadFull_Click;
            FlashTune!.Click += FlashTune_Click;
            FlashFull!.Click += FlashFull_Click;
            ModelBox = Activity.FindViewById<TextView>(Resource.Id.Model_Box);
            EngineBox = Activity.FindViewById<TextView>(Resource.Id.Engine_Box);
            DmeTypeBox = Activity.FindViewById<TextView>(Resource.Id.DMEType_Box);
            VinBox = Activity.FindViewById<TextView>(Resource.Id.VIN_Box);
            HwRefBox = Activity.FindViewById<TextView>(Resource.Id.HWRef_Box);
            ZifBox = Activity.FindViewById<TextView>(Resource.Id.ZIF_Box);
            SwRefBox = Activity.FindViewById<TextView>(Resource.Id.SWRef_Box);
            ProgramStatusBox = Activity.FindViewById<TextView>(Resource.Id.programStatus_Box);
        }

        public static void StatusText(string text)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusTextBlock.Text = text;
            });
        }

        public static void ProgressIndeterminate(bool state)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ProgressDme.Indeterminate = state;
            });
        }

        public static void UpdateProgressBar(uint progress)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ProgressDme.Progress = Convert.ToInt32(progress);
            });
        }

        public static void KeepAwake(bool state)
        {
            try
            {
                DeviceDisplay.KeepScreenOn = state;

                if (state)
                {
                    Global.WakeLock.Acquire();
                }
                else
                {
                    Global.WakeLock.Release();
                }
            }
            catch { /* ignored */ }
        }

        public static async Task Logger(string fileName, Exception ex, bool displayMessage = true)
        {
            await Logger(fileName, ex.ToString(), displayMessage);
        }

        public static async Task Logger(string fileName, string text, bool displayMessage = true)
        {
            Directory.CreateDirectory($"{Global.SavePath}Logs");
            await File.WriteAllTextAsync($"{Global.SavePath}Logs/{DateTime.Now.ToString(Global.DateFormat)}-{fileName}.log", text);
            if (displayMessage) await Message("Error", text);
        }

        public static async Task<bool> Message(string title = "", string message = "", string yes = "OK", string no = "Cancel", bool question = false)
        {
            var objDialog = new AlertDialog.Builder(Activity)
                .SetTitle(title)
                ?.SetMessage(message)
                ?.SetCancelable(false)
                ?.Create();
            var result = false;

            await Task.Run(() =>
            {
                var waitHandle = new AutoResetEvent(false);
                objDialog?.SetButton((int)DialogButtonType.Positive, yes, (sender, e) =>
                {
                    result = true;
                    waitHandle.Set();
                });

                if (question)
                {
                    objDialog?.SetButton((int)DialogButtonType.Negative, no, (sender, e) =>
                    {
                        result = false;
                        waitHandle.Set();
                    });
                }

                if (objDialog != null) Activity.RunOnUiThread(objDialog.Show);
                waitHandle.WaitOne();
            });
            return result;
        }

        private static async void IdentifyDME_Click(object sender, EventArgs e)
        {
            UpdateProgressBar(0);
            await MSS6x.IdentifyDme();
        }

        private static async void ReadTune_Click(object sender, EventArgs e)
        {
            if (AdvancedMenu.BatteryCheck(10)) return;
            if (!AdvancedMenu.IsAirplaneMode(Activity)) return;

            try
            {
                MSS6x.Transferring(true);
                await MSS6x.ReadTune();
                MSS6x.Transferring(false);
            }
            catch (Exception ex)
            {
                MSS6x.Transferring(false);
                await Logger("ReadTune", ex);
            }
        }

        private static async void ReadFull_Click(object sender, EventArgs e)
        {
            if (AdvancedMenu.BatteryCheck(15)) return;
            if (!AdvancedMenu.IsAirplaneMode(Activity)) return;

            try
            {
                MSS6x.Transferring(true);
                await MSS6x.ReadFull(true);
                MSS6x.Transferring(false);
            }
            catch (Exception ex)
            {
                MSS6x.Transferring(false);
                await Logger("ReadFull", ex);
            }
        }

        private static async void LoadFile_Click(object sender, EventArgs e)
        {
            UpdateProgressBar(0);
            await MSS6x.LoadFile();
        }

        private static void SaveFile_Click(object sender, EventArgs e)
        {
            Tweaks.SaveBinary();
        }

        private static async void FlashFull_Click(object sender, EventArgs e)
        {
            if (AdvancedMenu.BatteryCheck(20)) return;
            if (!AdvancedMenu.IsAirplaneMode(Activity)) return;

            Tweaks.TweakChanges();

            try
            {
                MSS6x.Transferring(true);
                await MSS6x.FlashFull(Global.BinaryFile, false);
                MSS6x.Transferring(false);
            }
            catch (Exception ex)
            {
                MSS6x.Transferring(false);
                await Logger("FlashFull", ex);
            }
        }

        private static async void FlashTune_Click(object sender, EventArgs e)
        {
            if (AdvancedMenu.BatteryCheck(10)) return;
            if (!AdvancedMenu.IsAirplaneMode(Activity)) return;

            Tweaks.TweakChanges();

            byte[] tune;
            if (Global.FullBinaryLoaded || Global.BinaryFile.Length > 0x20000)
            {
                tune = Global.BinaryFile.Skip(0x70000).Take(0x10000).Concat(Global.BinaryFile.Skip(0x2F0000).Take(0x10000)).ToArray();
            }
            else
            {
                tune = Global.BinaryFile;
            }

            try
            {
                MSS6x.Transferring(true);
                await MSS6x.FlashTune(tune, false);
                MSS6x.Transferring(false);
            }
            catch (Exception ex)
            {
                MSS6x.Transferring(false);
                await Logger("FlashTune", ex);
            }
        }

        public static async void ViewSwitch()
        {
            if (Tweaks.TweaksEnabled && !Global.IncompatibleZif && !Global.IsFlashing)
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ViewSwitcher.ShowNext();
                });
            else if (Global.BinaryFile == null) await Message("Error", "Please load a file first.");
            else if (Global.IncompatibleZif) await Message("Wrong Version", "Please update the vehicle's software.\n\n" +
                                                                               "M3: 241E\n" +
                                                                               "M5/M6: 160E\n");
        }

        public const string Disclaimer = "Make sure your car is on a battery charger and that airplane mode is turned on.\n\n" +
                                            "Do not minimize or switch apps while flashing.\n\n" +
                                            "The creator of this software is not responsible for any damages as a result of its use.\n\n" +
                                            "Use at your own risk.";

        public const string About = "Author: Matthew Wilding\n" +
                                    "Email: dev@mattwilding.com";
    }
}
