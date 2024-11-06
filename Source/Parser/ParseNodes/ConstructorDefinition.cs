using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser.ParseNodes
{
    internal class ConstructorDefinition : ICompilationEntity
    {
        public CompilationEntityType EntityType { get { return CompilationEntityType.CONSTRUCTOR; } }

        public PastelContext Context { get; private set; }
        public Token FirstToken { get; private set; }
        public PType[] ArgTypes { get; private set; }
        public Token[] ArgNames { get; private set; }
        public Statement[] Code { get; set; }
        public ClassDefinition ClassDef { get; private set; }

        public ConstructorDefinition(PastelContext context, Token constructorToken, IList<PType> argTypes, IList<Token> argNames, ClassDefinition classDef)
        {
            this.Context = context;
            this.FirstToken = constructorToken;
            this.ArgTypes = argTypes.ToArray();
            this.ArgNames = argNames.ToArray();
            this.ClassDef = classDef;
        }

        public void ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            this.Code = Statement.ResolveNamesAndCullUnusedCodeForBlock(this.Code, resolver).ToArray();
        }

        public void ResolveSignatureTypes(Resolver resolver)
        {
            for (int i = 0; i < ArgTypes.Length; ++i)
            {
                ArgTypes[i].FinalizeType(resolver);
            }
        }

        public void ResolveTypes(Resolver resolver)
        {
            VariableScope varScope = new VariableScope(this);
            for (int i = 0; i < ArgTypes.Length; ++i)
            {
                varScope.DeclareVariables(ArgNames[i], ArgTypes[i]);
            }

            for (int i = 0; i < Code.Length; ++i)
            {
                Code[i].ResolveTypes(varScope, resolver);
            }
        }
    }
}
