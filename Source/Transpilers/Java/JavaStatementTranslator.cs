using Pastel.Parser.ParseNodes;

namespace Pastel.Transpilers.Java
{
    internal class JavaStatementTranslator : CurlyBraceStatementTranslator
    {
        public JavaStatementTranslator(TranspilerContext ctx) : base(ctx) { }

        private JavaTypeTranspiler JavaTypeTranspiler
        {
            get { return (JavaTypeTranspiler)this.TypeTranspiler; }
        }

        public override void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut)
        {
            PType[] dictTypes = dictionary.ResolvedType.Generics;
            PType keyType = dictTypes[0];
            PType valueType = dictTypes[1];
            bool keyTypeIsBoxed = this.JavaTypeTranspiler.IsJavaPrimitiveTypeBoxed(keyType);
            bool keyExpressionIsSimple = key is Variable || key is InlineConstant;
            string keyVar = null;
            if (!keyExpressionIsSimple)
            {
                keyVar = "_PST_dictKey" + sb.SwitchCounter++;
                sb.Append(sb.CurrentTab);
                sb.Append(this.JavaTypeTranspiler.TranslateType(keyType));
                sb.Append(' ');
                sb.Append(keyVar);
                sb.Append(" = ");
                sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(key));
                sb.Append(";\n");
            }

            string lookupVar = "_PST_dictLookup" + sb.SwitchCounter++;
            sb.Append(sb.CurrentTab);
            sb.Append(this.JavaTypeTranspiler.TranslateJavaNestedType(valueType));
            sb.Append(' ');
            sb.Append(lookupVar);
            sb.Append(" = ");
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(dictionary));
            sb.Append(".get(");
            if (keyExpressionIsSimple)
            {
                sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(key));
            }
            else
            {
                sb.Append(keyVar);
            }
            sb.Append(");\n");
            sb.Append(sb.CurrentTab);
            sb.Append(varOut.Name);
            sb.Append(" = ");
            sb.Append(lookupVar);
            sb.Append(" == null ? (");

            if (!keyTypeIsBoxed)
            {
                // if the key is not a primitive, then we don't know if this null is a lack of a value or
                // if it's the actual desired value. We must explicitly call .containsKey to be certain.
                // In this specific case, we must do a double-lookup.
                sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(dictionary));
                sb.Append(".containsKey(");
                if (keyExpressionIsSimple) sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(key));
                else sb.Append(keyVar);
                sb.Append(") ? null : (");
                sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(fallbackValue));
                sb.Append(")");
            }
            else
            {
                sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(fallbackValue));
            }
            sb.Append(") : ");
            sb.Append(lookupVar);
            sb.Append(";\n");
        }

        public override void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(this.TypeTranspiler.TranslateType(varDecl.Type));
            sb.Append(' ');
            sb.Append(varDecl.VariableNameToken.Value);
            if (varDecl.Value != null)
            {
                sb.Append(" = ");
                sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(varDecl.Value));
            }
            sb.Append(";\n");
        }
    }
}
