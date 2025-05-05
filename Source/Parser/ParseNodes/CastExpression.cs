namespace Pastel.Parser.ParseNodes
{
    internal class CastExpression : Expression
    {
        public PType Type { get; set; }
        public Expression Expression { get; set; }

        public CastExpression(Token openParenToken, PType type, Expression expression) 
            : base(ExpressionType.CAST, openParenToken, expression.Owner)
        {
            this.Type = type;
            this.Expression = expression;
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            this.Expression = this.Expression.ResolveNamesAndCullUnusedCode(resolver);
            return this;
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            this.Expression = this.Expression.ResolveType(varScope, resolver);
            // TODO: check for silly casts
            this.ResolvedType = this.Type;
            return this;
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            this.Expression = this.Expression.ResolveWithTypeContext(resolver);
            return this;
        }
    }
}
