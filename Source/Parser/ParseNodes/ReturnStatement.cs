namespace Pastel.Parser.ParseNodes
{
    internal class ReturnStatement : Executable
    {
        public Expression Expression { get; set; }

        public ReturnStatement(Token returnToken, Expression expression) : base(returnToken)
        {
            Expression = expression;
        }

        public override Executable ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            if (Expression != null)
            {
                Expression = Expression.ResolveNamesAndCullUnusedCode(compiler);
            }
            return this;
        }

        internal override void ResolveTypes(VariableScope varScope, PastelCompiler compiler)
        {
            // TODO: the variable scope should NOT be the messenger of this information.
            ICompilationEntity ce = varScope.RootFunctionOrConstructorDefinition;
            FunctionDefinition fd = ce as FunctionDefinition;

            if (fd != null)
            {
                if (Expression != null)
                {
                    Expression = Expression.ResolveType(varScope, compiler);
                    if (!PType.CheckReturnType(compiler, fd.ReturnType, Expression.ResolvedType))
                    {
                        throw new ParserException(Expression.FirstToken, "This expression is not the expected return type of this function.");
                    }
                }
                else
                {
                    if (!fd.ReturnType.IsIdentical(compiler, PType.VOID))
                    {
                        throw new ParserException(FirstToken, "Must return a value in this function.");
                    }
                }
            }
            else // constructors
            {
                if (Expression == null)
                {
                    // This is fine.
                }
                else
                {
                    // This isn't.
                    throw new ParserException(FirstToken, "You cannot return a value from a constructor.");
                }
            }
        }

        internal override Executable ResolveWithTypeContext(PastelCompiler compiler)
        {
            if (Expression != null)
            {
                Expression = Expression.ResolveWithTypeContext(compiler);
            }
            return this;
        }
    }
}
