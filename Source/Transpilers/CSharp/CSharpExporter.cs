using System.Collections.Generic;
using System.Linq;

namespace Pastel.Transpilers.CSharp
{
    internal class CSharpExporter : AbstractExporter
    {
        protected override string PreferredTab => "    ";
        protected override string PreferredNewline => "\r\n";

        protected override Dictionary<string, string> GenerateFiles(ProjectConfig config, PastelContext context)
        {
            Dictionary<string, string> files = [];
            Dictionary<string, string> structDefinitions = context.GetCodeForStructs();
            foreach (string structName in structDefinitions.Keys)
            {
                this.GenerateStructImplementation(files, context, config, structName, structDefinitions[structName]);
            }

            this.GenerateFunctionImplementation(files, context, config, context.GetCodeForFunctions());
            return files;
        }

        private void GenerateFunctionImplementation(Dictionary<string, string> filesOut, PastelContext ctx, ProjectConfig config, string funcCode)
        {
            AbstractTranspiler transpiler = ctx.Transpiler;

            filesOut["@FUNC_FILE"] = string.Join('\n', [
                .. (config.Imports
                    .Append("System.Linq")
                    .Distinct()
                    .OrderBy(v => v)
                    .Select(v => "using " + v + ";")),
                "",
                "namespace " + config.NamespaceForFunctions,
                "{",
                "\tinternal static class " + config.WrappingClassNameForFunctions,
                "\t{",
                "#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.",
                "#pragma warning disable CS8602 // Dereference of a possibly null reference.",
                "#pragma warning disable CS8603 // Possible null reference return.",
                "#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.",
                .. this.SplitAndIndent(funcCode, "\t\t"),
                "\t}",
                "}",
                ""
            ]);
        }

        private void GenerateStructImplementation(Dictionary<string, string> filesOut, PastelContext ctx, ProjectConfig config, string structName, string structCode)
        {
            filesOut["@STRUCT_DIR/" + structName + ".cs"] = string.Join('\n', [
                "using System.Collections.Generic;",
                "",
                "namespace " + config.NamespaceForStructs,
                "{",
                .. this.SplitAndIndent(structCode, "\t"),
                "}",
                ""
            ]);
        }
    }
}
