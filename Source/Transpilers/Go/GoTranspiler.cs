using Pastel.Parser.ParseNodes;
using System;
using System.Linq;

namespace Pastel.Transpilers.Go
{
    internal class GoTranspiler : AbstractTranspiler
    {
        public GoTranspiler(TranspilerContext transpilerCtx)
            : base(transpilerCtx)
        {
            this.TypeTranspiler = new GoTypeTranspiler();
            this.Exporter = new GoExporter();
            this.ExpressionTranslator = new GoExpressionTranslator(transpilerCtx.PastelContext);
        }

        public override string HelperCodeResourcePath { get { return "Transpilers/Go/PastelHelper.go"; } }

        public override void TranslateAssignment(TranspilerContext sb, Assignment assignment)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(assignment.Target));
            sb.Append(' ').Append(assignment.OpToken.Value).Append(' ');
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(assignment.Value));
            sb.Append('\n');
        }

        public override void TranslateBreak(TranspilerContext sb)
        {
            throw new NotImplementedException();
        }

        public override void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStatements(TranspilerContext sb, Statement[] statements)
        {
            for (int i = 0; i < statements.Length; i++)
            {
                TranslateStatement(sb, statements[i]);
            }
        }

        public override void TranslateExpressionAsStatement(TranspilerContext sb, Expression expression)
        {
            throw new NotImplementedException();
        }

        public override void TranslateIfStatement(TranspilerContext sb, IfStatement ifStatement)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("if ");
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(ifStatement.Condition));
            sb.Append(" {\n");
            sb.TabDepth++;
            TranslateStatements(sb, ifStatement.IfCode);
            sb.TabDepth--;
            sb.Append(sb.CurrentTab).Append("}");
            if (ifStatement.ElseCode != null && ifStatement.ElseCode.Length > 0)
            {
                sb.Append(" else {\n");
                sb.TabDepth++;
                TranslateStatements(sb, ifStatement.ElseCode);
                sb.TabDepth--;
                sb.Append(sb.CurrentTab).Append("}");
            }
            sb.Append("\n");
        }

        public override void TranslateReturnStatemnt(TranspilerContext sb, ReturnStatement returnStatement)
        {
            sb.Append(sb.CurrentTab).Append("return");
            if (returnStatement.Expression != null)
            {
                sb.Append(' ');
                sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(returnStatement.Expression));
            }
            sb.Append("\n");
        }

        public override void TranslateSwitchStatement(TranspilerContext sb, SwitchStatement switchStatement)
        {
            throw new NotImplementedException();
        }


        public override void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl)
        {
            sb
                .Append(sb.CurrentTab)
                .Append("var v_")
                .Append(varDecl.VariableNameToken.Value)
                .Append(' ')
                .Append(this.TypeTranspiler.TranslateType(varDecl.Type))
                .Append(" = ")
                .Append(this.ExpressionTranslator.TranslateExpressionAsString(varDecl.Value))
                .Append("\n");
        }

        public override void TranslateWhileLoop(TranspilerContext sb, WhileLoop whileLoop)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("for ");
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(whileLoop.Condition));
            sb.Append(" {\n");
            sb.TabDepth++;
            TranslateStatements(sb, whileLoop.Code);
            sb.TabDepth--;
            sb.Append(sb.CurrentTab).Append("}\n");
        }

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
            if (funcDef.ReturnType.RootValue != "void")
            {
                sb.Append(" ").Append(this.TypeTranspiler.TranslateType(funcDef.ReturnType));
            }
            sb.Append(" {\n");
            sb.TabDepth++;
            TranslateStatements(sb, funcDef.Code);
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

            string[] fieldNames = CodeUtil.PadStringsToSameLength(structDef.FieldNames.Select(n => n.Value));
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

            sb
                .Append("}\n")
                .Append("type PtrBox_")
                .Append(structDef.NameToken.Value)
                .Append(" struct {\n");
            sb.TabDepth++;
            sb
                .Append(sb.CurrentTab)
                .Append("o *S_")
                .Append(structDef.NameToken.Value)
                .Append("\n");
            sb.TabDepth--;
            sb
                .Append("}\n");
        }
    }
}
