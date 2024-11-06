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
        private Dictionary<string, ClassDefinition> classDefinitions;

        internal HashSet<string> ResolvedFunctions { get; set; }
        internal Queue<string> ResolutionQueue { get; set; }

        public Resolver(
            PastelCompiler compilerContext,
            Dictionary<string, EnumDefinition> enumDefinitions,
            Dictionary<string, VariableDeclaration> constantDefinitions,
            Dictionary<string, FunctionDefinition> functionDefinitions,
            Dictionary<string, StructDefinition> structDefinitions,
            Dictionary<string, ClassDefinition> classDefinitions)
        {
            this.CompilerContext = compilerContext;
            this.enumDefinitions = enumDefinitions;
            this.constantDefinitions = constantDefinitions;
            this.functionDefinitions = functionDefinitions;
            this.structDefinitions = structDefinitions;
            this.classDefinitions = classDefinitions;
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
            ResolveClassHierarchy();
            ResolveConstants();
            ResolveStructTypes();
            ResolveStructParentChain();
            ResolveNamesAndCullUnusedCode();
            ResolveSignatureTypes();
            ResolveTypes();
            ResolveWithTypeContext();
        }

        private void ResolveClassHierarchy()
        {
            foreach (ClassDefinition cd in this.classDefinitions.Values)
            {
                if (cd.InheritTokens.Length > 1) throw new NotImplementedException(); // interfaces not implemented yet.
                foreach (Token parent in cd.InheritTokens)
                {
                    if (!this.classDefinitions.ContainsKey(parent.Value))
                        throw new ParserException(parent, "This is not a valid class.");

                    if (cd.ParentClass != null) throw new ParserException(parent, "Cannot have multpile base classes.");
                    cd.ParentClass = this.classDefinitions[parent.Value];
                }
            }

            HashSet<ClassDefinition> cycleCheck = new HashSet<ClassDefinition>();
            HashSet<ClassDefinition> safeClass = new HashSet<ClassDefinition>();
            foreach (ClassDefinition cd in this.classDefinitions.Values)
            {
                ClassDefinition walker = cd;
                while (walker != null && !safeClass.Contains(walker))
                {
                    if (cycleCheck.Contains(walker)) throw new ParserException(cd.FirstToken, "This class has a cycle in its inheritance chain.");
                    cycleCheck.Add(walker);
                    walker = walker.ParentClass;
                }

                foreach (ClassDefinition safe in cycleCheck)
                {
                    safeClass.Add(safe);
                }
                cycleCheck.Clear();
            }
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
            foreach (string className in this.classDefinitions.Keys.OrderBy(t => t))
            {
                ClassDefinition classDef = this.classDefinitions[className];
                foreach (FunctionDefinition fd in classDef.Methods)
                {
                    fd.ResolveSignatureTypes(this);
                }

                classDef.Constructor.ResolveSignatureTypes(this);
            }

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
            ResolvedFunctions = new HashSet<string>();
            ResolutionQueue = new Queue<string>();

            foreach (ClassDefinition cd in this.classDefinitions.Values)
            {
                cd.ResolveNamesAndCullUnusedCode(this);
            }

            foreach (string functionName in this.functionDefinitions.Keys)
            {
                ResolutionQueue.Enqueue(functionName);
            }

            while (ResolutionQueue.Count > 0)
            {
                string functionName = ResolutionQueue.Dequeue();
                if (!ResolvedFunctions.Contains(functionName)) // multiple invocations in a function will put it in the queue multiple times.
                {
                    ResolvedFunctions.Add(functionName);
                    this.functionDefinitions[functionName].ResolveNamesAndCullUnusedCode(this);
                }
            }

            List<string> unusedFunctions = new List<string>();
            foreach (string functionName in this.functionDefinitions.Keys)
            {
                if (!ResolvedFunctions.Contains(functionName))
                {
                    unusedFunctions.Add(functionName);
                }
            }

            Dictionary<string, FunctionDefinition> finalFunctions = new Dictionary<string, FunctionDefinition>();
            foreach (string usedFunctionName in ResolvedFunctions)
            {
                finalFunctions[usedFunctionName] = this.functionDefinitions[usedFunctionName];
            }
            this.functionDefinitions = finalFunctions;
        }

        private void ResolveTypes()
        {
            foreach (ClassDefinition cd in this.classDefinitions.Keys.OrderBy(s => s).Select(n => this.classDefinitions[n]))
            {
                cd.ResolveTypes(this);
            }

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
