using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EdiabasLib;
using Xamarin.Essentials;

// ReSharper disable StringLiteralTypo
// ReSharper disable AccessToModifiedClosure
// ReSharper disable IdentifierTypo

namespace MSS6xTool
{
    internal static class EdiabasFuncs
    {
        public static bool FlashBlock(EdiabasNet ediabas, byte[] toFlash, uint blockStart, uint blockEnd, string flashingText)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            uint blockStartOrig = blockStart;
            uint blockLength = blockEnd - blockStart + 1;

            byte[] flashAddressSet = new byte[22];
            flashAddressSet[0] = 1;
            flashAddressSet[21] = 3;

            BitConverter.GetBytes(blockStart).CopyTo(flashAddressSet, 17);
            BitConverter.GetBytes(blockLength).CopyTo(flashAddressSet, 13);

            byte[] flashHeader = new byte[21];
            byte[] three = { 3 };
            int flashSegLength = 0xFE;
            flashHeader[0] = 1;
            flashHeader[13] = (byte) flashSegLength;

            string flashAddressJob = "flash_schreiben_adresse";
            string flashJob = "flash_schreiben";
            string flashEndJob = "flash_schreiben_ende";

            if (!ExecuteJob(ediabas, flashAddressJob, flashAddressSet))
            {
                Ui.StatusText("Failed to set flash address\n0x" + blockStart.ToString("X"));
                return false;
            }

            while (blockLength > 0)
            {
                if (blockLength < flashSegLength)
                {
                    flashSegLength = (int)blockLength;
                    flashHeader[13] = (byte)flashSegLength;
                }
                BitConverter.GetBytes(blockStart).CopyTo(flashHeader, 17);

                Ui.StatusText(flashingText + "\n0x" + blockStart.ToString("X"));

                if (!ExecuteJob(ediabas, flashJob, flashHeader.Concat(toFlash.Skip((int)blockStart - (int)blockStartOrig).Take(flashSegLength)).Concat(three).ToArray()))
                {
                    Ui.StatusText("Flash failed at\n0x" + blockStart.ToString("X") + ". Resetting DME.");
                    ExecuteJob(ediabas, "STEUERGERAETE_RESET", string.Empty);
                    return false;
                }
                blockStart += (uint)flashSegLength;
                blockLength -= (uint)flashSegLength;

                if (blockEnd - blockStartOrig <= 255) continue;

                uint progress = (blockStart - blockStartOrig) * 4096 / (blockEnd - blockStartOrig);
                Ui.UpdateProgressBar(progress);
            }

            if (!ExecuteJob(ediabas, flashEndJob, flashAddressSet))
            {
                Ui.StatusText("Failed to end flash job");
                return false;
            }
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Ui.KeepAwake(false);
            });

            return true;
        }

        public static bool EraseEcu(EdiabasNet ediabas, uint blockLength, uint blockStart)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            byte[] eraseCommand = new byte[22];
            eraseCommand[0] = 1;
            eraseCommand[1] = 1;
            eraseCommand[4] = 0xFA;
            eraseCommand[9] = 0xFF;

            BitConverter.GetBytes(blockStart).CopyTo(eraseCommand, 17);
            BitConverter.GetBytes(blockLength).CopyTo(eraseCommand, 13);

            Ui.ProgressIndeterminate(true);

            if (!ExecuteJob(ediabas, "flash_loeschen", eraseCommand))
            {
                Ui.StatusText("Erase failed");
                Ui.ProgressIndeterminate(false);
                return false;
            }
            Ui.ProgressIndeterminate(false);
            return true;
        }

        public static bool RequestSecurityAccess(EdiabasNet ediabas)
        {
            Ui.StatusText("Requesting Security Access");
            Ui.ProgressIndeterminate(true);

            if (!ExecuteJob(ediabas, "seriennummer_lesen", string.Empty))
            {
                Ui.ProgressIndeterminate(false);
                return false;
            }

            byte[] serialReply = GetResult<byte[]>("_TEL_ANTWORT", ediabas.ResultSets);
            byte[] serialNumber = serialReply.Skip(serialReply.Length - 5).Take(4).ToArray();
            byte[] userId = new byte[4];
            Random rng = new Random();
            rng.NextBytes(userId);

            if (!ExecuteJob(ediabas, "authentisierung_zufallszahl_lesen", "3;0x" + BitConverter.ToUInt32(userId.Reverse().ToArray(), 0).ToString("X")))
            {
                Ui.ProgressIndeterminate(false);
            }
            byte[] seed = GetResult<byte[]>("ZUFALLSZAHL", ediabas.ResultSets);

            if (!ExecuteJob(ediabas, "authentisierung_start", Checksums.GetSecurityAccessMessage(userId, serialNumber, seed)))
            {
                Ui.ProgressIndeterminate(false);
                return false;
            }

            if (!ExecuteJob(ediabas, "diagnose_mode", "ECUPM"))
            {
                Ui.ProgressIndeterminate(false);
                return false;
            }
            Ui.ProgressIndeterminate(false);
            return true;
        }

        public static EdiabasNet StartEdiabas(string sgbd)
        {
            EdiabasNet ediabas = new EdiabasNet();
            EdInterfaceBase edInterface = new EdInterfaceObd();
            object connectParameter = new EdFtdiInterface.ConnectParameterType(Global.UsbManager, 1);
            ediabas.EdInterfaceClass = edInterface;
            ediabas.EdInterfaceClass.ConnectParameter = connectParameter;
            ((EdInterfaceObd)edInterface).ComPort = "FTDI0";
            ediabas.ArgBinary = ediabas.ArgBinaryStd = null;
            ediabas.ResultsRequests = string.Empty;
            ediabas.SetConfigProperty("EcuPath", Global.EcuPath);
            ediabas.ResolveSgbdFile(sgbd);

            return ediabas;
        }

        public static bool ExecuteJob(EdiabasNet ediabas, string job, string arg)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            ediabas.ArgString = arg;
            try
            {
                ediabas.ExecuteJob(job);
            }
            catch
            {
                return false;
            }
            return GetResult<string>("JOB_STATUS", ediabas.ResultSets) == "OKAY";
        }

        public static bool ExecuteJob(EdiabasNet ediabas, string job, byte[] arg)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            ediabas.ArgBinary = arg;
            try
            {
                ediabas.ExecuteJob(job);
            }
            catch
            {
                return false;
            }
            return GetResult<string>("JOB_STATUS", ediabas.ResultSets) == "OKAY";
        }

        public static T GetResult<T>(string resultName, List<Dictionary<string, EdiabasNet.ResultData>> resultSets)
        {
            foreach (var resultData in from dictionary in resultSets
                     from key in from x in dictionary.Keys orderby x select x select dictionary[key])
                if (resultData.Name == resultName && resultData.OpData.GetType() == typeof(T) && resultData.OpData is T data)
                    return data;
            return (T)Null<T>();
        }

        public static T GetResult<T>(string resultName, EdiabasNet.ResultData resultData)
        {
            if (resultData.Name == resultName && resultData.OpData.GetType() == typeof(T) && resultData.OpData is T data)
                return data;
            return (T)Null<T>();
        }

        public static object Null<T>()
        {
            return NullOverride.TryGetValue(typeof(T), out var ret) ? ret : default(T);
        }

        private static readonly Dictionary<Type, object> NullOverride = new()
        {
            { typeof(string), string.Empty }
        };
    }
}