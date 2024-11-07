using System.Collections.Generic;
using System.Text;

namespace Pastel.Transpilers
{
    public enum ExpressionTightness
    {
        UNKNOWN = -1,
        TERNARY = 10,
        SUFFIX_SEQUENCE = 20,
        ATOMIC = 30,
    }

    internal class StringBuffer
    {
        public int Tightness { get; set; } = (int)ExpressionTightness.UNKNOWN;
        public bool IsLeaf { get { return this.Value != null; } }
        public string? Value { get; set; }
        public StringBuffer? Left { get; set; }
        public StringBuffer? Right { get; set; }

        public static StringBuffer Create(string value)
        {
            return new StringBuffer() { Value = value };
        }

        public StringBuffer WithTightness(ExpressionTightness tightness)
        {
            this.Tightness = (int)tightness;
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
            return this;
        }

        public StringBuffer Wrap(string prefix, string suffix)
        {
            return this
                .Prepend(new StringBuffer() { Value = prefix })
                .Push(new StringBuffer() { Value = suffix });
        }

        public static string Flatten(StringBuffer b)
        {
            if (b == null) return "";
            if (b.IsLeaf) return b.Value!;
            StringBuilder sb = new StringBuilder();
            Stack<StringBuffer> stack = new Stack<StringBuffer>();
            stack.Push(b);
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
