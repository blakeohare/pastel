using System;

namespace Pastel.Parser.ParseNodes
{
    internal class CoreFunctionReference : Expression
    {
        public CoreFunction CoreFunctionId { get; set; }
        public Expression Context { get; set; }
        public PType ReturnType { get; set; }
        public PType[] ArgTypes { get; set; }
        public bool[] ArgTypesIsRepeated { get; set; }

        public CoreFunctionReference(Token firstToken, CoreFunction coreFunctionId, ICompilationEntity owner) : this(firstToken, coreFunctionId, null, owner) { }
        public CoreFunctionReference(Token firstToken, CoreFunction coreFunctionId, Expression context, ICompilationEntity owner) : base(firstToken, owner)
        {
            CoreFunctionId = coreFunctionId;
            Context = context;

            ReturnType = CoreFunctionUtil.GetCoreFunctionReturnType(CoreFunctionId);
            ArgTypes = CoreFunctionUtil.GetCoreFunctionArgTypes(CoreFunctionId);
            ArgTypesIsRepeated = CoreFunctionUtil.GetCoreFunctionIsArgTypeRepeated(CoreFunctionId);
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            // Introduced in ResolveTypes phase
            throw new NotImplementedException();
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            if (Context != null)
            {
                // CoreFunctionReferences only get introduced before the ResolveType phase for Core.* functions, in which case they have no Context and nothing to resolve.
                throw new Exception();
            }
            return this;
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            throw new ParserException(FirstToken, "Core Functions must be invoked and cannot be passed as function pointers in this manner.");
        }
    }
}
