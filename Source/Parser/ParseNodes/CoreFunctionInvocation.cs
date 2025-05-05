using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser.ParseNodes
{
    internal class CoreFunctionInvocation : Expression
    {
        public CoreFunction Function { get; set; }
        public Expression[] Args { get; set; }

        public CoreFunctionInvocation(Token firstToken, CoreFunction function, IList<Expression> args, ICompilationEntity owner) : base(firstToken, owner)
        {
            this.Function = function;
            this.Args = args.ToArray();
        }

        public CoreFunctionInvocation(Token firstToken, CoreFunction function, Expression context, IList<Expression> args, ICompilationEntity owner)
           : this(firstToken, function, [context, ..args], owner)
        { }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            throw new NotImplementedException();
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            // The args were already resolved.
            // This ensures that they match the core function definition

            PType[] expectedTypes = CoreFunctionUtil.GetCoreFunctionArgTypes(this.Function);
            bool[] isArgRepeated = CoreFunctionUtil.GetCoreFunctionIsArgTypeRepeated(this.Function);

            Dictionary<string, PType> templateLookup = new Dictionary<string, PType>();

            int verificationLength = expectedTypes.Length;
            if (verificationLength > 0 && isArgRepeated[isArgRepeated.Length - 1])
            {
                verificationLength--;
            }

            for (int i = 0; i < verificationLength; ++i)
            {
                if (!PType.CheckAssignmentWithTemplateOutput(resolver, expectedTypes[i], Args[i].ResolvedType, templateLookup))
                {
                    PType expectedType = expectedTypes[i];
                    if (templateLookup.ContainsKey(expectedType.ToString()))
                    {
                        expectedType = templateLookup[expectedType.ToString()];
                    }
                    throw new ParserException(Args[i].FirstToken, "Incorrect type. Expected " + expectedType + " but found " + Args[i].ResolvedType + ".");
                }
            }

            if (expectedTypes.Length < Args.Length)
            {
                if (isArgRepeated[isArgRepeated.Length - 1])
                {
                    PType expectedType = expectedTypes[expectedTypes.Length - 1];
                    for (int i = expectedTypes.Length; i < Args.Length; ++i)
                    {
                        if (!PType.CheckAssignment(resolver, expectedType, Args[i].ResolvedType))
                        {
                            throw new ParserException(Args[i].FirstToken, "Incorrect type. Expected " + expectedTypes[i] + " but found " + Args[i].ResolvedType + ".");
                        }
                    }
                }
                else
                {
                    throw new ParserException(FirstToken, "Too many arguments.");
                }
            }

            PType returnType = CoreFunctionUtil.GetCoreFunctionReturnType(Function);

            if (returnType.HasTemplates)
            {
                returnType = returnType.ResolveTemplates(templateLookup);
            }

            ResolvedType = returnType;

            return this;
        }

        internal override InlineConstant DoConstantResolution(HashSet<string> cycleDetection, Resolver resolver)
        {
            for (int i = 0; i < this.Args.Length; i++)
            {
                this.Args[i] = this.Args[i].DoConstantResolution(cycleDetection, resolver);
            }

            return this.TryCompileTimeResolve()
                ?? base.DoConstantResolution(cycleDetection, resolver);
        }

        private InlineConstant TryCompileTimeResolve()
        {
            if (this.Function == CoreFunction.ORD &&
                this.Args[0] is InlineConstant ic &&
                ic.Type.IsChar)
            {
                // TODO: why is it coming as both types? Is it still coming as both?
                char c = (ic.Value is string str) ? str[0] : (char)ic.Value;
                InlineConstant output = new InlineConstant(
                    PType.INT, this.FirstToken, (int)c, this.Owner);
                return output;
            }

            return null;
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            for (int i = 0; i < Args.Length; ++i)
            {
                Args[i] = Args[i].ResolveWithTypeContext(resolver);
            }

            return this.TryCompileTimeResolve() ?? (Expression)this;
        }
    }
}
