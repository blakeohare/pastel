using System.Collections.Generic;

namespace Pastel.Parser.ParseNodes
{
    internal class Variable : Expression
    {
        public Variable(Token token, ICompilationEntity owner) : base(token, owner)
        {
            this.IsFunctionInvocation = false;
            this.ApplyPrefix = true;
        }

        // Some generated code needs to namespace itself different to prevent collision with translated variables.
        // For example, some of the Python switch statement stuff uses temporary variables that are not in the original code.
        public bool ApplyPrefix { get; set; }

        public string Name { get { return this.FirstToken.Value; } }

        public bool IsFunctionInvocation { get; set; }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            string name = Name;

            InlineConstant constantValue = resolver.CompilerContext.GetConstantDefinition(name);
            if (constantValue != null)
            {
                return constantValue.CloneWithNewToken(FirstToken);
            }

            if (name == "Core")
            {
                return new CoreNamespaceReference(FirstToken, Owner);
            }

            if (name == "Extension")
            {
                return new ExtensibleNamespaceReference(FirstToken, Owner);
            }

            FunctionDefinition functionDefinition = resolver.GetFunctionDefinition(name);
            if (functionDefinition != null)
            {
                return new FunctionReference(FirstToken, functionDefinition, Owner);
            }

            EnumDefinition enumDefinition = resolver.GetEnumDefinition(name);
            if (enumDefinition != null)
            {
                return new EnumReference(FirstToken, enumDefinition, Owner);
            }

            return this;
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            PType type = varScope.GetTypeOfVariable(Name);
            ResolvedType = type;
            if (type == null)
            {
                throw new ParserException(
                    FirstToken,
                    "The " + (IsFunctionInvocation ? "function" : "variable") + " '" + Name + "' is not defined.");
            }

            return this;
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            return this;
        }
    }
}
