using System;

namespace Pastel.Parser.ParseNodes
{
    internal class CompileTimeFunctionReference : Expression
    {
        public Token NameToken { get; set; }

        public CompileTimeFunctionReference(Token atToken, Token nameToken, ICompilationEntity owner) : base(atToken, owner)
        {
            NameToken = nameToken;
        }

        public override Expression ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            throw new ParserException(FirstToken, "Compile time functions must be invoked and cannot be used like pointers.");
        }

        internal override Expression ResolveType(VariableScope varScope, PastelCompiler compiler)
        {
            throw new NotImplementedException();
        }

        internal override Expression ResolveWithTypeContext(PastelCompiler compiler)
        {
            throw new NotImplementedException();
        }
    }
}
