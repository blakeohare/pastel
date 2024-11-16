using Pastel.Parser;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pastel
{
    internal static class CodeUtil
    {
        public static void IndentLines(List<string> lines)
        {
            IndentLines(1, lines);
        }

        public static void IndentLines(int amount, List<string> lines)
        {
            string prefix = "";
            for (int i = 0; i < amount; i++) prefix += "\t";
            int length = lines.Count;
            for (int i = 0; i < length; ++i)
            {
                lines[i] = (prefix + lines[i]).TrimEnd();
            }
        }

        public static string ConvertCharToCharConstantCode(char value)
        {
            switch (value)
            {
                case '\n': return "'\\n'";
                case '\r': return "'\\r'";
                case '\0': return "'\\0'";
                case '\t': return "'\\t'";
                case '\\': return "'\\\\'";
                case '\'': return "'\\''";
                default: return "'" + value + "'";
            }
        }

        public static string ConvertStringValueToCode(string rawValue)
        {
            return ConvertStringValueToCode(rawValue, false);
        }

        private const char ASCII_MAX = (char)127;
        private static readonly string[] HEX_CHARS = "0 1 2 3 4 5 6 7 8 9 a b c d e f".Split(' ');

        public static string ConvertStringValueToCode(string rawValue, bool includeUnicodeEscape)
        {
            int uValue, d1, d2, d3, d4;
            List<string> output = new List<string>() { "\"" };
            foreach (char c in rawValue)
            {
                if (includeUnicodeEscape && c > ASCII_MAX)
                {
                    uValue = c;
                    output.Add("\\u");
                    d1 = uValue & 15;
                    d2 = (uValue >> 4) & 15;
                    d3 = (uValue >> 8) & 15;
                    d4 = (uValue >> 12) & 15;
                    output.Add(HEX_CHARS[d4]);
                    output.Add(HEX_CHARS[d3]);
                    output.Add(HEX_CHARS[d2]);
                    output.Add(HEX_CHARS[d1]);
                }
                else
                {
                    switch (c)
                    {
                        case '"': output.Add("\\\""); break;
                        case '\n': output.Add("\\n"); break;
                        case '\r': output.Add("\\r"); break;
                        case '\0': output.Add("\\0"); break;
                        case '\t': output.Add("\\t"); break;
                        case '\\': output.Add("\\\\"); break;
                        default: output.Add("" + c); break;
                    }
                }
            }
            output.Add("\"");

            return string.Join("", output);
        }

        private static Dictionary<char, char> ESCAPE_SEQUENCE_LOOKUP = new Dictionary<char, char>() {
            { '\\', '\\' },
            { 'n', '\n' },
            { 'r', '\r' },
            { 't', '\t' },
            { '\'', '\'' },
            { '"', '"' },
            { '0', '\0' },
        };
        public static string ConvertStringTokenToValue(Token throwToken, string tokenValue)
        {
            StringBuilder output = new StringBuilder();

            // skip quote marks
            int start = 1;
            int end = tokenValue.Length - 1;

            for (int i = start; i < end; ++i)
            {
                char c = tokenValue[i];

                // Don't have to worry about this '\' occuring as the last character escaping the end quote as
                // this would have tripped up the tokenizer.
                if (c == '\\')
                {
                    if (!ESCAPE_SEQUENCE_LOOKUP.TryGetValue(tokenValue[++i], out char escChar))
                    {
                        throw new ParserException(throwToken, "Invalid escape sequence in string constant.");
                    }
                    c = escChar;
                }
                output.Append(c);
            }
            return output.ToString();
        }

        /// <summary>
        /// Override C#'s default float to string behavior of not display the decimal portion if it's a whole number.
        /// </summary>
        public static string FloatToString(double value)
        {
            string output = value.ToString();
            if (output.Contains("E-"))
            {
                output = "0.";
                if (value < 0)
                {
                    value = -value;
                    output = "-" + output;
                }
                value *= 15;
#pragma warning disable RECS0018 // Comparison of floating point numbers with equality operator
                for (int i = 0; i < 20 && value != 0; ++i)
#pragma warning restore RECS0018 // Comparison of floating point numbers with equality operator
                {
                    output += (int)(value % 10);
                    value = value % 1;
                    value *= 10;
                }
            }

            if (!output.Contains("."))
            {
                output += ".0";
            }
            return output;
        }

        private static readonly List<string> spaceStringByLengthCache = ["", " "];
        private static string GetSpaceString(int length)
        {
            while (spaceStringByLengthCache.Count <= length)
            {
                string lastString = spaceStringByLengthCache[spaceStringByLengthCache.Count - 1];
                spaceStringByLengthCache.Add(lastString + " ");
            }
            return spaceStringByLengthCache[length];
        }

        internal static string[] PadStringsToSameLength(IEnumerable<string> strs)
        {
            int maxLen = strs.Select(s => s.Length).Max();
            return strs
                .Select(s => s + GetSpaceString(maxLen - s.Length))
                .ToArray();
        }
    }
}
