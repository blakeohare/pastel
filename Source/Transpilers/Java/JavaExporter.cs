using System.Collections.Generic;
using System.Linq;

namespace Pastel.Transpilers.Java
{
    internal class JavaExporter : AbstractExporter
    {
        protected override string PreferredTab => "  ";
        protected override string PreferredNewline => "\n";

        protected override Dictionary<string, string> GenerateFiles(ProjectConfig config, PastelContext context)
        {
            Dictionary<string, string> files = [];
            Dictionary<string, string> structDefinitions = context.GetCodeForStructs();
            foreach (string structName in structDefinitions.Keys)
            {
                GenerateStructImplementation(files, context, config, structName, structDefinitions[structName]);
            }
            GenerateFunctionImplementation(files, context, config, context.GetCodeForFunctions());
            return files;
        }

        private void GenerateFunctionImplementation(Dictionary<string, string> filesOut, PastelContext ctx, ProjectConfig config, string funcCode)
        {
            filesOut["@FUNC_FILE"] = string.Join('\n', [
                config.NamespaceForFunctions == null ? "" : ("package " + config.NamespaceForFunctions + ";"),
                "",
                .. config.Imports.Count == 0 ? [] : config.Imports
                        .OrderBy(t => t)
                        .Select(t => "import " + t + ";"),
                "import java.util.*;",
                "",
                "public final class " + config.WrappingClassNameForFunctions + " {",
                .. this.SplitAndIndent(funcCode, "\t"),
                "}",
                "",
            ]).Trim() + "\n";
        }

        private void GenerateStructImplementation(Dictionary<string, string> filesOut, PastelContext ctx, ProjectConfig config, string structName, string structCode)
        {
            filesOut["@STRUCT_DIR/" + structName + ".java"] = string.Join('\n', [
                config.NamespaceForStructs == null ? "" : ("package " + config.NamespaceForFunctions + ";"),
                "",
                structCode.Trim(),
            ]).Trim() + "\n";
        }
    }
}
