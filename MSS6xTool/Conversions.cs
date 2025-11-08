using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Stream = System.IO.Stream;

// ReSharper disable once IdentifierTypo

namespace MSS6xTool
{
    internal static class Conversions
    {
        public static int OffsetToInt(int offset)
        {
            var dec = HexToDec(OffsetToHex(offset, 2));
            return Convert.ToInt32(dec);
        }

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

        public static bool OffsetHexCompare(int offset, string check)
        {
            var bytes = HexToBytes(check);
            return !bytes.Where((b, i) =>
                b != Global.BinaryFile[offset + i]).Any();
        }

        public static byte[] HexToBytes(string hex)
        {
            var length = hex.Length;
            var array = new byte[length / 2];
            for (var i = 0; i < length; i += 2)
            {
                array[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return array;
        }

        private static decimal HexToDec(string hex)
        {
            hex = hex.Replace("x", string.Empty);
            long.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var result);
            return result;
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

        public static string IntToHexPadded(int convert, int count = 4)
        {
            var convertString = convert.ToString("X");

            while (convertString.Length < count)
            {
                convertString = convertString.Insert(0, "0");
            }
            return convertString;
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

        public static void ByteReplace(int offset, string replace)
        {
            var bytes = HexToBytes(replace);

            for (var i = 0; i < bytes.Length; i++)
            {
                Global.BinaryFile[offset + i] = bytes[i];
            }
        }

        public static void StringReplaceSequential(int offset, string data, int count)
        {
            string concat = string.Empty;
            for (var i = 0; i < count; i++)
            {
                concat += data;
            }
            ByteReplace(offset, concat);
        }

        public static string WildCardToRegular(string value)
        {
            return "^" + Regex.Escape(value)
                .Replace("\\?", ".")
                .Replace("\\*", ".*")
                + "$";
        }

        public static string FirstCharToUpper(string input)
        {
            if (input == null) return "Null";
            return input[0].ToString().ToUpper() + input[1..];
        }
    }
}