using System;
using System.Collections.Generic;
using System.Text;

namespace Hardware
{
    public enum RfidDataFormat
    {
        /// <summary>Messages made only of hex characters and separators are HexText; everything else is Binary.</summary>
        Auto,
        /// <summary>The reader sends strings like "E2 00 1A". Each line break ends one value.</summary>
        HexText,
        /// <summary>The reader sends raw bytes. The whole message is one value.</summary>
        Binary,
    }

    /// <summary>Converts a whole TCP message to a HEX value, without limiting its length or using only part of it.</summary>
    public static class RfidFrameDecoder
    {
        public static List<string> Decode(byte[] data, RfidDataFormat format)
        {
            var values = new List<string>();
            if (data == null || data.Length == 0) return values;

            bool text = format == RfidDataFormat.HexText || (format == RfidDataFormat.Auto && IsHexText(data));
            if (!text)
            {
                values.Add(ToHex(data));
                return values;
            }

            var line = new StringBuilder();
            foreach (byte value in data)
            {
                if (value == '\r' || value == '\n')
                {
                    AddLine(line, values);
                    continue;
                }
                if (IsHexDigit(value)) line.Append(char.ToUpperInvariant((char)value));
            }
            AddLine(line, values);
            return values;
        }

        /// <summary>Removes separators and uppercases so hand-edited JSON compares equal.</summary>
        public static string NormalizeHex(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var builder = new StringBuilder(value.Length);
            foreach (char c in value)
                if (c < 128 && IsHexDigit((byte)c)) builder.Append(char.ToUpperInvariant(c));
            return builder.ToString();
        }

        /// <summary>Converts the delimiter setting to bytes: an empty array when empty, null when malformed.</summary>
        public static byte[] ParseHexBytes(string value)
        {
            string hex = NormalizeHex(value);
            if (hex.Length % 2 != 0) return null;
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return bytes;
        }

        private static void AddLine(StringBuilder line, List<string> values)
        {
            if (line.Length == 0) return;
            values.Add(line.ToString());
            line.Clear();
        }

        private static bool IsHexText(byte[] data)
        {
            // Treat delimiter-only messages as text too, so a line break is never read as a card value.
            foreach (byte value in data)
                if (!IsHexDigit(value) && value != ' ' && value != '\t' && value != '\r' && value != '\n' &&
                    value != ':' && value != '-' && value != ',') return false;
            return true;
        }

        private static bool IsHexDigit(byte value)
        {
            return (value >= '0' && value <= '9') || (value >= 'A' && value <= 'F') || (value >= 'a' && value <= 'f');
        }

        private static string ToHex(byte[] data)
        {
            var builder = new StringBuilder(data.Length * 2);
            foreach (byte value in data) builder.Append(value.ToString("X2"));
            return builder.ToString();
        }
    }
}
