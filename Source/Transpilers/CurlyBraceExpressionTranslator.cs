using Pastel.Parser.ParseNodes;

namespace Pastel.Transpilers
{
    internal abstract class CurlyBraceExpressionTranslator : AbstractExpressionTranslator
    {
        public CurlyBraceExpressionTranslator(TranspilerContext ctx) : base(ctx) { }

        public override StringBuffer TranslateBooleanConstant(bool value)
        {
            return StringBuffer
                .Of(value ? "true" : "false")
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateBoolToString(Expression value)
        {
            if (value is InlineConstant ic)
            {
                return StringBuffer
                    .Of("\"" + (bool)ic.Value + "\"")
                    .WithTightness(ExpressionTightness.ATOMIC);
            }

            return this.TranslateExpression(value)
                .EnsureGreaterTightness(ExpressionTightness.TERNARY)
                .Push(" ? \"true\" : \"false\"")
                .WithTightness(ExpressionTightness.TERNARY);
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
            return this.TranslateVariableName(funcRef.Function.NameToken.Value);
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

        public override StringBuffer TranslateOpPair(OpPair opPair)
        {
            StringBuffer leftSb = this.TranslateExpression(opPair.Left);
            StringBuffer rightSb = this.TranslateExpression(opPair.Right);
            ExpressionTightness opTightness = this.GetTightnessOfOp(opPair.Op);
            leftSb.EnsureTightness(opTightness);
            rightSb.EnsureGreaterTightness(opTightness);
            return leftSb
                .Push(" ")
                .Push(opPair.Op)
                .Push(" ")
                .Push(rightSb)
                .WithTightness(opTightness);
        }

        public override StringBuffer TranslateStringConstant(string value)
        {
            return StringBuffer
                .Of(CodeUtil.ConvertStringValueToCode(value))
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateVariable(Variable variable)
        {
            return this.TranslateVariableName(variable.Name);
        }
    }
}
