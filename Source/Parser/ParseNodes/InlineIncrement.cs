namespace Pastel.Parser.ParseNodes
{
    internal class InlineIncrement : Expression
    {
        public Token IncrementToken { get; set; }
        public Expression Expression { get; set; }
        public bool IsPrefix { get; set; }

        public InlineIncrement(Token firstToken, Token incrementToken, Expression root, bool isPrefix) 
            : base(ExpressionType.INLINE_INCREMENT, firstToken, root.Owner)
        {
            this.IncrementToken = incrementToken;
            this.Expression = root;
            this.IsPrefix = isPrefix;
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            this.Expression = this.Expression.ResolveNamesAndCullUnusedCode(resolver);
            return this;
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            this.Expression = this.Expression.ResolveType(varScope, resolver);
            if (!this.Expression.ResolvedType.IsIdentical(resolver, PType.INT))
            {
                throw new ParserException(this.IncrementToken, "++ and -- can only be applied to integer types.");
            }
            this.ResolvedType = PType.INT;
            return this;
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            // TODO: check if this is either:
            // - exporting to a platform that supports this OR
            // - is running as the direct descendant of ExpressionAsStatement, and then swap out with += 1
            return this;
        }
    }
}
