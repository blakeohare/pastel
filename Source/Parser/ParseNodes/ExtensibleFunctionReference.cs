using System;

namespace Pastel.Parser.ParseNodes
{
    internal class ExtensibleFunctionReference : Expression
    {
        public string Name { get; set; }

        public ExtensibleFunctionReference(Token token, string name, ICompilationEntity owner) : base(token, owner)
        {
            Name = name;
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            throw new NotImplementedException();
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            return this;
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            throw new NotImplementedException();
        }
    }
}
