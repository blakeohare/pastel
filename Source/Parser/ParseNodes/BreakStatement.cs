namespace Pastel.Parser.ParseNodes
{
    internal class BreakStatement : Statement
    {
        public BreakStatement(Token breakToken) : base(breakToken)
        { }

        public override Statement ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            return this;
        }

        internal override void ResolveTypes(VariableScope varScope, PastelCompiler compiler)
        {
            // nothing to do
        }

        internal override Statement ResolveWithTypeContext(PastelCompiler compiler)
        {
            return this;
        }
    }
}
