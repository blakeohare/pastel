namespace Pastel.Parser.ParseNodes
{
    internal abstract class NamespaceReference : Expression
    {
        public NamespaceReference(Token firstToken, ICompilationEntity owner) : base(firstToken, owner) { }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            throw new ParserException(FirstToken, "Cannot use a partial namespace reference like this.");
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            throw new ParserException(FirstToken, "Cannot use a partial namespace reference like this.");
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            throw new ParserException(FirstToken, "Cannot use a partial namespace reference like this.");
        }
    }

    internal class CoreNamespaceReference : NamespaceReference
    {
        public CoreNamespaceReference(Token firstToken, ICompilationEntity owner) : base(firstToken, owner) { }
    }

    internal class ExtensibleNamespaceReference : NamespaceReference
    {
        public ExtensibleNamespaceReference(Token firstToken, ICompilationEntity owner) : base(firstToken, owner) { }
    }
}
