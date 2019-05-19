using Pastel.Transpilers;
using System.Collections.Generic;

namespace Pastel
{
    public enum Language
    {
        NONE,

        C,
        CSHARP,
        JAVA,
        JAVA6,
        JAVASCRIPT,
        PYTHON,
    }

    internal static class LanguageUtil
    {
        private static readonly Dictionary<Language, AbstractTranspiler> singletons = new Dictionary<Language, AbstractTranspiler>();

        internal static Language ParseLanguage(string value)
        {
            switch (value.ToLower())
            {
                case "c": return Language.C;
                case "csharp": return Language.CSHARP;
                case "java": return Language.JAVA;
                case "javascript": return Language.JAVASCRIPT;
                case "python": return Language.PYTHON;
                default: return Language.NONE;
            }
        }

        internal static AbstractTranspiler GetTranspiler(Language language)
        {
            if (singletons.ContainsKey(language))
            {
                return singletons[language];
            }

            AbstractTranspiler t;
            switch (language)
            {
                case Language.C: t = new CTranspiler(); break;
                case Language.CSHARP: t = new CSharpTranspiler(); break;
                case Language.JAVA: t = new JavaTranspiler(false); break;
                case Language.JAVA6: t = new JavaTranspiler(true); break;
                case Language.JAVASCRIPT: t = new JavaScriptTranspiler(); break;
                case Language.PYTHON: t = new PythonTranspiler(); break;
                default: throw new System.Exception();
            }
            singletons[language] = t;
            return t;
        }

        internal static Dictionary<string, object> GetLanguageConstants(Language lang)
        {
            Dictionary<string, object> output = new Dictionary<string, object>();

            output["ARRAY_IS_LIST"] = lang == Language.PYTHON || lang == Language.JAVASCRIPT;
            output["HAS_INCREMENT"] = lang != Language.PYTHON;
            output["INT_IS_FLOOR"] = lang == Language.JAVASCRIPT || lang == Language.C;
            output["IS_C"] = lang == Language.C;
            output["IS_CHAR_A_NUMBER"] = lang == Language.C || lang == Language.CSHARP || lang == Language.JAVA || lang == Language.JAVA6;
            output["IS_JAVASCRIPT"] = lang == Language.JAVASCRIPT;
            output["IS_PYTHON"] = lang == Language.PYTHON;
            output["PLATFORM_SUPPORTS_LIST_CLEAR"] = lang != Language.PYTHON;
            output["STRONGLY_TYPED"] = lang == Language.C || lang == Language.CSHARP || lang == Language.JAVA || lang == Language.JAVA6;

            return output;
        }
    }
}
