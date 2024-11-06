using Pastel.Parser.ParseNodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser
{
    internal class PastelCompiler
    {
        internal IDictionary<string, ExtensibleFunction> ExtensibleFunctions { get; private set; }
        internal Transpilers.AbstractTranspiler Transpiler { get; set; }
        public IInlineImportCodeLoader CodeLoader { get; private set; }

        public PastelContext Context { get; private set; }

        public PastelCompiler(
            PastelContext context,
            Language language,
            IDictionary<string, object> constants,
            IInlineImportCodeLoader inlineImportCodeLoader,
            ICollection<ExtensibleFunction> extensibleFunctions)
        {
            Context = context;

            CodeLoader = inlineImportCodeLoader;
            Transpiler = LanguageUtil.GetTranspiler(language);
            ExtensibleFunctions = extensibleFunctions == null
                ? new Dictionary<string, ExtensibleFunction>()
                : extensibleFunctions.ToDictionary(ef => ef.Name);
            StructDefinitions = new Dictionary<string, StructDefinition>();
            EnumDefinitions = new Dictionary<string, EnumDefinition>();
            ConstantDefinitions = new Dictionary<string, VariableDeclaration>();
            FunctionDefinitions = new Dictionary<string, FunctionDefinition>();
            ClassDefinitions = new Dictionary<string, ClassDefinition>();
            interpreterParser = new PastelParser(context, constants, inlineImportCodeLoader);
        }

        public override string ToString()
        {
            return "Pastel Compiler for " + Context.ToString();
        }

        private PastelParser interpreterParser;

        public Dictionary<string, StructDefinition> StructDefinitions { get; set; }
        internal Dictionary<string, EnumDefinition> EnumDefinitions { get; set; }
        internal Dictionary<string, VariableDeclaration> ConstantDefinitions { get; set; }
        public Dictionary<string, FunctionDefinition> FunctionDefinitions { get; set; }
        public Dictionary<string, ClassDefinition> ClassDefinitions { get; set; }

        public ClassDefinition[] GetClassDefinitions()
        {
            return ClassDefinitions.Keys
                .OrderBy(k => k)
                .Select(key => ClassDefinitions[key])
                .ToArray();
        }

        public StructDefinition[] GetStructDefinitions()
        {
            return StructDefinitions.Keys
                .OrderBy(k => k)
                .Select(key => StructDefinitions[key])
                .ToArray();
        }

        public FunctionDefinition[] GetFunctionDefinitions()
        {
            return FunctionDefinitions.Keys
                .OrderBy(k => k)
                .Select(key => FunctionDefinitions[key])
                .ToArray();
        }

        internal HashSet<string> ResolvedFunctions { get; set; }
        internal Queue<string> ResolutionQueue { get; set; }

        internal InlineConstant GetConstantDefinition(string name)
        {
            if (ConstantDefinitions.ContainsKey(name))
            {
                return (InlineConstant)ConstantDefinitions[name].Value;
            }
            return null;
        }

        internal EnumDefinition GetEnumDefinition(string name)
        {
            if (EnumDefinitions.ContainsKey(name))
            {
                return EnumDefinitions[name];
            }
            return null;
        }

        internal ClassDefinition GetClassDefinition(string name)
        {
            if (ClassDefinitions.ContainsKey(name))
            {
                return ClassDefinitions[name];
            }
            return null;
        }

        internal StructDefinition GetStructDefinition(string name)
        {
            if (StructDefinitions.ContainsKey(name))
            {
                return StructDefinitions[name];
            }
            return null;
        }

        internal FunctionDefinition GetFunctionDefinition(string name)
        {
            if (FunctionDefinitions.ContainsKey(name))
            {
                return FunctionDefinitions[name];
            }
            return null;
        }

        public void CompileBlobOfCode(string name, string code)
        {
            ICompilationEntity[] entities = interpreterParser.ParseText(name, code);
            foreach (ICompilationEntity entity in entities)
            {
                switch (entity.EntityType)
                {
                    case CompilationEntityType.FUNCTION:
                        FunctionDefinition fnDef = (FunctionDefinition)entity;
                        string functionName = fnDef.NameToken.Value;
                        if (FunctionDefinitions.ContainsKey(functionName))
                        {
                            throw new ParserException(fnDef.FirstToken, "Multiple definitions of function: '" + functionName + "'");
                        }
                        FunctionDefinitions[functionName] = fnDef;
                        break;

                    case CompilationEntityType.STRUCT:
                        StructDefinition structDef = (StructDefinition)entity;
                        string structName = structDef.NameToken.Value;
                        if (StructDefinitions.ContainsKey(structName))
                        {
                            throw new ParserException(structDef.FirstToken, "Multiple definitions of function: '" + structName + "'");
                        }
                        StructDefinitions[structName] = structDef;
                        break;

                    case CompilationEntityType.ENUM:
                        EnumDefinition enumDef = (EnumDefinition)entity;
                        string enumName = enumDef.NameToken.Value;
                        if (EnumDefinitions.ContainsKey(enumName))
                        {
                            throw new ParserException(enumDef.FirstToken, "Multiple definitions of function: '" + enumName + "'");
                        }
                        EnumDefinitions[enumName] = enumDef;
                        break;

                    case CompilationEntityType.CONSTANT:
                        VariableDeclaration assignment = (VariableDeclaration)entity;
                        string targetName = assignment.VariableNameToken.Value;
                        Dictionary<string, VariableDeclaration> lookup = ConstantDefinitions;
                        if (lookup.ContainsKey(targetName))
                        {
                            throw new ParserException(
                                assignment.FirstToken,
                                "Multiple definitions of : '" + targetName + "'");
                        }
                        lookup[targetName] = assignment;
                        break;

                    case CompilationEntityType.CLASS:
                        ClassDefinition classDef = (ClassDefinition)entity;
                        string className = classDef.NameToken.Value;
                        if (ClassDefinitions.ContainsKey(className))
                        {
                            throw new ParserException(classDef.FirstToken, "Multiple classes named '" + className + "'");
                        }
                        ClassDefinitions[className] = classDef;
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
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
            foreach (ClassDefinition cd in ClassDefinitions.Values)
            {
                if (cd.InheritTokens.Length > 1) throw new NotImplementedException(); // interfaces not implemented yet.
                foreach (Token parent in cd.InheritTokens)
                {
                    if (!ClassDefinitions.ContainsKey(parent.Value))
                        throw new ParserException(parent, "This is not a valid class.");

                    if (cd.ParentClass != null) throw new ParserException(parent, "Cannot have multpile base classes.");
                    cd.ParentClass = ClassDefinitions[parent.Value];
                }
            }

            HashSet<ClassDefinition> cycleCheck = new HashSet<ClassDefinition>();
            HashSet<ClassDefinition> safeClass = new HashSet<ClassDefinition>();
            foreach (ClassDefinition cd in ClassDefinitions.Values)
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
            foreach (string structName in StructDefinitions.Keys.OrderBy(t => t))
            {
                StructDefinition structDef = StructDefinitions[structName];
                for (int i = 0; i < structDef.LocalFieldTypes.Length; ++i)
                {
                    structDef.LocalFieldTypes[i].FinalizeType(this);
                }
            }
        }

        private void ResolveSignatureTypes()
        {
            foreach (string className in ClassDefinitions.Keys.OrderBy(t => t))
            {
                ClassDefinition classDef = ClassDefinitions[className];
                foreach (FunctionDefinition fd in classDef.Methods)
                {
                    fd.ResolveSignatureTypes(this);
                }

                classDef.Constructor.ResolveSignatureTypes(this);
            }

            foreach (string funcName in FunctionDefinitions.Keys.OrderBy(t => t))
            {
                FunctionDefinitions[funcName].ResolveSignatureTypes(this);
            }
        }

        private void ResolveConstants()
        {
            HashSet<string> cycleDetection = new HashSet<string>();
            foreach (EnumDefinition enumDef in EnumDefinitions.Values)
            {
                if (enumDef.UnresolvedValues.Count > 0)
                {
                    enumDef.DoConstantResolutions(cycleDetection, this);
                }
            }

            foreach (VariableDeclaration constDef in ConstantDefinitions.Values)
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
            StructDefinition[] structDefs = StructDefinitions.Values.ToArray();
            foreach (StructDefinition sd in structDefs)
            {
                cycleCheck[sd] = 0;
            }

            foreach (StructDefinition sd in structDefs)
            {
                sd.ResolveParentChain(StructDefinitions, cycleCheck);
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

            foreach (ClassDefinition cd in ClassDefinitions.Values)
            {
                cd.ResolveNamesAndCullUnusedCode(this);
            }

            foreach (string functionName in FunctionDefinitions.Keys)
            {
                ResolutionQueue.Enqueue(functionName);
            }

            while (ResolutionQueue.Count > 0)
            {
                string functionName = ResolutionQueue.Dequeue();
                if (!ResolvedFunctions.Contains(functionName)) // multiple invocations in a function will put it in the queue multiple times.
                {
                    ResolvedFunctions.Add(functionName);
                    FunctionDefinitions[functionName].ResolveNamesAndCullUnusedCode(this);
                }
            }

            List<string> unusedFunctions = new List<string>();
            foreach (string functionName in FunctionDefinitions.Keys)
            {
                if (!ResolvedFunctions.Contains(functionName))
                {
                    unusedFunctions.Add(functionName);
                }
            }

            Dictionary<string, FunctionDefinition> finalFunctions = new Dictionary<string, FunctionDefinition>();
            foreach (string usedFunctionName in ResolvedFunctions)
            {
                finalFunctions[usedFunctionName] = FunctionDefinitions[usedFunctionName];
            }
            FunctionDefinitions = finalFunctions;
        }

        private void ResolveTypes()
        {
            foreach (ClassDefinition cd in ClassDefinitions.Keys.OrderBy(s => s).Select(n => ClassDefinitions[n]))
            {
                cd.ResolveTypes(this);
            }

            string[] functionNames = FunctionDefinitions.Keys.OrderBy(s => s).ToArray();
            foreach (string functionName in functionNames)
            {
                FunctionDefinition functionDefinition = FunctionDefinitions[functionName];
                functionDefinition.ResolveTypes(this);
            }
        }

        private void ResolveWithTypeContext()
        {
            string[] functionNames = FunctionDefinitions.Keys.OrderBy(s => s).ToArray();
            foreach (string functionName in functionNames)
            {
                FunctionDefinition functionDefinition = FunctionDefinitions[functionName];
                functionDefinition.ResolveWithTypeContext(this);
            }
        }

        // Delete once migrated to PastelContext
        internal Dictionary<string, string> GetStructCodeByClassTEMP(Transpilers.TranspilerContext ctx, string indent)
        {
            Dictionary<string, string> output = new Dictionary<string, string>();
            foreach (StructDefinition sd in GetStructDefinitions())
            {
                string name = sd.NameToken.Value;
                ctx.Transpiler.GenerateCodeForStruct(ctx, sd);
                output[name] = ctx.FlushAndClearBuffer();
            }
            return output;
        }

        // Delete once migrated to PastelContext
        internal string GetFunctionDeclarationsTEMP(Transpilers.TranspilerContext ctx, string indent)
        {
            foreach (FunctionDefinition fd in GetFunctionDefinitions())
            {
                ctx.Transpiler.GenerateCodeForFunctionDeclaration(ctx, fd, true);
                ctx.Append('\n');
            }

            return Indent(ctx.FlushAndClearBuffer().Trim(), indent);
        }

        internal Dictionary<string, string> GetFunctionCodeAsLookupTEMP(Transpilers.TranspilerContext ctx, string indent)
        {
            Dictionary<string, string> output = new Dictionary<string, string>();
            foreach (FunctionDefinition fd in GetFunctionDefinitions())
            {
                ctx.Transpiler.GenerateCodeForFunction(ctx, fd, true);
                output[fd.NameToken.Value] = Indent(ctx.FlushAndClearBuffer().Trim(), indent);
            }

            return output;
        }

        private static string Indent(string code, string indent)
        {
            if (indent.Length == 0) return code;

            return string.Join('\n', code
                .Split('\n')
                .Select(s => s.Trim())
                .Select(s => s.Length > 0 ? indent + s : ""));
        }
    }
}
