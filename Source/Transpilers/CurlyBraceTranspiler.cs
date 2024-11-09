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
                .Push(this.TranslateExpression(unaryOp.Expression).EnsureTightness(ExpressionTightness.UNARY_PREFIX))
                .WithTightness(ExpressionTightness.UNARY_PREFIX);
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
            return StringBuffer
                .Of(CodeUtil.FloatToString(value))
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateFunctionInvocation(FunctionReference funcRef, Expression[] args)
        {
            return this.TranslateFunctionReference(funcRef)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("(")
                .Push(this.TranslateCommaDelimitedExpressions(args))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateFunctionReference(FunctionReference funcRef)
        {
            return StringBuffer
                .Of(funcRef.Function.NameToken.Value)
                .WithTightness(ExpressionTightness.ATOMIC);
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
            StringBuffer root = this.TranslateExpression(innerExpression);
            string op = isAddition ? "++" : "--";
            if (isPrefix)
            {
                return root
                    .EnsureTightness(ExpressionTightness.UNARY_PREFIX)
                    .Prepend(op)
                    .WithTightness(ExpressionTightness.UNARY_PREFIX);
            }
            else
            {
                return root
                    .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                    .Push(op)
                    .WithTightness(ExpressionTightness.UNARY_SUFFIX);
            }
        }

        public override StringBuffer TranslateIntegerConstant(int value)
        {
            return StringBuffer
                .Of(value.ToString())
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateNegative(UnaryOp unaryOp)
        {
            return StringBuffer
                .Of("-")
                .Push(this.TranslateExpression(unaryOp.Expression).EnsureTightness(ExpressionTightness.UNARY_PREFIX))
                .WithTightness(ExpressionTightness.UNARY_PREFIX);
        }

        private ExpressionTightness GetTightnessOfOp(string op)
        {
            switch (op)
            {
                case "&&":
                case "||":
                    return ExpressionTightness.BOOLEAN_LOGIC;

                case "+":
                case "-":
                    return ExpressionTightness.ADDITION;

                case "&":
                case "|":
                case "^":
                    return ExpressionTightness.BITWISE;

                case "<<":
                case ">>":
                    return ExpressionTightness.BITSHIFT;

                case "*":
                case "/":
                case "%":
                    return ExpressionTightness.MULTIPLICATION;

                case "==":
                case "!=":
                    return ExpressionTightness.EQUALITY;

                case "<":
                case ">":
                case ">=":
                case "<=":
                    return ExpressionTightness.INEQUALITY;

                default:
                    throw new System.NotImplementedException();
            }
        }

        public override StringBuffer TranslateOpChain(OpChain opChain)
        {
            StringBuffer acc;
            string firstOp = opChain.Ops[0].Value;
            ExpressionTightness opTightness = GetTightnessOfOp(firstOp);
            int expressionLength = opChain.Expressions.Length;
            int opLength = expressionLength - 1;
            bool isShortCircuit = false;
            if (firstOp == "&&" || firstOp == "||")
            {
                bool allSame = true;
                for (int i = 1; i < opLength; i++)
                {
                    if (opChain.Ops[i].Value != firstOp)
                    {
                        allSame = false;
                        break;
                    }
                }

                if (!allSame) isShortCircuit = true;
            }

            if (isShortCircuit)
            {
                // For shortcircuit operators, paren wrapping should start from the back.
                acc = this.TranslateExpression(opChain.Expressions[expressionLength - 1]);
                for (int i = expressionLength - 2; i >= 0; i--)
                {
                    string op = opChain.Ops[i].Value;
                    StringBuffer next = this.TranslateExpression(opChain.Expressions[i])
                        .EnsureGreaterTightness(ExpressionTightness.BOOLEAN_LOGIC);

                    acc = next
                        .Push(" ")
                        .Push(op)
                        .Push(" ")
                        .Push(acc.EnsureGreaterTightness(opTightness))
                        .WithTightness(opTightness);
                }
            }
            else
            {
                acc = this.TranslateExpression(opChain.Expressions[0]);
                for (int i = 1; i < expressionLength; i++)
                {
                    string op = opChain.Ops[i - 1].Value;
                    acc
                        .EnsureTightness(opTightness)
                        .Push(" ")
                        .Push(op)
                        .Push(" ")
                        .Push(this.TranslateExpression(opChain.Expressions[i]).EnsureGreaterTightness(opTightness))
                        .WithTightness(opTightness);
                }
            }

            return acc;
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
            return StringBuffer
                .Of(CodeUtil.ConvertStringValueToCode(value))
                .WithTightness(ExpressionTightness.ATOMIC);
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
            return StringBuffer
                .Of(variable.Name)
                .WithTightness(ExpressionTightness.ATOMIC);
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
