namespace Pastel
{
    internal static class DiskUtil
    {
        public static string TryReadTextFile(string fullPath)
        {
            if (System.IO.File.Exists(fullPath))
            {
                try
                {
                    return System.IO.File.ReadAllText(fullPath);
                }
                catch (System.Exception) { }
            }
            return null;
        }
    }
}
