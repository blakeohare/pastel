using System;
using System.Collections.Generic;
using System.Linq;
using Pastel.Parser.ParseNodes;

namespace Pastel.Parser
{
    internal class PastelParser
    {
        internal static readonly HashSet<string> OP_TOKENS =
            new HashSet<string>([ "=", "+=", "*=", "-=", "&=", "|=", "^=" ]);

        private IDictionary<string, object> constants;

        internal ICompilationEntity ActiveEntity { get; set; } = null;

        internal PastelContext Context { get; private set; }

        public PastelParser(
            PastelContext context,
            IDictionary<string, object> constants,
            IInlineImportCodeLoader importCodeLoader)
        {
            this.Context = context;
            this.constants = new Dictionary<string, object>(constants);
            this.ExpressionParser = new ExpressionParser(this);
            this.StatementParser = new StatementParser(this);
            this.EntityParser = new EntityParser(this);
        }

        public string LoadCode(Token throwToken, string path)
        {
            return this.Context.CodeLoader.LoadCode(throwToken, path);
        }

        public ExpressionParser ExpressionParser { get; private set; }
        public StatementParser StatementParser { get; private set; }
        public EntityParser EntityParser { get; private set; }

        internal object GetConstant(string name, object defaultValue)
        {
            if (this.constants.TryGetValue(name, out object output))
            {
                return output;
            }
            return defaultValue;
        }

        internal bool GetPastelFlagConstant(Token throwToken, string name)
        {
            string lang = LanguageUtil.GetFileExtension(this.Context.Language).Substring(1);
            Func<string, string, bool> yesNo = (yeses, nos) =>
            {
                if (yeses.Split(' ').Contains(lang)) return true;
                if (nos.Split(' ').Contains(lang)) return false;
                throw new UserErrorException("Unaccounted @pastel_flag+lang combination: " + name + "/" + lang);
            };
            switch (name)
            {
                // Deprecated in favor of adding more specific flags for language limitation
                // If it's the user's intention to add specific behavior to a specific platform for
                // application-feature reasons, ext_boolean is still available for that.
                case "IS_CSHARP": return yesNo("cs", "java js php py");
                case "IS_JAVA": return yesNo("java", "cs js php py");
                case "IS_JAVASCRIPT": return yesNo("js", "cs java php py");
                case "IS_PHP": return yesNo("php", "cs java js py");
                case "IS_PYTHON": return yesNo("py", "cs java js php");

                // deprecated in favor of STATICALLY_TYPED
                case "STRONGLY_TYPED": return yesNo("cs java", "js php py");

                case "ARRAY_IS_LIST": return yesNo("js php py", "cs java");
                case "DYNAMICALLY_TYPED": return yesNo("js php py", "cs java");
                case "HAS_INCREMENT": return yesNo("cs java js php", "py");
                case "INT_IS_FLOOR": return yesNo("js", "cs java php py");
                case "IS_CHAR_A_NUMBER": return yesNo("cs java", "php js py");
                case "PLATFORM_SUPPORTS_LIST_CLEAR": return yesNo("cs java php", "js py");
                case "STATICALLY_TYPED": return yesNo("cs java", "js php py");

                default:
                    throw new ParserException(throwToken, "Unknown @pastel_flag constant: '" + name + "'.");
            }
        }

        internal bool GetParseTimeBooleanConstant(string name)
        {
            return (bool)this.GetConstant(name, false);
        }

        internal string GetParseTimeStringConstant(string name)
        {
            return (string)this.GetConstant(name, "");
        }
    }
}
