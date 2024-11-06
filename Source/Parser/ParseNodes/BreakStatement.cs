namespace Pastel.Parser.ParseNodes
{
    internal class BreakStatement : Statement
    {
        public BreakStatement(Token breakToken) : base(breakToken)
        { }

        public override Statement ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            return this;
        }

        internal override void ResolveTypes(VariableScope varScope, Resolver resolver)
        {
            // nothing to do
        }

        internal override Statement ResolveWithTypeContext(Resolver resolver)
        {
            return this;
        }
    }
}
