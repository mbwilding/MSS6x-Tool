using System.IO;
using Android.Content.Res;

namespace MSS6x_Tool
{
    internal static class FileManagement
    {
        public static void AssetPrepare(AssetManager asset)
        {
            Directory.CreateDirectory(Global.SavePath);
            Directory.CreateDirectory(Global.EcuPath);

            AssetWrite(asset, Global.SgbdReading);
            AssetWrite(asset, Global.SgbdFlashing);
        }

        private static void AssetWrite(AssetManager assMan, string sgbdPath)
        {
            const int maxReadSize = 5120 * 1024;

            if (File.Exists(Global.EcuPath + sgbdPath)) return;
            var br = new BinaryReader(assMan.Open(sgbdPath));
            var data = br.ReadBytes(maxReadSize);
            File.WriteAllBytes(Global.EcuPath + sgbdPath, data);
        }

        public static void ClearCache()
        {
            try
            {
                var cachePath = "/storage/emulated/0/Android/data/com.argentraceworx.mss6xtool/cache/";

                // If exist, delete the cache directory and everything in it recursively
                if (Directory.Exists(cachePath))
                    Directory.Delete(cachePath, true);

                // If not exist, restore just the directory that was deleted
                if (!Directory.Exists(cachePath))
                    Directory.CreateDirectory(cachePath);
            }
            catch { /* ignored */ }
        }
    }
}