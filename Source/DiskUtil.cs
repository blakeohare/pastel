using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pastel
{
    internal static class DiskUtil
    {
        public static string TryReadTextFile(string fullPath)
        {
            if (System.IO.File.Exists(fullPath))
            {
                string data;
                try
                {
                    data = System.IO.File.ReadAllText(fullPath);
                }
                catch (System.Exception)
                {
                    return null;
                }
                return data;
            }
            return null;
        }

    }
}
