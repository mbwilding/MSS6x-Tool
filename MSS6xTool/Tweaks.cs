using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Android.App;
using Android.Views;
using Android.Widget;
using Xamarin.Essentials;

// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo

namespace MSS6xTool
{
    internal class Tweaks
    {
        private static readonly Activity Activity = Global.Activity;

        public static bool TweaksEnabled;

        public enum ECarType
        {
            None = 0,
            S85 = 1,
            S65 = 2
        }

        public static ECarType CarType = ECarType.None;

        // Cold Start
        public static Switch ColdStartSwitch;
        private static bool coldStartOriginal;

        // Speed Limit
        public static Switch SpeedLimitSwitch;
        private static bool speedLimitOriginal;

        // Primary Cat Delete
        public static Switch PrimaryCatSwitch;
        private static bool primaryCatOriginal;

        // SAP
        public static Switch SapSwitch;
        private static bool sapOriginal;

        // Pops Bangs
        public static Switch PopsSwitch;
        private static bool popsOriginal;

        // Post Cat O2
        public static Switch O2Switch;
        private static bool o2Original;

        // S85 Tune
        public static Switch S85TuneSwitch;
        private static bool s85TuneOriginal;

        // Idle RPM
        public static SeekBar IdleRpmSeek;
        public static TextView IdleRpmText;
        public static int IdleRpm;
        private static int idleRpmOriginal;
        private static int idleRpmOffset;
        private static readonly int IdleRpmOffsetCount = 3;

        // Neutral RPM Limit
        public static SeekBar NeutralRpmSeek;
        public static TextView NeutralRpmText;
        public static int NeutralRpmLimit;
        private static int neutralRpmLimitOriginal;
        private static int neutralRpmLimitOffset;

        // Drive RPM Limit
        public static SeekBar DriveRpmSeek;
        public static TextView DriveRpmText;
        public static int DriveRpmLimit;
        private static int driveRpmLimitOriginal;
        private static int driveRpmLimitOffset1;
        private static int driveRpmLimitOffset2;
        private static readonly int DriveRpmLimitOffset1Count = 7;

        // Throttle Response
        public static Spinner TrSportSpinner;
        public static Spinner TrNormalSpinner;
        public static Spinner TrComfortSpinner;
        public static TextView TrSportText;
        public static TextView TrNormalText;
        public static TextView TrComfortText;
        public static int TrSport;
        public static int TrNormal;
        public static int TrComfort;
        public static int TrSportOriginal;
        public static int TrNormalOriginal;
        public static int TrComfortOriginal;
        public static string TrSportReplace;
        public static string TrNormalReplace;
        public static string TrComfortReplace;
        private static int trComfortOffset;
        private static int trNormalOffset;
        private static int trSportOffset;
        public const string TrComfortStock = "0000001C004500D9020E03E8" +
                                             "0000001D004600DA021803E8" +
                                             "0000001E004700DB021903E8" +
                                             "00000020004A00DC022103E8" +
                                             "00000028005A00DB021803E8" +
                                             "00000029006700BB01CF03E8";

        public const string TrNormalStock =  "0000001C005B0103025E03E8" +
                                             "0000001D005D0105025F03E8" +
                                             "0000001E0060010A026203E8" +
                                             "0000002400680110026703E8" +
                                             "0000002C00780112025E03E8" +
                                             "0000002D008500F2021503E8";

        public const string TrSportStock =   "000000300083014902B803E8" +
                                             "000000310085014B02B903E8" +
                                             "000000320088015002BC03E8" +
                                             "000000380090015602C103E8" +
                                             "0000004000A0015802B803E8" +
                                             "0000004100AD0138026F03E8";

        public const string TrSuperSport =   "0000003C00D101AC02E703E8" +
                                             "0000003D00D401AE02E803E8" +
                                             "0000003E00D701B402EB03E8" +
                                             "0000004400E001BB02F003E8" +
                                             "0000004C00EA01AB02E703E8" +
                                             "0000004D00F70189029A03E8";

        public const string TrLinear =       "000000320096012C025803E8" +
                                             "000000320096012C025803E8" +
                                             "000000320096012C025803E8" +
                                             "000000320096012C025803E8" +
                                             "000000320096012C025803E8" +
                                             "000000320096012C025803E8";

        public static bool OptionCheck(Dictionary<int, string> check, int position = 0)
        {
            int newKey = check.Keys.ElementAt(position);
            if (Global.FullBinaryLoaded) return Conversions.OffsetHexCompare(newKey, check.Values.ElementAt(position));
            if (newKey >= 0x2F0000 && newKey < 0x300000)
            {
                newKey -= 0x2E0000;
            }
            else if (newKey >= 0x70000 && newKey < 0x80000)
            {
                newKey -= 0x70000;
            }
            return Conversions.OffsetHexCompare(newKey, check.Values.ElementAt(position));
        }

