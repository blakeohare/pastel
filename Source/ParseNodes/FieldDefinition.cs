namespace Pastel.Nodes
{
    class FieldDefinition : ICompilationEntity
    {
        public CompilationEntityType EntityType { get { return CompilationEntityType.FIELD; } }
        public PastelContext Context { get; private set; }
        public PType FieldType { get; private set; }
        public Token NameToken { get; private set; }
        public Expression Value { get; set; }
        public ClassDefinition ClassDef { get; private set; }

        public FieldDefinition(PastelContext context, PType type, Token name, ClassDefinition classDef)
        {
            this.Context = context;
            this.FieldType = type;
            this.NameToken = name;
            this.ClassDef = classDef;
        }

        public void ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            this.Value = this.Value.ResolveNamesAndCullUnusedCode(compiler);
        }

        public void ResolveTypes(PastelCompiler compiler)
        {
            this.Value = this.Value.ResolveType(new VariableScope(), compiler);
            if (!this.Value.ResolvedType.IsIdenticalOrChildOf(compiler, this.FieldType))
            {
                throw new ParserException(this.Value.FirstToken, "Cannot assign this value to this type.");
            }
        }
    }
}
