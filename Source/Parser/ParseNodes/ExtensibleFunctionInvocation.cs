using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser.ParseNodes
{
    internal class ExtensibleFunctionInvocation : Expression
    {
        public Expression[] Args { get; set; }
        public ExtensibleFunctionReference FunctionRef { get; set; }

        public ExtensibleFunctionInvocation(
            Token firstToken,
            ExtensibleFunctionReference functionRef,
            IList<Expression> args)
            : base(ExpressionType.EXTENSIBLE_FUNCTION_INVOCATION, firstToken, functionRef.Owner)
        {
            FunctionRef = functionRef;
            Args = args.ToArray();
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            throw new NotImplementedException();
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            // Args already resolved by FunctionInvocation.ResolveType().

            string name = FunctionRef.Name;
            ExtensibleFunction extensibleFunction;
            if (!resolver.CompilerContext.ExtensionSet.ExtensionLookup.TryGetValue(name, out extensibleFunction))
            {
                throw new ParserException(FirstToken, "Type information for '" + name + "' extensible function is not defined.");
            }
            ResolvedType = extensibleFunction.ReturnType;

            PType[] argTypes = extensibleFunction.ArgTypes;

            if (argTypes.Length != Args.Length)
            {
                throw new ParserException(FirstToken, "Incorrect number of args for this function. Expected " + argTypes.Length + " but instead found " + Args.Length + ".");
            }

            for (int i = 0; i < Args.Length; ++i)
            {
                if (!PType.CheckAssignment(resolver, argTypes[i], Args[i].ResolvedType))
                {
                    throw new ParserException(Args[i].FirstToken, "Invalid argument type. Expected '" + argTypes[i] + "' but found '" + Args[i].ResolvedType + "'.");
                }
            }

            return this;
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            for (int i = 0; i < Args.Length; ++i)
            {
                Args[i] = Args[i].ResolveWithTypeContext(resolver);
            }
            return this;
        }
    }
}
