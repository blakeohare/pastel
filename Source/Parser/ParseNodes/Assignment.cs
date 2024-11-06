namespace Pastel.Parser.ParseNodes
{
    internal class Assignment : Statement
    {
        public Expression Target { get; set; }
        public Token OpToken { get; set; }
        public Expression Value { get; set; }

        public Assignment(
            Expression target,
            Token opToken,
            Expression value) : base(target.FirstToken)
        {
            Target = target;
            OpToken = opToken;
            Value = value;
        }

        public override Statement ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            Target = Target.ResolveNamesAndCullUnusedCode(resolver);
            Value = Value.ResolveNamesAndCullUnusedCode(resolver);
            return this;
        }

        internal override void ResolveTypes(VariableScope varScope, Resolver resolver)
        {
            Value = Value.ResolveType(varScope, resolver);
            Target = Target.ResolveType(varScope, resolver);

            if (!PType.CheckAssignment(resolver, Target.ResolvedType, Value.ResolvedType))
            {
                if (OpToken.Value != "=" &&
                    Target.ResolvedType.IsIdentical(resolver, PType.DOUBLE) &&
                    Value.ResolvedType.IsIdentical(resolver, PType.INT))
                {
                    // You can apply incremental ops such as += with an int to a float and that is fine without explicit conversion in any platform.
                }
                else
                {
                    throw new ParserException(OpToken, "Cannot assign a " + Value.ResolvedType + " to a " + Target.ResolvedType);
                }
            }
        }

        internal override Statement ResolveWithTypeContext(Resolver resolver)
        {
            if (Target is BracketIndex)
            {
                BracketIndex bi = (BracketIndex)Target;
                if (OpToken.Value != "=" && bi.Root.ResolvedType.RootValue != "Array")
                {
                    // Java will need to be special as it will require things to be broken down into a get-then-set.
                    throw new ParserException(OpToken, "Incremental assignment on a key/index is not currently supported (although it really ought to be).");
                }

                string rootType = bi.Root.ResolvedType.RootValue;
                Expression[] args = new Expression[] { bi.Root, bi.Index, Value };
                CoreFunction nf;
                if (rootType == "Array")
                {
                    nf = CoreFunction.ARRAY_SET;
                }
                else if (rootType == "List")
                {
                    nf = CoreFunction.LIST_SET;
                }
                else if (rootType == "Dictionary")
                {
                    nf = CoreFunction.DICTIONARY_SET;
                }
                else
                {
                    throw new ParserException(bi.BracketToken, "Can't use brackets here.");
                }
                return new ExpressionAsStatement(new CoreFunctionInvocation(
                    FirstToken,
                    nf,
                    args,
                    bi.Owner)).ResolveWithTypeContext(resolver);
            }

            Target = Target.ResolveWithTypeContext(resolver);
            Value = Value.ResolveWithTypeContext(resolver);

            return this;
        }
    }
}
