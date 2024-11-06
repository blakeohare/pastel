using System;
using System.Collections.Generic;
using System.Linq;
using Pastel.Parser;

namespace Pastel
{
    public class Program
    {
        public static void Main(string[] args)
        {
#if DEBUG
            MainWrapped(args);
#else
            try
            {
                MainWrapped(args);
            }
            catch (UserErrorException uee)
            {
                System.Console.WriteLine(uee.Message);
            }
#endif
        }

        public static void MainWrapped(string[] args)
        {
            if (args.Length == 0 || args.Length > 2)
            {
                throw new UserErrorException("Incorrect usage. Please provide a path to a Pastel project config file (required) and a build target (optional).");
            }

            string projectPath = args[0];
            if (!System.IO.File.Exists(projectPath))
            {
                throw new UserErrorException("Project file does not exist: '" + projectPath + "'");
            }

            projectPath = System.IO.Path.GetFullPath(projectPath);

            BuildProject(projectPath, args.Length == 2 ? args[1] : null);
        }

        private static void BuildProject(string projectPath, string targetId)
        {
            ProjectConfig config = ProjectConfig.Parse(projectPath, targetId);
            if (config.Language == Language.NONE) throw new UserErrorException("Language not defined in " + projectPath);
            PastelContext context = CompilePastelContexts(config);
            GenerateFiles(config, context);
        }

        private static Dictionary<string, string> GenerateFiles(ProjectConfig config, PastelContext context)
        {
            Dictionary<string, string> output = new Dictionary<string, string>();

            if (context.UsesClassDefinitions)
            {
                Dictionary<string, string> classDefinitions = context.GetCodeForClasses();
                foreach (string className in classDefinitions.Keys.OrderBy(k => k))
                {
                    string classCode = classDefinitions[className];
                    if (context.ClassDefinitionsInSeparateFiles)
                    {
                        GenerateClassImplementation(context, config, className, classCode);
                    }
                    else
                    {
                        output["class_def:" + className] = classCode;
                    }
                }

                if (!context.ClassDefinitionsInSeparateFiles)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    foreach (string key in output.Keys.Where(k => k.StartsWith("class_def:")).OrderBy(k => k))
                    {
                        sb.Append(output[key]);
                        sb.Append("\n\n");
                    }
                    string code = sb.ToString().Trim();
                    if (code.Length > 0)
                    {
                        string classOutputDir = System.IO.Path.GetDirectoryName(config.OutputFileFunctions);
                        string path = System.IO.Path.Combine(classOutputDir, "Classes" + LanguageUtil.GetFileExtension(config.Language));
                        code += "\n";
                        code = CodeUtil.ConvertWhitespaceFromCanonicalFormToPreferred(code, context.Transpiler);
                        System.IO.File.WriteAllText(path, code);
                    }
                }
            }

            if (context.UsesStructDefinitions)
            {
                Dictionary<string, string> structDefinitions = context.GetCodeForStructs();
                string[] structOrder = structDefinitions.Keys.OrderBy(k => k.ToLower()).ToArray();
                if (context.HasStructsInSeparateFiles)
                {
                    foreach (string structName in structOrder)
                    {
                        GenerateStructImplementation(context, config, structName, structDefinitions[structName]);
                    }
                }
                else
                {
                    GenerateStructBundleImplementation(context.GetTranspilerContext(), config, structOrder, structDefinitions);
                }

                if (context.UsesStructDeclarations)
                {
                    Dictionary<string, string> structDeclarations = structOrder.ToDictionary(k => context.GetCodeForStructDeclaration(k));

                    foreach (string structName in structOrder)
                    {
                        output["struct_decl:" + structName] = structDeclarations[structName];
                    }
                }
            }

            if (context.UsesFunctionDeclarations)
            {
                string funcDeclarations = context.GetCodeForFunctionDeclarations();
                throw new NotImplementedException();
            }

            GenerateFunctionImplementation(context, config, context.GetCodeForFunctions());

            return output;
        }

        private static void GenerateFunctionImplementation(PastelContext ctx, ProjectConfig config, string funcCode)
        {
            Transpilers.AbstractTranspiler transpiler = ctx.Transpiler;
            funcCode = transpiler.WrapCodeForFunctions(ctx.GetTranspilerContext(), config, funcCode);
            funcCode = transpiler.WrapFinalExportedCode(funcCode, ctx.GetCompiler().GetFunctionDefinitions());
            funcCode = CodeUtil.ConvertWhitespaceFromCanonicalFormToPreferred(funcCode, transpiler);
            System.IO.File.WriteAllText(config.OutputFileFunctions, funcCode);
        }

        private static void GenerateStructImplementation(PastelContext ctx, ProjectConfig config, string structName, string structCode)
        {
            structCode = ctx.Transpiler.WrapCodeForStructs(ctx.GetTranspilerContext(), config, structCode);
            string fileExtension = LanguageUtil.GetFileExtension(config.Language);
            string path = System.IO.Path.Combine(config.OutputDirStructs, structName + fileExtension);
            structCode = CodeUtil.ConvertWhitespaceFromCanonicalFormToPreferred(structCode, ctx.Transpiler);
            System.IO.File.WriteAllText(path, structCode);
        }

