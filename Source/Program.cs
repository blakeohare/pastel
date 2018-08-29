using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string[]> argSet = new List<string[]>() { args };
#if DEBUG
            // Various test projects
            argSet.Clear();
            argSet.AddRange(new string[] {
                @"C:\Things\Pastel\Samples\ListAnalyzer\ListAnalyzer.json",
            }.Select(a => new string[] { a }));
#endif
            foreach (string[] a in argSet)
            {
                RunPastel(a);
            }
        }

        private static void RunPastel(string[] args)
        {
            string manifestPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), args[0]);
            manifestPath = System.IO.Path.GetFullPath(manifestPath);
            string projectRoot = System.IO.Path.GetDirectoryName(manifestPath);

            SimpleJson.JsonParser manifestJson = new SimpleJson.JsonParser(System.IO.File.ReadAllText(manifestPath));
            manifestJson.Parse();
            IDictionary<string, object> manifestRoot = manifestJson.ParseAsDictionary();
            SimpleJson.JsonLookup jl = new SimpleJson.JsonLookup(manifestRoot);
            string sourceDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(projectRoot, jl.GetAsString("source")));
            object[] targets = jl.GetAsList("targets");
            for (int i = 0; i < targets.Length; ++i)
            {
                SimpleJson.JsonLookup target = new SimpleJson.JsonLookup((IDictionary<string, object>)targets[i]);

                string platform = target.GetAsString("platform");
                string targetDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(projectRoot, target.GetAsString("output")));

                Language lang;
                switch (platform)
                {
                    case "c": lang = Language.C; break;
                    case "csharp": lang = Language.CSHARP; break;
                    case "java": lang = Language.JAVA; break;
                    case "javascript": lang = Language.JAVASCRIPT; break;
                    case "python": lang = Language.PYTHON; break;
                    default: throw new Exception();
                }

                Dictionary<string, string> files = new FileGatherer(sourceDir, ".pst").GatherFiles();
                List<ParseNodes.ICompilationEntity> nodes = new List<ParseNodes.ICompilationEntity>();
                PastelCompiler pc = new PastelCompiler(lang, new PastelCompiler[0], new Dictionary<string, object>(), new InlineImportCodeLoader());
                foreach (string filename in files.Keys.OrderBy(k => k))
                {
                    pc.CompileBlobOfCode(filename, files[filename]);
                    pc.Resolve();
                }

                Transpilers.AbstractTranspiler transpiler = LanguageUtil.GetTranspiler(lang);
                Dictionary<string, string> fileOutput = new Dictionary<string, string>();
                Transpilers.TranspilerContext transpilerContext = new Transpilers.TranspilerContext(lang);
                transpiler.GenerateCode(transpilerContext, pc, fileOutput);
                foreach (string filename in fileOutput.Keys)
                {
                    string absolutePath = System.IO.Path.Combine(targetDir, filename);
                    string code = fileOutput[filename];
                    Util.WriteFileText(absolutePath, code);
                }
            }
        }
    }
}