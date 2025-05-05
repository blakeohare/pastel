namespace Pastel.Parser.ParseNodes
{
    internal class ReturnStatement : Statement
    {
        public Expression Expression { get; set; }

        public ReturnStatement(Token returnToken, Expression expression) : base(returnToken)
        {
            this.Expression = expression;
        }

        public override Statement ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            if (this.Expression != null)
            {
                this.Expression = this.Expression.ResolveNamesAndCullUnusedCode(resolver);
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
                if (this.Expression != null)
                {
                    this.Expression = this.Expression.ResolveType(varScope, resolver);
                    if (!PType.CheckReturnType(resolver, fd.ReturnType, this.Expression.ResolvedType))
                    {
                        throw new ParserException(this.Expression.FirstToken, "This expression is not the expected return type of this function.");
                    }
                }
                else
                {
                    if (!fd.ReturnType.IsIdentical(resolver, PType.VOID))
                    {
                        throw new ParserException(this.FirstToken, "Must return a value in this function.");
                    }
                }
            }
            else // constructors
            {
                if (this.Expression == null)
                {
                    // This is fine.
                }
                else
                {
                    // This isn't.
                    throw new ParserException(this.FirstToken, "You cannot return a value from a constructor.");
                }
            }
        }

        internal override Statement ResolveWithTypeContext(Resolver resolver)
        {
            if (this.Expression != null)
            {
                this.Expression = this.Expression.ResolveWithTypeContext(resolver);
            }
            return this;
        }
    }
}
