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
            IList<Expression> args) : base(root.FirstToken, root.Owner)
        {
            if (root is Variable)
            {
                ((Variable)root).IsFunctionInvocation = true;
            }
            Root = root;
            OpenParenToken = openParen;
            Args = args.ToArray();
        }

        internal Expression MaybeImmediatelyResolve(PastelParser parser)
        {
            if (Root is CompileTimeFunctionReference)
            {
                CompileTimeFunctionReference constFunc = (CompileTimeFunctionReference)Root;
                InlineConstant argName = (InlineConstant)Args[0];
                switch (constFunc.NameToken.Value)
                {
                    case "ext_boolean":
                        return new InlineConstant(
                            PType.BOOL,
                            FirstToken,
                            parser.GetParseTimeBooleanConstant(argName.Value.ToString()),
                            Owner);

                    case "pastel_flag":
                        return new InlineConstant(
                            PType.BOOL,
                            FirstToken,
                            parser.GetPastelFlagConstant(constFunc.NameToken, argName.Value.ToString()),
                            Owner);

                    default:
                        return this;
                }
            }
            return this;
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            Root = Root.ResolveNamesAndCullUnusedCode(resolver);
            ResolveNamesAndCullUnusedCodeInPlace(Args, resolver);

            return this;
        }

        private void VerifyArgTypes(PType[] expectedTypes, Resolver resolver)
        {
            if (expectedTypes.Length != Args.Length)
            {
                throw new ParserException(OpenParenToken, "This function invocation has the wrong number of parameters. Expected " + expectedTypes.Length + " but found " + Args.Length + ".");
            }

            for (int i = 0; i < Args.Length; ++i)
            {
                if (!PType.CheckAssignment(resolver, expectedTypes[i], Args[i].ResolvedType))
                {
                    throw new ParserException(Args[i].FirstToken, "Wrong function arg type. Cannot convert a " + Args[i].ResolvedType + " to a " + expectedTypes[i]);
                }
            }
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            for (int i = 0; i < Args.Length; ++i)
            {
                Args[i] = Args[i].ResolveType(varScope, resolver);
            }

            Root = Root.ResolveType(varScope, resolver);

            if (Root is FunctionReference)
            {
                FunctionDefinition functionDefinition = ((FunctionReference)Root).Function;
                VerifyArgTypes(functionDefinition.ArgTypes, resolver);
                ResolvedType = functionDefinition.ReturnType;
                return this;
            }
            else if (Root is CoreFunctionReference)
            {
                CoreFunctionReference nfr = (CoreFunctionReference)Root;
                CoreFunctionInvocation nfi;
                if (nfr.Context == null)
                {
                    nfi = new CoreFunctionInvocation(FirstToken, nfr.CoreFunctionId, Args, Owner);
                }
                else
                {
                    nfi = new CoreFunctionInvocation(FirstToken, nfr.CoreFunctionId, nfr.Context, Args, Owner);
                }

                return nfi.ResolveType(varScope, resolver);
            }
            else if (Root is ExtensibleFunctionReference)
            {
                return new ExtensibleFunctionInvocation(FirstToken, (ExtensibleFunctionReference)Root, Args).ResolveType(varScope, resolver);
            }
            else if (Root is ConstructorReference)
            {
                PType typeToConstruct = ((ConstructorReference)Root).TypeToConstruct;
                typeToConstruct.FinalizeType(resolver);
                return new ConstructorInvocation(FirstToken, typeToConstruct, Args, Owner);
            }
            else if (Root.ResolvedType.RootValue == "Func")
            {
                return new FunctionPointerInvocation(resolver, FirstToken, Root, Args);
            }

            throw new ParserException(OpenParenToken, "This expression cannot be invoked like a function.");
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            Root = Root.ResolveWithTypeContext(resolver);

            if (Root is FunctionReference)
            {
                // this is okay.
            }
            else
            {
                throw new ParserException(OpenParenToken, "Cannot invoke this like a function.");
            }

            for (int i = 0; i < Args.Length; ++i)
            {
                Args[i] = Args[i].ResolveWithTypeContext(resolver);
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
