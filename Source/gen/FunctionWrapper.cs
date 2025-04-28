using System.Collections.Generic;
using System.Linq;

namespace Pastel.Generated
{
    internal static class FunctionWrapper
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        private static T PST_ListPop<T>(List<T> list)
        {
            if (list.Count == 0) throw new System.InvalidOperationException();
            int lastIndex = list.Count - 1;
            T val = list[lastIndex];
            list.RemoveAt(lastIndex);
            return val;
        }

        private static Dictionary<string, System.Func<object[], object>> PST_ExtCallbacks = new Dictionary<string, System.Func<object[], object>>();

        public static void PST_RegisterExtensibleCallback(string name, System.Func<object[], object> func)
        {
            PST_ExtCallbacks[name] = func;
        }

        public static string[] PadStringsToSameLength(string[] strs)
        {
            int maxLen = 0;
            int i = 0;
            while (i < strs.Length)
            {
                int sz = strs[i].Length;
                if (sz > maxLen)
                {
                    maxLen = sz;
                }
                i += 1;
            }
            string[] output = new string[strs.Length];
            System.Collections.Generic.List<string> buffer = new List<string>();
            int j = 0;
            while (j < strs.Length)
            {
                int sizeRequired = maxLen - strs[j].Length + 1;
                while (buffer.Count < sizeRequired)
                {
                    buffer.Add(" ");
                }
                while (buffer.Count > sizeRequired)
                {
                    PST_ListPop(buffer);
                }
                buffer[0] = strs[j];
                output[j] = string.Join("", buffer);
                j += 1;
            }
            return output;
        }
    }
}
