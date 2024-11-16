using Pastel.Parser.ParseNodes;
using System;

namespace Pastel.Transpilers.CommonScript
{
    internal class CommonScriptStatementTranslator : CurlyBraceStatementTranslator
    {
        public CommonScriptStatementTranslator(TranspilerContext ctx) : base(ctx) { }


        public override void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut)
        {
            throw new NotImplementedException();
        }

        public override void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(varDecl.VariableNameToken.Value);
            sb.Append(" = ");
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(varDecl.Value));
            sb.Append(";\n");
        }
    }
}
