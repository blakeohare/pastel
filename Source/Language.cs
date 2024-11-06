using Pastel.Transpilers;
using System.Collections.Generic;

namespace Pastel
{
    public enum Language
    {
        NONE,

        C,
        CSHARP,
        GO,
        JAVA,
        JAVASCRIPT,
        PHP,
        PYTHON,
    }

    internal static class LanguageUtil
    {
        private static readonly Dictionary<Language, AbstractTranspiler> singletons = new Dictionary<Language, AbstractTranspiler>();

        internal static string GetFileExtension(Language lang)
        {
            switch (lang)
            {
                case Language.CSHARP: return ".cs";
                case Language.GO: return ".go";
                case Language.JAVA: return ".java";
                case Language.JAVASCRIPT: return ".js";
                case Language.PHP: return ".php";
                case Language.PYTHON: return ".py";

                case Language.NONE:
                default:
                    throw new System.NotImplementedException();
            }
        }

        internal static Language ParseLanguage(string value)
        {
            switch (value.ToLower())
            {
                case "csharp": return Language.CSHARP;
                case "go": return Language.GO;
                case "java": return Language.JAVA;
                case "javascript": return Language.JAVASCRIPT;
                case "php": return Language.PHP;
                case "python": return Language.PYTHON;
                default: return Language.NONE;
            }
        }

        internal static AbstractTranspiler CreateTranspiler(Language lang, TranspilerContext ctx)
        {
            switch (lang)
            {
                case Language.C: return new CTranspiler(ctx);
                case Language.CSHARP: return new CSharpTranspiler(ctx);
                case Language.GO: return new GoTranspiler(ctx);
                case Language.JAVA: return new JavaTranspiler(ctx);
                case Language.JAVASCRIPT: return new JavaScriptTranspiler(ctx);
                case Language.PHP: return new PhpTranspiler(ctx);
                case Language.PYTHON: return new PythonTranspiler(ctx);
                default: throw new System.InvalidOperationException();
            }
        }

        internal static Dictionary<string, object> GetLanguageConstants(Language lang)
        {
            Dictionary<string, object> output = new Dictionary<string, object>();

            output["ARRAY_IS_LIST"] = lang == Language.PYTHON || lang == Language.JAVASCRIPT || lang == Language.PHP;
            output["HAS_INCREMENT"] = !(lang == Language.PYTHON || lang == Language.GO);
            output["INT_IS_FLOOR"] = lang == Language.JAVASCRIPT;
            output["IS_CHAR_A_NUMBER"] = lang == Language.CSHARP || lang == Language.GO || lang == Language.JAVA;
            output["IS_JAVASCRIPT"] = lang == Language.JAVASCRIPT;
            output["IS_PYTHON"] = lang == Language.PYTHON;
            output["PLATFORM_SUPPORTS_LIST_CLEAR"] = lang != Language.PYTHON;
            output["STRONGLY_TYPED"] = lang == Language.CSHARP || lang == Language.GO || lang == Language.JAVA;

            return output;
        }
    }
}
