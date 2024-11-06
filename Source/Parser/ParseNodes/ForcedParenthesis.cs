using System;

namespace Pastel.Parser.ParseNodes
{
    internal class ForcedParenthesis : Expression
    {
        public Expression Expression { get; set; }

        public ForcedParenthesis(Token token, Expression expression) : base(token, expression.Owner)
        {
            Expression = expression;
            ResolvedType = expression.ResolvedType;
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            throw new NotImplementedException();
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            throw new NotImplementedException();
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            Expression = Expression.ResolveWithTypeContext(resolver);
            return this;
        }
    }
}
