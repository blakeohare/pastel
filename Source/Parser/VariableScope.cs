using Pastel.Parser.ParseNodes;
using System.Collections.Generic;

namespace Pastel.Parser
{
    internal class VariableScope
    {
        // TODO: this should go away. It's being used to determine if return statements are returning the correct type.
        // Statements should KNOW what container they're in and not be relayed this information by the variable scope.
        public ICompilationEntity RootFunctionOrConstructorDefinition;

        private VariableScope parent = null;
        private Dictionary<string, PType> type = new Dictionary<string, PType>();

        public VariableScope() { }

        public VariableScope(ICompilationEntity functionDef)
        {
            if (!(functionDef is FunctionDefinition)) throw new System.InvalidOperationException();
            this.RootFunctionOrConstructorDefinition = functionDef;
        }

        public VariableScope(VariableScope parent)
        {
            this.parent = parent;
            this.RootFunctionOrConstructorDefinition = parent.RootFunctionOrConstructorDefinition;
        }

        public void DeclareVariables(Token nameToken, PType type)
        {
            string name = nameToken.Value;
            if (GetTypeOfVariable(name) != null)
            {
                throw new UNTESTED_ParserException(
                    nameToken, 
                    "This declaration of '" + name + "' conflicts with a previous declaration.");
            }

            this.type[name] = type;
        }

        public PType GetTypeOfVariable(string name)
        {
            if (this.type.TryGetValue(name, out PType output))
            {
                return output;
            }

            if (this.parent != null)
            {
                return this.parent.GetTypeOfVariable(name);
            }

            return null;
        }
    }
}
