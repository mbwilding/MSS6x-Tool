using Android.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EdiabasLib;
using Xamarin.Essentials;

// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo

namespace MSS6xTool
{
    // ReSharper disable once InconsistentNaming
    internal class MSS6x
    {
        private class FaultCodes
        {
            public string OrtNr;            // Error code
            public string OrtText;          // Error name
            public string SymptomText;      // Error description
            public string VorhandenText;    // Error occurred
            public string WarnungText;      // Check engine light status
        }

        public static async void ReadCodes()
        {
            try
            {
                if (Global.Vin.Length <= 0)
                {
                    await Ui.Message("Info", "Please identify your DME first.");
                    return;
                }

                using EdiabasNet ediabas = EdiabasFuncs.StartEdiabas(Global.SgbdReading);

                bool status = EdiabasFuncs.ExecuteJob(ediabas, "FS_LESEN", string.Empty);
                if (!status)
                {
                    await Ui.Message("Error", "Failed to retrieve fault codes.");
                    return;
                }

                List<FaultCodes> faultList = new List<FaultCodes>();

                foreach (Dictionary<string, EdiabasNet.ResultData> dictionary in ediabas.ResultSets)
                {
                    long ortNr = 0;
                    string ortText = string.Empty;
                    string symptomText = string.Empty;
                    string vorhandenText = string.Empty;
                    string warnungText = string.Empty;

                    foreach (string key in from x in dictionary.Keys orderby x select x)
                    {
                        var resultData = dictionary[key];

                        switch (resultData.Name)
                        {
                            case "F_ORT_NR":
                                ortNr = EdiabasFuncs.GetResult<long>("F_ORT_NR", resultData);
                                break;
                            case "F_ORT_TEXT":
                                ortText = Conversions.FirstCharToUpper(EdiabasFuncs.GetResult<string>("F_ORT_TEXT", resultData).ToLower());
                                break;
                            case "F_SYMPTOM_TEXT":
                                symptomText = Conversions.FirstCharToUpper(EdiabasFuncs.GetResult<string>("F_SYMPTOM_TEXT", resultData).ToLower());
                                break;
                            case "F_VORHANDEN_TEXT":
                                vorhandenText = Conversions.FirstCharToUpper(EdiabasFuncs.GetResult<string>("F_VORHANDEN_TEXT", resultData).ToLower());
                                break;
                            case "F_WARNUNG_TEXT":
                                warnungText = Conversions.FirstCharToUpper(EdiabasFuncs.GetResult<string>("F_WARNUNG_TEXT", resultData).ToLower());
                                break;
                        }
                    }
                    faultList.Add(new FaultCodes
                    {
                        OrtNr = ortNr.ToString("X"),
                        OrtText = ortText,
                        SymptomText = symptomText,
                        VorhandenText = vorhandenText,
                        WarnungText = warnungText
                    });
                }

                string faultReadout = string.Empty;
                bool fault = false;
                foreach (var codes in faultList.Where(codes => codes.OrtNr != "0"))
                {
                    fault = true;
                    bool dashLight = false;
                    if (codes.WarnungText != null)
                        dashLight = codes.WarnungText.Contains("Error would cause");
                    if (dashLight)
                        faultReadout += "[DASH LIGHT]\n";
                    faultReadout +=
                        $"{codes.OrtNr}\n"
                        + $"{codes.OrtText}\n"
                        + $"{codes.SymptomText}\n"
                        + $"{codes.VorhandenText}\n\n";
                }

                if (!fault)
                {
                    await Ui.Message("DTC", "No faults reported.");
                    return;
                }

                await Ui.Logger("ReadCodes", faultReadout, false);
                await Ui.Message("DTC", faultReadout);
            }
            catch (Exception ex)
            {
                await Ui.Logger("ReadCodes-Error", ex);
            }
        }

        public static async void ResetCodes()
        {
            if (Global.Vin.Length <= 0)
            {
                await Ui.Message("Info", "Please identify your DME first.");
                return;
            }

            Ui.StatusText("Resetting Diagnostic Codes");

            using var ediabas = EdiabasFuncs.StartEdiabas(Global.SgbdReading);
            bool status = EdiabasFuncs.ExecuteJob(ediabas, "FS_LOESCHEN", string.Empty);
            if (status)
            {
                Ui.StatusText("Fault codes cleared");
                await Ui.Message("Success", "Fault codes have been cleared.");
            }
            else
            {
                Ui.StatusText("Failed to clear faults");
                await Ui.Message("Error", "Failed to clear fault codes.");
            }
        }

        public static async void RegisterBattery()
        {
            if (Global.Vin.Length <= 0)
            {
                await Ui.Message("Info", "Please identify your DME first.");
                return;
            }

            Ui.StatusText("Registering new battery");

            using var ediabas = EdiabasFuncs.StartEdiabas(Global.SgbdReading);
            bool status = EdiabasFuncs.ExecuteJob(ediabas, "STEUERN_BATTERIETAUSCH_REGISTRIEREN", string.Empty);
            if (status)
            {
                Ui.StatusText("Battery was registered");
                await Ui.Message("Success", "Battery was registered.");
            }
            else
            {
                Ui.StatusText("Failed to register battery");
                await Ui.Message("Error", "Failed to register battery.");
            }
        }

