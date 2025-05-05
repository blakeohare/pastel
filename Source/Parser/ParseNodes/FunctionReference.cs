namespace Pastel.Parser.ParseNodes
{
    internal class FunctionReference : Expression
    {
        public FunctionDefinition Function { get; set; }
        public bool IsLibraryScopedFunction { get; set; }

        public FunctionReference(Token firstToken, FunctionDefinition functionDefinition, ICompilationEntity owner) 
            : base(ExpressionType.FUNCTION_REFERENCE, firstToken, owner)
        {
            this.Function = functionDefinition;
            this.IsLibraryScopedFunction = false;
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            return this;
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            this.ResolvedType = PType.FunctionOf(
                this.FirstToken,
                this.Function.ReturnType,
                this.Function.ArgTypes);
            return this;
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            return this;
        }
    }
}
