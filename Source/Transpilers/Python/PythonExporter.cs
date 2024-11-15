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
            files["@FUNC_FILE"] = string.Join('\n', [
                .. config.Imports.Count == 0
                    ? []
                    : config.Imports
                        .OrderBy(t => t)
                        .Select(t => "import " + t)
                        .Append(""),
                context.GetCodeForFunctions().Trim(),
                "",
            ]);

            return files;
        }
    }
}
