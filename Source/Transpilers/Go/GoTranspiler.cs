using Pastel.Parser.ParseNodes;
using System.Linq;

namespace Pastel.Transpilers.Go
{
    internal class GoTranspiler : AbstractTranspiler
    {
        public GoTranspiler(TranspilerContext transpilerCtx)
            : base(
                transpilerCtx,
                new GoExporter(),
                new GoTypeTranspiler(),
                new GoExpressionTranslator(transpilerCtx),
                new GoStatementTranslator(transpilerCtx)
            )
        { }

        public override string HelperCodeResourcePath { get { return "Transpilers/Go/PastelHelper.go"; } }

        public override void GenerateCodeForFunction(TranspilerContext sb, FunctionDefinition funcDef, bool isStatic)
        {
            sb
                .Append("func fn_")
                .Append(funcDef.Name)
                .Append('(');
            for (int i = 0; i < funcDef.ArgNames.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb
                    .Append("v_")
                    .Append(funcDef.ArgNames[i].Value)
                    .Append(' ')
                    .Append(this.TypeTranspiler.TranslateType(funcDef.ArgTypes[i]));
            }
            sb.Append(')');
            if (!funcDef.ReturnType.IsVoid)
            {
                sb.Append(" ").Append(this.TypeTranspiler.TranslateType(funcDef.ReturnType));
            }
            sb.Append(" {\n");
            sb.TabDepth++;
            this.StatementTranslator.TranslateStatements(sb, funcDef.Code);
            sb.TabDepth--;
            sb.Append("}\n\n");
        }

        public override void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef)
        {
            sb
                .Append("type S_")
                .Append(structDef.NameToken.Value)
                .Append(" struct {\n");

            sb.TabDepth++;

            string[] fieldNames = Pastel.Generated.FunctionWrapper.PadStringsToSameLength(structDef.FieldNames.Select(n => n.Value).ToArray());
            for (int i = 0; i < fieldNames.Length; i++)
            {
                sb.Append(sb.CurrentTab);
                sb.Append("f_");
                sb.Append(fieldNames[i]);
                sb.Append(" ");
                sb.Append(TypeTranspiler.TranslateType(structDef.FieldTypes[i]));
                sb.Append('\n');
            }
            sb.TabDepth--;
            sb.Append("}\n");
        }
    }
}
