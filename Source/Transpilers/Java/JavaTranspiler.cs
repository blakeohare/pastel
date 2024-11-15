using Pastel.Parser;
using Pastel.Parser.ParseNodes;
using System.Linq;

namespace Pastel.Transpilers.Java
{
    internal class JavaTranspiler : CurlyBraceTranspiler
    {
        public JavaTranspiler(TranspilerContext transpilerCtx)
            : base(transpilerCtx, true)
        {
            this.TypeTranspiler = new JavaTypeTranspiler();
            this.Exporter = new JavaExporter();
            this.ExpressionTranslator = new JavaExpressionTranslator(transpilerCtx.PastelContext);
        }

        public override string HelperCodeResourcePath { get { return "Transpilers/Java/PastelHelper.java"; } }

        private JavaTypeTranspiler JavaTypeTranspiler { get { return (JavaTypeTranspiler)TypeTranspiler; } }

        public override void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut)
        {
            PType[] dictTypes = dictionary.ResolvedType.Generics;
            PType keyType = dictTypes[0];
            PType valueType = dictTypes[1];
            bool keyTypeIsBoxed = JavaTypeTranspiler.IsJavaPrimitiveTypeBoxed(keyType);
            bool keyExpressionIsSimple = key is Variable || key is InlineConstant;
            string keyVar = null;
            if (!keyExpressionIsSimple)
            {
                keyVar = "_PST_dictKey" + sb.SwitchCounter++;
                sb.Append(sb.CurrentTab);
                sb.Append(this.TypeTranspiler.TranslateType(keyType));
                sb.Append(' ');
                sb.Append(keyVar);
                sb.Append(" = ");
                sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(key));
                sb.Append(";\n");
            }

            string lookupVar = "_PST_dictLookup" + sb.SwitchCounter++;
            sb.Append(sb.CurrentTab);
            sb.Append(JavaTypeTranspiler.TranslateJavaNestedType(valueType));
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
            sb.Append(TypeTranspiler.TranslateType(varDecl.Type));
            sb.Append(' ');
            sb.Append(varDecl.VariableNameToken.Value);
            if (varDecl.Value != null)
            {
                sb.Append(" = ");
                sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(varDecl.Value));
            }
            sb.Append(";\n");
        }

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
            TranslateStatements(sb, funcDef.Code);
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
