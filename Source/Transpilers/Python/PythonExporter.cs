using System.Collections.Generic;
using System.Linq;

namespace Pastel.Transpilers.Python
{
    internal class PythonExporter : AbstractExporter
    {
        protected override string PreferredTab => "  ";
        protected override string PreferredNewline => "\n";

        protected override Dictionary<string, string> GenerateFiles(ProjectConfig config, PastelContext context)
        {
            Dictionary<string, string> files = [];

            string userCode = context.GetCodeForFunctions();

            string[] imports = [.. context.TranspilerContext.GetFeatures()
                .Where(f => f.StartsWith("IMPORT:"))
                .Select(f => f["IMPORT:".Length..])
                .OrderBy(v => v)];

            files["@FUNC_FILE"] = string.Join('\n', [
                .. imports.Select(imp => "import " + imp),
                "",
                userCode.Trim(),
                "",
            ]).Trim();

            return files;
        }
    }
}
