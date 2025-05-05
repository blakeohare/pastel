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
            this.Target = target;
            this.OpToken = opToken;
            this.Value = value;
        }

        public override Statement ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            this.Target = this.Target.ResolveNamesAndCullUnusedCode(resolver);
            this.Value = this.Value.ResolveNamesAndCullUnusedCode(resolver);
            return this;
        }

        internal override void ResolveTypes(VariableScope varScope, Resolver resolver)
        {
            this.Value = this.Value.ResolveType(varScope, resolver);
            this.Target = this.Target.ResolveType(varScope, resolver);

            if (!PType.CheckAssignment(resolver, this.Target.ResolvedType, this.Value.ResolvedType))
            {
                if (this.OpToken.Value != "=" &&
                    this.Target.ResolvedType.IsIdentical(resolver, PType.DOUBLE) &&
                    this.Value.ResolvedType.IsIdentical(resolver, PType.INT))
                {
                    // You can apply incremental ops such as += with an int to a float and that is fine without explicit conversion in any platform.
                }
                else
                {
                    throw new UNTESTED_ParserException(
                        this.OpToken,
                        "Cannot assign a " + this.Value.ResolvedType + " to a " + this.Target.ResolvedType);
                }
            }
        }

        internal override Statement ResolveWithTypeContext(Resolver resolver)
        {
            if (this.Target is BracketIndex bi)
            {
                if (this.OpToken.Value != "=" && bi.Root.ResolvedType.RootValue != "Array")
                {
                    // Java will need to be special as it will require things to be broken down into a get-then-set.
                    throw new UNTESTED_ParserException(
                        this.OpToken,
                        "Incremental assignment on a key/index is not currently supported (although it really ought to be).");
                }

                string rootType = bi.Root.ResolvedType.RootValue;
                Expression[] args = [bi.Root, bi.Index, this.Value];
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
                    throw new UNTESTED_ParserException(
                        bi.BracketToken,
                        "Can't use brackets here.");
                }
                
                return new ExpressionAsStatement(new CoreFunctionInvocation(
                    this.FirstToken,
                    nf,
                    args,
                    bi.Owner)).ResolveWithTypeContext(resolver);
            }

            this.Target = this.Target.ResolveWithTypeContext(resolver);
            this.Value = this.Value.ResolveWithTypeContext(resolver);

            return this;
        }
    }
}
