namespace Pastel.Parser.ParseNodes
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
            Context = context;
            FieldType = type;
            NameToken = name;
            ClassDef = classDef;
        }

        public void ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            Value = Value.ResolveNamesAndCullUnusedCode(compiler);
        }

        public void ResolveTypes(PastelCompiler compiler)
        {
            Value = Value.ResolveType(new VariableScope(), compiler);
            if (!Value.ResolvedType.IsIdenticalOrChildOf(compiler, FieldType))
            {
                throw new ParserException(Value.FirstToken, "Cannot assign this value to this type.");
            }
        }
    }
}
