using System.Collections.Generic;
using System.Linq;

namespace Pastel
{
    internal static class ResourceReader
    {
        public static string ReadTextFile(string path)
        {
            return ReadTextFile(path, false);
        }

        public static string ReadTextFile(string path, bool failSilently)
        {
            System.Reflection.Assembly assembly = typeof(ResourceReader).Assembly;
            byte[] bytes = ResourceReader.ReadBinaryFile(assembly, path, failSilently);
            if (bytes == null)
            {
                return null;
            }
            if (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191)
            {
                bytes = bytes.Skip(3).ToArray();
            }
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        private static Dictionary<System.Reflection.Assembly, Dictionary<string, string>> caseInsensitiveLookup =
            new Dictionary<System.Reflection.Assembly, Dictionary<string, string>>();

        private static readonly byte[] BUFFER = new byte[1000];

        public static byte[] ReadBinaryFile(System.Reflection.Assembly assembly, string path, bool failSilently)
        {
            string canonicalizedPath = path.Replace('/', '.');
#if WINDOWS
            // a rather odd difference...
            canonicalizedPath = canonicalizedPath.Replace('-', '_');
#endif
            string assemblyName = assembly.GetName().Name.ToLower();

            Dictionary<string, string> nameLookup;
            if (!caseInsensitiveLookup.TryGetValue(assembly, out nameLookup))
            {
                nameLookup = new Dictionary<string, string>();
                caseInsensitiveLookup[assembly] = nameLookup;
                foreach (string resource in assembly.GetManifestResourceNames())
                {
                    string lookupName = resource.ToLower();
                    if (resource.Contains("_"))
                    {
                        // this is silly, but VS gets confused easily, even when marked as embedded resources.
                        lookupName = lookupName
                            .Replace("_cs.txt", ".cs")
                            .Replace("_csproj.txt", ".csproj")
                            .Replace("_sln.txt", ".sln")
                            .Replace("_go.txt", ".go")
                            .Replace("_java.txt", ".java")
                            .Replace("_js.txt", ".js")
                            .Replace("_php.txt", ".php")
                            .Replace("_py.txt", ".py")
                            .Replace("_script.txt", ".script")
                            .Replace("_xml.txt", ".xml");
                    }

                    nameLookup[lookupName] = resource;
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

    }
}
