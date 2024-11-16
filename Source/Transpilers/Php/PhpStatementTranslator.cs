using Pastel.Parser.ParseNodes;

namespace Pastel.Transpilers.Php
{
    internal class PhpStatementTranslator : CurlyBraceStatementTranslator
    {
        public PhpStatementTranslator(TranspilerContext ctx) : base(ctx) { }

        private PhpExpressionTranslator PhpExpressionTranslator
        {
            get { return (PhpExpressionTranslator)this.ExpressionTranslator; }
        }

        public override void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut)
        {
            bool keyExpressionIsSimple = key is Variable || key is InlineConstant;
            string keyVar = null;
            sb.Append(sb.CurrentTab);
            if (!keyExpressionIsSimple)
            {
                keyVar = "$_PST_dictKey" + transpilerCtx.SwitchCounter++;
                sb.Append(keyVar);
                sb.Append(" = ");
                sb.Append(this.PhpExpressionTranslator.TranslateDictionaryKeyExpression(key).Flatten());
                sb.Append(";\n");
                sb.Append(sb.CurrentTab);
            }

            sb.Append('$');
            sb.Append(varOut.Name);
            sb.Append(" = isset(");
            sb.Append(this.ExpressionTranslator.TranslateExpression(dictionary).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE).Flatten());
            sb.Append("->arr[");
            if (keyExpressionIsSimple)
            {
                sb.Append(this.PhpExpressionTranslator.TranslateDictionaryKeyExpression(key).Flatten());
            }
            else
            {
                sb.Append(keyVar);
            }
            sb.Append("]) ? ");
            sb.Append(this.ExpressionTranslator.TranslateExpression(dictionary).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE).Flatten());
            sb.Append("->arr[");
            if (keyExpressionIsSimple)
            {
                sb.Append(this.PhpExpressionTranslator.TranslateDictionaryKeyExpression(key).Flatten());
            }
            else
            {
                sb.Append(keyVar);
            }
            sb.Append("] : (");
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(fallbackValue));
            sb.Append(");\n");
        }

        public override void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl)
        {
            sb.Append(sb.CurrentTab);
            sb.Append('$');
            sb.Append(varDecl.VariableNameToken.Value);
            sb.Append(" = ");
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(varDecl.Value));
            sb.Append(";\n");
        }
    }
}
