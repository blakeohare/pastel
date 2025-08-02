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
        public CoreFunctionReference(Token firstToken, CoreFunction coreFunctionId, Expression context, ICompilationEntity owner) 
            : base(ExpressionType.CORE_FUNCTION_REFERENCE, firstToken, owner)
        {
            this.CoreFunctionId = coreFunctionId;
            this.Context = context;

            this.ReturnType = CoreFunctionUtil.GetCoreFunctionReturnType(this.CoreFunctionId);
            this.ArgTypes = CoreFunctionUtil.GetCoreFunctionArgTypes(this.CoreFunctionId);
            this.ArgTypesIsRepeated = CoreFunctionUtil.GetCoreFunctionIsArgTypeRepeated(this.CoreFunctionId);
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            // Introduced in ResolveTypes phase
            throw new NotImplementedException();
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            if (this.Context != null)
            {
                // CoreFunctionReferences only get introduced before the ResolveType phase for core
                // functions, in which case they have no Context and nothing to resolve.
                throw new Exception();
            }
            return this;
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            throw new UNTESTED_ParserException(
                this.FirstToken, 
                "Core Functions must be invoked and cannot be passed as function pointers in this manner.");
        }
    }
}
