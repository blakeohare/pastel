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

        public static bool EnsureDirectoryExists(string path)
        {
            string fullPath = System.IO.Path.GetFullPath(path);
            if (fullPath == "" || fullPath == "/" || fullPath.EndsWith(':')) return true;

            if (!System.IO.Directory.Exists(fullPath))
            {
                if (!EnsureDirectoryExists(System.IO.Path.GetDirectoryName(fullPath)))
                {
                    return false;
                }

                try
                {
                    System.IO.Directory.CreateDirectory(fullPath);
                    return true;
                }
                catch (System.IO.IOException)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
