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
            throw new NotImplementedException();
        }

        public override void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef)
        {
            throw new NotImplementedException();
        }
    }
}
