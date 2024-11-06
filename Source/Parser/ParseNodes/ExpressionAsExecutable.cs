using System;

namespace Pastel.Parser.ParseNodes
{
    internal class ExpressionAsExecutable : Executable
    {
        public Expression Expression { get; set; }

        public ExpressionAsExecutable(Expression expression) : base(expression.FirstToken)
        {
            Expression = expression;
        }

        internal Executable[] ImmediateResolveMaybe(PastelParser parser)
        {
            if (Expression is FunctionInvocation)
            {
                if (((FunctionInvocation)Expression).Root is CompileTimeFunctionReference)
                {
                    FunctionInvocation functionInvocation = (FunctionInvocation)Expression;
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

        public override Executable ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            Expression = Expression.ResolveNamesAndCullUnusedCode(compiler);
            return this;
        }

        internal override void ResolveTypes(VariableScope varScope, PastelCompiler compiler)
        {
            Expression = Expression.ResolveType(varScope, compiler);
        }

        internal override Executable ResolveWithTypeContext(PastelCompiler compiler)
        {
            Expression = Expression.ResolveWithTypeContext(compiler);
            return this;
        }
    }
}
