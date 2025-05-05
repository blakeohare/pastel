using System;

namespace Pastel.Parser.ParseNodes
{
    internal class EnumReference : Expression
    {
        public EnumDefinition EnumDef { get; set; }

        public EnumReference(Token firstToken, EnumDefinition enumDef, ICompilationEntity owner) 
            : base(ExpressionType.ENUM_REFERENCE, firstToken, owner)
        {
            EnumDef = enumDef;
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            // created by this phase
            throw new NotImplementedException();
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            // should be resolved out by now
            throw new NotImplementedException();
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            throw new NotImplementedException();
        }
    }
}
