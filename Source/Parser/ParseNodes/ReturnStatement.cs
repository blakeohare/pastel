namespace Pastel.Parser.ParseNodes
{
    internal class ReturnStatement : Statement
    {
        public Expression Expression { get; set; }

        public ReturnStatement(Token returnToken, Expression expression) : base(returnToken)
        {
            Expression = expression;
        }

        public override Statement ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            if (Expression != null)
            {
                Expression = Expression.ResolveNamesAndCullUnusedCode(resolver);
            }
            return this;
        }

        internal override void ResolveTypes(VariableScope varScope, Resolver resolver)
        {
            // TODO: the variable scope should NOT be the messenger of this information.
            ICompilationEntity ce = varScope.RootFunctionOrConstructorDefinition;
            FunctionDefinition fd = ce as FunctionDefinition;

            if (fd != null)
            {
                if (Expression != null)
                {
                    Expression = Expression.ResolveType(varScope, resolver);
                    if (!PType.CheckReturnType(resolver, fd.ReturnType, Expression.ResolvedType))
                    {
                        throw new ParserException(Expression.FirstToken, "This expression is not the expected return type of this function.");
                    }
                }
                else
                {
                    if (!fd.ReturnType.IsIdentical(resolver, PType.VOID))
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

        internal override Statement ResolveWithTypeContext(Resolver resolver)
        {
            if (Expression != null)
            {
                Expression = Expression.ResolveWithTypeContext(resolver);
            }
            return this;
        }
    }
}
