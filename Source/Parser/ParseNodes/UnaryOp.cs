namespace Pastel.Parser.ParseNodes
{
    internal class UnaryOp : Expression
    {
        public Expression Expression { get; set; }
        public Token OpToken { get; set; }

        public UnaryOp(Token op, Expression root) 
            : base(ExpressionType.UNARY_OP, op, root.Owner)
        {
            this.Expression = root;
            this.OpToken = op;
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            this.Expression = this.Expression.ResolveNamesAndCullUnusedCode(resolver);

            if (this.Expression is InlineConstant ic)
            {
                if (this.FirstToken.Value == "!" && ic.Value is bool boolVal)
                {
                    return InlineConstant.OfBoolean(!boolVal, this.FirstToken, this.Owner);
                }
                
                if (this.FirstToken.Value == "-")
                {
                    if (ic.Value is int intVal)
                    {
                        return InlineConstant.OfInteger(-intVal, this.FirstToken, this.Owner);
                    }
                    if (ic.Value is double floatVal)
                    {
                        return InlineConstant.OfFloat(-floatVal, this.FirstToken, this.Owner);
                    }
                }
                throw new ParserException(
                    this.OpToken, 
                    "The op '" + this.OpToken.Value + "' is not valid on this type of expression.");
            }
            
            return this;
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            this.Expression = this.Expression.ResolveType(varScope, resolver);
            this.ResolvedType = this.Expression.ResolvedType;

            if (this.OpToken.Value == "-")
            {
                if (!(this.ResolvedType.IsIdentical(resolver, PType.INT) || 
                      this.ResolvedType.IsIdentical(resolver, PType.DOUBLE)))
                {
                    throw new ParserException(this.OpToken, "Cannot apply '-' to type: " + this.ResolvedType.ToString());
                }
            }
            else // '!'
            {
                if (!this.ResolvedType.IsIdentical(resolver, PType.BOOL))
                {
                    throw new ParserException(this.OpToken, "Cannot apply '!' to type: " + this.ResolvedType.ToString());
                }
            }
            
            return this;
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            this.Expression = this.Expression.ResolveWithTypeContext(resolver);
            return this;
        }
    }
}
