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
            this.FunctionRef = functionRef;
            this.Args = args.ToArray();
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
                throw new UNTESTED_ParserException(
                    this.FirstToken,
                    "Type information for '" + name + "' extensible function is not defined.");
            }
            this.ResolvedType = extensibleFunction.ReturnType;

            PType[] argTypes = extensibleFunction.ArgTypes;

            if (argTypes.Length != this.Args.Length)
            {
                throw new UNTESTED_ParserException(
                    this.FirstToken,
                    "Incorrect number of args for this function. Expected " + argTypes.Length + " but instead found " + this.Args.Length + ".");
            }

            for (int i = 0; i < this.Args.Length; ++i)
            {
                if (!PType.CheckAssignment(resolver, argTypes[i], this.Args[i].ResolvedType))
                {
                    throw new UNTESTED_ParserException(
                        this.Args[i].FirstToken, 
                        "Invalid argument type. Expected '" + argTypes[i] + "' but found '" + this.Args[i].ResolvedType + "'.");
                }
            }

            return this;
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            for (int i = 0; i < this.Args.Length; ++i)
            {
                this.Args[i] = this.Args[i].ResolveWithTypeContext(resolver);
            }
            return this;
        }
    }
}
