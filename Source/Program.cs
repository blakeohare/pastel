using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] effectiveArgs = args;

#if DEBUG
            effectiveArgs = new string[] {
                @"C:\Things\Pastel\Samples\ListAnalyzer\pastel",
                @"C:\Things\Pastel\Samples\ListAnalyzer\csharp\gen",
                "csharp"
            };
#endif

            string sourceDir = effectiveArgs[0];
            string targetDir = effectiveArgs[1];
            string platform = effectiveArgs[2];

            Language lang;
            switch (platform)
            {
                case "python": lang = Language.PYTHON; break;
                case "csharp": lang = Language.CSHARP; break;
                default: throw new Exception();
            }

            Dictionary<string, string> files = new FileGatherer(sourceDir, ".pst").GatherFiles();
            Parser parser = new Parser(new Dictionary<string, object>(), new InlineImportCodeLoader());
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