        private static void GenerateClassImplementation(PastelContext ctx, ProjectConfig config, string className, string classCode)
        {
            classCode = ctx.Transpiler.WrapCodeForClasses(ctx.GetTranspilerContext(), config, classCode);
            string fileExtension = LanguageUtil.GetFileExtension(config.Language);
            string path = System.IO.Path.Combine(config.OutputDirStructs, className + fileExtension);
            classCode = CodeUtil.ConvertWhitespaceFromCanonicalFormToPreferred(classCode, ctx.Transpiler);
            System.IO.File.WriteAllText(path, classCode);
        }

        private static void GenerateStructBundleImplementation(Transpilers.TranspilerContext ctx, ProjectConfig config, string[] structOrder, Dictionary<string, string> structCodeByName)
        {
            List<string> codeLines = new List<string>();
            foreach (string structName in structOrder)
            {
                codeLines.Add(ctx.Transpiler.WrapCodeForStructs(ctx, config, structCodeByName[structName]));
            }
            string dir = config.OutputDirStructs;
            string path;
            switch (config.Language)
            {
                case Language.PHP:
                    codeLines.Insert(0, "<?php");
                    codeLines.Add("?>");
                    path = System.IO.Path.Combine(dir, "gen_classes.php");
                    break;

                case Language.GO:
                    path = System.IO.Path.Combine(dir, "genstructs.go");
                    break;

                default:
                    throw new NotImplementedException();
            }

            string codeString = string.Join('\n', codeLines);
            codeString = CodeUtil.ConvertWhitespaceFromCanonicalFormToPreferred(codeString, ctx.Transpiler);
            System.IO.File.WriteAllText(path, codeString);
        }

        private static PastelContext CompilePastelContexts(ProjectConfig rootConfig)
        {
            Dictionary<string, ProjectConfig> configsLookup = new Dictionary<string, ProjectConfig>();
            string[] contextPaths = GetContextsInDependencyOrder(rootConfig, configsLookup);
            Dictionary<string, PastelContext> contexts = new Dictionary<string, PastelContext>();
            foreach (string contextPath in contextPaths)
            {
                ProjectConfig config = configsLookup[contextPath];
                PastelContext context = GetContextForConfigImpl(config, contexts, new HashSet<string>());
                string source = DiskUtil.TryReadTextFile(config.Source);
                if (source == null) throw new UserErrorException("Source file not found: " + config.Source);
                context.CompileCode(config.Source, source);
                context.FinalizeCompilation();
            }
            return contexts[rootConfig.Path];
        }

        // The reason this order is needed is because the Compiler objects are required for
        // adding dependencies, but Compiler objects cannot be instantiated until the dependencies
        // are resolved.
        private static string[] GetContextsInDependencyOrder(ProjectConfig rootConfig, Dictionary<string, ProjectConfig> configLookupOut)
        {
            List<string> contextPaths = new List<string>();
            AddConfigsInDependencyOrder(rootConfig, contextPaths, configLookupOut);
            return contextPaths.ToArray();
        }

        private static void AddConfigsInDependencyOrder(ProjectConfig current, List<string> paths, Dictionary<string, ProjectConfig> configLookupOut)
        {
            if (configLookupOut.ContainsKey(current.Path)) return;
            paths.Add(current.Path);
            configLookupOut[current.Path] = current;
        }

        private static PastelContext GetContextForConfigImpl(
            ProjectConfig config,
            Dictionary<string, PastelContext> contexts,
            HashSet<string> recursionCheck)
        {
            if (contexts.ContainsKey(config.Path)) return contexts[config.Path];
            if (recursionCheck.Contains(config.Path))
            {
                throw new UserErrorException("Project config dependencies have a cycle involving: " + config.Path);
            }

            recursionCheck.Add(config.Path);

            string sourceRootDir = System.IO.Path.GetDirectoryName(config.Source);
            PastelContext context = new PastelContext(sourceRootDir, config.Language, new CodeLoader(sourceRootDir));

            foreach (string constantName in config.Flags.Keys)
            {
                context.SetConstant(constantName, config.Flags[constantName]);
            }

            foreach (ExtensibleFunction exFn in config.GetExtensibleFunctions())
            {
                // TODO(pastel-split): Translation is already set on the extensible function in the
                // new codepath, so the 2nd parameter here ought to be removed.
                context.AddExtensibleFunction(exFn, exFn.Translation);
            }

            contexts[config.Path] = context;
            recursionCheck.Remove(config.Path);
            return context;
        }

        private class CodeLoader : IInlineImportCodeLoader
        {
            private string root;
            public CodeLoader(string root)
            {
                this.root = root;
            }

            public string LoadCode(Token throwLocation, string path)
            {
                path = System.IO.Path.Combine(this.root, path);
                path = System.IO.Path.GetFullPath(path);
                string code = DiskUtil.TryReadTextFile(path);
                if (code == null) throw new ParserException(throwLocation, "File does not exist: " + path);
                return code;
            }
        }
    }
}
