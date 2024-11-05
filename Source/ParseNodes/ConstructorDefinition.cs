using System.Collections.Generic;
using System.Linq;

namespace Pastel.Nodes
{
    internal class ConstructorDefinition : ICompilationEntity
    {
        public CompilationEntityType EntityType { get { return CompilationEntityType.CONSTRUCTOR; } }

        public PastelContext Context { get; private set; }
        public Token FirstToken { get; private set; }
        public PType[] ArgTypes { get; private set; }
        public Token[] ArgNames { get; private set; }
        public Executable[] Code { get; set; }
        public ClassDefinition ClassDef { get; private set; }

        public ConstructorDefinition(PastelContext context, Token constructorToken, IList<PType> argTypes, IList<Token> argNames, ClassDefinition classDef)
        {
            this.Context = context;
            this.FirstToken = constructorToken;
            this.ArgTypes = argTypes.ToArray();
            this.ArgNames = argNames.ToArray();
            this.ClassDef = classDef;
        }

        public void ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            this.Code = Executable.ResolveNamesAndCullUnusedCodeForBlock(this.Code, compiler).ToArray();
        }

        public void ResolveSignatureTypes(PastelCompiler compiler)
        {
            for (int i = 0; i < this.ArgTypes.Length; ++i)
            {
                this.ArgTypes[i].FinalizeType(compiler);
            }
        }

        public void ResolveTypes(PastelCompiler compiler)
        {
            VariableScope varScope = new VariableScope(this);
            for (int i = 0; i < this.ArgTypes.Length; ++i)
            {
                varScope.DeclareVariables(this.ArgNames[i], this.ArgTypes[i]);
            }

            for (int i = 0; i < this.Code.Length; ++i)
            {
                this.Code[i].ResolveTypes(varScope, compiler);
            }
        }
    }
}
