namespace Pastel.Parser.ParseNodes
{
    internal class InlineIncrement : Expression
    {
        public Token IncrementToken { get; set; }
        public Expression Expression { get; set; }
        public bool IsPrefix { get; set; }

        public InlineIncrement(Token firstToken, Token incrementToken, Expression root, bool isPrefix) : base(firstToken, root.Owner)
        {
            IncrementToken = incrementToken;
            Expression = root;
            IsPrefix = isPrefix;
        }

        public override Expression ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            Expression = Expression.ResolveNamesAndCullUnusedCode(compiler);
            return this;
        }

        internal override Expression ResolveType(VariableScope varScope, PastelCompiler compiler)
        {
            Expression = Expression.ResolveType(varScope, compiler);
            if (!Expression.ResolvedType.IsIdentical(compiler, PType.INT))
            {
                throw new ParserException(IncrementToken, "++ and -- can only be applied to integer types.");
            }
            ResolvedType = PType.INT;
            return this;
        }

        internal override Expression ResolveWithTypeContext(PastelCompiler compiler)
        {
            // TODO: check if this is either:
            // - exporting to a platform that supports this OR
            // - is running as the direct descendant of ExpressionAsExecutable, and then swap out with += 1
            return this;
        }
    }
}
