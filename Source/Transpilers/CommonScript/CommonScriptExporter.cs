using System.Collections.Generic;

namespace Pastel.Transpilers.CommonScript
{
    internal class CommonScriptExporter : AbstractExporter
    {
        public CommonScriptExporter() : base() { }

        protected override string PreferredNewline => "\n";
        protected override string PreferredTab => "  ";

        protected override Dictionary<string, string> GenerateFiles(ProjectConfig config, PastelContext context)
        {
            Dictionary<string, string> files = [];
            files["@FUNC_FILE"] = string.Join('\n', [
                context.GetCodeForFunctions().Trim(),
                "",
            ]);

            return files;
        }

    }
}
