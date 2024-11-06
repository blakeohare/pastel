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

        internal Statement[] ImmediateResolveMaybe(PastelParser parser)
        {
            if (this.Expression is FunctionInvocation)
            {
                if (((FunctionInvocation)this.Expression).Root is CompileTimeFunctionReference)
                {
                    FunctionInvocation functionInvocation = (FunctionInvocation)this.Expression;
                    CompileTimeFunctionReference compileTimeFunction = (CompileTimeFunctionReference)functionInvocation.Root;
                    switch (compileTimeFunction.NameToken.Value)
                    {
                        case "import":
                            string path = ((InlineConstant)functionInvocation.Args[0]).Value.ToString();
                            return parser.StatementParser.ParseImportedCode(compileTimeFunction.NameToken, path);

                        default:
                            throw new NotImplementedException();
                    }
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
