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

        public override Expression ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            Expression = Expression.ResolveNamesAndCullUnusedCode(compiler);
            return this;
        }

        internal override Expression ResolveType(VariableScope varScope, PastelCompiler compiler)
        {
            Expression = Expression.ResolveType(varScope, compiler);
            // TODO: check for silly casts
            ResolvedType = Type;
            return this;
        }

        internal override Expression ResolveWithTypeContext(PastelCompiler compiler)
        {
            Expression = Expression.ResolveWithTypeContext(compiler);
            return this;
        }
    }
}
