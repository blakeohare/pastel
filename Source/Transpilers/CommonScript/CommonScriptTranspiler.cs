using Pastel.Parser;
using Pastel.Parser.ParseNodes;
using System;

namespace Pastel.Transpilers.CommonScript
{
    internal class CommonScriptTranspiler : CurlyBraceTranspiler
    {
        public CommonScriptTranspiler(TranspilerContext ctx)
            : base(
                ctx,
                new CommonScriptExporter(),
                null,
                new CommonScriptExpressionTranslator(ctx),
                new CommonScriptStatementTranslator(ctx))
        { }

        public override string HelperCodeResourcePath => "Transpilers/CommonScript/PastelHelper.script";

        public override void GenerateCodeForFunction(TranspilerContext sb, FunctionDefinition funcDef, bool isStatic)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("function ");
            sb.Append(funcDef.NameToken.Value);
            sb.Append('(');
            for (int i = 0; i < funcDef.ArgNames.Length; ++i)
            {
                Token arg = funcDef.ArgNames[i];
                if (i > 0) sb.Append(", ");
                sb.Append(arg.Value);
            }
            sb.Append(") {\n");
            sb.TabDepth++;

            this.StatementTranslator.TranslateStatements(sb, funcDef.Code);

            sb.TabDepth--;
            sb.Append(sb.CurrentTab);
            sb.Append("}\n\n");
        }

        public override void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef)
        {
            throw new NotImplementedException();
        }
    }
}
