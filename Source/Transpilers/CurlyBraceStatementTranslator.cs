using Pastel.Parser.ParseNodes;

namespace Pastel.Transpilers
{
    internal abstract class CurlyBraceStatementTranslator : AbstractStatementTranslator
    {
        private bool IsAllmanBraces { get { return !this.IsKRBraces; } }
        private bool IsKRBraces { get { return this.transpilerCtx.PastelContext.Language != Language.CSHARP; } }

        public CurlyBraceStatementTranslator(TranspilerContext ctx) 
            : base(ctx) 
        { }

        public override void TranslateAssignment(TranspilerContext sb, Assignment assignment)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(assignment.Target));
            sb.Append(' ');
            sb.Append(assignment.OpToken.Value);
            sb.Append(' ');
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(assignment.Value));
            sb.Append(";\n");
        }

        public override void TranslateBreak(TranspilerContext sb)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("break;\n");
        }

        public override void TranslateExpressionAsStatement(TranspilerContext sb, Expression expression)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(expression));
            sb.Append(";\n");
        }

        public override void TranslateIfStatement(TranspilerContext sb, IfStatement ifStatement)
        {
            this.TranslateIfStatementImpl(sb, ifStatement, true);
        }

        private void TranslateIfStatementImpl(TranspilerContext sb, IfStatement ifStatement, bool includeInitialTab)
        {
            if (includeInitialTab) sb.Append(sb.CurrentTab);
            sb.Append("if (");
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(ifStatement.Condition));
            if (this.IsKRBraces)
            {
                sb.Append(") {\n");
            }
            else
            {
                sb.Append(")\n");
                sb.Append(sb.CurrentTab);
                sb.Append("{\n");
            }

            sb.TabDepth++;
            this.TranslateStatements(sb, ifStatement.IfCode);
            sb.TabDepth--;
            sb.Append(sb.CurrentTab);
            sb.Append("}");

            if (ifStatement.ElseCode.Length > 0)
            {
                bool isIfElseChain = ifStatement.ElseCode.Length == 1 && ifStatement.ElseCode[0] is IfStatement;

                if (this.IsKRBraces)
                {
                    sb.Append(" else ");

                    if (!isIfElseChain)
                    {
                        sb.Append("{\n");
                    }
                }
                else
                {
                    sb.Append('\n');
                    sb.Append(sb.CurrentTab);
                    sb.Append("else");

                    if (isIfElseChain)
                    {
                        sb.Append(' ');
                    }
                    else
                    {
                        sb.Append('\n');

                        sb.Append(sb.CurrentTab);
                        sb.Append("{\n");
                    }
                }

                if (isIfElseChain)
                {
                    this.TranslateIfStatementImpl(sb, (IfStatement)ifStatement.ElseCode[0], false);
                }
                else
                {
                    sb.TabDepth++;
                    this.TranslateStatements(sb, ifStatement.ElseCode);
                    sb.TabDepth--;

                    sb.Append(sb.CurrentTab);
                    sb.Append("}");
                }
            }

            if (includeInitialTab) sb.Append("\n");
        }

        public override void TranslateReturnStatemnt(TranspilerContext sb, ReturnStatement returnStatement)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("return");
            if (returnStatement.Expression != null)
            {
                sb.Append(' ');
                sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(returnStatement.Expression));
            }
            sb.Append(";\n");
        }

        public override void TranslateSwitchStatement(TranspilerContext sb, SwitchStatement switchStatement)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("switch (");
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(switchStatement.Condition));
            sb.Append(")");
            if (this.IsKRBraces)
            {
                sb.Append(" {");
            }
            else
            {
                sb.Append("\n");
                sb.Append(sb.CurrentTab);
                sb.Append('{');
            }
            sb.Append("\n");

            sb.TabDepth++;

            foreach (SwitchStatement.SwitchChunk chunk in switchStatement.Chunks)
            {
                for (int i = 0; i < chunk.Cases.Length; ++i)
                {
                    sb.Append(sb.CurrentTab);
                    Expression c = chunk.Cases[i];
                    if (c == null)
                    {
                        sb.Append("default:");
                    }
                    else
                    {
                        sb.Append("case ");
                        sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(c));
                        sb.Append(':');
                    }
                    sb.Append("\n");
                    sb.TabDepth++;
                    this.TranslateStatements(sb, chunk.Code);
                    sb.TabDepth--;
                }
            }

            sb.TabDepth--;
            sb.Append(sb.CurrentTab);
            sb.Append("}\n");
        }

        public override void TranslateWhileLoop(TranspilerContext sb, WhileLoop whileLoop)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("while (");
            sb.Append(this.ExpressionTranslator.TranslateExpressionAsString(whileLoop.Condition));
            sb.Append(')');
            if (this.IsKRBraces)
            {
                sb.Append(" {\n");
            }
            else
            {
                sb.Append("\n");
                sb.Append(sb.CurrentTab);
                sb.Append("{\n");
            }
            sb.TabDepth++;
            this.TranslateStatements(sb, whileLoop.Code);
            sb.TabDepth--;
            sb.Append(sb.CurrentTab);
            sb.Append("}\n");
        }
    }
}
