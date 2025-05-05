using System.Collections.Generic;
using System.Linq;

namespace Pastel.Transpilers.Php
{
    internal class PhpExporter : AbstractExporter
    {
        protected override string PreferredTab => "\t";
        protected override string PreferredNewline => "\n";

        protected override Dictionary<string, string> GenerateFiles(ProjectConfig config, PastelContext context)
        {
            Dictionary<string, string> files = [];
            Dictionary<string, string> structDefinitions = context.GetCodeForStructs();
            List<string> structCodeLines = ["<?php", ""];
            foreach (string structName in structDefinitions.Keys.OrderBy(k => k))
            {
                structCodeLines.AddRange(this.SplitAndIndent(structDefinitions[structName], "\t"));
                structCodeLines.Add("");
            }
            structCodeLines.Add("?>");

            files["@STRUCT_DIR/gen_classes.php"] = string.Join('\n', structCodeLines);

            this.GenerateFunctionImplementation(files, context, config, context.GetCodeForFunctions());
            return files;
        }

        private void GenerateFunctionImplementation(Dictionary<string, string> filesOut, PastelContext ctx, ProjectConfig config, string funcCode)
        {
            AbstractTranspiler transpiler = ctx.Transpiler;

            string className = config.WrappingClassNameForFunctions ?? "PastelGeneratedCode";
            bool usesIntBuffer16 = funcCode.Contains("PST_intBuffer16");

            filesOut["@FUNC_FILE"] = string.Join('\n', [
                "<?php",
                "",
                "\tclass " + className + " {",
                .. this.SplitAndIndent(funcCode, "\t\t"),
                "\t}",
                .. (usesIntBuffer16
                    ? ["", "\t" + className + "::$PST_intBuffer16 = pastelWrapList(array_fill(0, 16, 0));"]
                    : new string[0]),
                "",
                "?>"
            ]);
        }
    }
}
