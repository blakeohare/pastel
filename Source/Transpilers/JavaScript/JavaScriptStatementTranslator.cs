using Pastel.Parser.ParseNodes;

namespace Pastel.Transpilers.JavaScript
{
    internal class JavaScriptStatementTranslator : CurlyBraceStatementTranslator
    {
        public JavaScriptStatementTranslator(TranspilerContext ctx) : base(ctx) { }

        public override void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut)
        {
            sb.Append(sb.CurrentTab);
            sb.AppendVariableNameSafe(varOut.Name);
            sb.Append(" = ");
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(dictionary));
            sb.Append('[');
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(key));
            sb.Append("];\n");
            sb.Append(sb.CurrentTab);
            sb.Append("if (");
            sb.AppendVariableNameSafe(varOut.Name);
            sb.Append(" === undefined) ");
            sb.AppendVariableNameSafe(varOut.Name);
            sb.Append(" = ");
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(fallbackValue));
            sb.Append(";\n");
        }

        public override void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("let ");
            sb.AppendVariableNameSafe(varDecl.VariableNameToken.Value);
            sb.Append(" = ");
            if (varDecl.Value == null)
            {
                sb.Append("null");
            }
            else
            {
                sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(varDecl.Value));
            }
            sb.Append(";\n");
        }
    }
}
