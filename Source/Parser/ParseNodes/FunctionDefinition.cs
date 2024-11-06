using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser.ParseNodes
{
    internal class FunctionDefinition : ICompilationEntity
    {
        public CompilationEntityType EntityType { get { return CompilationEntityType.FUNCTION; } }

        public Token FirstToken { get; set; }
        public PType ReturnType { get; set; }
        public Token NameToken { get; set; }
        public string Name { get { return NameToken.Value; } }
        public PType[] ArgTypes { get; set; }
        public Token[] ArgNames { get; set; }
        public Executable[] Code { get; set; }
        public ClassDefinition ClassDef { get; set; }

        public FunctionDefinition(
            Token nameToken,
            PType returnType,
            IList<PType> argTypes,
            IList<Token> argNames,
            ClassDefinition nullableClassOwner) // null if not associated with a class
        {
            FirstToken = returnType.FirstToken;
            NameToken = nameToken;
            ReturnType = returnType;
            ArgTypes = argTypes.ToArray();
            ArgNames = argNames.ToArray();
            ClassDef = nullableClassOwner;
        }

        public void ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            Code = Executable.ResolveNamesAndCullUnusedCodeForBlock(Code, compiler).ToArray();
        }

        public void ResolveSignatureTypes(PastelCompiler compiler)
        {
            ReturnType.FinalizeType(compiler);
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

        public void ResolveWithTypeContext(PastelCompiler compiler)
        {
            Executable.ResolveWithTypeContext(compiler, Code);
        }
    }
}
