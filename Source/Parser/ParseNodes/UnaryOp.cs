namespace Pastel.Parser.ParseNodes
{
    internal class UnaryOp : Expression
    {
        public Expression Expression { get; set; }
        public Token OpToken { get; set; }

        public UnaryOp(Token op, Expression root) : base(op, root.Owner)
        {
            Expression = root;
            OpToken = op;
        }

        public override Expression ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            Expression = Expression.ResolveNamesAndCullUnusedCode(compiler);

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

        internal override Expression ResolveType(VariableScope varScope, PastelCompiler compiler)
        {
            Expression = Expression.ResolveType(varScope, compiler);
            ResolvedType = Expression.ResolvedType;

            if (OpToken.Value == "-")
            {
                if (!(ResolvedType.IsIdentical(compiler, PType.INT) || ResolvedType.IsIdentical(compiler, PType.DOUBLE)))
                {
                    throw new ParserException(OpToken, "Cannot apply '-' to type: " + ResolvedType.ToString());
                }
            }
            else // '!'
            {
                if (!ResolvedType.IsIdentical(compiler, PType.BOOL))
                {
                    throw new ParserException(OpToken, "Cannot apply '!' to type: " + ResolvedType.ToString());
                }
            }
            return this;
        }

        internal override Expression ResolveWithTypeContext(PastelCompiler compiler)
        {
            Expression = Expression.ResolveWithTypeContext(compiler);
            return this;
        }
    }
}
