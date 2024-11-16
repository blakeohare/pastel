using Pastel.Parser;
using Pastel.Parser.ParseNodes;
using System.Collections.Generic;

namespace Pastel.Transpilers.CSharp
{
    internal class CSharpTranspiler : CurlyBraceTranspiler
    {
        public CSharpTranspiler(TranspilerContext transpilerCtx)
            : base(transpilerCtx)
        {
            this.TypeTranspiler = new CSharpTypeTranspiler();
            this.Exporter = new CSharpExporter();
            this.ExpressionTranslator = new CSharpExpressionTranslator(transpilerCtx.PastelContext);
            this.StatementTranslator = new CSharpStatementTranslator(transpilerCtx);
        }

        public override string HelperCodeResourcePath { get { return "Transpilers/CSharp/PastelHelper.cs"; } }

        public override void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef)
        {
            PType[] types = structDef.FieldTypes;
            Token[] names = structDef.FieldNames;

            string name = structDef.NameToken.Value;
            List<string> lines = new List<string>();

            string defline = "public class " + name;

            lines.Add(defline);
            lines.Add("{");
            for (int i = 0; i < names.Length; ++i)
            {
                lines.Add("    public " + this.TypeTranspiler.TranslateType(types[i]) + " " + names[i].Value + ";");
            }
            lines.Add("");

            System.Text.StringBuilder constructorDeclaration = new System.Text.StringBuilder();
            constructorDeclaration.Append("    public ");
            constructorDeclaration.Append(name);
            constructorDeclaration.Append('(');
            for (int i = 0; i < types.Length; ++i)
            {
                if (i > 0) constructorDeclaration.Append(", ");
                constructorDeclaration.Append(this.TypeTranspiler.TranslateType(types[i]));
                constructorDeclaration.Append(' ');
                constructorDeclaration.Append(names[i].Value);
            }
            constructorDeclaration.Append(')');

            lines.Add(constructorDeclaration.ToString());
            lines.Add("    {");
            for (int i = 0; i < types.Length; ++i)
            {
                string fieldName = names[i].Value;
                lines.Add("        this." + fieldName + " = " + fieldName + ";");
            }
            lines.Add("    }");

            lines.Add("}");
            lines.Add("");

            // TODO: rewrite this function to use the string builder inline and use this.NL
            sb.Append(string.Join("\n", lines));
        }

        public override void GenerateCodeForFunction(TranspilerContext output, FunctionDefinition funcDef, bool isStatic)
        {
            PType returnType = funcDef.ReturnType;
            string funcName = funcDef.NameToken.Value;
            PType[] argTypes = funcDef.ArgTypes;
            Token[] argNames = funcDef.ArgNames;

            output.Append(output.CurrentTab);
            output.Append("public ");
            if (isStatic) output.Append("static ");
            output.Append(TypeTranspiler.TranslateType(returnType));
            output.Append(' ');
            output.Append(funcName);
            output.Append("(");
            for (int i = 0; i < argTypes.Length; ++i)
            {
                if (i > 0) output.Append(", ");
                output.Append(TypeTranspiler.TranslateType(argTypes[i]));
                output.Append(' ');
                output.Append(argNames[i].Value);
            }
            output.Append(")\n");
            output.Append(output.CurrentTab);
            output.Append("{\n");
            output.TabDepth++;
            this.StatementTranslator.TranslateStatements(output, funcDef.Code);
            output.TabDepth--;
            output.Append(output.CurrentTab);
            output.Append("}");
        }
    }
}
