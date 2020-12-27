using System.Collections.Generic;
using System.Linq;

namespace Pastel.Nodes
{
    internal class FunctionDefinition : ICompilationEntity
    {
        public CompilationEntityType EntityType { get { return CompilationEntityType.FUNCTION; } }

        public Token FirstToken { get; set; }
        public PType ReturnType { get; set; }
        public Token NameToken { get; set; }
        public string Name { get { return this.NameToken.Value; } }
        public PType[] ArgTypes { get; set; }
        public Token[] ArgNames { get; set; }
        public Executable[] Code { get; set; }
        public ClassDefinition ClassDef { get; set; }
        public PastelContext Context { get; private set; }

        public FunctionDefinition(
            Token nameToken,
            PType returnType,
            IList<PType> argTypes,
            IList<Token> argNames,
            PastelContext context,
            ClassDefinition nullableClassOwner) // null if not associated with a class
        {
            this.Context = context;
            this.FirstToken = returnType.FirstToken;
            this.NameToken = nameToken;
            this.ReturnType = returnType;
            this.ArgTypes = argTypes.ToArray();
            this.ArgNames = argNames.ToArray();
            this.ClassDef = nullableClassOwner;
        }

        public void ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            this.Code = Executable.ResolveNamesAndCullUnusedCodeForBlock(this.Code, compiler).ToArray();
        }

        public void ResolveSignatureTypes(PastelCompiler compiler)
        {
            this.ReturnType.FinalizeType(compiler);
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

        public void ResolveWithTypeContext(PastelCompiler compiler)
        {
            Executable.ResolveWithTypeContext(compiler, this.Code);
        }
    }
}
