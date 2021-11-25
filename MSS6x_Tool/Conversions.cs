using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Stream = System.IO.Stream;

// ReSharper disable once IdentifierTypo

namespace MSS6x_Tool
{
    internal static class Conversions
    {
        public static string OffsetToHex(int offset, int length, bool hexInput = false)
        {
            if (hexInput) length /= 2;
            var byteList = new List<byte>();
            for (var i = 0; i < length; i++)
            {
                byteList.Add(Global.BinaryFile[offset + i]);
            }
            return BitConverter.ToString(byteList.ToArray()).Replace("-", string.Empty);
        }

        public static string OffsetToAscii(int offset, int length)
        {
            return HexToAscii(OffsetToHex(offset, length));
        }

        public static string HexToAscii(string hex)
        {
            var text = string.Empty;
            for (var i = 0; i < hex.Length; i += 2)
            {
                text += ((char)Convert.ToInt32(hex.Substring(i, 2), 16)).ToString();
            }
            return text;
        }

        public static byte[] AsciiToBytes(string text)
        {
            return Encoding.ASCII.GetBytes(text);
        }

        public static byte[] StreamToBytes(Stream stream)
        {
            var buffer = new byte[16 * 1024];
            using var ms = new MemoryStream();
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, read);
            }
            return ms.ToArray();
        }

        public static string WildCardToRegular(string value)
        {
            return "^" + Regex.Escape(value)
                .Replace("\\?", ".")
                .Replace("\\*", ".*")
                + "$";
        }
    }
}