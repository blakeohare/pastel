using System.Collections.Generic;
using System.Text;

namespace Pastel.Transpilers
{
    public enum ExpressionTightness
    {
        UNKNOWN = -1,
        TERNARY = 10,
        PYTHON_NOT = 15,
        PYTHON_OR = 17,
        PYTHON_AND = 18,
        PYTHON_COMPARE = 19,
        BOOLEAN_LOGIC = 20,
        PYTHON_BITWISE_OR = 25,
        PYTHON_BITWISE_AND = 26,
        PYTHON_BITWISE_XOR = 27,
        BITWISE = 30,
        EQUALITY = 40,
        INEQUALITY = 50,
        PHP_STRING_CONCAT = 55,
        BITSHIFT = 60,
        ADDITION = 70,
        MULTIPLICATION = 80,
        UNARY_PREFIX = 90,
        UNARY_SUFFIX = 91,
        PYTHON_EXPONENT = 95,
        SUFFIX_SEQUENCE = 100,
        ATOMIC = 999,
    }

    internal class StringBuffer
    {
        public int Tightness { get; set; } = (int)ExpressionTightness.UNKNOWN;
        public bool IsLeaf { get { return this.Value != null; } }
        public string? Value { get; set; }
        public StringBuffer? Left { get; set; }
        public StringBuffer? Right { get; set; }

        public static StringBuffer Of(string value)
        {
            return new StringBuffer() { Value = value };
        }

        public StringBuffer WithTightness(ExpressionTightness tightness)
        {
            this.Tightness = (int)tightness;
            return this;
        }

        public StringBuffer EnsureTightness(ExpressionTightness tightness) { return this.EnsureTightnessImpl(tightness, true); }
        public StringBuffer EnsureGreaterTightness(ExpressionTightness tightness) { return this.EnsureTightnessImpl(tightness, false); }

        private StringBuffer EnsureTightnessImpl(ExpressionTightness tightness, bool tieOkay)
        {
            if (this.Tightness > (int)tightness) return this;
            if (tieOkay && this.Tightness == (int)tightness) return this;

            this.Prepend("(").Push(")");
            this.Tightness = (int)ExpressionTightness.ATOMIC;

            return this;
        }

        public StringBuffer Push(string value) { return this.Push(new StringBuffer() { Value = value }); }
        public StringBuffer Push(StringBuffer value)
        {
            if (this.Value == null)
            {
                this.Right = new StringBuffer() { Left = this.Right, Right = value, };
            }
            else
            {
                this.Left = new StringBuffer() { Value = this.Value };
                this.Right = value;
                this.Value = null;
            }
            this.Tightness = (int)ExpressionTightness.UNKNOWN;
            return this;
        }

        public StringBuffer Prepend(string value) { return this.Prepend(new StringBuffer() { Value = value }); }
        public StringBuffer Prepend(StringBuffer value)
        {
            if (this.Value == null)
            {
                this.Left = new StringBuffer() { Left = value, Right = this.Left };
            }
            else
            {
                this.Right = new StringBuffer() { Value = this.Value };
                this.Value = null;
                this.Left = value;
            }
            this.Tightness = (int)ExpressionTightness.UNKNOWN;
            return this;
        }

        public string Flatten()
        {
            if (this.IsLeaf) return this.Value!;
            StringBuilder sb = new StringBuilder();
            Stack<StringBuffer> stack = new Stack<StringBuffer>();
            stack.Push(this);
            while (stack.Count > 0)
            {
                StringBuffer current = stack.Pop();
                if (current.IsLeaf) sb.Append(current.Value);
                else
                {
                    stack.Push(current.Right!);
                    stack.Push(current.Left!);
                }
            }
            return sb.ToString();
        }
    }
}
