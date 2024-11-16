using Pastel.Parser.ParseNodes;
using System;

namespace Pastel.Transpilers.C
{
    internal class CStatementTranslator : CurlyBraceStatementTranslator
    {
        public CStatementTranslator(TranspilerContext ctx) : base(ctx) { }

        public override void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut)
        {
            throw new NotImplementedException();
        }

        public override void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl)
        {
            throw new NotImplementedException();
        }
    }
}
