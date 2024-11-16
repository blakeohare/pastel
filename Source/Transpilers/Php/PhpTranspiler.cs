using Pastel.Parser;
using Pastel.Parser.ParseNodes;
using System.Linq;

namespace Pastel.Transpilers.Php
{
    internal class PhpTranspiler : CurlyBraceTranspiler
    {
        public PhpTranspiler(TranspilerContext transpilerCtx)
            : base(
                transpilerCtx,
                new PhpExporter(),
                null,
                new PhpExpressionTranslator(transpilerCtx),
                new PhpStatementTranslator(transpilerCtx))
        { }

        public PhpExpressionTranslator PhpExpressionTranslator { get { return (PhpExpressionTranslator)this.ExpressionTranslator; } }

        public override string HelperCodeResourcePath { get { return "Transpilers/Php/PastelHelper.php"; } }

        public override void GenerateCodeForFunction(TranspilerContext sb, FunctionDefinition funcDef, bool isStatic)
        {
            sb.Append("\n");
            sb.Append(sb.CurrentTab);
            sb.Append("public static function ");
            sb.Append(funcDef.NameToken.Value);
            sb.Append('(');
            for (int i = 0; i < funcDef.ArgNames.Length; ++i)
            {
                Token arg = funcDef.ArgNames[i];
                if (i > 0) sb.Append(", ");
                sb.Append('$');
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
            string name = structDef.NameToken.Value;
            sb.Append("class ");
            sb.Append(name);
            sb.Append(" {\n");
            sb.TabDepth++;

            string[] fieldNames = structDef.FieldNames.Select(a => a.Value).ToArray();

            foreach (string fieldName in fieldNames)
            {
                sb.Append(sb.CurrentTab);
                sb.Append("var $");
                sb.Append(fieldName);
                sb.Append(";\n");
            }
            sb.Append(sb.CurrentTab);
            sb.Append("function __construct(");
            for (int i = 0; i < fieldNames.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                sb.Append("$a");
                sb.Append(i);
            }
            sb.Append(") {\n");
            sb.TabDepth++;
            for (int i = 0; i < fieldNames.Length; ++i)
            {
                sb.Append(sb.CurrentTab);
                sb.Append("$this->");
                sb.Append(fieldNames[i]);
                sb.Append(" = $a");
                sb.Append(i);
                sb.Append(";\n");
            }
            sb.TabDepth--;
            sb.Append(sb.CurrentTab);
            sb.Append("}\n");

            sb.TabDepth--;
            sb.Append(sb.CurrentTab);
            sb.Append("}\n");
        }
    }
}
