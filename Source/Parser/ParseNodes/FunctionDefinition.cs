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
        public Statement[] Code { get; set; }

        public FunctionDefinition(
            Token nameToken,
            PType returnType,
            IList<PType> argTypes,
            IList<Token> argNames) // null if not associated with a class
        {
            this.FirstToken = returnType.FirstToken;
            this.NameToken = nameToken;
            this.ReturnType = returnType;
            this.ArgTypes = [.. argTypes];
            this.ArgNames = [.. argNames];
        }

        public void ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            this.Code = Statement.ResolveNamesAndCullUnusedCodeForBlock(this.Code, resolver).ToArray();
        }

        public void ResolveSignatureTypes(Resolver resolver)
        {
            this.ReturnType.FinalizeType(resolver);
            for (int i = 0; i < this.ArgTypes.Length; ++i)
            {
                this.ArgTypes[i].FinalizeType(resolver);
            }
        }

        public void ResolveTypes(Resolver resolver)
        {
            VariableScope varScope = new VariableScope(this);
            for (int i = 0; i < this.ArgTypes.Length; ++i)
            {
                varScope.DeclareVariables(this.ArgNames[i], this.ArgTypes[i]);
            }

            for (int i = 0; i < this.Code.Length; ++i)
            {
                this.Code[i].ResolveTypes(varScope, resolver);
            }
        }

        public void ResolveWithTypeContext(Resolver resolver)
        {
            Statement.ResolveWithTypeContext(resolver, this.Code);
        }
    }
}
