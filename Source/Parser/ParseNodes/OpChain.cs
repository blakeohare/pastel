using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser.ParseNodes
{
    internal class OpChain : Expression
    {
        public Expression[] Expressions { get; set; }
        public Token[] Ops { get; set; }

        public OpChain(
            IList<Expression> expressions,
            IList<Token> ops) : base(expressions[0].FirstToken, expressions[0].Owner)
        {
            Expressions = expressions.ToArray();
            Ops = ops.ToArray();
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            Expression.ResolveNamesAndCullUnusedCodeInPlace(this.Expressions, resolver);
            string op = this.Ops[0].Value;
            Expression acc;

            // Don't do short-circuiting yet for && and ||
            if (op == "&&" || op == "||")
            {
                acc = this.Expressions[this.Expressions.Length - 1];
                for (int i = this.Expressions.Length - 2; i >= 0; i--)
                {
                    acc = new OpPair(this.Expressions[i], this.Ops[i], acc);
                }
            }
            else
            {
                acc = this.Expressions[0];
                for (int i = 1; i < this.Expressions.Length; i++)
                {
                    acc = new OpPair(acc, this.Ops[i - 1], this.Expressions[i]);
                }
            }

            return acc;
        }

        internal override InlineConstant DoConstantResolution(HashSet<string> cycleDetection, Resolver resolver)
        {
            for (int i = 0; i < Expressions.Length; ++i)
            {
                Expressions[i] = Expressions[i].DoConstantResolution(cycleDetection, resolver);
            }

            InlineConstant current = (InlineConstant)Expressions[0];
            for (int i = 1; i < Expressions.Length; ++i)
            {
                InlineConstant next = (InlineConstant)Expressions[i];
                string lookup = current.Type.RootValue + Ops[i - 1].Value + next.Type.RootValue;
                switch (lookup)
                {
                    case "int+int":
                        current = new InlineConstant(PType.INT, current.FirstToken, (int)current.Value + (int)next.Value, next.Owner);
                        break;
                    case "int-int":
                        current = new InlineConstant(PType.INT, current.FirstToken, (int)current.Value - (int)next.Value, next.Owner);
                        break;
                    case "int*int":
                        current = new InlineConstant(PType.INT, current.FirstToken, (int)current.Value * (int)next.Value, next.Owner);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            return current;
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            throw new InvalidOperationException();
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            throw new InvalidOperationException();
        }
    }
}
