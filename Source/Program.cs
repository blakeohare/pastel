using System.Collections.Generic;
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
            context.Transpiler.Exporter.DoExport(config, context);
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
                context.ExtensionSet.AddExtensibleFunction(exFn, exFn.Translation);
            }
            context.ExtensionSet.LockExtensibleFunctions();

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