        public static Task IdentifyDme()
        {
            using var ediabas = EdiabasFuncs.StartEdiabas(Global.SgbdReading);
            EdiabasFuncs.ExecuteJob(ediabas, "aif_lesen", string.Empty);

            Global.Vin = EdiabasFuncs.GetResult<string>("AIF_FG_NR", ediabas.ResultSets);

            EdiabasFuncs.ExecuteJob(ediabas, "hardware_referenz_lesen", string.Empty);

            Global.HwRef = EdiabasFuncs.GetResult<string>("HARDWARE_REFERENZ", ediabas.ResultSets);

            EdiabasFuncs.ExecuteJob(ediabas, "daten_referenz_lesen", string.Empty);

            var swRef = EdiabasFuncs.GetResult<string>("DATEN_REFERENZ", ediabas.ResultSets);
            if (swRef.Length > 12)
                swRef = swRef[12..];

            EdiabasFuncs.ExecuteJob(ediabas, "zif_lesen", string.Empty);
            var zif = string.Empty;
            if (EdiabasFuncs.GetResult<string>("ZIF_PROGRAMM_REFERENZ", ediabas.ResultSets).Contains(Global.HwRef))
                zif = EdiabasFuncs.GetResult<string>("ZIF_PROGRAMM_STAND", ediabas.ResultSets);
            else
            {
                EdiabasFuncs.ExecuteJob(ediabas, "zif_backup_lesen", string.Empty);
                if (EdiabasFuncs.GetResult<string>("ZIF_BACKUP_PROGRAMM_REFERENZ", ediabas.ResultSets).Contains(Global.HwRef))
                    zif = EdiabasFuncs.GetResult<string>("ZIF_BACKUP_PROGRAMM_STAND", ediabas.ResultSets);
            }

            Global.Zif = zif;
            EdiabasFuncs.ExecuteJob(ediabas, "flash_programmier_status_lesen", string.Empty);

            var programmingStatus = EdiabasFuncs.GetResult<string>("FLASH_PROGRAMMIER_STATUS_TEXT", ediabas.ResultSets);

            var model = string.Empty;
            var engine = string.Empty;
            var dmeType = string.Empty;
            bool success;

            switch (Global.HwRef)
            {
                case "0569Q60":
                    model = @"M5 / M6";
                    engine = "S85 [V10]";
                    dmeType = "MSS65";
                    success = true;
                    break;

                case "0569QT0":
                    model = "M3";
                    engine = "S65 [V8]";
                    dmeType = "MSS60";
                    success = true;
                    break;

                default:
                    success = false;
                    break;
            }

            if (success)
            {
                Ui.ModelBox.Text = model;
                Ui.EngineBox.Text = engine;
                Ui.DmeTypeBox.Text = dmeType;
                Ui.VinBox.Text = Global.Vin;
                Ui.HwRefBox.Text = Global.HwRef;
                Ui.ZifBox.Text = Global.Zif;
                Ui.SwRefBox.Text = swRef;
            }

            if (programmingStatus != string.Empty)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Ui.ProgramStatusBox.Text = programmingStatus.ToUpper();
                });
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Ui.ProgramStatusBox.Text = "NO CONNECTION";
                });
            }

            if (!success) return Task.CompletedTask;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Ui.ReadFull.Enabled = true;
                Ui.ReadTune.Enabled = true;
                Ui.LoadFile.Enabled = true;
                AdvancedMenu.Menu.FindItem(Resource.Id.Read_Full_Long)?.SetEnabled(true);
                AdvancedMenu.Menu.FindItem(Resource.Id.Read_ISN_SK)?.SetEnabled(true);
                AdvancedMenu.Menu.FindItem(Resource.Id.Read_RAM)?.SetEnabled(true);
                Global.SuccessfulIdentify = true;
                Ui.VehicleInfo.Visibility = ViewStates.Visible;
            });

            return Task.CompletedTask;
        }

        public static async Task LoadFile(byte[] read = null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Ui.SaveFile.Enabled = false;
            });

            Tweaks.TweaksEnabled = false;
            Global.BinaryFile = null;

            Ui.VehicleInfo.Visibility = ViewStates.Invisible;
            Ui.ModelBox.Text = string.Empty;
            Ui.EngineBox.Text = string.Empty;
            Ui.DmeTypeBox.Text = string.Empty;
            Ui.VinBox.Text = string.Empty;
            Ui.HwRefBox.Text = string.Empty;
            Ui.ZifBox.Text = string.Empty;
            Ui.SwRefBox.Text = string.Empty;

            string filePath = string.Empty;

            if (read == null)
            {
                var openFile = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Select your binary"
                });
                try
                {
                    Global.FileName = Path.GetFileNameWithoutExtension(openFile.FileName);
                    filePath = Path.GetFileName(openFile.FileName);
                    var stream = await openFile.OpenReadAsync();
                    Global.BinaryFile = Conversions.StreamToBytes(stream);
                }
                catch
                {
                    Tweaks.TweaksEnabled = false;

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Ui.FlashTune.Enabled = false;
                        Ui.FlashFull.Enabled = false;
                        AdvancedMenu.Menu.FindItem(Resource.Id.RSA_Bypass_Fast)?.SetEnabled(false);
                        Ui.StatusTextBlock.Text = string.Empty;
                    });
                    return;
                }
            }
            else
                Global.BinaryFile = read;

            if (Global.BinaryFile != null)
            {
                switch (Global.BinaryFile.Length)
                {
                    case 0x20007:
                    case 0x20000:
                        try
                        {
                            if (Global.BinaryFile.Length == 0x20007)
                            {
                                Ui.VinBox.Text = Global.Vin = Conversions.OffsetToAscii(0x20000, 7);
                                Global.BinaryFile = Global.BinaryFile.Take(0x20000).ToArray();
                            }
                            Ui.HwRefBox.Text = Conversions.OffsetToAscii(0x256, 7);
                            var zifTemp = Conversions.OffsetToAscii(0x25E, 4);
                            if (zifTemp == "240E") zifTemp = "241E";
                            Ui.ZifBox.Text = Global.Zif = zifTemp;
                            Ui.SwRefBox.Text = Conversions.OffsetToAscii(0x262, 5);
                        }
                        catch { return; }

                        VehicleCheck(Ui.HwRefBox?.Text);

                        if (VerifyParameterMatch(Global.BinaryFile, Global.Zif))
                        {
                            Global.FullBinaryLoaded = false;

                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                Ui.FlashTune.Enabled = true;
                                Ui.FlashFull.Enabled = false;
                                AdvancedMenu.Menu.FindItem(Resource.Id.RSA_Bypass_Fast)?.SetEnabled(false);
                            });

                            Tweaks.TweaksCheck(Ui.ZifBox.Text);
                        }
                        else
                        {
                            Tweaks.TweaksEnabled = false;

                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                Ui.FlashTune.Enabled = false;
                                Ui.FlashFull.Enabled = false;
                                AdvancedMenu.Menu.FindItem(Resource.Id.RSA_Bypass_Fast)?.SetEnabled(false);
                            });
                        }

                        break;

                    case 0x500000:
                        try
                        {
                            Ui.VinBox.Text = Global.Vin = Conversions.OffsetToAscii(0x7E01, 7);
                            Ui.HwRefBox.Text = Conversions.OffsetToAscii(0x7DC0, 7);
                            Ui.ZifBox.Text = Global.Zif = Conversions.OffsetToAscii(0x10250, 4);
                            Ui.SwRefBox.Text = Conversions.OffsetToAscii(0x70262, 5);
                        }
                        catch { return; }

                        VehicleCheck(Ui.HwRefBox?.Text);

                        if (VerifyProgramMatch(Global.BinaryFile))
                        {
                            Global.FullBinaryLoaded = true;

                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                Ui.FlashTune.Enabled = true;
                                Ui.FlashFull.Enabled = true;
                                AdvancedMenu.Menu.FindItem(Resource.Id.RSA_Bypass_Fast)?.SetEnabled(true);
                            });
                            
                            Tweaks.TweaksCheck(Ui.ZifBox?.Text);
                        }
                        else
                        {
                            Tweaks.TweaksEnabled = false;

                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                Ui.FlashTune.Enabled = false;
                                Ui.FlashFull.Enabled = false;
                                AdvancedMenu.Menu.FindItem(Resource.Id.RSA_Bypass_Fast)?.SetEnabled(false);
                            });
                        }

                        break;

                    default:
                        Tweaks.TweaksEnabled = false;

                        Ui.StatusText(string.Empty);

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            Ui.FlashTune.Enabled = false;
                            Ui.FlashFull.Enabled = false;
                            AdvancedMenu.Menu.FindItem(Resource.Id.RSA_Bypass_Fast)?.SetEnabled(false);
                        });
                        break;
                }

                switch (Tweaks.CarType)
                {
                    case Tweaks.ECarType.S85:
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            Ui.ModelBox.Text = @"M5 / M6";
                            Ui.EngineBox.Text = "S85 [V10]";
                            Ui.DmeTypeBox.Text = "MSS65";
                        });
                        break;

                    case Tweaks.ECarType.S65:
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            Ui.ModelBox.Text = @"M3";
                            Ui.EngineBox.Text = "S65 [V8]";
                            Ui.DmeTypeBox.Text = "MSS60";
                        });
                        break;

                    case Tweaks.ECarType.None:

                    default:
                        Tweaks.TweaksEnabled = false;

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            Ui.FlashTune.Enabled = false;
                            Global.BinaryFile = null;
                        });
                        break;
                }
            }

            if (Ui.FlashTune.Enabled)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Ui.VehicleInfo.Visibility = ViewStates.Visible;
                });
                Ui.StatusText($"Loaded\n{Path.GetFileNameWithoutExtension(filePath)}");
            }
        }

        private static void VehicleCheck(string hwRef)
        {
            try
            {
                Tweaks.CarType = hwRef switch
                {
                    "0569Q60" => Tweaks.ECarType.S85,
                    "0569QT0" => Tweaks.ECarType.S65,
                    _ => Tweaks.ECarType.None
                };
            }
            catch { /* ignored */ }
        }

        public static void Transferring(bool status)
        {
            try
            {
                Global.IsFlashing = status;
                Ui.KeepAwake(status);
                Ui.UpdateProgressBar(0);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Ui.FunctionStack.Visibility = status ? ViewStates.Invisible : ViewStates.Visible;
                    Ui.IdentifyDme.Visibility = status ? ViewStates.Invisible : ViewStates.Visible;
                    Ui.VehicleInfo.Visibility = status ? ViewStates.Invisible : ViewStates.Visible;
                    AdvancedMenu.Menu.SetGroupVisible(0, !status);
                });
            }
            catch { /* ignored */ }
        }

        public static int FindSk(byte[] buffer)
        {
            const int keyLength = 0x10;
            var searchLimit = buffer.Length - 3 * keyLength;
            for (var i = 0; i < searchLimit; ++i)
            {
                var k = 0;
                for (; k < keyLength; k++)
                {
                    if ((buffer[i + k] ^ 0xFF) != buffer[i + k + keyLength] && (buffer[i + k] ^ 0xAA) != buffer[i + k + keyLength * 2])
                        break;
                }
                if (k == keyLength)
                    return i;
            }
            return -1;
        }

        public static async Task ReadISN_SK()
        {
            Transferring(true);

            const uint start = 0x3F8000;
            const uint end = 0x3fffff;
            var injRamDump = Array.Empty<byte>();
            var isn = Array.Empty<byte>();
            byte[] protectedRead = {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};

            byte[] ews4SkHeader = {0xA5, 0x00, 0xFF, 0xAA, 0xFF, 0xFF, 0xFF, 0xFF};
            var ews4Sk = Array.Empty<byte>();

            using var ediabas = EdiabasFuncs.StartEdiabas(Global.SgbdReading);
            string readingText;
            switch (Global.HwRef)
            {
                case "0569Q60":
                {
                    readingText = "Reading ISN";
                    await Task.Run(() => isn = ReadMemory(ediabas, 0x7940, 0x7945, readingText));
                    if (isn.SequenceEqual(protectedRead))
                    {
                        Ui.StatusText("Could not read ISN, please flash RSA Bypass to enable reading");
                        Transferring(false);
                        return;
                    }

                    byte[] casisn = {isn[2], isn[1]};

                    Ui.StatusText($"CAS ISN: {BitConverter.ToString(casisn)} | DME ISN: {BitConverter.ToString(isn)}");

                    var saveDirectory = Directory.CreateDirectory($@"{Global.SavePath}{Global.Vin}/");
                    try
                    {
                        await File.WriteAllBytesAsync($"{saveDirectory.FullName}ISN.bin", isn);
                    }
                    catch
                    {
                        await Ui.Message("Error", "Error trying to save file.");
                    }
                    break;
                }

                case "0569QT0":
                {
                    readingText = "Reading EWS4 SK";
                    await Task.Run(() => ews4Sk = ReadMemory(ediabas, 0x7948, 0x797F, readingText));

                    if (ews4Sk.Take(0x8).ToArray().SequenceEqual(ews4SkHeader))
                    {
                        Ui.StatusText($"Secret Key: {BitConverter.ToString(ews4Sk.Skip(0x8).Take(0x10).ToArray())}");
                    }

                    else
                    {
                        readingText = "Reading RAM";

                        await Task.Run(() => injRamDump = ReadMemory(ediabas, start, end, readingText));

                        Ui.StatusText("Searching for secret key");

                        var indexOfSk = FindSk(injRamDump);
                        if (indexOfSk == -1)
                        {
                            Ui.StatusText("Could not find secret key");
                            Transferring(false);
                            return;
                        }

                        var sk = injRamDump.Skip(indexOfSk).Take(0x30).ToArray();
                        Ui.StatusText($"Secret Key: {BitConverter.ToString(sk.Take(0x10).ToArray())}");
                        ews4Sk = ews4SkHeader.Concat(sk).ToArray();
                    }

                    var saveDirectory = Directory.CreateDirectory($@"{Global.SavePath}{Global.Vin}/");
                    try
                    {
                        await File.WriteAllBytesAsync($"{saveDirectory.FullName}EWS4_SK.bin", ews4Sk);
                    }
                    catch
                    {
                        await Ui.Message("Error", "Error trying to save file.");
                    }
                    break;
                }
            }

            Transferring(false);
        }

        public static byte[] ReadMemory(EdiabasNet ediabas, uint start, uint end, string readingText)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            byte[] memoryDump = { };
            var length = end - start + 1;
            var lengthRemaining = length;
            uint segLength = 0x63;
            uint bytesRead = 0;

            while (bytesRead < length)
            {
                if (lengthRemaining < segLength)
                    segLength = lengthRemaining;
                if (!EdiabasFuncs.ExecuteJob(ediabas, "ram_lesen", + start + ";" + segLength))
                    return memoryDump;

                if (readingText != string.Empty)
                    Ui.StatusText(readingText + "\n0x" + start.ToString("X"));

                bytesRead += segLength;
                var memoryRead = EdiabasFuncs.GetResult<byte[]>("RAM_LESEN_WERT", ediabas.ResultSets);

                start += segLength;
                lengthRemaining -= segLength;


                if (length > 255)
                {
                    var progress = bytesRead * 4096 / length;
                    Ui.UpdateProgressBar(progress);
                }

                memoryDump = memoryDump.Concat(memoryRead).ToArray();
            }
            return memoryDump;
        }

        public static async Task ReadRam()
        {
            Transferring(true);

            using var ediabas = EdiabasFuncs.StartEdiabas(Global.SgbdReading);
            uint start = 0x3F8000;
            uint end = 0x3fffff;
            var injection = Array.Empty<byte>();
            var ignition = Array.Empty<byte>();

            var readingText = "Reading Injection RAM";
            await Task.Run(() => injection = ReadMemory(ediabas, start, end, readingText));

            start += 0x800000;
            end += 0x800000;

            readingText = "Reading Ignition RAM";
            await Task.Run(() => ignition = ReadMemory(ediabas, start, end, readingText));

            Ui.StatusText(string.Empty);

            if (injection.Length == 0x8000 && ignition.Length == 0x8000)
            {
                var saveDirectory = Directory.CreateDirectory($@"{Global.SavePath}{Global.Vin}/");
                string prefix = $"{saveDirectory}{Global.Vin}_{Global.Zif}_";
                string suffix = $"_RAM_{DateTime.Now.ToString(Global.DateFormat)}.bin";
                string injectionRam = $"{prefix}Injection{suffix}";
                string ignitionRam = $"{prefix}Ignition{suffix}";
                await File.WriteAllBytesAsync(injectionRam, injection);
                await File.WriteAllBytesAsync(ignitionRam, ignition);

                Ui.StatusText($"Done reading! Files Saved to: Download/MSS6x/{Global.Vin}");
            }
            else
            {
                Ui.StatusText("Something went wrong, please try again");
            }
            
            Transferring(false);
        }

        public static async Task ReadTune()
        {
            using var ediabas = EdiabasFuncs.StartEdiabas(Global.SgbdReading);

            uint start = 0x70000;
            uint end = 0x7FFFF;
            var injection = Array.Empty<byte>();
            var ignition = Array.Empty<byte>();

            var readingText = "Reading Injection Tune";
            await Task.Run(() => injection = ReadMemory(ediabas, start, end, readingText));

            start += 0x800000;
            end += 0x800000;

            readingText = "Reading Ignition Tune";
            await Task.Run(() => ignition = ReadMemory(ediabas, start, end, readingText));

            var dumpedTune = injection.Concat(ignition).ToArray();
            if (dumpedTune.Length == 0x20000)
            {
                var vinBytes = Conversions.AsciiToBytes(Global.Vin);
                var vinAppendedTune = dumpedTune.Concat(vinBytes).ToArray();

                var saveDirectory = Directory.CreateDirectory($@"{Global.SavePath}{Global.Vin}/");
                string filePath =
                    $"{saveDirectory}{Global.Vin}_{Global.Zif}_Tune_{DateTime.Now.ToString(Global.DateFormat)}.bin";
                await File.WriteAllBytesAsync(filePath, vinAppendedTune);

                await LoadFile(read: vinAppendedTune);

                Ui.StatusText($"Done reading! File Saved to: Download/MSS6x/{Global.Vin}/");
            }
            else
                Ui.StatusText("Something went wrong, please try again");
        }

        public static async Task ReadFull(bool quickRead)
        {
            uint start;
            uint end;
            var injection = Array.Empty<byte>();
            var ignition = Array.Empty<byte>();

            var injectionExt = Array.Empty<byte>();
            var ignitionExt = Array.Empty<byte>();

            var injectionExtStart = Array.Empty<byte>();
            var ignitionExtStart = Array.Empty<byte>();

            using (var ediabas = EdiabasFuncs.StartEdiabas(Global.SgbdReading))
            {
                start = 0x00000;
                end = 0x7FFFF;
                var readingText = "Reading Injection Internal Flash";
                await Task.Run(() => injection = ReadMemory(ediabas, start, end, readingText));

                if (quickRead)
                {
                    start = 0x400000;
                    end = 0x407FFF;
                    readingText = "Reading Injection Manufacturing data";
                    await Task.Run(() => injectionExtStart = ReadMemory(ediabas, start, end, readingText));
                    Array.Resize(ref injectionExtStart, 0x30000);
                    for (var i = 0x8000; i < injectionExtStart.Length; ++i)
                        injectionExtStart[i] = 0xFF;

                    readingText = "Reading Injection External Flash";
                    start = 0x430000;

                    try
                    {
                        end = BitConverter.ToUInt32(injection.Skip(0x1001c).Take(4).Reverse().ToArray(), 0) + 7;
                    }
                    catch
                    {
                        Ui.StatusText("Something went wrong, please try again");
                        return;
                    }

                    await Task.Run(() => injectionExt = ReadMemory(ediabas, start, end, readingText));
                    injectionExt = injectionExtStart.Concat(injectionExt).ToArray();

                    var injExtLength = injectionExt.Length;
                    Array.Resize(ref injectionExt, 0x200000);
                    for (var i = injExtLength; i < 0x200000; ++i)
                        injectionExt[i] = 0xFF;
                }
                else
                {
                    start = 0x400000;
                    end = 0x5FFFFF;
                    readingText = "Reading Injection External Flash";
                    await Task.Run(() => injectionExt = ReadMemory(ediabas, start, end, readingText));
                }

                start = 0x800000;
                end = 0x87FFFF;
                readingText = "Reading Ignition Internal Flash";
                await Task.Run(() => ignition = ReadMemory(ediabas, start, end, readingText));

                if (quickRead)
                {
                    start = 0xC00000;
                    end = 0xC07FFF;
                    readingText = "Reading Ignition Manufacturing data";
                    await Task.Run(() => ignitionExtStart = ReadMemory(ediabas, start, end, readingText));
                    Array.Resize(ref ignitionExtStart, 0x30000);
                    for (var i = 0x8000; i < ignitionExtStart.Length; ++i)
                    {
                        ignitionExtStart[i] = 0xFF;
                    }

                    readingText = "Reading Ignition External Flash";
                    start = 0xC30000;

                    try
                    {
                        end = BitConverter.ToUInt32(ignition.Skip(0x1001c).Take(4).Reverse().ToArray(), 0) + 7 + 0x800000;
                    }
                    catch
                    {
                        Ui.StatusText("Something went wrong, please try again");
                        return;
                    }

                    await Task.Run(() => ignitionExt = ReadMemory(ediabas, start, end, readingText));
                    ignitionExt = ignitionExtStart.Concat(ignitionExt).ToArray();

                    var ignExtLength = ignitionExt.Length;
                    Array.Resize(ref ignitionExt, 0x200000);
                    for (var i = ignExtLength; i < 0x200000; ++i)
                        ignitionExt[i] = 0xFF;
                }
                else
                {
                    start = 0xC00000;
                    end = 0xDFFFFF;
                    readingText = "Reading Ignition External Flash";
                    await Task.Run(() => ignitionExt = ReadMemory(ediabas, start, end, readingText));
                }

                if (Global.HwRef == "0569QT0")
                {
                    byte[] ews4SkHeader = { 0xA5, 0x00, 0xFF, 0xAA, 0xFF, 0xFF, 0xFF, 0xFF };
                    if (!injection.Skip(0x7948).Take(0x8).ToArray().SequenceEqual(ews4SkHeader))
                    {
                        var injRamDump = Array.Empty<byte>();
                        readingText = "Reading RAM";

                        await Task.Run(() => injRamDump = ReadMemory(ediabas, 0x3F8000, 0x3FFFFF, readingText));

                        var indexOfSk = FindSk(injRamDump);
                        if (indexOfSk == -1)
                            Ui.StatusText("Could not find secret key");
                        else
                        {
                            var sk = injRamDump.Skip(indexOfSk).Take(0x30).ToArray();
                            var ews4Sk = ews4SkHeader.Concat(sk).ToArray();
                            for (var i = 0; i < 0x38; ++i)
                                injection[0x7948 + i] = ews4Sk[i];
                        }
                    }
                }
            }

            var dumpedFlash = injection.Concat(injectionExt.Concat(ignition.Concat(ignitionExt))).ToArray();
            if (dumpedFlash.Length == 0x500000)
            {
                var vinBytes = Conversions.AsciiToBytes(Global.Vin);
                var vinAppendedTune = injection.Skip(0x70000).Take(0x10000).Concat(ignition.Skip(0x70000).Take(0x10000)).ToArray().Concat(vinBytes).ToArray();

                var saveDirectory = Directory.CreateDirectory($@"{Global.SavePath}{Global.Vin}/");
                string prefix = $"{saveDirectory}{Global.Vin}_{Global.Zif}_";
                string suffix = $"_{DateTime.Now.ToString(Global.DateFormat)}.bin";
                string fullTune = $"{prefix}Full{suffix}";
                string partTune = $"{prefix}Tune{suffix}";
                await File.WriteAllBytesAsync(fullTune, dumpedFlash);
                await File.WriteAllBytesAsync(partTune, vinAppendedTune);

                await LoadFile(read: dumpedFlash);

                Ui.StatusText($"Done reading! Files Saved to: Download/MSS6x/{Global.Vin}");
            }
            else
                Ui.StatusText("Something went wrong, please try again");
        }

        public static bool IsRsaBypassed(EdiabasNet ediabas)
        {
            const uint rsaSegmentsLocation = 0x10204;

            byte[] stockRsaSegments =
            {
                0x00, 0x00, 0x00, 0x05, 0x00, 0x07, 0x00, 0x00, 0x00, 0x07, 0x00, 0x3F, 0x00, 0x07, 0x01, 0xC0,
                0x00, 0x07, 0xFF, 0xFE, 0x00, 0x02, 0x00, 0x00, 0x00, 0x03, 0xFF, 0xFF, 0x00, 0x04, 0x00, 0x00,
                0x00, 0x06, 0xFF, 0xFF, 0x00, 0x45, 0x00, 0x00, 0x00, 0x5F, 0xFF, 0xFF,
            };

            var rsaSegments2Injection = ReadMemory(ediabas, rsaSegmentsLocation, rsaSegmentsLocation + 0x2C - 1, string.Empty);
            var rsaSegments2Ignition = ReadMemory(ediabas, rsaSegmentsLocation + 0x800000, rsaSegmentsLocation + 0x800000 + 0x2C - 1, string.Empty);

            return !stockRsaSegments.SequenceEqual(rsaSegments2Injection) && !stockRsaSegments.SequenceEqual(rsaSegments2Ignition);
        }

        public static async Task<bool> FlashTune(byte[] tune, bool fromRsaBypassRoutine, bool skipDisclaimer = false)
        {
            if (!skipDisclaimer)
            {
                if (!Global.SuccessfulIdentify)
                {
                    await Ui.Message("Identify DME First", "Please connect to your car and identify your DME first.");
                    return false;
                }

                var run = await Ui.Message("Flash Tune", Ui.Disclaimer, "Continue", "Cancel", true);
                if (!run) return false;
            }

            var success = true;
            var isSigValid = Checksums.IsParamSignatureValid(tune);
            var flashingText = string.Empty;

            using (var ediabas = EdiabasFuncs.StartEdiabas(Global.SgbdReading))
            {
                if (!isSigValid && !IsRsaBypassed(ediabas) && Global.HwRef != "0569Q60")
                {
                    var answer = await Ui.Message("No RSA Bypass Detected", "Warning: We detected you are trying to flash a non-stock tune without our RSA bypass installed.\n" +
                                                            "This is likely to fail unless you have an alternative RSA bypass installed.",
                                                        "Continue", "Cancel", true);
                    if (!answer)
                    {
                        Ui.StatusText("Tune write cancelled");
                        return false;
                    }
                }
            }

            using (var ediabas = EdiabasFuncs.StartEdiabas(Global.SgbdFlashing))
            {
                await Task.Run(() =>
                {
                    EdiabasFuncs.ExecuteJob(ediabas, "FLASH_PARAMETER_SETZEN", "0x12;64;64;254;Asymetrisch");

                    EdiabasFuncs.ExecuteJob(ediabas, "aif_lesen", string.Empty);

                    EdiabasFuncs.ExecuteJob(ediabas, "hardware_referenz_lesen", string.Empty);

                    EdiabasFuncs.ExecuteJob(ediabas, "daten_referenz_lesen", string.Empty);

                    EdiabasFuncs.ExecuteJob(ediabas, "flash_programmier_status_lesen", string.Empty);

                    EdiabasFuncs.ExecuteJob(ediabas, "FLASH_ZEITEN_LESEN", string.Empty);

                    EdiabasFuncs.ExecuteJob(ediabas, "FLASH_BLOCKLAENGE_LESEN", string.Empty);

                    if (!EdiabasFuncs.RequestSecurityAccess(ediabas))
                    {
                        success = false;
                        Ui.StatusText("Security Access Denied");
                    }
                });

                const uint eraseStart = 0x70000;
                const uint eraseBlock = 0x10000;
                const uint flashStart = 0x70000;
                const uint flashEnd = 0x7FFFF;
                const uint ignitionOffset = 0x800000;

                var toFlashInj = Checksums.CorrectParameterChecksum(tune.Take(0x10000).ToArray());
                var toFlashIgn = Checksums.CorrectParameterChecksum(tune.Skip(0x10000).Take(0x10000).ToArray());

                if (!EdiabasFuncs.ExecuteJob(ediabas, "normaler_datenverkehr", "nein;nein;ja"))
                    return false;
                if (!EdiabasFuncs.ExecuteJob(ediabas, "normaler_datenverkehr", "ja;nein;nein"))
                    return false;

                Ui.StatusText("Erasing Flash");

                await Task.Run(() => success = EdiabasFuncs.EraseEcu(ediabas, eraseBlock, eraseStart));

                if (success)
                {
                    flashingText = fromRsaBypassRoutine switch
                    {
                        true => "Injection: Preparing for program flash",
                        false => "Injection: Flashing Tune"
                    };

                    await Task.Run(() => success = EdiabasFuncs.FlashBlock(ediabas, toFlashInj.Take(0x80).ToArray(), flashStart, flashStart + 0x7F, flashingText)); //0x70080 -> 0x700FF is protected on MSS60.
                }

                if (success)
                    await Task.Run(() => success = EdiabasFuncs.FlashBlock(ediabas, toFlashInj.Skip(0x100).ToArray(), flashStart + 0x100, flashEnd, flashingText));

                if (success)
                {
                    if (Global.HwRef == "0569Q60" && !isSigValid)
                    {
                        byte[] mss65RsaBypass =
                        {
                        0xAF, 0xFE, 0x08, 0x15, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    };
                        await Task.Run(() => success = EdiabasFuncs.FlashBlock(ediabas, mss65RsaBypass, flashStart + 0x80, flashStart + 0xBF, flashingText));
                    }
                }

                if (success)
                {
                    flashingText = fromRsaBypassRoutine switch
                    {
                        true => "Ignition: Preparing for program flash",
                        false => "Ignition: Flashing Tune"
                    };

                    await Task.Run(() => success = EdiabasFuncs.FlashBlock(ediabas, toFlashIgn.Take(0x80).ToArray(), flashStart + ignitionOffset, flashStart + 0x7F + ignitionOffset, flashingText));
                }
                if (success)
                    await Task.Run(() => success = EdiabasFuncs.FlashBlock(ediabas, toFlashIgn.Skip(0x100).ToArray(), flashStart + 0x100 + ignitionOffset, flashEnd + ignitionOffset, flashingText));

                if (success)
                {
                    if (Global.HwRef == "0569Q60" && !isSigValid)
                    {
                        byte[] mss65RsaBypass =
                        {
                        0xAF, 0xFE, 0x08, 0x15, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                         };
                        await Task.Run(() => success = EdiabasFuncs.FlashBlock(ediabas, mss65RsaBypass, flashStart + 0x80 + ignitionOffset, flashStart + 0xBF + ignitionOffset, flashingText));
                    }
                }

                if (success)
                {
                    await Task.Run(() =>
                    {
                        if (!EdiabasFuncs.ExecuteJob(ediabas, "normaler_datenverkehr", "ja;nein;ja"))
                        {
                            success = false;
                            return;
                        }


                        if (!EdiabasFuncs.ExecuteJob(ediabas, "FLASH_PROGRAMMIER_STATUS_LESEN", string.Empty))
                        {
                            success = false;
                            return;
                        }

                        if (Global.HwRef != "0569Q60" || isSigValid)
                        {
                            Ui.StatusText("Checking signature");
                            Ui.ProgressIndeterminate(true);

                            if (!EdiabasFuncs.ExecuteJob(ediabas, "FLASH_SIGNATUR_PRUEFEN", "Daten;35"))
                            {
                                Ui.ProgressIndeterminate(false);
                                Ui.StatusText("Signature check failed");

                                success = false;
                                EdiabasFuncs.ExecuteJob(ediabas, "STEUERGERAETE_RESET", string.Empty);
                                return;
                            }
                            Ui.ProgressIndeterminate(false);
                        }
                        if (!EdiabasFuncs.ExecuteJob(ediabas, "FLASH_PROGRAMMIER_STATUS_LESEN", string.Empty))
                        {
                            success = false;
                            return;
                        }
                        Ui.StatusText("Resetting DME");
                        if (!EdiabasFuncs.ExecuteJob(ediabas, "STEUERGERAETE_RESET", string.Empty))
                            Ui.StatusText("Failed to reset DME");
                    });

                    if (success)
                    {
                        Ui.StatusText(fromRsaBypassRoutine ? "Now flashing remaining program code" : "Tune flash success");
                        Ui.UpdateProgressBar(0);
                    }
                }
                if (!skipDisclaimer) await IdentifyDme();
                return success;
            }
        }

        public static async Task<bool> FlashFull(byte[] full, bool bypassRsa)
        {
            if (!Global.SuccessfulIdentify)
            {
                await Ui.Message("Identify DME First", "Please connect to your car and identify your DME first.");
                return false;
            }

            var run = await Ui.Message("Flash Full", Ui.Disclaimer, "Continue", "Cancel", true);
            if (!run)
            {
                return false;
            }

            var success = true;
            var flashingText = string.Empty;

            var sigValid = Checksums.IsProgramSignatureValid(full);
            bool rsaBypassInstalled;

            using (var ediabas = EdiabasFuncs.StartEdiabas(Global.SgbdReading))
            {
                rsaBypassInstalled = IsRsaBypassed(ediabas);
                if (!sigValid && !rsaBypassInstalled)
                {
                    var answer = await Ui.Message("No RSA Bypass Detected", "Warning: We detected you are trying to flash a non-stock program without having our RSA bypass installed.\n" +
                                                            "This is likely to fail unless you know your Program RSA check is bypassed some other way.\n" +
                                                            "The RSA Bypass can be flashed from the advanced menu.",
                                                        "Continue", "Cancel", true);
                    if (!answer)
                    {
                        Ui.StatusText("Program flash cancelled");
                        return false;
                    }
                }
            }

            using (var ediabas = EdiabasFuncs.StartEdiabas(Global.SgbdFlashing))
            {

                uint flashStartSection1 = 0x10000;
                uint flashEndSection1 = 0x1FFFF;

                uint ignitionOffset = 0x800000;

                var injection = full.Take(0x280000).ToArray();
                var ignition = full.Skip(0x280000).Take(0x280000).ToArray();

                if (!(sigValid && !rsaBypassInstalled) || bypassRsa)
                {
                    injection = Checksums.BypassRsa(injection);
                    ignition = Checksums.BypassRsa(ignition);
                }

                if (!bypassRsa)
                {
                    injection = Checksums.CorrectProgramChecksum(injection);
                    ignition = Checksums.CorrectProgramChecksum(ignition);
                }

                var toFlashInjIntSect1 = injection.Skip(0x10000).Take(0x10000).ToArray();
                var toFlashInjIntSect2 = injection.Skip(0x20000).Take(0x50000).ToArray();
                var toFlashInjExt = injection.Skip(0xD0000).Take(0xB0000).ToArray();

                var toFlashIgnIntSect1 = ignition.Skip(0x10000).Take(0x10000).ToArray();
                var toFlashIgnIntSect2 = ignition.Skip(0x20000).Take(0x50000).ToArray();
                var toFlashIgnExt = ignition.Skip(0xD0000).Take(0xB0000).ToArray();

                await Task.Run(() =>
                {
                    EdiabasFuncs.ExecuteJob(ediabas, "FLASH_PARAMETER_SETZEN", "0x12;64;64;254;Asymetrisch");

                    EdiabasFuncs.ExecuteJob(ediabas, "aif_lesen", string.Empty);

                    EdiabasFuncs.ExecuteJob(ediabas, "hardware_referenz_lesen", string.Empty);

                    EdiabasFuncs.ExecuteJob(ediabas, "daten_referenz_lesen", string.Empty);

                    EdiabasFuncs.ExecuteJob(ediabas, "flash_programmier_status_lesen", string.Empty);

                    EdiabasFuncs.ExecuteJob(ediabas, "FLASH_ZEITEN_LESEN", string.Empty);

                    if (EdiabasFuncs.RequestSecurityAccess(ediabas)) return;
                    success = false;
                    Ui.StatusText("Security Access Denied");
                });

                uint eraseStart = 0x10000;
                uint eraseBlock = 0x13f6c8;

                uint eraseStartRsa = 0x70000;
                uint eraseBlockRsa = 0x10000;

                uint flashStartSection2 = 0x20000;
                uint flashEndSection2 = 0x6FFFF;

                uint flashExtStart = 0x450000;
                var flashExtEndInj = BitConverter.ToUInt32(injection.Skip(0x1001c).Take(4).Reverse().ToArray(), 0) + 7;
                var flashExtEndIgn = BitConverter.ToUInt32(ignition.Skip(0x1001c).Take(4).Reverse().ToArray(), 0) + 7;

                if (flashExtEndInj > 0x4FFFFF)
                    flashExtEndInj = 0x4FFFFF;
                if (flashExtEndIgn > 0x4FFFFF)
                    flashExtEndIgn = 0x4FFFFF;

                await Task.Run(() =>
                {
                    EdiabasFuncs.ExecuteJob(ediabas, "normaler_datenverkehr", "nein;nein;ja");
                    EdiabasFuncs.ExecuteJob(ediabas, "normaler_datenverkehr", "ja;nein;nein");
                });

                if (success)
                {
                    Ui.StatusText("Erasing DME");

                    if (bypassRsa)
                    {
                        await Task.Run(() => success = EdiabasFuncs.EraseEcu(ediabas, eraseBlockRsa, eraseStartRsa));
                    }
                    else
                    {
                        await Task.Run(() => success = EdiabasFuncs.EraseEcu(ediabas, eraseBlock, eraseStart));
                    }
                }

                if (success)
                {
                    flashingText = bypassRsa ? "Injection: Flashing RSA Bypass" : "Injection: Flashing Boot Region";
                    await Task.Run(() => success = EdiabasFuncs.FlashBlock(ediabas, toFlashInjIntSect1.Take(0x80).ToArray(), flashStartSection1, flashStartSection1 + 0x7F, flashingText));
                }

                if (success)
                    await Task.Run(() => success = EdiabasFuncs.FlashBlock(ediabas, toFlashInjIntSect1.Skip(0x100).ToArray(), flashStartSection1 + 0x100, flashEndSection1, flashingText));

                if (success)
                {
                    if (!bypassRsa)
                    {
                        flashingText = "Injection: Flashing Program Region 1";
                        await Task.Run(() => success = EdiabasFuncs.FlashBlock(ediabas, toFlashInjIntSect2, flashStartSection2, flashEndSection2, flashingText));
                        flashingText = "Injection: Flashing Program Region 2";
                        await Task.Run(() => success = EdiabasFuncs.FlashBlock(ediabas, toFlashInjExt, flashExtStart, flashExtEndInj, flashingText));

                    }
                }

                if (success)
                {
                    flashingText = bypassRsa ? "Ignition: Flashing RSA Bypass" : "Ignition: Flashing Boot Region";
                    await Task.Run(() => success = EdiabasFuncs.FlashBlock(ediabas, toFlashIgnIntSect1.Take(0x80).ToArray(), flashStartSection1 + ignitionOffset, flashStartSection1 + 0x7F + ignitionOffset, flashingText));
                }
                if (success)
                    await Task.Run(() => success = EdiabasFuncs.FlashBlock(ediabas, toFlashIgnIntSect1.Skip(0x100).ToArray(), flashStartSection1 + 0x100 + ignitionOffset, flashEndSection1 + ignitionOffset, flashingText));


                if (!bypassRsa)
                {
                    if (success)
                    {
                        flashingText = "Ignition: Flashing Program Region 1";
                        await Task.Run(() => success = EdiabasFuncs.FlashBlock(ediabas, toFlashIgnIntSect2, flashStartSection2 + ignitionOffset, flashEndSection2 + ignitionOffset, flashingText));
                    }

                    if (success)
                    {
                        flashingText = "Ignition: Flashing Program Region 2";
                        await Task.Run(() => success = EdiabasFuncs.FlashBlock(ediabas, toFlashIgnExt, flashExtStart + ignitionOffset, flashExtEndIgn + ignitionOffset, flashingText));
                    }
                }

                if (success)
                {
                    await Task.Run(() =>
                    {

                        if (!EdiabasFuncs.ExecuteJob(ediabas, "normaler_datenverkehr", "ja;nein;ja"))
                        {
                            success = false;
                            return;
                        }

                        if (!EdiabasFuncs.ExecuteJob(ediabas, "FLASH_PROGRAMMIER_STATUS_LESEN", string.Empty))
                        {
                            success = false;
                            return;
                        }

                        Ui.StatusText("Checking signature");
                        Ui.ProgressIndeterminate(true);

                        if (!EdiabasFuncs.ExecuteJob(ediabas, "FLASH_SIGNATUR_PRUEFEN", "Programm;64"))
                        {
                            Ui.ProgressIndeterminate(false);
                            Ui.StatusText("Signature check failed");
                            success = false;
                            EdiabasFuncs.ExecuteJob(ediabas, "STEUERGERAETE_RESET", string.Empty);
                            return;
                        }
                        Ui.ProgressIndeterminate(false);
                        if (!EdiabasFuncs.ExecuteJob(ediabas, "FLASH_PROGRAMMIER_STATUS_LESEN", string.Empty))
                        {
                            success = false;
                        }
                    });

                    if (success)
                    {
                        Ui.UpdateProgressBar(0);

                        if (!bypassRsa)
                        {
                            Ui.StatusText("Flashing tune");
                            await FlashTune(full
                                .Skip(0x70000)
                                .Take(0x10000)
                                .Concat(full
                                .Skip(0x2F0000)
                                .Take(0x10000))
                                .ToArray(),
                                false,
                                true);
                        }
                        else
                        {
                            Ui.StatusText("RSA Bypass accepted. Preparing DME for program");
                            if (!EdiabasFuncs.ExecuteJob(ediabas, "STEUERGERAETE_RESET", string.Empty))
                                return false;
                        }
                    }
                    else
                    {
                        Ui.StatusText("Flash failed");
                    }
                }
                await IdentifyDme();
                return success;
            }
        }

        public static async Task RsaBypassTasks()
        {
            Transferring(true);

            var full = Global.BinaryFile;
            var isSigValid = Checksums.IsProgramSignatureValid(full);

            using var ediabas = EdiabasFuncs.StartEdiabas(Global.SgbdReading);
            if (!isSigValid)
            {
                await Ui.Message("Non-stock binary", "The file you loaded is not a stock binary. The RSA bypass requires a stock binary be used.\n" +
                                      "Please reload the appropriate file and try again.");
                Ui.StatusText("RSA Bypass Cancelled");
                Transferring(false);
                return;
            }

            var progRefFromBinary = System.Text.Encoding.ASCII.GetString(full.Skip(0x10248).Take(0x24).ToArray());
            var progRefFromDme = System.Text.Encoding.ASCII.GetString(ReadMemory(ediabas, 0x10248, 0x10248 + 0x24 - 1, string.Empty));

            if (progRefFromBinary != progRefFromDme)
            {
                await Ui.Message("Loaded program does not match installed software", "The file you loaded is not the same as what is installed on the DME.\n" +
                                      "The RSA bypass routine requires you use the same program as currently on the DME.");
                Ui.StatusText("RSA Bypass Cancelled");
                Transferring(false);
                return;
            }

            var answer = await Ui.Message("RSA Bypass", "Warning: If you are not using a cable with the EdiabasLib D-CAN firmware, performing this operation will destroy your DME.\n" +
                                                    "If you are unsure what firmware you have, please cancel now.", "Continue", "Cancel", true);

            if (!answer)
            {
                Transferring(false);
                return;
            }

            var success = true;
            await Task.Run(() => success = FlashFull(full, true).Result);
            if (success)
            {
                await Task.Delay(5000); //Probably don't need to wait 5 seconds
                await Task.Run(() => success = FlashTune(full.Skip(0x70000).Take(0x10000).Concat(full.Skip(0x2F0000).Take(0x10000)).ToArray(), true).Result);
            }
            if (success)
            {
                await Task.Delay(5000);
                await Task.Run(() => success = FlashFull(full, false).Result);
            }

            if (!success) Ui.StatusText("RSA Bypass Failed, aborting...");

            Transferring(false);
        }

        public static bool VerifyParameterMatch(byte[] flash, string zif)
        {
            var binheader1 = flash.Take(0x8).ToArray();
            var binheader2 = flash.Skip(0x10000).Take(0x8).ToArray();
            byte[] binheadercompare = { 0x5A, 0x5A, 0x5A, 0x5A, 0xCC, 0xCC, 0xCC, 0xCC };

            var binref1 = System.Text.Encoding.ASCII.GetString(flash.Skip(0x256).Take(0x37).ToArray());
            var binref2 = System.Text.Encoding.ASCII.GetString(flash.Skip(0x10256).Take(0x37).ToArray());

            if (!binheader1.SequenceEqual(binheadercompare) || !binheader1.SequenceEqual(binheader2))
            {
                Ui.StatusText("Not a valid tune");
                return false;
            }

            if (!binref1.Contains(Global.HwRef))
            {
                Ui.StatusText("Tune does not match hardware");
                return false;
            }

            zif = "*" + zif[..2] + "?" + zif[3..] + "*";
            if (!Regex.IsMatch(binref1, Conversions.WildCardToRegular(zif)))
            {
                Ui.StatusText("Tune does not match program");
                return false;
            }

            if (binref1 != binref2)
            {
                Ui.StatusText("Injection and Ignition tunes do not match");
                return false;
            }


            if (flash[0x252] == 1 || flash[0x10252] == 2) return true;
            Ui.StatusText("Not in Injection / Ignition order");
            return false;
        }

        public static bool VerifyProgramMatch(byte[] flash)
        {
            var binHeader1 = flash.Skip(0x10000).Take(0x8).ToArray();
            var binHeader2 = flash.Skip(0x290000).Take(0x8).ToArray();
            byte[] binHeaderCompare = { 0x5A, 0x5A, 0x5A, 0x5A, 0x33, 0x33, 0x33, 0x33 };

            var flashExtEndInj = BitConverter.ToUInt32(flash.Skip(0x1001c).Take(4).Reverse().ToArray(), 0);
            var flashExtEndIgn = BitConverter.ToUInt32(flash.Skip(0x29001c).Take(4).Reverse().ToArray(), 0);

            if (flashExtEndInj > 0x4FFFFF)
                flashExtEndInj = 0x4FFFFF;
            if (flashExtEndIgn > 0x4FFFFF)
                flashExtEndIgn = 0x4FFFFF;

            var flashfooter1 = flash.Skip((int)(flashExtEndInj - 0x380000)).Take(4).ToArray();
            var flashfooter2 = flash.Skip((int)(flashExtEndIgn - 0x100000)).Take(4).ToArray();

            var binref1 = System.Text.Encoding.ASCII.GetString(flash.Skip(0x10248).Take(0x24).ToArray());
            var binref2 = System.Text.Encoding.ASCII.GetString(flash.Skip(0x290248).Take(0x24).ToArray());

            if (!binHeader1.SequenceEqual(binHeaderCompare) || !binHeader1.SequenceEqual(binHeader2))
            {
                Ui.StatusText("Not a valid program");
                return false;
            }

            if (!flashfooter1.SequenceEqual(flashfooter2) || !flashfooter1.SequenceEqual(binHeaderCompare.Take(4).ToArray()))
            {
                Ui.StatusText("External flash not valid");
                return false;
            }

            if (!binref1.Contains(Global.HwRef))
            {
                Ui.StatusText("Program does not match hardware");
                return false;
            }

            if (binref1 != binref2)
            {
                Ui.StatusText("Injection and Ignition programs do not match");
                return false;
            }
            var zif = binref1.Substring(8, 4);

            return VerifyParameterMatch(flash.Skip(0x70000).Take(0x10000).Concat(flash.Skip(0x70000 + 0x280000).Take(0x10000)).ToArray(), zif);
        }
    }
}
