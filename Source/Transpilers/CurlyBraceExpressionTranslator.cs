using Pastel.Parser.ParseNodes;

namespace Pastel.Transpilers
{
    internal abstract class CurlyBraceExpressionTranslator : AbstractExpressionTranslator
    {
        public CurlyBraceExpressionTranslator(PastelContext ctx) : base(ctx) { }

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

        public override StringBuffer TranslateEmitComment(string value)
        {
            return StringBuffer
                .Of("// ")
                .Push(value.Replace("\n", "\\n"));
        }

        public override StringBuffer TranslateFloatConstant(double value)
        {
            return StringBuffer
                .Of(CodeUtil.FloatToString(value))
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateFunctionInvocation(FunctionReference funcRef, Expression[] args)
        {
            StringBuffer buf = this.TranslateFunctionReference(funcRef)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("(");

            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0) buf.Push(", ");
                buf.Push(this.TranslateExpression(args[i]));
            }
            return buf
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateFunctionReference(FunctionReference funcRef)
        {
            return StringBuffer
                .Of(funcRef.Function.NameToken.Value)
                .WithTightness(ExpressionTightness.ATOMIC);
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

        public override StringBuffer TranslateStringConstant(string value)
        {
            return StringBuffer
                .Of(CodeUtil.ConvertStringValueToCode(value))
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateVariable(Variable variable)
        {
            return StringBuffer
                .Of(variable.Name)
                .WithTightness(ExpressionTightness.ATOMIC);
        }

    }
}
