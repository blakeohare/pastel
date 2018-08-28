using System;

namespace PastelGeneratedNamespace
{
    public static class TranslationHelper
    {
        private static readonly string[] STRING_SPLIT_SEP = new string[1];
        public static string[] StringSplit(string value, string sep)
        {
            STRING_SPLIT_SEP[0] = sep;
            return value.Split(STRING_SPLIT_SEP, StringSplitOptions.None);
        }
    }
}
