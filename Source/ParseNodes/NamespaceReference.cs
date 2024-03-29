﻿namespace Pastel.Nodes
{
    internal abstract class NamespaceReference : Expression
    {
        public NamespaceReference(Token firstToken, ICompilationEntity owner) : base(firstToken, owner) { }

        public override Expression ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            throw new ParserException(this.FirstToken, "Cannot use a partial namespace reference like this.");
        }

        internal override Expression ResolveType(VariableScope varScope, PastelCompiler compiler)
        {
            throw new ParserException(this.FirstToken, "Cannot use a partial namespace reference like this.");
        }

        internal override Expression ResolveWithTypeContext(PastelCompiler compiler)
        {
            throw new ParserException(this.FirstToken, "Cannot use a partial namespace reference like this.");
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
