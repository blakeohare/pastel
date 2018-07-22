namespace Pastel
{
    internal static class ResourceReader
    {
        public static string GetTextResource(string path)
        {
            return Util.ReadAssemblyFileText(typeof(ResourceReader).Assembly, path);
        }
    }
}
