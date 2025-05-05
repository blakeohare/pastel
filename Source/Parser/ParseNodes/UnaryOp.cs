namespace Pastel.Parser.ParseNodes
{
    internal class UnaryOp : Expression
    {
        public Expression Expression { get; set; }
        public Token OpToken { get; set; }

        public UnaryOp(Token op, Expression root) 
            : base(ExpressionType.UNARY_OP, op, root.Owner)
        {
            Expression = root;
            OpToken = op;
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            Expression = Expression.ResolveNamesAndCullUnusedCode(resolver);

            if (Expression is InlineConstant)
            {
                InlineConstant ic = (InlineConstant)Expression;
                if (FirstToken.Value == "!" && ic.Value is bool)
                {
                    return new InlineConstant(PType.BOOL, FirstToken, !(bool)ic.Value, Owner);
                }
                if (FirstToken.Value == "-")
                {
                    if (ic.Value is int)
                    {
                        return new InlineConstant(PType.INT, FirstToken, -(int)ic.Value, Owner);
                    }
                    if (ic.Value is double)
                    {
                        return new InlineConstant(PType.DOUBLE, FirstToken, -(double)ic.Value, Owner);
                    }
                }
                throw new ParserException(OpToken, "The op '" + OpToken.Value + "' is not valid on this type of expression.");
            }
            return this;
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            Expression = Expression.ResolveType(varScope, resolver);
            ResolvedType = Expression.ResolvedType;

            if (OpToken.Value == "-")
            {
                if (!(ResolvedType.IsIdentical(resolver, PType.INT) || ResolvedType.IsIdentical(resolver, PType.DOUBLE)))
                {
                    throw new ParserException(OpToken, "Cannot apply '-' to type: " + ResolvedType.ToString());
                }
            }
            else // '!'
            {
                if (!ResolvedType.IsIdentical(resolver, PType.BOOL))
                {
                    throw new ParserException(OpToken, "Cannot apply '!' to type: " + ResolvedType.ToString());
                }
            }
            return this;
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            Expression = Expression.ResolveWithTypeContext(resolver);
            return this;
        }
    }
}
