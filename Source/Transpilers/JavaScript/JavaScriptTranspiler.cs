using Pastel.Parser;
using Pastel.Parser.ParseNodes;
using System;

namespace Pastel.Transpilers.JavaScript
{
    internal class JavaScriptTranspiler : CurlyBraceTranspiler
    {
        public JavaScriptTranspiler(TranspilerContext transpilerCtx)
            : base(transpilerCtx, true)
        {
            this.Exporter = new JavaScriptExporter();
            this.ExpressionTranslator = new JavaScriptExpressionTranslator(transpilerCtx.PastelContext);
        }

        public override string HelperCodeResourcePath { get { return "Transpilers/JavaScript/PastelHelper.js"; } }

        public override void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(varOut.Name);
            sb.Append(" = ");
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(dictionary));
            sb.Append('[');
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(key));
            sb.Append("];\n");
            sb.Append(sb.CurrentTab);
            sb.Append("if (");
            sb.Append(varOut.Name);
            sb.Append(" === undefined) ");
            sb.Append(varOut.Name);
            sb.Append(" = ");
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(fallbackValue));
            sb.Append(";\n");
        }

        public override void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("let ");
            sb.Append(varDecl.VariableNameToken.Value);
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

        public override void GenerateCodeForFunction(TranspilerContext sb, FunctionDefinition funcDef, bool isStatic)
        {
            sb.Append("let ");
            sb.Append(funcDef.NameToken.Value);
            sb.Append(" = function(");
            Token[] args = funcDef.ArgNames;
            for (int i = 0; i < args.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(args[i].Value);
            }
            sb.Append(") {\n");

            sb.TabDepth = 1;
            TranslateStatements(sb, funcDef.Code);
            sb.TabDepth = 0;

            sb.Append("};\n\n");
        }

        public override void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef)
        {
            throw new NotImplementedException();
        }
    }
}
