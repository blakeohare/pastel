using System.Collections.Generic;
using System.Linq;

namespace Pastel.Transpilers.Go
{
    internal class GoExporter : AbstractExporter
    {
        protected override string PreferredTab => "  ";
        protected override string PreferredNewline => "\n";

        protected override Dictionary<string, string> GenerateFiles(ProjectConfig config, PastelContext context)
        {
            Dictionary<string, string> files = [];
            Dictionary<string, string> structDefinitions = context.GetCodeForStructs();
            string[] structOrder = [.. structDefinitions.Keys.OrderBy(k => k.ToLower())];
            GenerateStructBundleImplementation(files, context.TranspilerContext, config, structOrder, structDefinitions);
            GenerateFunctionImplementation(files, context, config, context.GetCodeForFunctions());
            return files;
        }

        private void GenerateFunctionImplementation(Dictionary<string, string> filesOut, PastelContext ctx, ProjectConfig config, string funcCode)
        {
            string[] imports = [.. ctx.TranspilerContext.GetFeatures()
                .Where(f => f.StartsWith("IMPORT:"))
                .Select(f => f["IMPORT:".Length..])
                .OrderBy(v => v)];

            string[] importLines;
            if (imports.Length == 0) importLines = [];
            else if (imports.Length == 1) importLines = ["import \"" + imports[0] + "\"", ""];
            else importLines = ["import (", .. imports.Select(v => "  \"" + v + "\""), ")", ""];

            filesOut["@FUNC_FILE"] = string.Join('\n', [
                "package main",
                "",
                .. importLines,
                funcCode.Trim(),
                "",
            ]);
        }

        private void GenerateStructBundleImplementation(Dictionary<string, string> filesOut, TranspilerContext ctx, ProjectConfig config, string[] structOrder, Dictionary<string, string> structCodeByName)
        {
            List<string> codeLines = [
                "package main",
                "",
            ];
            foreach (string structName in structOrder)
            {
                codeLines.Add(structCodeByName[structName]);
                codeLines.Add("");
            }
            filesOut["@STRUCT_DIR/genstructs.go"] = string.Join('\n', codeLines);
        }
    }
}
