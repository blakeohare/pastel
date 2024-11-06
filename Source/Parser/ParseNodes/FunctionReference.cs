namespace Pastel.Parser.ParseNodes
{
    internal class FunctionReference : Expression
    {
        public FunctionDefinition Function { get; set; }
        public bool IsLibraryScopedFunction { get; set; }

        public FunctionReference(Token firstToken, FunctionDefinition functionDefinition, ICompilationEntity owner) : base(firstToken, owner)
        {
            Function = functionDefinition;
            IsLibraryScopedFunction = false;
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            return this;
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            ResolvedType = PType.FunctionOf(FirstToken, Function.ReturnType, Function.ArgTypes);
            return this;
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            return this;
        }
    }
}
