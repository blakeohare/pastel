using Pastel.Parser.ParseNodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser
{
    internal class PastelCompiler
    {
        internal ExtensionSet ExtensionSet { get; private set; }
        internal Transpilers.AbstractTranspiler Transpiler { get; set; }
        public IInlineImportCodeLoader CodeLoader { get; private set; }

        public PastelContext Context { get; private set; }

        public PastelCompiler(
            PastelContext context,
            Language language,
            IDictionary<string, object> constants,
            IInlineImportCodeLoader inlineImportCodeLoader,
            ExtensionSet extensionSet)
        {
            Context = context;

            CodeLoader = inlineImportCodeLoader;
            Transpiler = context.Transpiler;
            this.ExtensionSet = extensionSet;
            StructDefinitions = new Dictionary<string, StructDefinition>();
            EnumDefinitions = new Dictionary<string, EnumDefinition>();
            ConstantDefinitions = new Dictionary<string, VariableDeclaration>();
            FunctionDefinitions = new Dictionary<string, FunctionDefinition>();
            ClassDefinitions = new Dictionary<string, ClassDefinition>();
            parser = new PastelParser(context, constants, inlineImportCodeLoader);
        }

        public override string ToString()
        {
            return "Pastel Compiler for " + Context.ToString();
        }

        private PastelParser parser;

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
            ICompilationEntity[] entities = parser.EntityParser.ParseText(name, code);
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
