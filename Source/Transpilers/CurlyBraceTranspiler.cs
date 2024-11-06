using Pastel.Parser.ParseNodes;

namespace Pastel.Transpilers
{
    internal abstract class CurlyBraceTranspiler : AbstractTranspiler
    {
        private bool IsAllmanBraces { get; set; }
        private bool IsKRBraces { get; set; }

        public CurlyBraceTranspiler(TranspilerContext transpilerCtx, bool isKRBraces)
            : base(transpilerCtx)
        {
            this.IsKRBraces = isKRBraces;
            this.IsAllmanBraces = !isKRBraces;
        }

        public override void TranslateAssignment(TranspilerContext sb, Assignment assignment)
        {
            sb.Append(sb.CurrentTab);
            this.TranslateExpression(sb, assignment.Target);
            sb.Append(' ');
            sb.Append(assignment.OpToken.Value);
            sb.Append(' ');
            this.TranslateExpression(sb, assignment.Value);
            sb.Append(";\n");
        }

        public override void TranslateBooleanConstant(TranspilerContext sb, bool value)
        {
            sb.Append(value ? "true" : "false");
        }

        public override void TranslateBooleanNot(TranspilerContext sb, UnaryOp unaryOp)
        {
            sb.Append('!');
            this.TranslateExpression(sb, unaryOp.Expression);
        }

        public override void TranslateBreak(TranspilerContext sb)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("break;\n");
        }

        public override void TranslateEmitComment(TranspilerContext sb, string value)
        {
            sb.Append("// ");
            sb.Append(value.Replace("\n", "\\n"));
        }

        public override void TranslateExpressionAsStatement(TranspilerContext sb, Expression expression)
        {
            sb.Append(sb.CurrentTab);
            this.TranslateExpression(sb, expression);
            sb.Append(";\n");
        }

        public override void TranslateFloatConstant(TranspilerContext sb, double value)
        {
            sb.Append(CodeUtil.FloatToString(value));
        }

        public override void TranslateFunctionInvocation(TranspilerContext sb, FunctionReference funcRef, Expression[] args)
        {
            this.TranslateFunctionReference(sb, funcRef);
            sb.Append('(');
            this.TranslateCommaDelimitedExpressions(sb, args);
            sb.Append(')');
        }

        public override void TranslateFunctionReference(TranspilerContext sb, FunctionReference funcRef)
        {
            sb.Append(funcRef.Function.NameToken.Value);
        }

        public override void TranslateIfStatement(TranspilerContext sb, IfStatement ifStatement)
        {
            this.TranslateIfStatementImpl(sb, ifStatement, true);
        }

        private void TranslateIfStatementImpl(TranspilerContext sb, IfStatement ifStatement, bool includeInitialTab)
        {
            if (includeInitialTab) sb.Append(sb.CurrentTab);
            sb.Append("if (");
            this.TranslateExpression(sb, ifStatement.Condition);
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

        public override void TranslateInlineIncrement(TranspilerContext sb, Expression innerExpression, bool isPrefix, bool isAddition)
        {
            if (isPrefix) sb.Append(isAddition ? "++" : "--");
            this.TranslateExpression(sb, innerExpression);
            if (!isPrefix) sb.Append(isAddition ? "++" : "--");
        }

        public override void TranslateIntegerConstant(TranspilerContext sb, int value)
        {
            sb.Append(value.ToString());
        }

        public override void TranslateNegative(TranspilerContext sb, UnaryOp unaryOp)
        {
            sb.Append('-');
            this.TranslateExpression(sb, unaryOp.Expression);
        }

        public override void TranslateOpChain(TranspilerContext sb, OpChain opChain)
        {
            // Need to do something about these parenthesis.
            sb.Append('(');
            for (int i = 0; i < opChain.Expressions.Length; ++i)
            {
                if (i > 0)
                {
                    sb.Append(' ');
                    sb.Append(opChain.Ops[i - 1].Value);
                    sb.Append(' ');
                }
                this.TranslateExpression(sb, opChain.Expressions[i]);
            }
            sb.Append(')');
        }

        public override void TranslateReturnStatemnt(TranspilerContext sb, ReturnStatement returnStatement)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("return");
            if (returnStatement.Expression != null)
            {
                sb.Append(' ');
                this.TranslateExpression(sb, returnStatement.Expression);
            }
            sb.Append(";\n");
        }

        public override void TranslateStringConstant(TranspilerContext sb, string value)
        {
            sb.Append(CodeUtil.ConvertStringValueToCode(value));
        }

        public override void TranslateSwitchStatement(TranspilerContext sb, SwitchStatement switchStatement)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("switch (");
            this.TranslateExpression(sb, switchStatement.Condition);
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
                        this.TranslateExpression(sb, c);
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

        public override void TranslateVariable(TranspilerContext sb, Variable variable)
        {
            sb.Append(variable.Name);
        }

        public override void TranslateWhileLoop(TranspilerContext sb, WhileLoop whileLoop)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("while (");
            this.TranslateExpression(sb, whileLoop.Condition);
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
