namespace Pastel.Parser.ParseNodes
{
    internal class CastExpression : Expression
    {
        public PType Type { get; set; }
        public Expression Expression { get; set; }

        public CastExpression(Token openParenToken, PType type, Expression expression) : base(openParenToken, expression.Owner)
        {
            Type = type;
            Expression = expression;
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            Expression = Expression.ResolveNamesAndCullUnusedCode(resolver);
            return this;
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            Expression = Expression.ResolveType(varScope, resolver);
            // TODO: check for silly casts
            ResolvedType = Type;
            return this;
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            Expression = Expression.ResolveWithTypeContext(resolver);
            return this;
        }
    }
}
