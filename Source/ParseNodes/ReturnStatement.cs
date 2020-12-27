namespace Pastel.Nodes
{
    internal class ReturnStatement : Executable
    {
        public Expression Expression { get; set; }

        public ReturnStatement(Token returnToken, Expression expression) : base(returnToken)
        {
            this.Expression = expression;
        }

        public override Executable ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            if (this.Expression != null)
            {
                this.Expression = this.Expression.ResolveNamesAndCullUnusedCode(compiler);
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
                if (this.Expression != null)
                {
                    this.Expression = this.Expression.ResolveType(varScope, compiler);
                    if (!PType.CheckReturnType(compiler, fd.ReturnType, this.Expression.ResolvedType))
                    {
                        throw new ParserException(this.Expression.FirstToken, "This expression is not the expected return type of this function.");
                    }
                }
                else
                {
                    if (!fd.ReturnType.IsIdentical(compiler, PType.VOID))
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

        internal override Executable ResolveWithTypeContext(PastelCompiler compiler)
        {
            if (this.Expression != null)
            {
                this.Expression = this.Expression.ResolveWithTypeContext(compiler);
            }
            return this;
        }
    }
}
