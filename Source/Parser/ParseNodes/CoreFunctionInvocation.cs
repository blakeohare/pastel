﻿using System;
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
            Function = function;
            Args = args.ToArray();
        }

        public CoreFunctionInvocation(Token firstToken, CoreFunction function, Expression context, IList<Expression> args, ICompilationEntity owner)
           : this(firstToken, function, PushInFront(context, args), owner)
        { }

        private static IList<Expression> PushInFront(Expression ex, IList<Expression> others)
        {
            List<Expression> expressions = new List<Expression>() { ex };
            expressions.AddRange(others);
            return expressions;
        }

        public override Expression ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            throw new NotImplementedException();
        }

        internal override Expression ResolveType(VariableScope varScope, PastelCompiler compiler)
        {
            // The args were already resolved.
            // This ensures that they match the core function definition

            PType[] expectedTypes = CoreFunctionUtil.GetCoreFunctionArgTypes(Function);
            bool[] isArgRepeated = CoreFunctionUtil.GetCoreFunctionIsArgTypeRepeated(Function);

            switch (Function)
            {
                case CoreFunction.FORCE_PARENS:
                    if (Args.Length != 1) throw new ParserException(FirstToken, "Expected 1 arg.");

                    return new ForcedParenthesis(FirstToken, Args[0]);
            }

            Dictionary<string, PType> templateLookup = new Dictionary<string, PType>();

            int verificationLength = expectedTypes.Length;
            if (verificationLength > 0 && isArgRepeated[isArgRepeated.Length - 1])
            {
                verificationLength--;
            }

            for (int i = 0; i < verificationLength; ++i)
            {
                if (!PType.CheckAssignmentWithTemplateOutput(compiler, expectedTypes[i], Args[i].ResolvedType, templateLookup))
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
                        if (!PType.CheckAssignment(compiler, expectedType, Args[i].ResolvedType))
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

        internal override Expression ResolveWithTypeContext(PastelCompiler compiler)
        {
            for (int i = 0; i < Args.Length; ++i)
            {
                Args[i] = Args[i].ResolveWithTypeContext(compiler);
            }
            return this;
        }
    }
}