using System.Collections.Generic;
using System.Text;

namespace Pastel
{
    internal static class Util
    {
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

        public static string ConvertStringTokenToValue(Token token)
        {
            if (token.Type != TokenType.STRING) throw new System.Exception(); // shouldn't happen.

            StringBuilder sb = new StringBuilder();
            string value = token.Value;
            char c;
            for (int i = 1; i < value.Length - 1; ++i)
            {
                c = value[i];
                if (c == '\\')
                {
                    if (i + 2 < value.Length)
                    {
                        throw new ParserException(token, "Invalid string escape sequence. Backslash at end of string.");
                    }

                    switch (value[i + 1])
                    {
                        case 't': sb.Append('\t'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case '\\': sb.Append('\\'); break;
                        case '\'': sb.Append('\''); break;
                        case '"': sb.Append('"'); break;
                        default:
                            throw new ParserException(token, "Invalid escape sequence: \\" + value[i + 1]);
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        public static string ConvertStringValueToCode(string value)
        {
            throw new System.Exception("Not implemented."); // also, you should take in the quote type as a parameter as well.
        }

        public static string FloatToString(double value)
        {
            string output = value.ToString();
            if (!output.Contains("."))
            {
                output += ".0";
            }
            return output;
        }

        public static string ReadAssemblyFileText(System.Reflection.Assembly assembly, string path)
        {
            return ReadAssemblyFileText(assembly, path, false);
        }

        public static string ReadAssemblyFileText(System.Reflection.Assembly assembly, string path, bool failSilently)
        {
            byte[] bytes = Util.ReadAssemblyFileBytes(assembly, path, failSilently);
            if (bytes == null)
            {
                return null;
            }
            return MysteryTextDecoder.DecodeArbitraryBytesAsAppropriatelyAsPossible(bytes);
        }

        private static bool IsWindows
        {
            get
            {
                switch (System.Environment.OSVersion.Platform)
                {
                    case System.PlatformID.Win32NT:
                    case System.PlatformID.Win32S:
                    case System.PlatformID.Win32Windows:
                    case System.PlatformID.WinCE:
                    case System.PlatformID.Xbox:
                        return true;

                    default:
                        return false;
                }
            }
        }

        private static readonly byte[] BUFFER = new byte[1000];

        private static Dictionary<System.Reflection.Assembly, Dictionary<string, string>> caseInsensitiveLookup =
            new Dictionary<System.Reflection.Assembly, Dictionary<string, string>>();

        public static byte[] ReadAssemblyFileBytes(System.Reflection.Assembly assembly, string path, bool failSilently)
        {
            string canonicalizedPath = path.Replace('/', '.');

            if (IsWindows)
            {
                // a rather odd difference...
                canonicalizedPath = canonicalizedPath.Replace('-', '_');
            }

            string assemblyName = assembly.GetName().Name.ToLower();
            Dictionary<string, string> nameLookup;
            if (!caseInsensitiveLookup.TryGetValue(assembly, out nameLookup))
            {
                nameLookup = new Dictionary<string, string>();
                caseInsensitiveLookup[assembly] = nameLookup;
                foreach (string resource in assembly.GetManifestResourceNames())
                {
                    nameLookup[resource.ToLower()] = resource;
                }
            }

            string fullPath = assembly.GetName().Name + "." + canonicalizedPath;
            if (!nameLookup.ContainsKey(fullPath.ToLower()))
            {
                if (failSilently)
                {
                    return null;
                }

                throw new System.Exception(path + " not marked as an embedded resource.");
            }

            System.IO.Stream stream = assembly.GetManifestResourceStream(nameLookup[fullPath.ToLower()]);
            List<byte> output = new List<byte>();
            int bytesRead = 1;
            while (bytesRead > 0)
            {
                bytesRead = stream.Read(BUFFER, 0, BUFFER.Length);
                if (bytesRead == BUFFER.Length)
                {
                    output.AddRange(BUFFER);
                }
                else
                {
                    for (int i = 0; i < bytesRead; ++i)
                    {
                        output.Add(BUFFER[i]);
                    }
                    bytesRead = 0;
                }
            }

            return output.ToArray();
        }

        public static void MergeDictionaryInto<K, V>(Dictionary<K, V> newDict, Dictionary<K, V> mergeIntoThis)
        {
            foreach (KeyValuePair<K, V> kvp in newDict)
            {
                mergeIntoThis[kvp.Key] = kvp.Value;
            }
        }

        public static Dictionary<K, V> MergeDictionaries<K, V>(params Dictionary<K, V>[] dictionaries)
        {
            if (dictionaries.Length == 0) return new Dictionary<K, V>();
            if (dictionaries.Length == 1) return new Dictionary<K, V>(dictionaries[0]);
            if (dictionaries.Length == 2)
            {
                // Super common.
                if (dictionaries[0].Count == 0) return new Dictionary<K, V>(dictionaries[1]);
                if (dictionaries[1].Count == 0) return new Dictionary<K, V>(dictionaries[0]);
            }

            Dictionary<K, V> output = new Dictionary<K, V>(dictionaries[0]);
            for (int i = 0; i < dictionaries.Length; ++i)
            {
                Dictionary<K, V> dict = dictionaries[i];
                foreach (K k in dict.Keys)
                {
                    output[k] = dict[k];
                }
            }
            return output;
        }
    }
}
