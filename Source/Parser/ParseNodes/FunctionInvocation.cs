using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser.ParseNodes
{
    internal class FunctionInvocation : Expression
    {
        public Expression Root { get; set; }
        public Token OpenParenToken { get; set; }
        public Expression[] Args { get; set; }

        public FunctionInvocation(
            Expression root,
            Token openParen,
            IList<Expression> args) 
            : base(ExpressionType.FUNCTION_INVOCATION, root.FirstToken, root.Owner)
        {
            if (root is Variable v) v.IsFunctionInvocation = true;
            
            this.Root = root;
            this.OpenParenToken = openParen;
            this.Args = args.ToArray();
        }

        internal Expression MaybeImmediatelyResolve(PastelParser parser)
        {
            if (this.Root is CompileTimeFunctionReference constFunc)
            {
                InlineConstant? argName = this.Args.Length == 1 ? this.Args[0] as InlineConstant : null;
                string argValue = argName == null ? "" : argName.Value.ToString()!;
                switch (constFunc.NameToken.Value)
                {
                    case "ext_boolean":
                        return InlineConstant.OfBoolean(
                            parser.GetParseTimeBooleanConstant(argValue),
                            this.FirstToken,
                            this.Owner);

                    case "pastel_flag":
                        return InlineConstant.OfBoolean(
                            parser.GetPastelFlagConstant(constFunc.NameToken, argValue),
                            this.FirstToken,
                            this.Owner);

                    // This will be resolved later.
                    case "import":
                        return this;

                    default:
                        throw new ParserException(this.FirstToken, "Unknown compile-time function: " + constFunc.NameToken.Value);
                }
            }

            return this;
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            if (this.Root is CompileTimeFunctionReference)
            {
                throw new ParserException(
                    this.FirstToken,
                    "Compile-time functions can only be used as standalone statements and cannot be used in expressions.");
            }

            this.Root = this.Root.ResolveNamesAndCullUnusedCode(resolver);
            Expression.ResolveNamesAndCullUnusedCodeInPlace(this.Args, resolver);

            return this;
        }

        private void VerifyArgTypes(PType[] expectedTypes, Resolver resolver)
        {
            if (expectedTypes.Length != this.Args.Length)
            {
                throw new ParserException(this.OpenParenToken, "This function invocation has the wrong number of parameters. Expected " + expectedTypes.Length + " but found " + Args.Length + ".");
            }

            for (int i = 0; i < Args.Length; ++i)
            {
                if (!PType.CheckAssignment(resolver, expectedTypes[i], Args[i].ResolvedType))
                {
                    throw new ParserException(this.Args[i].FirstToken, "Wrong function arg type. Cannot convert a " + Args[i].ResolvedType + " to a " + expectedTypes[i]);
                }
            }
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            for (int i = 0; i < this.Args.Length; ++i)
            {
                this.Args[i] = this.Args[i].ResolveType(varScope, resolver);
            }

            this.Root = this.Root.ResolveType(varScope, resolver);

            if (this.Root is FunctionReference)
            {
                FunctionDefinition functionDefinition = ((FunctionReference)Root).Function;
                VerifyArgTypes(functionDefinition.ArgTypes, resolver);
                ResolvedType = functionDefinition.ReturnType;
                return this;
            }
            else if (this.Root is CoreFunctionReference cfr)
            {
                bool hasTypeHint = CoreFunctionUtil.PerformAdditionalTypeResolution(cfr, this.Args);
                CoreFunctionInvocation nfi;
                if (cfr.Context == null)
                {
                    nfi = new CoreFunctionInvocation(this.FirstToken, cfr.CoreFunctionId, this.Args, this.Owner);
                }
                else
                {
                    nfi = new CoreFunctionInvocation(this.FirstToken, cfr.CoreFunctionId, cfr.Context, this.Args, this.Owner);
                }

                if (hasTypeHint)
                {
                    nfi.ResolvedType = cfr.ReturnType;
                    return nfi;
                }

                return nfi.ResolveType(varScope, resolver);
            }
            else if (Root is ExtensibleFunctionReference)
            {
                return new ExtensibleFunctionInvocation(FirstToken, (ExtensibleFunctionReference)Root, Args).ResolveType(varScope, resolver);
            }
            else if (this.Root is ConstructorReference ctorRef)
            {
                PType typeToConstruct = ctorRef.TypeToConstruct;
                typeToConstruct.FinalizeType(resolver);
                return new ConstructorInvocation(this.FirstToken, typeToConstruct, this.Args, this.Owner);
            }
            else if (this.Root.ResolvedType.IsFunction)
            {
                return new FunctionPointerInvocation(resolver, FirstToken, Root, Args);
            }

            throw new ParserException(this.OpenParenToken, "This expression cannot be invoked like a function.");
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            this.Root = this.Root.ResolveWithTypeContext(resolver);

            if (this.Root.Type != ExpressionType.FUNCTION_REFERENCE)
            {
                throw new ParserException(this.OpenParenToken, "Cannot invoke this like a function.");
            }

            for (int i = 0; i < this.Args.Length; ++i)
            {
                this.Args[i] = this.Args[i].ResolveWithTypeContext(resolver);
            }
            
            return this;
        }

        internal override InlineConstant DoConstantResolution(HashSet<string> cycleDetection, Resolver resolver)
        {
            for (int i = 0; i < this.Args.Length; i++)
            {
                this.Args[i] = this.Args[i].DoConstantResolution(cycleDetection, resolver);
            }
            
            if (this.Root is DotField df && df.Root is Variable v && v.Name == "Core")
            {
                CoreFunction cf;
                switch (df.FieldName.Value)
                {
                    case "Ord": cf = CoreFunction.ORD; break;
                    default: return base.DoConstantResolution(cycleDetection, resolver);
                }
                
                CoreFunctionInvocation cfi = new CoreFunctionInvocation(this.FirstToken, cf, this.Args, this.Owner);
                return cfi.DoConstantResolution(cycleDetection, resolver);
            }
            
            this.Root = this.Root.DoConstantResolution(cycleDetection, resolver);

            return base.DoConstantResolution(cycleDetection, resolver);
        }
    }
}
