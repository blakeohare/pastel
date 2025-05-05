using System;

namespace Pastel.Parser.ParseNodes
{
    internal class ExpressionAsStatement : Statement
    {
        public Expression Expression { get; set; }

        public ExpressionAsStatement(Expression expression) : base(expression.FirstToken)
        {
            this.Expression = expression;
        }

        internal Statement[]? ImmediateResolveMaybe(PastelParser parser)
        {
            if (this.Expression is FunctionInvocation funcInvoke &&
                funcInvoke.Root is CompileTimeFunctionReference compTimeFn)
            {
                Expression[] args = funcInvoke.Args;
                int argc = args.Length;
                string funcName = compTimeFn.NameToken.Value;
                switch (funcName)
                {
                    case "import":
                        if (argc != 1 || !(args[0] is InlineConstant ic) || !(ic.Value is string path))
                        {
                            throw new UNTESTED_ParserException(
                                funcInvoke.FirstToken,
                                "@import() expects 1 string constant argument.");
                        }

                        return parser.StatementParser.ParseImportedCode(compTimeFn.FirstToken, path);

                    default:
                        throw new UNTESTED_ParserException(
                            funcInvoke.FirstToken,
                            "Unknown compile-time function: @" + funcName);
                }
            }

            return null;
        }

        public override Statement ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            this.Expression = this.Expression.ResolveNamesAndCullUnusedCode(resolver);
            return this;
        }

        internal override void ResolveTypes(VariableScope varScope, Resolver resolver)
        {
            this.Expression = this.Expression.ResolveType(varScope, resolver);
        }

        internal override Statement ResolveWithTypeContext(Resolver resolver)
        {
            this.Expression = this.Expression.ResolveWithTypeContext(resolver);
            return this;
        }
    }
}
