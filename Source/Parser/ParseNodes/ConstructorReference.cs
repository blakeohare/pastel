using System;

namespace Pastel.Parser.ParseNodes
{
    internal class ConstructorReference : Expression
    {
        public PType TypeToConstruct { get; set; }

        public ConstructorReference(Token newToken, PType type, ICompilationEntity owner) 
            : base(ExpressionType.CONSTRUCTOR_REFERENCE, newToken, owner)
        {
            TypeToConstruct = type;
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            return this;
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            // no function pointer type
            return this;
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            throw new NotImplementedException();
        }
    }
}
