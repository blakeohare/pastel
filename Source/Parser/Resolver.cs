using Pastel.Parser.ParseNodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser
{
    internal class Resolver
    {
        public PastelCompiler CompilerContext { get; private set; }

        private Dictionary<string, EnumDefinition> enumDefinitions;
        private Dictionary<string, VariableDeclaration> constantDefinitions;
        private Dictionary<string, FunctionDefinition> functionDefinitions;
        private Dictionary<string, StructDefinition> structDefinitions;

        internal HashSet<string> ResolvedFunctions { get; set; }
        internal Queue<string> ResolutionQueue { get; set; }

        public Resolver(
            PastelCompiler compilerContext,
            Dictionary<string, EnumDefinition> enumDefinitions,
            Dictionary<string, VariableDeclaration> constantDefinitions,
            Dictionary<string, FunctionDefinition> functionDefinitions,
            Dictionary<string, StructDefinition> structDefinitions)
        {
            this.CompilerContext = compilerContext;
            this.enumDefinitions = enumDefinitions;
            this.constantDefinitions = constantDefinitions;
            this.functionDefinitions = functionDefinitions;
            this.structDefinitions = structDefinitions;
        }

        public EnumDefinition GetEnumDefinition(string name)
        {
            return this.enumDefinitions.TryGetValue(name, out EnumDefinition val) ? val : null;
        }

        public FunctionDefinition GetFunctionDefinition(string name)
        {
            return this.functionDefinitions.TryGetValue(name, out FunctionDefinition fd) ? fd : null;
        }

        public void Resolve()
        {
            ResolveConstants();
            ResolveStructTypes();
            ResolveStructParentChain();
            ResolveNamesAndCullUnusedCode();
            ResolveSignatureTypes();
            ResolveTypes();
            ResolveWithTypeContext();
        }

        private void ResolveStructTypes()
        {
            foreach (string structName in this.structDefinitions.Keys.OrderBy(t => t))
            {
                StructDefinition structDef = this.structDefinitions[structName];
                for (int i = 0; i < structDef.LocalFieldTypes.Length; ++i)
                {
                    structDef.LocalFieldTypes[i].FinalizeType(this);
                }
            }
        }

        private void ResolveSignatureTypes()
        {
            foreach (string funcName in this.functionDefinitions.Keys.OrderBy(t => t))
            {
                this.functionDefinitions[funcName].ResolveSignatureTypes(this);
            }
        }

        private void ResolveConstants()
        {
            HashSet<string> cycleDetection = new HashSet<string>();
            foreach (EnumDefinition enumDef in this.enumDefinitions.Values)
            {
                if (enumDef.UnresolvedValues.Count > 0)
                {
                    enumDef.DoConstantResolutions(cycleDetection, this);
                }
            }

            foreach (VariableDeclaration constDef in this.constantDefinitions.Values)
            {
                if (!(constDef.Value is InlineConstant))
                {
                    string name = constDef.VariableNameToken.Value;
                    cycleDetection.Add(name);
                    constDef.DoConstantResolutions(cycleDetection, this);
                    cycleDetection.Remove(name);
                }
            }
        }

        private void ResolveStructParentChain()
        {
            Dictionary<StructDefinition, int> cycleCheck = new Dictionary<StructDefinition, int>();
            StructDefinition[] structDefs = this.structDefinitions.Values.ToArray();
            foreach (StructDefinition sd in structDefs)
            {
                cycleCheck[sd] = 0;
            }

            foreach (StructDefinition sd in structDefs)
            {
                sd.ResolveParentChain(this.structDefinitions, cycleCheck);
            }

            foreach (StructDefinition sd in structDefs)
            {
                sd.ResolveInheritedFields();
            }
        }

        private void ResolveNamesAndCullUnusedCode()
        {
            this.ResolvedFunctions = new HashSet<string>();
            this.ResolutionQueue = new Queue<string>();

            foreach (string functionName in this.functionDefinitions.Keys)
            {
                this.ResolutionQueue.Enqueue(functionName);
            }

            while (this.ResolutionQueue.Count > 0)
            {
                string functionName = this.ResolutionQueue.Dequeue();
                if (!this.ResolvedFunctions.Contains(functionName)) // multiple invocations in a function will put it in the queue multiple times.
                {
                    this.ResolvedFunctions.Add(functionName);
                    this.functionDefinitions[functionName].ResolveNamesAndCullUnusedCode(this);
                }
            }

            List<string> unusedFunctions = [];
            foreach (string functionName in this.functionDefinitions.Keys)
            {
                if (!this.ResolvedFunctions.Contains(functionName))
                {
                    unusedFunctions.Add(functionName);
                }
            }

            Dictionary<string, FunctionDefinition> finalFunctions = [];
            foreach (string usedFunctionName in ResolvedFunctions)
            {
                finalFunctions[usedFunctionName] = this.functionDefinitions[usedFunctionName];
            }
            this.functionDefinitions = finalFunctions;
        }

        private void ResolveTypes()
        {
            string[] functionNames = this.functionDefinitions.Keys.OrderBy(s => s).ToArray();
            foreach (string functionName in functionNames)
            {
                FunctionDefinition functionDefinition = this.functionDefinitions[functionName];
                functionDefinition.ResolveTypes(this);
            }
        }

        private void ResolveWithTypeContext()
        {
            string[] functionNames = this.functionDefinitions.Keys.OrderBy(s => s).ToArray();
            foreach (string functionName in functionNames)
            {
                FunctionDefinition functionDefinition = this.functionDefinitions[functionName];
                functionDefinition.ResolveWithTypeContext(this);
            }
        }
    }
}
