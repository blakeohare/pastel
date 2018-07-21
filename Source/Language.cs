using Pastel.Transpilers;
using System.Collections.Generic;

namespace Pastel
{
    public enum Language
    {
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
    }
}
