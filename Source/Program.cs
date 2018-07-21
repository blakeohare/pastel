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
                @"C:\Things\Pastel\Samples\ListAnalyzer\python\gen",
                "python"
            };
#endif

            string sourceDir = effectiveArgs[0];
            string targetDir = effectiveArgs[1];
            string platform = effectiveArgs[2];

            Dictionary<string, string> files = new FileGatherer(sourceDir, ".pst").GatherFiles();
            Parser parser = new Parser(new Dictionary<string, object>(), new InlineImportCodeLoader());
            List<ParseNodes.ICompilationEntity> nodes = new List<ParseNodes.ICompilationEntity>();
            foreach (string filename in files.Keys.OrderBy(k => k))
            {
                nodes.AddRange(parser.ParseText(filename, files[filename]));
            }
        }
    }
}
