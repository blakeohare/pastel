using Pastel.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel
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
            this.Context = context;

            this.CodeLoader = inlineImportCodeLoader;
            this.Transpiler = LanguageUtil.GetTranspiler(language);
            this.ExtensibleFunctions = extensibleFunctions == null
                ? new Dictionary<string, ExtensibleFunction>()
                : extensibleFunctions.ToDictionary(ef => ef.Name);
            this.StructDefinitions = new Dictionary<string, StructDefinition>();
            this.EnumDefinitions = new Dictionary<string, EnumDefinition>();
            this.ConstantDefinitions = new Dictionary<string, VariableDeclaration>();
            this.FunctionDefinitions = new Dictionary<string, FunctionDefinition>();
            this.ClassDefinitions = new Dictionary<string, ClassDefinition>();
            this.interpreterParser = new PastelParser(context, constants, inlineImportCodeLoader);
        }

        public override string ToString()
        {
            return "Pastel Compiler for " + this.Context.ToString();
        }

        private PastelParser interpreterParser;

        public Dictionary<string, StructDefinition> StructDefinitions { get; set; }
        internal Dictionary<string, EnumDefinition> EnumDefinitions { get; set; }
        internal Dictionary<string, VariableDeclaration> ConstantDefinitions { get; set; }
        public Dictionary<string, FunctionDefinition> FunctionDefinitions { get; set; }
        public Dictionary<string, ClassDefinition> ClassDefinitions { get; set; }

        public ClassDefinition[] GetClassDefinitions()
        {
            return this.ClassDefinitions.Keys
                .OrderBy(k => k)
                .Select(key => this.ClassDefinitions[key])
                .ToArray();
        }

        public StructDefinition[] GetStructDefinitions()
        {
            return this.StructDefinitions.Keys
                .OrderBy(k => k)
                .Select(key => this.StructDefinitions[key])
                .ToArray();
        }

        public FunctionDefinition[] GetFunctionDefinitions()
        {
            return this.FunctionDefinitions.Keys
                .OrderBy(k => k)
                .Select(key => this.FunctionDefinitions[key])
                .ToArray();
        }

        internal HashSet<string> ResolvedFunctions { get; set; }
        internal Queue<string> ResolutionQueue { get; set; }

        internal InlineConstant GetConstantDefinition(string name)
        {
            if (this.ConstantDefinitions.ContainsKey(name))
            {
                return (InlineConstant)this.ConstantDefinitions[name].Value;
            }
            return null;
        }

        internal EnumDefinition GetEnumDefinition(string name)
        {
            if (this.EnumDefinitions.ContainsKey(name))
            {
                return this.EnumDefinitions[name];
            }
            return null;
        }

        internal ClassDefinition GetClassDefinition(string name)
        {
            if (this.ClassDefinitions.ContainsKey(name))
            {
                return this.ClassDefinitions[name];
            }
            return null;
        }

        internal StructDefinition GetStructDefinition(string name)
        {
            if (this.StructDefinitions.ContainsKey(name))
            {
                return this.StructDefinitions[name];
            }
            return null;
        }

        internal FunctionDefinition GetFunctionDefinition(string name)
        {
            if (this.FunctionDefinitions.ContainsKey(name))
            {
                return this.FunctionDefinitions[name];
            }
            return null;
        }

        public void CompileBlobOfCode(string name, string code)
        {
            ICompilationEntity[] entities = this.interpreterParser.ParseText(name, code);
            foreach (ICompilationEntity entity in entities)
            {
                switch (entity.EntityType)
                {
                    case CompilationEntityType.FUNCTION:
                        FunctionDefinition fnDef = (FunctionDefinition)entity;
                        string functionName = fnDef.NameToken.Value;
                        if (this.FunctionDefinitions.ContainsKey(functionName))
                        {
                            throw new ParserException(fnDef.FirstToken, "Multiple definitions of function: '" + functionName + "'");
                        }
                        this.FunctionDefinitions[functionName] = fnDef;
                        break;

                    case CompilationEntityType.STRUCT:
                        StructDefinition structDef = (StructDefinition)entity;
                        string structName = structDef.NameToken.Value;
                        if (this.StructDefinitions.ContainsKey(structName))
                        {
                            throw new ParserException(structDef.FirstToken, "Multiple definitions of function: '" + structName + "'");
                        }
                        this.StructDefinitions[structName] = structDef;
                        break;

                    case CompilationEntityType.ENUM:
                        EnumDefinition enumDef = (EnumDefinition)entity;
                        string enumName = enumDef.NameToken.Value;
                        if (this.EnumDefinitions.ContainsKey(enumName))
                        {
                            throw new ParserException(enumDef.FirstToken, "Multiple definitions of function: '" + enumName + "'");
                        }
                        this.EnumDefinitions[enumName] = enumDef;
                        break;

                    case CompilationEntityType.CONSTANT:
                        VariableDeclaration assignment = (VariableDeclaration)entity;
                        string targetName = assignment.VariableNameToken.Value;
                        Dictionary<string, VariableDeclaration> lookup = this.ConstantDefinitions;
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
                        if (this.ClassDefinitions.ContainsKey(className))
                        {
                            throw new ParserException(classDef.FirstToken, "Multiple classes named '" + className + "'");
                        }
                        this.ClassDefinitions[className] = classDef;
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public void Resolve()
        {
            this.ResolveClassHierarchy();
            this.ResolveConstants();
            this.ResolveStructTypes();
            this.ResolveStructParentChain();
            this.ResolveNamesAndCullUnusedCode();
            this.ResolveSignatureTypes();
            this.ResolveTypes();
            this.ResolveWithTypeContext();
        }

        private void ResolveClassHierarchy()
        {
            foreach (ClassDefinition cd in this.ClassDefinitions.Values)
            {
                if (cd.InheritTokens.Length > 1) throw new NotImplementedException(); // interfaces not implemented yet.
                foreach (Token parent in cd.InheritTokens)
                {
                    if (!this.ClassDefinitions.ContainsKey(parent.Value))
                        throw new ParserException(parent, "This is not a valid class.");

                    if (cd.ParentClass != null) throw new ParserException(parent, "Cannot have multpile base classes.");
                    cd.ParentClass = this.ClassDefinitions[parent.Value];
                }
            }

            HashSet<ClassDefinition> cycleCheck = new HashSet<ClassDefinition>();
            HashSet<ClassDefinition> safeClass = new HashSet<ClassDefinition>();
            foreach (ClassDefinition cd in this.ClassDefinitions.Values)
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
            foreach (string structName in this.StructDefinitions.Keys.OrderBy(t => t))
            {
                StructDefinition structDef = this.StructDefinitions[structName];
                for (int i = 0; i < structDef.LocalFieldTypes.Length; ++i)
                {
                    structDef.LocalFieldTypes[i].FinalizeType(this);
                }
            }
        }

        private void ResolveSignatureTypes()
        {
            foreach (string className in this.ClassDefinitions.Keys.OrderBy(t => t))
            {
                ClassDefinition classDef = this.ClassDefinitions[className];
                foreach (FunctionDefinition fd in classDef.Methods)
                {
                    fd.ResolveSignatureTypes(this);
                }

                classDef.Constructor.ResolveSignatureTypes(this);
            }

            foreach (string funcName in this.FunctionDefinitions.Keys.OrderBy(t => t))
            {
                this.FunctionDefinitions[funcName].ResolveSignatureTypes(this);
            }
        }

        private void ResolveConstants()
        {
            HashSet<string> cycleDetection = new HashSet<string>();
            foreach (EnumDefinition enumDef in this.EnumDefinitions.Values)
            {
                if (enumDef.UnresolvedValues.Count > 0)
                {
                    enumDef.DoConstantResolutions(cycleDetection, this);
                }
            }

            foreach (VariableDeclaration constDef in this.ConstantDefinitions.Values)
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
            StructDefinition[] structDefs = this.StructDefinitions.Values.ToArray();
            foreach (StructDefinition sd in structDefs)
            {
                cycleCheck[sd] = 0;
            }

            foreach (StructDefinition sd in structDefs)
            {
                sd.ResolveParentChain(this.StructDefinitions, cycleCheck);
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

            foreach (ClassDefinition cd in this.ClassDefinitions.Values)
            {
                cd.ResolveNamesAndCullUnusedCode(this);
            }

            foreach (string functionName in this.FunctionDefinitions.Keys)
            {
                this.ResolutionQueue.Enqueue(functionName);
            }

            while (this.ResolutionQueue.Count > 0)
            {
                string functionName = this.ResolutionQueue.Dequeue();
                if (!this.ResolvedFunctions.Contains(functionName)) // multiple invocations in a function will put it in the queue multiple times.
                {
                    this.ResolvedFunctions.Add(functionName);
                    this.FunctionDefinitions[functionName].ResolveNamesAndCullUnusedCode(this);
                }
            }

            List<string> unusedFunctions = new List<string>();
            foreach (string functionName in this.FunctionDefinitions.Keys)
            {
                if (!this.ResolvedFunctions.Contains(functionName))
                {
                    unusedFunctions.Add(functionName);
                }
            }

            Dictionary<string, FunctionDefinition> finalFunctions = new Dictionary<string, FunctionDefinition>();
            foreach (string usedFunctionName in this.ResolvedFunctions)
            {
                finalFunctions[usedFunctionName] = this.FunctionDefinitions[usedFunctionName];
            }
            this.FunctionDefinitions = finalFunctions;
        }

        private void ResolveTypes()
        {
            foreach (ClassDefinition cd in this.ClassDefinitions.Keys.OrderBy(s => s).Select(n => this.ClassDefinitions[n]))
            {
                cd.ResolveTypes(this);
            }

            string[] functionNames = this.FunctionDefinitions.Keys.OrderBy(s => s).ToArray();
            foreach (string functionName in functionNames)
            {
                FunctionDefinition functionDefinition = this.FunctionDefinitions[functionName];
                functionDefinition.ResolveTypes(this);
            }
        }

        private void ResolveWithTypeContext()
        {
            string[] functionNames = this.FunctionDefinitions.Keys.OrderBy<string, string>(s => s).ToArray();
            foreach (string functionName in functionNames)
            {
                FunctionDefinition functionDefinition = this.FunctionDefinitions[functionName];
                functionDefinition.ResolveWithTypeContext(this);
            }
        }

        // Delete once migrated to PastelContext
        internal Dictionary<string, string> GetStructCodeByClassTEMP(Transpilers.TranspilerContext ctx, string indent)
        {
            Dictionary<string, string> output = new Dictionary<string, string>();
            foreach (StructDefinition sd in this.GetStructDefinitions())
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
            foreach (FunctionDefinition fd in this.GetFunctionDefinitions())
            {
                ctx.Transpiler.GenerateCodeForFunctionDeclaration(ctx, fd, true);
                ctx.Append(ctx.Transpiler.NewLine);
            }

            return Indent(ctx.FlushAndClearBuffer().Trim(), ctx.Transpiler.NewLine, indent);
        }

        internal Dictionary<string, string> GetFunctionCodeAsLookupTEMP(Transpilers.TranspilerContext ctx, string indent)
        {
            Dictionary<string, string> output = new Dictionary<string, string>();
            foreach (FunctionDefinition fd in this.GetFunctionDefinitions())
            {
                ctx.Transpiler.GenerateCodeForFunction(ctx, fd, true);
                output[fd.NameToken.Value] = Indent(ctx.FlushAndClearBuffer().Trim(), ctx.Transpiler.NewLine, indent);
            }

            return output;
        }

        private static string Indent(string code, string newline, string indent)
        {
            if (indent.Length == 0) return code;

            return string.Join(newline, code
                .Split('\n')
                .Select(s => s.Trim())
                .Select(s => s.Length > 0 ? indent + s : ""));
        }
    }
}
