using Pastel.Parser.ParseNodes;
using System;

namespace Pastel.Transpilers.C
{
    internal class CTranspiler : CurlyBraceTranspiler
    {
        public CTranspiler(TranspilerContext transpilerCtx)
            : base(transpilerCtx)
        {
            this.TypeTranspiler = new CTypeTranspiler();
            this.Exporter = new CExporter();
            this.ExpressionTranslator = new CExpressionTranslator(transpilerCtx.PastelContext);
            this.StatementTranslator = new CStatementTranslator(transpilerCtx);
        }

        public override string HelperCodeResourcePath { get { return "Transpilers/C/PastelHelper.c"; } }

        public override void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef)
        {
            throw new NotImplementedException();
        }

        public override void GenerateCodeForFunction(TranspilerContext output, FunctionDefinition funcDef, bool isStatic)
        {
            throw new NotImplementedException();
        }
    }
}
