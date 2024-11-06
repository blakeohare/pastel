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
            Context = context;
            FirstToken = constructorToken;
            ArgTypes = argTypes.ToArray();
            ArgNames = argNames.ToArray();
            ClassDef = classDef;
        }

        public void ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            Code = Statement.ResolveNamesAndCullUnusedCodeForBlock(Code, compiler).ToArray();
        }

        public void ResolveSignatureTypes(PastelCompiler compiler)
        {
            for (int i = 0; i < ArgTypes.Length; ++i)
            {
                ArgTypes[i].FinalizeType(compiler);
            }
        }

        public void ResolveTypes(PastelCompiler compiler)
        {
            VariableScope varScope = new VariableScope(this);
            for (int i = 0; i < ArgTypes.Length; ++i)
            {
                varScope.DeclareVariables(ArgNames[i], ArgTypes[i]);
            }

            for (int i = 0; i < Code.Length; ++i)
            {
                Code[i].ResolveTypes(varScope, compiler);
            }
        }
    }
}
