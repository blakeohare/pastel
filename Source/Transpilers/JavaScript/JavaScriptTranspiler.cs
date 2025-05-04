using Pastel.Parser;
using Pastel.Parser.ParseNodes;
using System;

namespace Pastel.Transpilers.JavaScript
{
    internal class JavaScriptTranspiler : CurlyBraceTranspiler
    {
        public JavaScriptTranspiler(TranspilerContext transpilerCtx)
            : base(
                transpilerCtx,
                new JavaScriptExporter(),
                null,
                new JavaScriptExpressionTranslator(transpilerCtx),
                new JavaScriptStatementTranslator(transpilerCtx)
            )
        {
            transpilerCtx.VariablePrefix = "$";
        }

        public override string HelperCodeResourcePath { get { return "Transpilers/JavaScript/PastelHelper.js"; } }

        public override void GenerateCodeForFunction(TranspilerContext sb, FunctionDefinition funcDef, bool isStatic)
        {
            sb.Append("let ");
            sb.AppendVariableNameSafe(funcDef.NameToken.Value);
            sb.Append(" = function(");
            Token[] args = funcDef.ArgNames;
            for (int i = 0; i < args.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                sb.AppendVariableNameSafe(args[i].Value);
            }
            sb.Append(") {\n");

            sb.TabDepth = 1;
            this.StatementTranslator.TranslateStatements(sb, funcDef.Code);
            sb.TabDepth = 0;

            sb.Append("};\n\n");
        }

        public override void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef)
        {
            throw new InvalidOperationException();
        }
    }
}
