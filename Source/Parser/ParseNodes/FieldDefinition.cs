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

        public void ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            Value = Value.ResolveNamesAndCullUnusedCode(resolver);
        }

        public void ResolveTypes(Resolver resolver)
        {
            Value = Value.ResolveType(new VariableScope(), resolver);
            if (!Value.ResolvedType.IsIdenticalOrChildOf(resolver, FieldType))
            {
                throw new ParserException(Value.FirstToken, "Cannot assign this value to this type.");
            }
        }
    }
}