        public static void TweaksCheck(string zif)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                switch (CarType)
                {
                    case ECarType.S85:

                        if (zif != "160E")
                        {
                            Fail();
                            return;
                        }

                        TweaksEnabled = true;
                        Global.IncompatibleZif = false;
                        Ui.SaveFile.Enabled = true;

                        PopsSwitch.Visibility = ViewStates.Visible;
                        IdleRpmText.Visibility = ViewStates.Visible;
                        IdleRpmSeek.Visibility = ViewStates.Visible;
                        TrSportText.Visibility = ViewStates.Visible;
                        TrSportSpinner.Visibility = ViewStates.Visible;
                        TrNormalText.Visibility = ViewStates.Visible;
                        TrNormalSpinner.Visibility = ViewStates.Visible;
                        TrComfortText.Visibility = ViewStates.Visible;
                        TrComfortSpinner.Visibility = ViewStates.Visible;
                        S85TuneSwitch.Visibility = ViewStates.Visible;
                        O2Switch.Visibility = ViewStates.Gone;

                        // Cold Start
                        ColdStartSwitch.Checked = coldStartOriginal = OptionCheck(S85ColdStartModified);

                        // Speed Limit
                        SpeedLimitSwitch.Checked = speedLimitOriginal = OptionCheck(S85SpeedLimitModified);

                        // Primary Cat
                        PrimaryCatSwitch.Checked = primaryCatOriginal = OptionCheck(S85PrimaryCatModified);

                        // SAP
                        SapSwitch.Checked = sapOriginal = OptionCheck(S85SapModified);

                        // Pops Bangs
                        PopsSwitch.Checked = popsOriginal = OptionCheck(S85PopsModified);

                        // Tune
                        S85TuneSwitch.Checked = s85TuneOriginal = OptionCheck(S85Stage1Plus, 5);

                        // Idle RPM
                        if (Global.FullBinaryLoaded)
                        {
                            idleRpmOffset = 0x2F49EC;
                        }
                        else
                        {
                            idleRpmOffset = 0x2F49EC - 0x2E0000;
                        }
                        IdleRpm = idleRpmOriginal = Conversions.OffsetToInt(idleRpmOffset + 4);
                        IdleRpmSeek.Progress = IdleRpm;
                        IdleRpmText.Text = "Idle RPM: " + IdleRpm;

                        // Neutral RPM Limit
                        if (Global.FullBinaryLoaded)
                        {
                            neutralRpmLimitOffset = 0x74C9A;
                        }
                        else
                        {
                            neutralRpmLimitOffset = 0x74C9A - 0x70000;
                        }
                        NeutralRpmLimit = neutralRpmLimitOriginal = Conversions.OffsetToInt(neutralRpmLimitOffset);
                        NeutralRpmSeek.Progress = NeutralRpmLimit;
                        NeutralRpmText.Text = "Neutral RPM Limit: " + NeutralRpmLimit;

                        // Drive RPM Limit
                        if (Global.FullBinaryLoaded)
                        {
                            driveRpmLimitOffset1 = 0x74C9C;
                            driveRpmLimitOffset2 = 0x74CD6;
                        }
                        else
                        {
                            driveRpmLimitOffset1 = 0x74C9C - 0x70000;
                            driveRpmLimitOffset2 = 0x74CD6 - 0x70000;
                        }
                        DriveRpmLimit = driveRpmLimitOriginal = Conversions.OffsetToInt(driveRpmLimitOffset2);
                        DriveRpmSeek.Progress = DriveRpmLimit;
                        DriveRpmText.Text = "Drive RPM Limit: " + DriveRpmLimit;

                        if (Global.FullBinaryLoaded)
                        {
                            trComfortOffset = 0x2FA88A;
                            trNormalOffset = 0x2FA8EC;
                            trSportOffset = 0x2FA94E;
                        }
                        else
                        {
                            trComfortOffset = 0x2FA88A - 0x2E0000;
                            trNormalOffset = 0x2FA8EC - 0x2E0000;
                            trSportOffset = 0x2FA94E - 0x2E0000;
                        }

                        // TR Sport
                        switch (Conversions.OffsetToHex(trSportOffset, TrSportStock.Length, true))
                        {
                            case TrComfortStock:
                                TrSportSpinner.SetSelection(0);
                                TrSport = TrSportOriginal = 0;
                                break;
                            case TrNormalStock:
                                TrSportSpinner.SetSelection(1);
                                TrSport = TrSportOriginal = 1;
                                break;
                            case TrSportStock:
                                TrSportSpinner.SetSelection(2);
                                TrSport = TrSportOriginal = 2;
                                break;
                            case TrSuperSport:
                                TrSportSpinner.SetSelection(3);
                                TrSport = TrSportOriginal = 3;
                                break;
                            case TrLinear:
                                TrSportSpinner.SetSelection(4);
                                TrSport = TrSportOriginal = 4;
                                break;
                            default:
                                TrSportSpinner.SetSelection(2);
                                TrSport = TrSportOriginal = 2;
                                break;
                        }

                        // TR Normal
                        switch (Conversions.OffsetToHex(trNormalOffset, TrNormalStock.Length, true))
                        {
                            case TrComfortStock:
                                TrNormalSpinner.SetSelection(0);
                                TrNormal = TrNormalOriginal = 0;
                                break;
                            case TrNormalStock:
                                TrNormalSpinner.SetSelection(1);
                                TrNormal = TrNormalOriginal = 1;
                                break;
                            case TrSportStock:
                                TrNormalSpinner.SetSelection(2);
                                TrNormal = TrNormalOriginal = 2;
                                break;
                            case TrSuperSport:
                                TrNormalSpinner.SetSelection(3);
                                TrNormal = TrNormalOriginal = 3;
                                break;
                            case TrLinear:
                                TrNormalSpinner.SetSelection(4);
                                TrNormal = TrNormalOriginal = 4;
                                break;
                            default:
                                TrNormalSpinner.SetSelection(1);
                                TrNormal = TrNormalOriginal = 1;
                                break;
                        }

                        // TR Comfort
                        switch (Conversions.OffsetToHex(trComfortOffset, TrComfortStock.Length, true))
                        {
                            case TrComfortStock:
                                TrComfortSpinner.SetSelection(0);
                                TrComfort = TrComfortOriginal = 0;
                                break;
                            case TrNormalStock:
                                TrComfortSpinner.SetSelection(1);
                                TrComfort = TrComfortOriginal = 1;
                                break;
                            case TrSportStock:
                                TrComfortSpinner.SetSelection(2);
                                TrComfort = TrComfortOriginal = 2;
                                break;
                            case TrSuperSport:
                                TrComfortSpinner.SetSelection(3);
                                TrComfort = TrComfortOriginal = 3;
                                break;
                            case TrLinear:
                                TrComfortSpinner.SetSelection(4);
                                TrComfort = TrComfortOriginal = 4;
                                break;
                            default:
                                TrComfortSpinner.SetSelection(0);
                                TrComfort = TrComfortOriginal = 0;
                                break;
                        }

                        break;

                    case ECarType.S65:

                        if (zif != "241E")
                        {
                            Fail();
                            return;
                        }

                        TweaksEnabled = true;
                        Ui.SaveFile.Enabled = true;
                        Global.IncompatibleZif = false;

                        PopsSwitch.Visibility = ViewStates.Gone;
                        IdleRpmText.Visibility = ViewStates.Gone;
                        IdleRpmSeek.Visibility = ViewStates.Gone;
                        TrSportText.Visibility = ViewStates.Gone;
                        TrSportSpinner.Visibility = ViewStates.Gone;
                        TrNormalText.Visibility = ViewStates.Gone;
                        TrNormalSpinner.Visibility = ViewStates.Gone;
                        TrComfortText.Visibility = ViewStates.Gone;
                        TrComfortSpinner.Visibility = ViewStates.Gone;
                        S85TuneSwitch.Visibility = ViewStates.Gone;
                        O2Switch.Visibility = ViewStates.Visible;

                        // Cold Start
                        ColdStartSwitch.Checked = coldStartOriginal = OptionCheck(S65ColdStartModified);

                        // Speed Limit
                        SpeedLimitSwitch.Checked = speedLimitOriginal = OptionCheck(S65SpeedLimitModified);

                        // Primary Cat
                        PrimaryCatSwitch.Checked = primaryCatOriginal = OptionCheck(S65PrimaryCatModified);

                        // SAP
                        SapSwitch.Checked = sapOriginal = OptionCheck(S65SapModified);

                        // Post Cat O2
                        O2Switch.Checked = o2Original = OptionCheck(S65PostCatO2Modified);

                        // Neutral RPM Limit
                        if (Global.FullBinaryLoaded)
                        {
                            neutralRpmLimitOffset = 0x76062;
                        }
                        else
                        {
                            neutralRpmLimitOffset = 0x76062 - 0x70000;
                        }

                        NeutralRpmLimit = neutralRpmLimitOriginal = Conversions.OffsetToInt(neutralRpmLimitOffset);
                        NeutralRpmSeek.Progress = NeutralRpmLimit;
                        NeutralRpmText.Text = "Neutral RPM Limit: " + NeutralRpmLimit;

                        // Drive RPM Limit
                        if (Global.FullBinaryLoaded)
                        {
                            driveRpmLimitOffset1 = 0x76064;
                            driveRpmLimitOffset2 = 0x7609E;
                        }
                        else
                        {
                            driveRpmLimitOffset1 = 0x76064 - 0x70000;
                            driveRpmLimitOffset2 = 0x7609E - 0x70000;
                        }
                        DriveRpmLimit = driveRpmLimitOriginal = Conversions.OffsetToInt(driveRpmLimitOffset2);
                        DriveRpmSeek.Progress = DriveRpmLimit;
                        DriveRpmText.Text = "Drive RPM Limit: " + DriveRpmLimit;

                        break;

                    default:
                        Fail();
                        break;
                }
            });
        }

        public static void Fail()
        {
            TweaksEnabled = false;
            Ui.SaveFile.Enabled = false;
            Global.IncompatibleZif = true;
        }

        public static void TweakChanges()
        {
            switch (CarType)
            {
                case ECarType.S85:
                {
                    // Tune
                    if (S85TuneSwitch.Checked != s85TuneOriginal)
                    {
                        Patch(S85TuneSwitch.Checked ? S85Stage1Plus : S85StockFromStage1Plus);
                    }

                    // Cold Start
                    if (ColdStartSwitch.Checked != coldStartOriginal)
                    {
                        Patch(ColdStartSwitch.Checked ? S85ColdStartModified : S85ColdStartOriginal);
                    }

                    // Speed Limit
                    if (SpeedLimitSwitch.Checked != speedLimitOriginal)
                    {
                        Patch(SpeedLimitSwitch.Checked ? S85SpeedLimitModified : S85SpeedLimitOriginal);
                    }

                    // Primary Cat
                    if (PrimaryCatSwitch.Checked != primaryCatOriginal)
                    {
                        Patch(PrimaryCatSwitch.Checked ? S85PrimaryCatModified : S85PrimaryCatOriginal);
                    }

                    // SAP
                    if (SapSwitch.Checked != sapOriginal)
                    {
                        Patch(SapSwitch.Checked ? S85SapModified : S85SapOriginal);
                    }

                    // Pops Bangs
                    if (PopsSwitch.Checked != popsOriginal)
                    {
                        Patch(PopsSwitch.Checked ? S85PopsModified : S85PopsOriginal);
                    }

                    // Idle RPM
                    if (IdleRpm != idleRpmOriginal)
                    {
                        string idleRpmLimitString = Conversions.IntToHexPadded(IdleRpmSeek.Progress);
                        Conversions.StringReplaceSequential(idleRpmOffset, idleRpmLimitString, IdleRpmOffsetCount);
                    }

                    // Neutral RPM Limit
                    if (NeutralRpmLimit != neutralRpmLimitOriginal)
                    {
                        string neutralRpmLimitString = Conversions.IntToHexPadded(NeutralRpmSeek.Progress);
                        Conversions.ByteReplace(neutralRpmLimitOffset, neutralRpmLimitString);
                    }

                    // Drive RPM Limit
                    if (DriveRpmLimit != driveRpmLimitOriginal)
                    {
                        string driveRpmLimitString = Conversions.IntToHexPadded(DriveRpmSeek.Progress);
                        Conversions.StringReplaceSequential(driveRpmLimitOffset1, driveRpmLimitString,
                            DriveRpmLimitOffset1Count);
                        Conversions.ByteReplace(driveRpmLimitOffset2, driveRpmLimitString);
                    }

                    // TR
                    if (TrSport != TrSportOriginal)
                    {
                        Conversions.ByteReplace(trSportOffset, TrSportReplace);
                    }

                    if (TrNormal != TrNormalOriginal)
                    {
                        Conversions.ByteReplace(trNormalOffset, TrNormalReplace);
                    }

                    if (TrComfort != TrComfortOriginal)
                    {
                        Conversions.ByteReplace(trComfortOffset, TrComfortReplace);
                    }

                    break;
                }

                case ECarType.S65:
                {
                    // Cold Start
                    if (ColdStartSwitch.Checked != coldStartOriginal)
                    {
                        Patch(ColdStartSwitch.Checked ? S65ColdStartModified : S65ColdStartOriginal);
                    }

                    // Speed Limit
                    if (SpeedLimitSwitch.Checked != speedLimitOriginal)
                    {
                        Patch(SpeedLimitSwitch.Checked ? S65SpeedLimitModified : S65SpeedLimitOriginal);
                    }

                    // Primary Cat
                    if (PrimaryCatSwitch.Checked != primaryCatOriginal)
                    {
                        Patch(PrimaryCatSwitch.Checked ? S65PrimaryCatModified : S65PrimaryCatOriginal);
                    }

                    // SAP
                    if (SapSwitch.Checked != sapOriginal)
                    {
                        Patch(SapSwitch.Checked ? S65SapModified : S65SapOriginal);
                    }

                    // Post Cat O2
                    if (O2Switch.Checked != o2Original)
                    {
                        Patch(O2Switch.Checked ? S65PostCatO2Modified : S65PostCatO2Original);
                    }

                    // Neutral RPM Limit
                    if (NeutralRpmLimit != neutralRpmLimitOriginal)
                    {
                        string neutralRpmLimitString = Conversions.IntToHexPadded(NeutralRpmSeek.Progress);
                        Conversions.ByteReplace(neutralRpmLimitOffset, neutralRpmLimitString);
                    }

                    // Drive RPM Limit
                    if (DriveRpmLimit != driveRpmLimitOriginal)
                    {
                        string driveRpmLimitString = Conversions.IntToHexPadded(DriveRpmSeek.Progress);
                        Conversions.StringReplaceSequential(driveRpmLimitOffset1, driveRpmLimitString, DriveRpmLimitOffset1Count);
                        Conversions.ByteReplace(driveRpmLimitOffset2, driveRpmLimitString);
                    }
                    break;
                }
            }
        }

        private Regex _dateRegEx = new("^.*-([0-9]+(-[0-9]+)+):([0-9]+(-[0-9]+)+)$", RegexOptions.IgnoreCase);
        public static async void SaveBinary()
        {
            TweakChanges();

            var binary = Global.BinaryFile;
            if (!Global.FullBinaryLoaded)
            {
                var vinBytes = Conversions.AsciiToBytes(Global.Vin);
                binary = Global.BinaryFile.Concat(vinBytes).ToArray();
            }
            string filename = Regex.Replace(Global.FileName,
                "_[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]_[0-9][0-9][0-9][0-9]_[0-9][0-9]",
                string.Empty);

            string newFilename = $"{filename}_{DateTime.Now.ToString(Global.DateFormat)}";

            var fullPath = $"{Global.SavePath}{newFilename}.bin";
            await File.WriteAllBytesAsync(fullPath, binary);
            await Ui.Message("Binary Saved", "Modifications have been saved");
            Ui.StatusTextBlock.Text = $"Modified file saved to:\nDownload/MSS6x/{newFilename}";
        }

        private static void Patch(Dictionary<int, string> payloads)
        {
            if (Global.FullBinaryLoaded)
            {
                foreach (var (key, value) in payloads)
                {
                    Conversions.ByteReplace(key, value);
                }
            }
            else
            {
                foreach (var (key, value) in payloads)
                {
                    int newKey = key;
                    if (newKey >= 0x2F0000 && newKey < 0x300000)
                    {
                        newKey -= 0x2E0000;
                        Conversions.ByteReplace(newKey, value);
                    }
                    else if (newKey >= 0x70000 && newKey < 0x80000)
                    {
                        newKey -= 0x70000;
                        Conversions.ByteReplace(newKey, value);
                    }
                }
            }
        }

        public static void UiLink()
        {
            O2Switch = Activity.FindViewById<Switch>(Resource.Id.o2Switch);
            TrSportText = Activity.FindViewById<TextView>(Resource.Id.trSportText);
            TrNormalText = Activity.FindViewById<TextView>(Resource.Id.trNormalText);
            TrComfortText = Activity.FindViewById<TextView>(Resource.Id.trComfortText);

            // Idle RPM
            IdleRpmSeek = IdleRpmSeek = Activity.FindViewById<SeekBar>(Resource.Id.idleRpmSeek);
            IdleRpmText = IdleRpmText = Activity.FindViewById<TextView>(Resource.Id.idleRpmText);
            IdleRpmSeek!.ProgressChanged += (sender, e) =>
            {
                if (!e.FromUser) return;

                const int stepSize = 10;
                var temp = e.Progress / stepSize * stepSize;
                IdleRpmSeek.Progress = IdleRpm = temp;

                IdleRpmText.Text = $"Idle RPM: {temp}";
            };

            // Neutral RPM Limit
            NeutralRpmSeek = NeutralRpmSeek = Activity.FindViewById<SeekBar>(Resource.Id.neutralRpmSeek);
            NeutralRpmText = NeutralRpmText = Activity.FindViewById<TextView>(Resource.Id.neutralRpmText);
            NeutralRpmSeek!.ProgressChanged += (sender, e) =>
            {
                if (!e.FromUser) return;

                const int stepSize = 50;
                var temp = e.Progress / stepSize * stepSize;
                NeutralRpmSeek.Progress = NeutralRpmLimit = temp;

                NeutralRpmText.Text = $"Neutral RPM Limit: {temp}";
            };

            // Drive RPM Limit
            DriveRpmSeek = DriveRpmSeek = Activity.FindViewById<SeekBar>(Resource.Id.driveRpmSeek);
            DriveRpmText = DriveRpmText = Activity.FindViewById<TextView>(Resource.Id.driveRpmText);
            DriveRpmSeek!.ProgressChanged += (sender, e) =>
            {
                if (!e.FromUser) return;

                const int stepSize = 50;
                var temp = e.Progress / stepSize * stepSize;
                DriveRpmSeek.Progress = DriveRpmLimit = temp;

                DriveRpmText.Text = $"Drive RPM Limit: {temp}";
            };

            // TR Spinners
            TrSportSpinner = Activity.FindViewById<Spinner>(Resource.Id.trSportSpinner);
            TrSportSpinner!.ItemSelected += TrSport_ItemSelected;
            var trSportAdapter = ArrayAdapter.CreateFromResource(Activity, Resource.Array.ThrottleResponseArray, Android.Resource.Layout.SimpleSpinnerItem);
            trSportAdapter.SetDropDownViewResource(Resource.Layout.support_simple_spinner_dropdown_item);
            TrSportSpinner.Adapter = trSportAdapter;

            TrNormalSpinner = Activity.FindViewById<Spinner>(Resource.Id.trNormalSpinner);
            TrNormalSpinner!.ItemSelected += TrNormal_ItemSelected;
            var trNormalAdapter = ArrayAdapter.CreateFromResource(Activity, Resource.Array.ThrottleResponseArray, Android.Resource.Layout.SimpleSpinnerItem);
            trNormalAdapter.SetDropDownViewResource(Resource.Layout.support_simple_spinner_dropdown_item);
            TrNormalSpinner.Adapter = trNormalAdapter;

            TrComfortSpinner = Activity.FindViewById<Spinner>(Resource.Id.trComfortSpinner);
            TrComfortSpinner!.ItemSelected += TrComfort_ItemSelected;
            var trComfortAdapter = ArrayAdapter.CreateFromResource(Activity, Resource.Array.ThrottleResponseArray, Android.Resource.Layout.SimpleSpinnerItem);
            trComfortAdapter.SetDropDownViewResource(Resource.Layout.support_simple_spinner_dropdown_item);
            TrComfortSpinner.Adapter = trComfortAdapter;

            var coldStartSwitch = Activity.FindViewById<Switch>(Resource.Id.coldStartSwitch);
            ColdStartSwitch = coldStartSwitch;
            coldStartSwitch!.Checked = ColdStartSwitch.Checked;
            coldStartSwitch.CheckedChange += delegate (object sender, CompoundButton.CheckedChangeEventArgs e)
            {
                ColdStartSwitch.Checked = e.IsChecked;
            };

            // ReSharper disable once InconsistentNaming
            var speedLimitSwitch = Activity.FindViewById<Switch>(Resource.Id.speedLimitSwitch);
            SpeedLimitSwitch = speedLimitSwitch;
            speedLimitSwitch!.Checked = SpeedLimitSwitch.Checked;
            speedLimitSwitch.CheckedChange += delegate (object sender, CompoundButton.CheckedChangeEventArgs e)
            {
                SpeedLimitSwitch.Checked = e.IsChecked;
            };

            var primaryCatSwitch = Activity.FindViewById<Switch>(Resource.Id.primaryCatSwitch);
            PrimaryCatSwitch = primaryCatSwitch;
            primaryCatSwitch!.Checked = PrimaryCatSwitch.Checked;
            primaryCatSwitch.CheckedChange += delegate (object sender, CompoundButton.CheckedChangeEventArgs e)
            {
                PrimaryCatSwitch.Checked = e.IsChecked;
            };

            var sapSwitch = Activity.FindViewById<Switch>(Resource.Id.sapSwitch);
            SapSwitch = sapSwitch;
            sapSwitch!.Checked = SapSwitch.Checked;
            sapSwitch.CheckedChange += delegate (object sender, CompoundButton.CheckedChangeEventArgs e)
            {
                SapSwitch.Checked = e.IsChecked;
            };

            var popsSwitch = Activity.FindViewById<Switch>(Resource.Id.popsSwitch);
            PopsSwitch = popsSwitch;
            popsSwitch!.Checked = PopsSwitch.Checked;
            popsSwitch.CheckedChange += delegate (object sender, CompoundButton.CheckedChangeEventArgs e)
            {
                PopsSwitch.Checked = e.IsChecked;
            };

            var s85TuneSwitch = Activity.FindViewById<Switch>(Resource.Id.s85TuneSwitch);
            S85TuneSwitch = s85TuneSwitch;
            s85TuneSwitch!.Checked = S85TuneSwitch.Checked;
            s85TuneSwitch.CheckedChange += delegate (object sender, CompoundButton.CheckedChangeEventArgs e)
            {
                S85TuneSwitch.Checked = e.IsChecked;
            };
        }

        public static void TrSport_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            TrSportReplace = e.Position switch
            {
                0 => TrComfortStock,
                1 => TrNormalStock,
                2 => TrSportStock,
                3 => TrSuperSport,
                4 => TrLinear,
                _ => TrSportReplace
            };
            TrSport = e.Position;
        }

        public static void TrNormal_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            TrNormalReplace = e.Position switch
            {
                0 => TrComfortStock,
                1 => TrNormalStock,
                2 => TrSportStock,
                3 => TrSuperSport,
                4 => TrLinear,
                _ => TrNormalReplace
            };
            TrNormal = e.Position;
        }

        public static void TrComfort_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            TrComfortReplace = e.Position switch
            {
                0 => TrComfortStock,
                1 => TrNormalStock,
                2 => TrSportStock,
                3 => TrSuperSport,
                4 => TrLinear,
                _ => TrComfortReplace
            };
            TrComfort = e.Position;
        }

        #region S85

        private static readonly Dictionary<int, string> S85ColdStartModified = new()
        {
            {0x2F3C06, "0000"}
        };
        private static readonly Dictionary<int, string> S85ColdStartOriginal = new()
        {
            {0x2F3C06, "1388"}
        };

        private static readonly Dictionary<int, string> S85SpeedLimitModified = new()
        {
            {0x744C1, "68"},
            {0x76D76, "16801680168016801680168016801680"},
            {0x76D98, "16801680168016801680168016801680"},
            {0x78DC6, "16"},
            {0x78DC8, "16"},
            {0x78DCA, "16"},
            {0x78DCC, "16"},
            {0x78DCE, "16"},
            {0x78DD0, "16"},
            {0x78DD2, "16"},
            {0x78DD4, "16"},
            {0x78DE8, "16"},
            {0x78DEA, "16"},
            {0x78DEC, "16"},
            {0x78DEE, "16"},
            {0x78DF0, "16"},
            {0x78DF2, "16"},
            {0x78DF4, "16"},
            {0x78DF6, "16"},
            {0x78E0A, "16"},
            {0x78E0C, "16"},
            {0x78E0E, "16"},
            {0x78E10, "16"},
            {0x78E12, "16"},
            {0x78E14, "16"},
            {0x78E16, "16"},
            {0x78E18, "16"},
            {0x78E2C, "16"},
            {0x78E2E, "16"},
            {0x78E30, "16"},
            {0x78E32, "16"},
            {0x78E34, "16"},
            {0x78E36, "16"},
            {0x78E38, "16"},
            {0x78E3A, "16"},
            {0x78E4E, "16801680168016801680168016801680"}
        };
        private static readonly Dictionary<int, string> S85SpeedLimitOriginal = new()
        {
            {0x744C1, "2C"},
            {0x76D76, "0800091C0AAC0BD80D040DCC0E940F8E"},
            {0x76D98, "0800091C0A700B920C960D7C0E440EF8"},
            {0x78DC6, "12"},
            {0x78DC8, "12"},
            {0x78DCA, "12"},
            {0x78DCC, "12"},
            {0x78DCE, "12"},
            {0x78DD0, "12"},
            {0x78DD2, "12"},
            {0x78DD4, "12"},
            {0x78DE8, "12"},
            {0x78DEA, "12"},
            {0x78DEC, "12"},
            {0x78DEE, "12"},
            {0x78DF0, "12"},
            {0x78DF2, "12"},
            {0x78DF4, "12"},
            {0x78DF6, "12"},
            {0x78E0A, "12"},
            {0x78E0C, "12"},
            {0x78E0E, "12"},
            {0x78E10, "12"},
            {0x78E12, "12"},
            {0x78E14, "12"},
            {0x78E16, "12"},
            {0x78E18, "12"},
            {0x78E2C, "12"},
            {0x78E2E, "12"},
            {0x78E30, "12"},
            {0x78E32, "12"},
            {0x78E34, "12"},
            {0x78E36, "12"},
            {0x78E38, "12"},
            {0x78E3A, "12"},
            {0x78E4E, "0FF00FF00FF00FF00FF00FF00FF00FF0"}
        };
        private static readonly Dictionary<int, string> S85PrimaryCatModified = new()
        {
            {0x7151E, "00000000"},
            {0x71528, "00000000000000"},
            {0x71530, "00"},
            {0x71532, "00000000000000000000"},
            {0x71542, "00000000000000"},
            {0x7154A, "00"},
            {0x7154C, "000000000000"}
        };
        private static readonly Dictionary<int, string> S85PrimaryCatOriginal = new()
        {
            {0x7151E, "27890420"},
            {0x71528, "01010101010102"},
            {0x71530, "58"},
            {0x71532, "5801581E5C55278A0430"},
            {0x71542, "01010101010102"},
            {0x7154A, "58"},
            {0x7154C, "5801581F5C55"}
        };
        private static readonly Dictionary<int, string> S85SapModified = new()
        {
            {0x70CFE, "000000000000"},
            {0x70D08, "000000000000"},
            {0x70D10, "0000000000000000"},
            {0x718FA, "0000"},
            {0x71902, "0000000000000000"},
            {0x7190C, "00000000000000000000"},
            {0x71918, "000000000000000000000000"},
            {0x71926, "00000000000000000000"},
            {0x71932, "000000000000000000000000"},
            {0x71940, "000000000000000000000000000000000000000000000000"},
            {0x7195A, "0000000000000000"},
            {0x71B36, "0000"},
            {0x71B3E, "0000000000000000"},
            {0x71B48, "0000000000000000"}
        };
        private static readonly Dictionary<int, string> S85SapOriginal = new()
        {
            {0x70CFE, "273924332432"},
            {0x70D08, "010101010101"},
            {0x70D10, "5C6F580758045803"},
            {0x718FA, "27AF"},
            {0x71902, "1411010101010101"},
            {0x7190C, "5C715C705802580327B0"},
            {0x71918, "244004912448010101010101"},
            {0x71926, "5C715C785C79581D27B1"},
            {0x71932, "244204922449010101010101"},
            {0x71940, "5C715C785C79581D27B20411140D14181412010101010101"},
            {0x7195A, "5C715C785C79582B"},
            {0x71B36, "27C5"},
            {0x71B3E, "2430010101010101"},
            {0x71B48, "5C6F580758045803"}
        };
        private static readonly Dictionary<int, string> S85PopsModified = new()
        {
            {0x785AC, "0398"},
            {0x785C4, "0398"},
            {0x785DC, "0398"},
            {0x785F4, "0398"},
            {0x7860C, "0398"},
            {0x78624, "0398"},
            {0x7863C, "0398"},
            {0x78654, "0398"},
            {0x7866C, "0398"},
            {0x78684, "0398"},
            {0x2FA24C, "FFCE"},
            {0x2FA258, "FF9C"},
            {0x2FA264, "FF9C"},
            {0x2FA270, "FF9C"},
            {0x2FA27C, "FF9C"},
            {0x2FA288, "FF9C"}
        };
        private static readonly Dictionary<int, string> S85PopsOriginal = new()
        {
            {0x785AC, "0528"},
            {0x785C4, "0528"},
            {0x785DC, "0528"},
            {0x785F4, "0528"},
            {0x7860C, "0528"},
            {0x78624, "0528"},
            {0x7863C, "0528"},
            {0x78654, "0528"},
            {0x7866C, "0528"},
            {0x78684, "0528"},
            {0x2FA24C, "0064"},
            {0x2FA258, "00C8"},
            {0x2FA264, "00FA"},
            {0x2FA270, "012C"},
            {0x2FA27C, "015E"},
            {0x2FA288, "0190"}
        };
        private static readonly Dictionary<int, string> S85Stage1Plus = new()
        {
            {0x12A68, "6000"},
            {0x12A6B, "00"},
            {0x74CB2, "21342134"},
            {0x74CE6, "21341F40"},
            {0x74D00, "21342008"},
            {0x74D1E, "21342134"},
            {0x74D36, "2134"},
            {0x74D3F, "C8"},
            {0x754DE, "03250325"},
            {0x7866B, "2E"},
            {0x78683, "4C"},
            {0x7869B, "42"},
            {0x7880D, "BF"},
            {0x78824, "03EA"},
            {0x7883B, "47"},
            {0x7883D, "1E"},
            {0x78853, "5B"},
            {0x78855, "6D"},
            {0x78C7B, "96012C0258"},
            {0x78C87, "96012C0258"},
            {0x78C93, "96012C0258"},
            {0x78C9F, "96012C0258"},
            {0x78CAB, "96012C0258"},
            {0x79598, "2328"},
            {0x2F039E, "2260"},
            {0x2F1C14, "0B940D500EBE12CD1DEA229D282B356B401A401A401A401A401A401A"},
            {0x2F1C40, "0C130DE310EC144E26763125401A48A64CEC53555355535553555355"},
            {0x2F1C6C, "0C960EB112CD16A6289933483A8B43175355557857D757D757D757D7"},
            {0x2F1C98, "0D42100812CD15CB1BC728992DB9304A378E557857D757D757D757D7"},
            {0x2F1CC4, "0DE310731304157917EE1D0F24532E273192401A57D757D757D757D7"},
            {0x2F1CF0, "0E27107A12CD14F017131C342453267628993BD4557857D757D757D7"},
            {0x2F1D1C, "0E87103D11F213A8155E17811A111D0F24532F024CEC57D757D757D7"},
            {0x2F1D48, "0EF510E112CD1483163818C91CA12230259B2B96378E57D157D757D7"},
            {0x2F1D74, "0FCF11BC13A815B017B71A7E1D0F207A245328992F0239B157D157D7"},
            {0x2F1DA0, "0EF51118133B163619321B591DEA21C226082ABC33483BD457D157D7"},
            {0x2F1DCC, "0F0D118313F816D919BA1BC71E57223026762B2B31573DF757D157D7"},
            {0x2F1DF8, "10DA1305153018D71B591EC52020248228992CB53262401A57D157D7"},
            {0x2F1E24, "1132137F15CC199D1D6D20CC229F277A292B2E273490423D57D157D7"},
            {0x2F34CB, "28"},
            {0x2F34D2, "01320199"},
            {0x2F34D7, "D8"},
            {0x2F3508, "03F503DF03B30392035002D5012F00F300C100A3008F008500850085"},
            {0x2F5D3A, "2112211F214F"},
            {0x2F5D41, "AE"},
            {0x2F5D43, "BB"},
            {0x2F5D45, "E12277"},
            {0x2F5D49, "F32392244A255D271628A428942879283B"},
            {0x2F5FDB, "C3"},
            {0x2F5FDD, "66"},
            {0x2F5FDF, "DA1239"},
            {0x2F5FE3, "AD17101972"},
            {0x2F5FF3, "DE"},
            {0x2F5FF5, "981030"},
            {0x2F5FF9, "9E"},
            {0x2F5FFB, "FF17E9"},
            {0x2F5FFF, "D9"},
            {0x2F600B, "E9"},
            {0x2F600D, "A11038130C157818F61A71"},
            {0x2F6023, "E0"},
            {0x2F6025, "8C1021"},
            {0x2F6029, "FA15D118D91A53"},
            {0x2F603B, "E8"},
            {0x2F603D, "8D1016"},
            {0x2F6041, "EF15CD18D81A52"},
            {0x2F6053, "FA"},
            {0x2F6055, "95101B"},
            {0x2F6059, "F115DD18E91A65"},
            {0x2F606B, "D30D3E"},
            {0x2F606F, "C0124115B318BA1A34"},
            {0x2F6082, "0B19"},
            {0x2F6085, "64"},
            {0x2F6087, "D0125D15B018B61A31"},
            {0x2F609A, "0B22"},
            {0x2F609D, "4E"},
            {0x2F609F, "BA123A159018931A0B"},
            {0x2F60B2, "0B17"},
            {0x2F60B5, "4E"},
            {0x2F60B7, "CD125815A318A91A22"},
            {0x2F60CA, "0B06"},
            {0x2F60CD, "5D"},
            {0x2F60CF, "BE1226159018931A0A"},
            {0x2F60E2, "0B06"},
            {0x2F60E5, "5D"},
            {0x2F60E7, "BE1226159018931A0A"},
            {0x2F6349, "45"},
            {0x2F634B, "370109"},
            {0x2F6351, "A6"},
            {0x2F6353, "9E"},
            {0x2F6355, "91"},
            {0x2F6357, "87"},
            {0x2F6359, "87"},
            {0x2F635B, "8C"},
            {0x2F6361, "7C"},
            {0x2F6363, "5E"},
            {0x2F6365, "2C0104"},
            {0x2F6369, "E6"},
            {0x2F636B, "C4"},
            {0x2F636D, "BE"},
            {0x2F636F, "B4"},
            {0x2F6371, "B4"},
            {0x2F6373, "B4"},
            {0x2F6379, "A4"},
            {0x2F637B, "86"},
            {0x2F637D, "40"},
            {0x2F637F, "36"},
            {0x2F6381, "23"},
            {0x2F6383, "F7"},
            {0x2F6385, "F0"},
            {0x2F6387, "F0"},
            {0x2F6389, "F0"},
            {0x2F638B, "F0"},
            {0x2F6391, "CB"},
            {0x2F6393, "AE"},
            {0x2F6395, "8E"},
            {0x2F6397, "6A"},
            {0x2F6399, "59"},
            {0x2F639B, "2F"},
            {0x2F639D, "2C"},
            {0x2F639F, "2C"},
            {0x2F63A1, "2C"},
            {0x2F63A3, "2C"},
            {0x2F63A9, "CB"},
            {0x2F63AB, "AC"},
            {0x2F63AD, "87"},
            {0x2F63AF, "68"},
            {0x2F63B1, "68"},
            {0x2F63B3, "66"},
            {0x2F63B5, "40"},
            {0x2F63B7, "40"},
            {0x2F63B9, "40"},
            {0x2F63BB, "40"},
            {0x2F63C1, "DC"},
            {0x2F63C3, "CA"},
            {0x2F63C5, "9F"},
            {0x2F63C7, "86"},
            {0x2F63C9, "7C"},
            {0x2F63CB, "54"},
            {0x2F63CD, "2C"},
            {0x2F63CF, "2C"},
            {0x2F63D1, "2C"},
            {0x2F63D3, "2C"},
            {0x2F63D9, "E5"},
            {0x2F63DB, "D7"},
            {0x2F63DD, "AA"},
            {0x2F63DF, "8D"},
            {0x2F63E1, "79"},
            {0x2F63E3, "77"},
            {0x2F63E5, "40"},
            {0x2F63E7, "40"},
            {0x2F63E9, "40"},
            {0x2F63EB, "40"},
            {0x2F63F1, "E5"},
            {0x2F63F3, "E6"},
            {0x2F63F5, "B8"},
            {0x2F63F7, "82"},
            {0x2F63F9, "64"},
            {0x2F63FB, "75"},
            {0x2F63FD, "68"},
            {0x2F63FF, "6B"},
            {0x2F6401, "75"},
            {0x2F6403, "86"},
            {0x2F6409, "FA"},
            {0x2F640B, "E9"},
            {0x2F640D, "AC"},
            {0x2F640F, "68"},
            {0x2F6411, "54"},
            {0x2F6413, "86"},
            {0x2F6415, "81"},
            {0x2F6417, "7C"},
            {0x2F6419, "7C"},
            {0x2F641B, "7C"},
            {0x2F6421, "F4"},
            {0x2F6423, "C8"},
            {0x2F6425, "8C"},
            {0x2F6427, "7C"},
            {0x2F6429, "50"},
            {0x2F642B, "90"},
            {0x2F642D, "90"},
            {0x2F642F, "90"},
            {0x2F6431, "90"},
            {0x2F6433, "90"},
            {0x2F6439, "340208"},
            {0x2F643D, "A1"},
            {0x2F643F, "6B"},
            {0x2F6441, "5C"},
            {0x2F6443, "75"},
            {0x2F6445, "7C"},
            {0x2F6447, "7C"},
            {0x2F6449, "7C"},
            {0x2F644B, "7C"},
            {0x2F6451, "41"},
            {0x2F6453, "2F"},
            {0x2F6455, "AD"},
            {0x2F6457, "78"},
            {0x2F6459, "50"},
            {0x2F645B, "6B"},
            {0x2F645D, "5E"},
            {0x2F645F, "54"},
            {0x2F6461, "4A"},
            {0x2F6463, "4A"},
            {0x2F6469, "58"},
            {0x2F646B, "3A"},
            {0x2F646D, "B8"},
            {0x2F646F, "82"},
            {0x2F6471, "5A"},
            {0x2F6473, "72"},
            {0x2F6475, "6F"},
            {0x2F6477, "4A"},
            {0x2F6479, "40"},
            {0x2F647B, "40"},
            {0x2F6481, "58"},
            {0x2F6483, "3A"},
            {0x2F6485, "CD"},
            {0x2F6487, "89"},
            {0x2F6489, "66"},
            {0x2F648B, "7D"},
            {0x2F648D, "72"},
            {0x2F648F, "54"},
            {0x2F6491, "4A"},
            {0x2F6493, "4A"},
            {0x2F6499, "58"},
            {0x2F649B, "3A"},
            {0x2F649D, "F4"},
            {0x2F649F, "B8"},
            {0x2F64A1, "86"},
            {0x2F64A3, "88"},
            {0x2F64A5, "68"},
            {0x2F64A7, "4A"},
            {0x2F64A9, "4A"},
            {0x2F64AB, "4A"},
            {0x2F64B1, "58"},
            {0x2F64B3, "58"},
            {0x2F64B5, "580208"},
            {0x2F64B9, "B8"},
            {0x2F64BB, "86"},
            {0x2F64BD, "68"},
            {0x2F64BF, "54"},
            {0x2F64C1, "4A"},
            {0x2F64C3, "4A"},
            {0x2F7B1A, "060408480AC60C060CA60CA6060408480AC60DC80ED60F8C060408480AC60EB4128813DC060408480AC610B214DF1633060408480AC6107A14F617C6060408480AC6102A146A1770"},
            {0x2F8D38, "03030405060602030404050502020303040402020203040402020303040503030304060804040405070A05050607090B"},
            {0x2F8D8D, "040506070804040506070804050606080905050607090A060608080A0B060708090B0C0708090A0C"},
            {0x2F8DD4, "02040506060803040506060803040506070803040607080903040708090A050608090A0B0708090A0B0C090A0B0C0D0E"},
            {0x2F8E22, "05070809090A06070809090A0608090A0A0B0708090A0A0B07080A0B0B0C090A0A0B0C0D0A0A0B0C0D0D0B0B0C0D0D"},
            {0x2F8E70, "02030405060602020304050502020304050502020304050502020304040602030405060803040506070A040507090A0B"},
            {0x2F8EBE, "060809090909050708080809050708080909060708090A0A060708090A0B0608090A0B0C090A0B0B0C0C0A0B0B0C0C"},
            {0x2F8F07, "0B090907070B09070705050907070706060807070706060A0908080708"},
            {0x2F8F3D, "0C0B0A0909"},
            {0x2F8F43, "0B0A0908080B0A0A0908080B0A0A0908080B0B0A0A090A"},
            {0x2F8F72, "040507070909030305050709020304040505020304040505030405050707"},
            {0x2F8FA8, "070709090B"},
            {0x2F8FAE, "050507070A0A040506070709030405060708040506070809"},
            {0x2F8FDE, "020202030304020202030303020202030303020203040404030404050505"},
            {0x2F901F, "07"},
            {0x2F9025, "07"},
            {0x2F902B, "07"},
            {0x2F9CD3, "00"},
            {0x2F9CD5, "00"},
            {0x2F9CD7, "00"},
            {0x2F9CD9, "00"},
            {0x2F9CDB, "00"},
            {0x2F9CDD, "00"},
            {0x2F9CDF, "00"},
            {0x2F9CE1, "00"},
            {0x2F9CE5, "00"},
            {0x2F9CE7, "00"},
            {0x2F9CE9, "00"},
            {0x2F9CEB, "00"},
            {0x2F9CED, "00"},
            {0x2F9CEF, "00"},
            {0x2F9CF1, "00"},
            {0x2F9CF3, "00"},
            {0x2F9CF9, "00"},
            {0x2F9CFB, "00"},
            {0x2F9CFD, "00"},
            {0x2F9CFF, "00"},
            {0x2F9D01, "00"},
            {0x2F9D03, "00"},
            {0x2F9D05, "00"},
            {0x2F9D0B, "00"},
            {0x2F9D0D, "00"},
            {0x2F9D0F, "00"},
            {0x2F9D11, "00"},
            {0x2F9D13, "00"},
            {0x2F9D15, "00"},
            {0x2F9D17, "00"},
            {0x2F9D1D, "00"},
            {0x2F9D1F, "00"},
            {0x2F9D21, "00"},
            {0x2F9D23, "00"},
            {0x2F9D25, "00"},
            {0x2F9D27, "00"},
            {0x2F9D29, "00"},
            {0x2F9D2F, "00"},
            {0x2F9D31, "00"},
            {0x2F9D33, "00"},
            {0x2F9D35, "00"},
            {0x2F9D37, "00"},
            {0x2F9D39, "00"},
            {0x2F9D3B, "00"},
            {0x2F9D41, "00"},
            {0x2F9D43, "00"},
            {0x2F9D45, "00"},
            {0x2F9D47, "00"},
            {0x2F9D49, "00"},
            {0x2F9D4B, "00"},
            {0x2F9D4D, "00"},
            {0x2F9DF1, "EC"},
            {0x2F9DF3, "EC"},
            {0x2F9DF5, "E2"},
            {0x2F9DF7, "CE"},
            {0x2F9DF9, "C4"},
            {0x2F9DFB, "C4"},
            {0x2F9DFD, "C4"},
            {0x2F9E95, "50"},
            {0x2F9E97, "50"},
            {0x2F9E9D, "82"},
            {0x2F9E9F, "82"},
            {0x2F9EA5, "BE"},
            {0x2F9EA7, "AA"},
            {0x2F9EAD, "DA"},
            {0x2F9EAF, "C6"},
            {0x2F9EB5, "F1"},
            {0x2F9EB7, "E6"},
            {0x2F9EBD, "F4"},
            {0x2F9EBF, "F0"},
            {0x2F9EC5, "22"},
            {0x2F9EC7, "19"},
            {0x2F9F93, "CB"},
            {0x2F9F95, "AE"},
            {0x2F9F97, "8E"},
            {0x2F9F99, "6A"},
            {0x2F9F9B, "59"},
            {0x2F9F9D, "2F"},
            {0x2F9F9F, "2C"},
            {0x2F9FA1, "2C"},
            {0x2F9FA3, "2C"},
            {0x2F9FA5, "2C"},
            {0x2F9FAB, "CB"},
            {0x2F9FAD, "AC"},
            {0x2F9FAF, "87"},
            {0x2F9FB1, "68"},
            {0x2F9FB3, "68"},
            {0x2F9FB5, "65"},
            {0x2F9FB7, "40"},
            {0x2F9FB9, "40"},
            {0x2F9FBB, "40"},
            {0x2F9FBD, "40"},
            {0x2F9FC3, "DC"},
            {0x2F9FC5, "CA"},
            {0x2F9FC7, "9F"},
            {0x2F9FC9, "86"},
            {0x2F9FCB, "7C"},
            {0x2F9FCD, "54"},
            {0x2F9FCF, "2C"},
            {0x2F9FD1, "2C"},
            {0x2F9FD3, "2C"},
            {0x2F9FD5, "2C"},
            {0x2F9FDB, "E5"},
            {0x2F9FDD, "D7"},
            {0x2F9FDF, "AA"},
            {0x2F9FE1, "8D"},
            {0x2F9FE3, "79"},
            {0x2F9FE5, "77"},
            {0x2F9FE7, "40"},
            {0x2F9FE9, "40"},
            {0x2F9FEB, "40"},
            {0x2F9FED, "40"},
            {0x2F9FF3, "E5"},
            {0x2F9FF5, "E6"},
            {0x2F9FF7, "B8"},
            {0x2F9FF9, "82"},
            {0x2F9FFB, "64"},
            {0x2F9FFD, "60"},
            {0x2F9FFF, "54"},
            {0x2FA001, "57"},
            {0x2FA003, "60"},
            {0x2FA005, "72"},
            {0x2FA00B, "FA"},
            {0x2FA00D, "E9"},
            {0x2FA00F, "AC"},
            {0x2FA011, "68"},
            {0x2FA013, "54"},
            {0x2FA015, "86"},
            {0x2FA017, "81"},
            {0x2FA019, "7C"},
            {0x2FA01B, "7C"},
            {0x2FA01D, "7C"},
            {0x2FA023, "F4"},
            {0x2FA025, "C8"},
            {0x2FA027, "8C"},
            {0x2FA029, "7C"},
            {0x2FA02B, "50"},
            {0x2FA02D, "90"},
            {0x2FA02F, "90"},
            {0x2FA031, "90"},
            {0x2FA033, "90"},
            {0x2FA035, "90"},
            {0x2FA03B, "340208"},
            {0x2FA03F, "A1"},
            {0x2FA041, "6A"},
            {0x2FA043, "5B"},
            {0x2FA045, "75"},
            {0x2FA047, "7C"},
            {0x2FA049, "7C"},
            {0x2FA04B, "7C"},
            {0x2FA04D, "7C"},
            {0x2FA053, "41"},
            {0x2FA055, "2F"},
            {0x2FA057, "AD"},
            {0x2FA059, "78"},
            {0x2FA05B, "50"},
            {0x2FA05D, "6B"},
            {0x2FA05F, "5E"},
            {0x2FA061, "54"},
            {0x2FA063, "4A"},
            {0x2FA065, "4A"},
            {0x2FA06B, "58"},
            {0x2FA06D, "3A"},
            {0x2FA06F, "B8"},
            {0x2FA071, "82"},
            {0x2FA073, "5A"},
            {0x2FA075, "72"},
            {0x2FA077, "6F"},
            {0x2FA079, "4A"},
            {0x2FA07B, "40"},
            {0x2FA07D, "40"},
            {0x2FA083, "58"},
            {0x2FA085, "3A"},
            {0x2FA087, "CD"},
            {0x2FA089, "89"},
            {0x2FA08B, "66"},
            {0x2FA08D, "7D"},
            {0x2FA08F, "72"},
            {0x2FA091, "54"},
            {0x2FA093, "4A"},
            {0x2FA095, "4A"},
            {0x2FA09B, "58"},
            {0x2FA09D, "3A"},
            {0x2FA09F, "F4"},
            {0x2FA0A1, "B8"},
            {0x2FA0A3, "86"},
            {0x2FA0A5, "88"},
            {0x2FA0A7, "68"},
            {0x2FA0A9, "4A"},
            {0x2FA0AB, "4A"},
            {0x2FA0AD, "4A"},
            {0x2FA0B3, "58"},
            {0x2FA0B5, "58"},
            {0x2FA0B7, "580208"},
            {0x2FA0BB, "B8"},
            {0x2FA0BD, "86"},
            {0x2FA0BF, "68"},
            {0x2FA0C1, "54"},
            {0x2FA0C3, "4A"},
            {0x2FA0C5, "4A"},
            {0x2FA0F5, "5F"},
            {0x2FA0F7, "5A"},
            {0x2FA0FB, "5F"},
            {0x2FA0FD, "5A"},
            {0x2FA101, "C3"},
            {0x2FA103, "BE"},
            {0x2FA109, "E6"},
            {0x2FA10F, "22"},
            {0x2FA113, "59"},
            {0x2FA115, "4A"},
            {0x2FA119, "59"},
            {0x2FA11B, "4A"},
            {0x2FA11F, "59"},
            {0x2FA121, "54"},
            {0x2FA127, "7C"},
            {0x2FA12B, "95"},
            {0x2FA12D, "7C"},
            {0x2FA131, "95"},
            {0x2FA133, "7C"},
            {0x2FA137, "95"},
            {0x2FA139, "90"},
            {0x2FA13D, "81"},
            {0x2FA13F, "72"},
            {0x2FA143, "6D"},
            {0x2FA145, "5E"},
            {0x2FA149, "54"},
            {0x2FA14B, "4A"},
            {0x2FA14F, "4A"},
            {0x2FA151, "40"},
            {0x2FA153, "45"},
            {0x2FA155, "45"},
            {0x2FA157, "40"},
            {0x2FA159, "45"},
            {0x2FA15B, "45"},
            {0x2FA15D, "36"},
            {0x2FA55E, "7FFF7FFF"},
            {0x2FA564, "7FFF7FFF7FFF7FFF"},
            {0x2FA572, "1B5805FF00"},
            {0x2FA582, "251C"},
            {0x2FA5A0, "21F6233823D725BF26E42871297D29F0"},
            {0x2FA654, "140C1AD31FD6"},
            {0x2FA668, "14191CCA2271"},
            {0x2FA67C, "15652011260C"},
            {0x2FA690, "155F1FE02650"},
            {0x2FA6A4, "154B1FE026DA"},
            {0x2FA6B8, "154B1FE62695"},
            {0x2FA808, "08980B040E241130134C146408980B040E241130134C146408980B040E241130134C146408980B040E241130134C146408980B040E241130134C146408980B040E241130134C1464"},
            {0x2FA996, "2134"},
            {0x2FA9D2, "1540"}

        };
        private static readonly Dictionary<int, string> S85StockFromStage1Plus = new()
        {
            {0x12A68, "4081"},
            {0x12A6B, "20"},
            {0x74CB2, "203A1B58"},
            {0x74CE6, "203A1E78"},
            {0x74D00, "203A1E78"},
            {0x74D1E, "203A1F40"},
            {0x74D36, "1F40"},
            {0x74D3F, "8C"},
            {0x754DE, "09C409C4"},
            {0x7866B, "38"},
            {0x78683, "56"},
            {0x7869B, "4C"},
            {0x7880D, "CA"},
            {0x78824, "0406"},
            {0x7883B, "79"},
            {0x7883D, "38"},
            {0x78853, "76"},
            {0x78855, "9C"},
            {0x78C7B, "4600460046"},
            {0x78C87, "0500050014"},
            {0x78C93, "0A000A0014"},
            {0x78C9F, "4600460046"},
            {0x78CAB, "0A000A0014"},
            {0x79598, "20D0"},
            {0x2F039E, "203A"},
            {0x2F1C14, "0A960C2C0D7A11301B581FA424B830D43A983A983A983A983A983A98"},
            {0x2F1C40, "0B0A0CB20F78129023282CEC3A98426846504C2C4C2C4C2C4C2C4C2C"},
            {0x2F1C6C, "0B820D6E113014B4251C2EE035843D544C2C4E20504B504B504B504B"},
            {0x2F1C98, "0C1F0EA8113013EC1964251C29CC2C2432C84E20504B504B504B504B"},
            {0x2F1CC4, "0CB20F0A116213A115E01A9021342A302D503A98504B504B504B504B"},
            {0x2F1CF0, "0CF00F1011301324151819C821342328251C36B04E20504B504B504B"},
            {0x2F1D1C, "0D480ED8106811F81388157C17D41A9021342AF84650504B504B504B"},
            {0x2F1D48, "0DAC0F6E113012C0145016A81A2C1F40226027D832C85046504B504B"},
            {0x2F1D74, "0E74103611F813D315AE18381A901DB02134251C2AF834BC5046504B"},
            {0x2F1DA0, "0DAC0FA01194144E170819001B581EDC22C427102EE036B05046504B"},
            {0x2F1DCC, "0DC21002124114E3178519641BBC1F40232827762D1A38A45046504B"},
            {0x2F1DF8, "0F681163135E16B519001C211D5E215F251C28DE2E0E3A985046504B"},
            {0x2F1E24, "0FB811D313ED176A1AE61DFB1FA6241625A22A30300C3C8C5046504B"},
            {0x2F34CB, "32"},
            {0x2F34D2, "02660266"},
            {0x2F34D7, "CE"},
            {0x2F3508, "0400059A059A04660533046604000400039A02CD0400040004000400"},
            {0x2F5D3A, "206C207920A8"},
            {0x2F5D41, "05"},
            {0x2F5D43, "12"},
            {0x2F5D45, "3721CA"},
            {0x2F5D49, "4422E0239424A2265227D827C927AE2772"},
            {0x2F5FDB, "8D"},
            {0x2F5FDD, "23"},
            {0x2F5FDF, "8B11DE"},
            {0x2F5FE3, "46169D18F3"},
            {0x2F5FF3, "A8"},
            {0x2F5FF5, "540FDF"},
            {0x2F5FF9, "41"},
            {0x2F5FFB, "9616F7"},
            {0x2F5FFF, "58"},
            {0x2F600B, "B3"},
            {0x2F600D, "5D0FE7124C149F17021965"},
            {0x2F6023, "AA"},
            {0x2F6025, "480FD1"},
            {0x2F6029, "3A148616E71949"},
            {0x2F603B, "B2"},
            {0x2F603D, "490FC6"},
            {0x2F6041, "2F148316E61948"},
            {0x2F6053, "C3"},
            {0x2F6055, "510FCB"},
            {0x2F6059, "31149216F6195A"},
            {0x2F606B, "9D0CFC"},
            {0x2F606F, "21112E1400165418A7"},
            {0x2F6082, "0AE2"},
            {0x2F6085, "21"},
            {0x2F6087, "31114713FD165018A4"},
            {0x2F609A, "0AEB"},
            {0x2F609D, "0C"},
            {0x2F609F, "1B112713E016301881"},
            {0x2F60B2, "0AE0"},
            {0x2F60B5, "0C"},
            {0x2F60B7, "2E114213F116441897"},
            {0x2F60CA, "0ACF"},
            {0x2F60CD, "1A"},
            {0x2F60CF, "1F111313E016301880"},
            {0x2F60E2, "0ACF"},
            {0x2F60E5, "1A"},
            {0x2F60E7, "1F111313E016301880"},
            {0x2F6349, "36"},
            {0x2F634B, "2800FA"},
            {0x2F6351, "97"},
            {0x2F6353, "8F"},
            {0x2F6355, "78"},
            {0x2F6357, "64"},
            {0x2F6359, "64"},
            {0x2F635B, "64"},
            {0x2F6361, "68"},
            {0x2F6363, "4A"},
            {0x2F6365, "1800F0"},
            {0x2F6369, "D2"},
            {0x2F636B, "B0"},
            {0x2F636D, "AA"},
            {0x2F636F, "A0"},
            {0x2F6371, "A0"},
            {0x2F6373, "A0"},
            {0x2F6379, "90"},
            {0x2F637B, "72"},
            {0x2F637D, "2C"},
            {0x2F637F, "22"},
            {0x2F6381, "0F"},
            {0x2F6383, "E3"},
            {0x2F6385, "DC"},
            {0x2F6387, "DC"},
            {0x2F6389, "DC"},
            {0x2F638B, "DC"},
            {0x2F6391, "B7"},
            {0x2F6393, "9A"},
            {0x2F6395, "7A"},
            {0x2F6397, "56"},
            {0x2F6399, "45"},
            {0x2F639B, "1B"},
            {0x2F639D, "18"},
            {0x2F639F, "18"},
            {0x2F63A1, "18"},
            {0x2F63A3, "18"},
            {0x2F63A9, "B7"},
            {0x2F63AB, "98"},
            {0x2F63AD, "73"},
            {0x2F63AF, "54"},
            {0x2F63B1, "54"},
            {0x2F63B3, "53"},
            {0x2F63B5, "2C"},
            {0x2F63B7, "2C"},
            {0x2F63B9, "2C"},
            {0x2F63BB, "2C"},
            {0x2F63C1, "C8"},
            {0x2F63C3, "B6"},
            {0x2F63C5, "8B"},
            {0x2F63C7, "72"},
            {0x2F63C9, "68"},
            {0x2F63CB, "40"},
            {0x2F63CD, "18"},
            {0x2F63CF, "18"},
            {0x2F63D1, "18"},
            {0x2F63D3, "18"},
            {0x2F63D9, "D1"},
            {0x2F63DB, "C3"},
            {0x2F63DD, "96"},
            {0x2F63DF, "79"},
            {0x2F63E1, "65"},
            {0x2F63E3, "63"},
            {0x2F63E5, "2C"},
            {0x2F63E7, "2C"},
            {0x2F63E9, "2C"},
            {0x2F63EB, "2C"},
            {0x2F63F1, "D1"},
            {0x2F63F3, "D2"},
            {0x2F63F5, "A4"},
            {0x2F63F7, "6E"},
            {0x2F63F9, "50"},
            {0x2F63FB, "4E"},
            {0x2F63FD, "40"},
            {0x2F63FF, "43"},
            {0x2F6401, "4E"},
            {0x2F6403, "5E"},
            {0x2F6409, "E6"},
            {0x2F640B, "D5"},
            {0x2F640D, "98"},
            {0x2F640F, "54"},
            {0x2F6411, "40"},
            {0x2F6413, "5E"},
            {0x2F6415, "59"},
            {0x2F6417, "54"},
            {0x2F6419, "54"},
            {0x2F641B, "54"},
            {0x2F6421, "E0"},
            {0x2F6423, "B4"},
            {0x2F6425, "78"},
            {0x2F6427, "68"},
            {0x2F6429, "3C"},
            {0x2F642B, "68"},
            {0x2F642D, "68"},
            {0x2F642F, "68"},
            {0x2F6431, "68"},
            {0x2F6433, "68"},
            {0x2F6439, "2001F4"},
            {0x2F643D, "8D"},
            {0x2F643F, "58"},
            {0x2F6441, "49"},
            {0x2F6443, "4D"},
            {0x2F6445, "54"},
            {0x2F6447, "54"},
            {0x2F6449, "54"},
            {0x2F644B, "54"},
            {0x2F6451, "2D"},
            {0x2F6453, "1B"},
            {0x2F6455, "99"},
            {0x2F6457, "64"},
            {0x2F6459, "3C"},
            {0x2F645B, "43"},
            {0x2F645D, "36"},
            {0x2F645F, "2C"},
            {0x2F6461, "22"},
            {0x2F6463, "22"},
            {0x2F6469, "44"},
            {0x2F646B, "26"},
            {0x2F646D, "A4"},
            {0x2F646F, "6E"},
            {0x2F6471, "46"},
            {0x2F6473, "4A"},
            {0x2F6475, "47"},
            {0x2F6477, "22"},
            {0x2F6479, "18"},
            {0x2F647B, "18"},
            {0x2F6481, "44"},
            {0x2F6483, "26"},
            {0x2F6485, "B9"},
            {0x2F6487, "75"},
            {0x2F6489, "52"},
            {0x2F648B, "55"},
            {0x2F648D, "4A"},
            {0x2F648F, "2C"},
            {0x2F6491, "22"},
            {0x2F6493, "22"},
            {0x2F6499, "44"},
            {0x2F649B, "26"},
            {0x2F649D, "E0"},
            {0x2F649F, "A4"},
            {0x2F64A1, "72"},
            {0x2F64A3, "60"},
            {0x2F64A5, "40"},
            {0x2F64A7, "22"},
            {0x2F64A9, "22"},
            {0x2F64AB, "22"},
            {0x2F64B1, "44"},
            {0x2F64B3, "44"},
            {0x2F64B5, "4401F4"},
            {0x2F64B9, "A4"},
            {0x2F64BB, "5E"},
            {0x2F64BD, "40"},
            {0x2F64BF, "2C"},
            {0x2F64C1, "22"},
            {0x2F64C3, "22"},
            {0x2F7B1A, "02BC02BC038403840384038402BC02BC03840384038403E802BC02BC038404B004B004B002BC02BC038404B004B004B002BC02BC038405780578057802BC02BC0384057805780578"},
            {0x2F8D38, "02020304050501020303040401010202030301010102030301010202030402020203050703030304060904040506080A"},
            {0x2F8D8D, "0304050607020304050607030404050607040404050708040405060809050505080A0B060607090B"},
            {0x2F8DD4, "00020304040601020304040601020304050601020405060701020506070803040607080905060708090A0708090A0B0C"},
            {0x2F8E22, "04060708080905060708080905070809090A06070809090A0607090A0A0B0809090A0B0C09090A0B0C0C0A0A0B0C0C"},
            {0x2F8E70, "01020304050501010203040401010203040401010203040401010203030501020304050702030405060903040608090A"},
            {0x2F8EBE, "04060707070703050606060703050606070704050607080804050607080904060708090A050708090A0B0708090A0B"},
            {0x2F8F07, "0A080806060A0806060404080606060505070606060505090807070607"},
            {0x2F8F3D, "0A09090808"},
            {0x2F8F43, "0A080807070A09090807070A09090807070A0A09090809"},
            {0x2F8F72, "030406060808020204040608010203030404010203030404020304040606"},
            {0x2F8FA8, "060608080A"},
            {0x2F8FAE, "040406060808030405060608020304050607030405060708"},
            {0x2F8FDE, "010101020203010101020202010101020202010102030303020303040404"},
            {0x2F901F, "06"},
            {0x2F9025, "06"},
            {0x2F902B, "06"},
            {0x2F9CD3, "05"},
            {0x2F9CD5, "25"},
            {0x2F9CD7, "42"},
            {0x2F9CD9, "3F"},
            {0x2F9CDB, "46"},
            {0x2F9CDD, "57"},
            {0x2F9CDF, "50"},
            {0x2F9CE1, "5A"},
            {0x2F9CE5, "03"},
            {0x2F9CE7, "27"},
            {0x2F9CE9, "4A"},
            {0x2F9CEB, "30"},
            {0x2F9CED, "51"},
            {0x2F9CEF, "66"},
            {0x2F9CF1, "46"},
            {0x2F9CF3, "40"},
            {0x2F9CF9, "24"},
            {0x2F9CFB, "3B"},
            {0x2F9CFD, "28"},
            {0x2F9CFF, "38"},
            {0x2F9D01, "41"},
            {0x2F9D03, "3C"},
            {0x2F9D05, "3C"},
            {0x2F9D0B, "22"},
            {0x2F9D0D, "1F"},
            {0x2F9D0F, "1B"},
            {0x2F9D11, "22"},
            {0x2F9D13, "41"},
            {0x2F9D15, "37"},
            {0x2F9D17, "3A"},
            {0x2F9D1D, "1A"},
            {0x2F9D1F, "27"},
            {0x2F9D21, "1B"},
            {0x2F9D23, "1E"},
            {0x2F9D25, "2C"},
            {0x2F9D27, "32"},
            {0x2F9D29, "38"},
            {0x2F9D2F, "15"},
            {0x2F9D31, "18"},
            {0x2F9D33, "12"},
            {0x2F9D35, "19"},
            {0x2F9D37, "26"},
            {0x2F9D39, "2D"},
            {0x2F9D3B, "32"},
            {0x2F9D41, "13"},
            {0x2F9D43, "0C"},
            {0x2F9D45, "0A"},
            {0x2F9D47, "14"},
            {0x2F9D49, "1E"},
            {0x2F9D4B, "1E"},
            {0x2F9D4D, "28"},
            {0x2F9DF1, "E2"},
            {0x2F9DF3, "E2"},
            {0x2F9DF5, "CE"},
            {0x2F9DF7, "BA"},
            {0x2F9DF9, "B0"},
            {0x2F9DFB, "B0"},
            {0x2F9DFD, "B0"},
            {0x2F9E95, "37"},
            {0x2F9E97, "32"},
            {0x2F9E9D, "50"},
            {0x2F9E9F, "46"},
            {0x2F9EA5, "50"},
            {0x2F9EA7, "46"},
            {0x2F9EAD, "8A"},
            {0x2F9EAF, "50"},
            {0x2F9EB5, "B6"},
            {0x2F9EB7, "8C"},
            {0x2F9EBD, "D6"},
            {0x2F9EBF, "D2"},
            {0x2F9EC5, "18"},
            {0x2F9EC7, "0F"},
            {0x2F9F93, "B7"},
            {0x2F9F95, "9A"},
            {0x2F9F97, "7A"},
            {0x2F9F99, "56"},
            {0x2F9F9B, "45"},
            {0x2F9F9D, "1B"},
            {0x2F9F9F, "18"},
            {0x2F9FA1, "18"},
            {0x2F9FA3, "18"},
            {0x2F9FA5, "18"},
            {0x2F9FAB, "B7"},
            {0x2F9FAD, "98"},
            {0x2F9FAF, "73"},
            {0x2F9FB1, "54"},
            {0x2F9FB3, "54"},
            {0x2F9FB5, "53"},
            {0x2F9FB7, "2C"},
            {0x2F9FB9, "2C"},
            {0x2F9FBB, "2C"},
            {0x2F9FBD, "2C"},
            {0x2F9FC3, "C8"},
            {0x2F9FC5, "B6"},
            {0x2F9FC7, "8B"},
            {0x2F9FC9, "72"},
            {0x2F9FCB, "68"},
            {0x2F9FCD, "40"},
            {0x2F9FCF, "18"},
            {0x2F9FD1, "18"},
            {0x2F9FD3, "18"},
            {0x2F9FD5, "18"},
            {0x2F9FDB, "D1"},
            {0x2F9FDD, "C3"},
            {0x2F9FDF, "96"},
            {0x2F9FE1, "79"},
            {0x2F9FE3, "65"},
            {0x2F9FE5, "63"},
            {0x2F9FE7, "2C"},
            {0x2F9FE9, "2C"},
            {0x2F9FEB, "2C"},
            {0x2F9FED, "2C"},
            {0x2F9FF3, "D1"},
            {0x2F9FF5, "D2"},
            {0x2F9FF7, "A4"},
            {0x2F9FF9, "6E"},
            {0x2F9FFB, "50"},
            {0x2F9FFD, "4E"},
            {0x2F9FFF, "40"},
            {0x2FA001, "43"},
            {0x2FA003, "4E"},
            {0x2FA005, "5E"},
            {0x2FA00B, "E6"},
            {0x2FA00D, "D5"},
            {0x2FA00F, "98"},
            {0x2FA011, "54"},
            {0x2FA013, "40"},
            {0x2FA015, "5E"},
            {0x2FA017, "59"},
            {0x2FA019, "54"},
            {0x2FA01B, "54"},
            {0x2FA01D, "54"},
            {0x2FA023, "E0"},
            {0x2FA025, "B4"},
            {0x2FA027, "78"},
            {0x2FA029, "68"},
            {0x2FA02B, "3C"},
            {0x2FA02D, "68"},
            {0x2FA02F, "68"},
            {0x2FA031, "68"},
            {0x2FA033, "68"},
            {0x2FA035, "68"},
            {0x2FA03B, "2001F4"},
            {0x2FA03F, "8D"},
            {0x2FA041, "58"},
            {0x2FA043, "49"},
            {0x2FA045, "4D"},
            {0x2FA047, "54"},
            {0x2FA049, "54"},
            {0x2FA04B, "54"},
            {0x2FA04D, "54"},
            {0x2FA053, "2D"},
            {0x2FA055, "1B"},
            {0x2FA057, "99"},
            {0x2FA059, "64"},
            {0x2FA05B, "3C"},
            {0x2FA05D, "43"},
            {0x2FA05F, "36"},
            {0x2FA061, "2C"},
            {0x2FA063, "22"},
            {0x2FA065, "22"},
            {0x2FA06B, "44"},
            {0x2FA06D, "26"},
            {0x2FA06F, "A4"},
            {0x2FA071, "6E"},
            {0x2FA073, "46"},
            {0x2FA075, "4A"},
            {0x2FA077, "47"},
            {0x2FA079, "22"},
            {0x2FA07B, "18"},
            {0x2FA07D, "18"},
            {0x2FA083, "44"},
            {0x2FA085, "26"},
            {0x2FA087, "B9"},
            {0x2FA089, "75"},
            {0x2FA08B, "52"},
            {0x2FA08D, "55"},
            {0x2FA08F, "4A"},
            {0x2FA091, "2C"},
            {0x2FA093, "22"},
            {0x2FA095, "22"},
            {0x2FA09B, "44"},
            {0x2FA09D, "26"},
            {0x2FA09F, "E0"},
            {0x2FA0A1, "A4"},
            {0x2FA0A3, "72"},
            {0x2FA0A5, "60"},
            {0x2FA0A7, "40"},
            {0x2FA0A9, "22"},
            {0x2FA0AB, "22"},
            {0x2FA0AD, "22"},
            {0x2FA0B3, "44"},
            {0x2FA0B5, "44"},
            {0x2FA0B7, "4401F4"},
            {0x2FA0BB, "A4"},
            {0x2FA0BD, "5E"},
            {0x2FA0BF, "40"},
            {0x2FA0C1, "2C"},
            {0x2FA0C3, "22"},
            {0x2FA0C5, "22"},
            {0x2FA0F5, "5A"},
            {0x2FA0F7, "50"},
            {0x2FA0FB, "5A"},
            {0x2FA0FD, "50"},
            {0x2FA101, "BE"},
            {0x2FA103, "B4"},
            {0x2FA109, "DC"},
            {0x2FA10F, "18"},
            {0x2FA113, "54"},
            {0x2FA115, "40"},
            {0x2FA119, "54"},
            {0x2FA11B, "2C"},
            {0x2FA11F, "54"},
            {0x2FA121, "40"},
            {0x2FA127, "72"},
            {0x2FA12B, "90"},
            {0x2FA12D, "68"},
            {0x2FA131, "72"},
            {0x2FA133, "68"},
            {0x2FA137, "86"},
            {0x2FA139, "7C"},
            {0x2FA13D, "7C"},
            {0x2FA13F, "68"},
            {0x2FA143, "54"},
            {0x2FA145, "36"},
            {0x2FA149, "40"},
            {0x2FA14B, "2C"},
            {0x2FA14F, "40"},
            {0x2FA151, "36"},
            {0x2FA153, "40"},
            {0x2FA155, "40"},
            {0x2FA157, "36"},
            {0x2FA159, "40"},
            {0x2FA15B, "40"},
            {0x2FA15D, "2C"},
            {0x2FA55E, "0064044C"},
            {0x2FA564, "0352035203520352"},
            {0x2FA572, "0DAC041F0A"},
            {0x2FA582, "02BC"},
            {0x2FA5A0, "20A8213421CA239624A4265227D82774"},
            {0x2FA654, "124116F71959"},
            {0x2FA668, "122F16EB1948"},
            {0x2FA67C, "112E165418A7"},
            {0x2FA690, "112716301881"},
            {0x2FA6A4, "111316301880"},
            {0x2FA6B8, "111316301880"},
            {0x2FA808, "03E803E804B004B004B004B003E803E804B004B004B0051403E803E804B005DC05DC05DC03E803E804B005DC05DC05DC03E803E804B006A406A406A403E803E804B006A406A406A4"},
            {0x2FA996, "01F4"},
            {0x2FA9D2, "F9C0"}
        };

        #endregion

        #region S65

        private static readonly Dictionary<int, string> S65ColdStartModified = new()
        {
            {0x2F409E, "0000"}
        };
        private static readonly Dictionary<int, string> S65ColdStartOriginal = new()
        {
            {0x2F409E, "1194"}
        };
        private static readonly Dictionary<int, string> S65SpeedLimitModified = new()
        {
            {0x7B0A0, "17A017A017A017A017A017A017A017A0"},
            {0x7B0C2, "17A017A017A017A017A017A017A017A0"},
            {0x7B0E4, "17A017A017A017A017A017A017A017A0"},
            {0x7B106, "17A017A017A017A017A017A017A017A0"},
            {0x7B128, "17A017A017A017A017A017A017A017A0"}
        };
        private static readonly Dictionary<int, string> S65SpeedLimitOriginal = new()
        {
            {0x7B0A0, "11601160116011601160116011601160"},
            {0x7B0C2, "11601160116011601160116011601160"},
            {0x7B0E4, "11601160116011601160116011601160"},
            {0x7B106, "11601160116011601160116011601160"},
            {0x7B128, "0FC00FC00FC00FC00FC00FC00FC00FC0"}
        };
        private static readonly Dictionary<int, string> S65PrimaryCatModified = new()
        {
            {0x7170C, "00000000"},
            {0x71716, "00000000000000"},
            {0x7171E, "00"},
            {0x71720, "00000000000000000000"},
            {0x71730, "00000000000000"},
            {0x71738, "00"},
            {0x7173A, "000000000000"},
        };
        private static readonly Dictionary<int, string> S65PrimaryCatOriginal = new()
        {
            {0x7170C, "27890420"},
            {0x71716, "01010101010102"},
            {0x7171E, "58"},
            {0x71720, "5801581E5C55278A0430"},
            {0x71730, "01010101010102"},
            {0x71738, "58"},
            {0x7173A, "5801581F5C55"},
        };
        private static readonly Dictionary<int, string> S65SapModified = new()
        {
            {0x70EEC, "000000000000"},
            {0x70EF4, "0000000000000000"},
            {0x70EFE, "0000000000000000"},
            {0x71AE8, "0000"},
            {0x71AF0, "000000000000000000"},
            {0x71AFA, "00000000000000000000"},
            {0x71B08, "0000"},
            {0x71B0C, "000000000000"},
            {0x71B14, "00000000000000000000"},
            {0x71B22, "0000"},
            {0x71B26, "000000000000"},
            {0x71B2E, "0000000000000000000000000000"},
            {0x71B3E, "0000000000000000"},
            {0x71B48, "0000000000000000"},
            {0x71D24, "0000"},
            {0x71D2C, "0000000000000000"},
            {0x71D36, "0000000000000000"},
        };
        private static readonly Dictionary<int, string> S65SapOriginal = new()
        {
            {0x70EEC, "273924332432"},
            {0x70EF4, "2430010101010101"},
            {0x70EFE, "5C6F580758045803"},
            {0x71AE8, "27AF"},
            {0x71AF0, "141101010101010102"},
            {0x71AFA, "580258035CD65CDB27B0"},
            {0x71B08, "0491"},
            {0x71B0C, "010101010101"},
            {0x71B14, "5C565C715C785C7927B1"},
            {0x71B22, "0492"},
            {0x71B26, "010101010101"},
            {0x71B2E, "5C565C715C785C7927B204112430"},
            {0x71B3E, "1412010101010101"},
            {0x71B48, "5C565C715C785C79"},
            {0x71D24, "27C5"},
            {0x71D2C, "2430010101010101"},
            {0x71D36, "5C795CD65CDB5CDD"},
        };
        private static readonly Dictionary<int, string> S65PostCatO2Modified = new()
        {
            {0x70D9A, "0000"},
            {0x70D9D, "00"},
            {0x70D9F, "00"},
            {0x70DA1, "00"},
            {0x70DA4, "00000000000000"},
            {0x70DAC, "00"},
            {0x70DAE, "0000000000000000"},
            {0x70DB7, "00"},
            {0x70DB9, "00"},
            {0x70DBB, "00"},
            {0x70DBE, "00000000000000"},
            {0x70DC6, "00"},
            {0x70DC8, "000000000000"},
        };
        private static readonly Dictionary<int, string> S65PostCatO2Original = new()
        {
            {0x70D9A, "272C"},
            {0x70D9D, "38"},
            {0x70D9F, "37"},
            {0x70DA1, "36"},
            {0x70DA4, "1E1E0101010102"},
            {0x70DAC, "58"},
            {0x70DAE, "58015C045803272D"},
            {0x70DB7, "58"},
            {0x70DB9, "57"},
            {0x70DBB, "56"},
            {0x70DBE, "1E1E0101010102"},
            {0x70DC6, "58"},
            {0x70DC8, "58015C055803"},
        };

        #endregion
    }
}
