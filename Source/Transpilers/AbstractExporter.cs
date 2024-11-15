using System.Collections.Generic;

namespace Pastel.Transpilers
{
    internal abstract class AbstractExporter
    {
        protected abstract Dictionary<string, string> GenerateFiles(ProjectConfig config, PastelContext context);

        public void DoExport(ProjectConfig config, PastelContext context)
        {
            Dictionary<string, string> files = GenerateFiles(config, context);

            foreach (string path in files.Keys)
            {
                string actualPath = path
                    .Replace("@FUNC_FILE", config.OutputFileFunctions)
                    .Replace("@STRUCT_DIR", config.OutputDirStructs)
                    .Replace('/', System.IO.Path.DirectorySeparatorChar);
                System.IO.File.WriteAllText(actualPath, files[path]);
            }
        }
    }
}
