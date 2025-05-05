using System.Collections.Generic;

namespace Pastel.Parser.ParseNodes
{
    internal class Variable : Expression
    {
        public Variable(Token token, ICompilationEntity owner) 
            : base(ExpressionType.VARIABLE, token, owner)
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
            string name = this.Name;

            InlineConstant constantValue = resolver.CompilerContext.GetConstantDefinition(name);
            if (constantValue != null)
            {
                return constantValue.CloneWithNewToken(this.FirstToken);
            }

            if (name == "Core")
            {
                throw new UNTESTED_ParserException(
                    this.FirstToken,
                    "Core is a namespace and cannot be used like this.");
            }

            FunctionDefinition functionDefinition = resolver.GetFunctionDefinition(name);
            if (functionDefinition != null)
            {
                return new FunctionReference(this.FirstToken, functionDefinition, this.Owner);
            }

            EnumDefinition enumDefinition = resolver.GetEnumDefinition(name);
            if (enumDefinition != null)
            {
                return new EnumReference(this.FirstToken, enumDefinition, this.Owner);
            }

            return this;
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            PType type = varScope.GetTypeOfVariable(this.Name);
            this.ResolvedType = type;
            if (type == null)
            {
                if (this.IsFunctionInvocation)
                {
                    throw new TestedParserException(
                        this.FirstToken,
                        "The function '" + this.Name + "' is not defined.");
                }

                throw new TestedParserException(
                    this.FirstToken,
                    "The variable '" + this.Name + "' is not defined.");
            }

            return this;
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            return this;
        }
    }
}
