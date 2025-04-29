using Pastel.Parser.ParseNodes;
using System;

namespace Pastel.Transpilers.Go
{
    internal class GoStatementTranslator : AbstractStatementTranslator
    {
        public GoStatementTranslator(TranspilerContext ctx) : base(ctx) { }


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
            sb.Append(sb.CurrentTab);
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(expression));
            sb.Append("\n");
        }

        public override void TranslateIfStatement(TranspilerContext sb, IfStatement ifStatement)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("if ");
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(ifStatement.Condition));
            sb.Append(" {\n");
            sb.TabDepth++;
            this.TranslateStatements(sb, ifStatement.IfCode);
            sb.TabDepth--;
            sb.Append(sb.CurrentTab).Append("}");
            if (ifStatement.ElseCode != null && ifStatement.ElseCode.Length > 0)
            {
                sb.Append(" else {\n");
                sb.TabDepth++;
                this.TranslateStatements(sb, ifStatement.ElseCode);
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
    }
}
