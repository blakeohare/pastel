using Pastel.Parser;
using Pastel.Parser.ParseNodes;
using System.Linq;

namespace Pastel.Transpilers.Java
{
    internal class JavaTranspiler : CurlyBraceTranspiler
    {
        public JavaTranspiler(TranspilerContext transpilerCtx)
            : base(
                transpilerCtx,
                new JavaExporter(),
                new JavaTypeTranspiler(),
                new JavaExpressionTranslator(transpilerCtx),
                new JavaStatementTranslator(transpilerCtx)
            )
        { }

        public override string HelperCodeResourcePath { get { return "Transpilers/Java/PastelHelper.java"; } }

        private JavaTypeTranspiler JavaTypeTranspiler { get { return (JavaTypeTranspiler)TypeTranspiler; } }

        public override void GenerateCodeForFunction(TranspilerContext sb, FunctionDefinition funcDef, bool isStatic)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("public static ");
            sb.Append(TypeTranspiler.TranslateType(funcDef.ReturnType));
            sb.Append(' ');
            sb.Append(funcDef.NameToken.Value);
            sb.Append('(');
            Token[] argNames = funcDef.ArgNames;
            PType[] argTypes = funcDef.ArgTypes;
            for (int i = 0; i < argTypes.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(TypeTranspiler.TranslateType(argTypes[i]));
                sb.Append(' ');
                sb.Append(argNames[i].Value);
            }
            sb.Append(") {\n");
            sb.TabDepth++;
            this.StatementTranslator.TranslateStatements(sb, funcDef.Code);
            sb.TabDepth--;
            sb.Append(sb.CurrentTab);
            sb.Append("}\n");
        }

        public override void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef)
        {
            string[] names = structDef.FieldNames.Select(token => token.Value).ToArray();
            string[] types = structDef.FieldTypes.Select(type => TypeTranspiler.TranslateType(type)).ToArray();

            string name = structDef.NameToken.Value;

            sb.Append("public class ");
            sb.Append(name);
            sb.Append(" {\n");
            for (int i = 0; i < names.Length; ++i)
            {
                sb.Append("  public ");
                sb.Append(types[i]);
                sb.Append(' ');
                sb.Append(names[i]);
                sb.Append(";\n");
            }

            sb.Append("  public static final ");
            sb.Append(name);
            sb.Append("[] EMPTY_ARRAY = new ");
            sb.Append(name);
            sb.Append("[0];\n");

            if (CrayonHacks.IsJavaValueStruct(structDef))
            {
                // The overhead of having extra fields on each Value is much less than the overhead
                // of Java's casting. Particularly on Android.
                sb.Append("  public int intValue;\n");
            }

            sb.Append("\n  public ");
            sb.Append(structDef.NameToken.Value);
            sb.Append('(');
            for (int i = 0; i < names.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(types[i]);
                sb.Append(' ');
                sb.Append(names[i]);
            }
            sb.Append(") {\n");
            for (int i = 0; i < names.Length; ++i)
            {
                sb.Append("    this.");
                sb.Append(names[i]);
                sb.Append(" = ");
                sb.Append(names[i]);
                sb.Append(";\n");
            }
            sb.Append("  }");

            if (CrayonHacks.IsJavaValueStruct(structDef))
            {
                sb.Append("\n\n");
                sb.Append("  public Value(int intValue) {\n");
                sb.Append("    this.type = 3;\n");
                sb.Append("    this.intValue = intValue;\n");
                sb.Append("    this.internalValue = intValue;\n");
                sb.Append("  }\n\n");
                sb.Append("  public Value(boolean boolValue) {\n");
                sb.Append("    this.type = 2;\n");
                sb.Append("    this.intValue = boolValue ? 1 : 0;\n");
                sb.Append("    this.internalValue = boolValue;\n");
                sb.Append("  }");
            }

            sb.Append("\n}");
        }
    }
}
