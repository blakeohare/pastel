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
            sb.Append(this.TranslateExpressionAsString(assignment.Target));
            sb.Append(' ');
            sb.Append(assignment.OpToken.Value);
            sb.Append(' ');
            sb.Append(this.TranslateExpressionAsString(assignment.Value));
            sb.Append(";\n");
        }

        public override StringBuffer TranslateBooleanConstant(bool value)
        {
            return StringBuffer.Of(value ? "true" : "false");
        }

        public override StringBuffer TranslateBooleanNot(UnaryOp unaryOp)
        {
            return StringBuffer
                .Of("!")
                .Push(this.TranslateExpression(unaryOp.Expression));
        }

        public override void TranslateBreak(TranspilerContext sb)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("break;\n");
        }

        public override StringBuffer TranslateEmitComment(string value)
        {
            return StringBuffer
                .Of("// ")
                .Push(value.Replace("\n", "\\n"));
        }

        public override void TranslateExpressionAsStatement(TranspilerContext sb, Expression expression)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(this.TranslateExpressionAsString(expression));
            sb.Append(";\n");
        }

        public override StringBuffer TranslateFloatConstant(double value)
        {
            return StringBuffer.Of(CodeUtil.FloatToString(value));
        }

        public override StringBuffer TranslateFunctionInvocation(FunctionReference funcRef, Expression[] args)
        {
            return this.TranslateFunctionReference(funcRef)
                .Push('(')
                .Push(this.TranslateCommaDelimitedExpressions(args))
                .Push(')');
        }

        public override StringBuffer TranslateFunctionReference(FunctionReference funcRef)
        {
            return StringBuffer.Of(funcRef.Function.NameToken.Value);
        }

        public override void TranslateIfStatement(TranspilerContext sb, IfStatement ifStatement)
        {
            this.TranslateIfStatementImpl(sb, ifStatement, true);
        }

        private void TranslateIfStatementImpl(TranspilerContext sb, IfStatement ifStatement, bool includeInitialTab)
        {
            if (includeInitialTab) sb.Append(sb.CurrentTab);
            sb.Append("if (");
            sb.Append(this.TranslateExpressionAsString(ifStatement.Condition));
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

        public override StringBuffer TranslateInlineIncrement(Expression innerExpression, bool isPrefix, bool isAddition)
        {
            StringBuffer buf = StringBuffer.Of("");
            if (isPrefix) buf.Push(isAddition ? "++" : "--");
            buf.Push(this.TranslateExpression(innerExpression));
            if (!isPrefix) buf.Push(isAddition ? "++" : "--");
            return buf;
        }

        public override StringBuffer TranslateIntegerConstant(int value)
        {
            return StringBuffer.Of(value.ToString());
        }

        public override StringBuffer TranslateNegative(UnaryOp unaryOp)
        {
            return StringBuffer
                .Of("-")
                .Push(this.TranslateExpression(unaryOp.Expression));
        }

        public override StringBuffer TranslateOpChain(OpChain opChain)
        {
            StringBuffer buf = StringBuffer.Of("(");
            for (int i = 0; i < opChain.Expressions.Length; ++i)
            {
                if (i > 0)
                {
                    buf
                        .Push(' ')
                        .Push(opChain.Ops[i - 1].Value)
                        .Push(' ');
                }
                buf.Push(this.TranslateExpression(opChain.Expressions[i]));
            }
            return buf.Push(')');
        }

        public override void TranslateReturnStatemnt(TranspilerContext sb, ReturnStatement returnStatement)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("return");
            if (returnStatement.Expression != null)
            {
                sb.Append(' ');
                sb.Append(this.TranslateExpressionAsString(returnStatement.Expression));
            }
            sb.Append(";\n");
        }

        public override StringBuffer TranslateStringConstant(string value)
        {
            return StringBuffer.Of(CodeUtil.ConvertStringValueToCode(value));
        }

        public override void TranslateSwitchStatement(TranspilerContext sb, SwitchStatement switchStatement)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("switch (");
            sb.Append(this.TranslateExpressionAsString(switchStatement.Condition));
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
                        sb.Append(this.TranslateExpressionAsString(c));
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

        public override StringBuffer TranslateVariable(Variable variable)
        {
            return StringBuffer.Of(variable.Name);
        }

        public override void TranslateWhileLoop(TranspilerContext sb, WhileLoop whileLoop)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("while (");
            sb.Append(this.TranslateExpressionAsString(whileLoop.Condition));
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
