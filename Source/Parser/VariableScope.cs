using Pastel.Parser.ParseNodes;
using System.Collections.Generic;

namespace Pastel.Parser
{
    internal class VariableScope
    {
        // TODO: this should go away. It's being used to determine if return statements are returning the correct type.
        // Executables should KNOW what container they're in and not be relayed this information by the variable scope.
        public ICompilationEntity RootFunctionOrConstructorDefinition;

        private VariableScope parent = null;
        private Dictionary<string, PType> type = new Dictionary<string, PType>();

        public VariableScope() { }

        public VariableScope(ICompilationEntity functionDef)
        {
            if (!(functionDef is FunctionDefinition) && !(functionDef is ConstructorDefinition)) throw new System.InvalidOperationException();
            RootFunctionOrConstructorDefinition = functionDef;
        }

        public VariableScope(VariableScope parent)
        {
            this.parent = parent;
            RootFunctionOrConstructorDefinition = parent.RootFunctionOrConstructorDefinition;
        }

        public void DeclareVariables(Token nameToken, PType type)
        {
            string name = nameToken.Value;
            if (GetTypeOfVariable(name) != null)
            {
                throw new ParserException(nameToken, "This declaration of '" + name + "' conflicts with a previous declaration.");
            }

            this.type[name] = type;
        }

        public PType GetTypeOfVariable(string name)
        {
            PType output;
            if (type.TryGetValue(name, out output))
            {
                return output;
            }

            if (parent != null)
            {
                return parent.GetTypeOfVariable(name);
            }

            return null;
        }
    }
}
