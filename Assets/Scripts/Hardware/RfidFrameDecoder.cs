using System;
using System.Collections.Generic;
using System.Text;

namespace Hardware
{
    public enum RfidDataFormat
    {
        /// <summary>Hex 문자와 구분자로만 된 메시지는 HexText, 그 외는 Binary로 처리한다.</summary>
        Auto,
        /// <summary>리더가 "E2 00 1A" 같은 문자열을 보낸다. 줄바꿈마다 값 하나로 처리한다.</summary>
        HexText,
        /// <summary>리더가 원시 바이트를 보낸다. 메시지 전체를 값 하나로 처리한다.</summary>
        Binary,
    }

    /// <summary>TCP 메시지 전체를 HEX 값으로 바꾼다. 길이를 제한하거나 일부만 잘라 쓰지 않는다.</summary>
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

        /// <summary>손으로 고친 JSON도 같은 값으로 비교되도록 구분자를 없애고 대문자로 맞춘다.</summary>
        public static string NormalizeHex(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var builder = new StringBuilder(value.Length);
            foreach (char c in value)
                if (c < 128 && IsHexDigit((byte)c)) builder.Append(char.ToUpperInvariant(c));
            return builder.ToString();
        }

        /// <summary>구분자 설정값을 바이트로 바꾼다. 비어 있으면 빈 배열, 형식이 잘못되면 null이다.</summary>
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
            // 구분자만 있는 메시지도 텍스트로 처리해, 줄바꿈이 카드 값으로 인식되지 않게 한다.
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
