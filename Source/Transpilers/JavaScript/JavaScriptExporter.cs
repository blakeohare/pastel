using Pastel.Parser.ParseNodes;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pastel.Transpilers.JavaScript
{
    internal class JavaScriptExporter : AbstractExporter
    {
        protected override string PreferredTab => "\t";
        protected override string PreferredNewline => "\n";
        
        protected override Dictionary<string, string> GenerateFiles(ProjectConfig config, PastelContext context)
        {
            Dictionary<string, string> files = [];
            this.GenerateFunctionImplementation(files, context, config, context.GetCodeForFunctions());
            return files;
        }

        private void GenerateFunctionImplementation(Dictionary<string, string> filesOut, PastelContext ctx, ProjectConfig config, string funcCode)
        {
            funcCode = this.WrapFinalExportedCode(funcCode, ctx.GetCompiler().GetFunctionDefinitions());
            filesOut["@FUNC_FILE"] = funcCode;
        }

        private string WrapFinalExportedCode(string code, FunctionDefinition[] functions)
        {
            // TODO: public annotation to only export certain functions.

            // TODO: internally minify names. As this is being exported with a list, the order
            // is the only important thing to assign it to the proper external alias.
            StringBuilder sb = new StringBuilder();
            sb.Append("const [PASTEL_regCallback");
            string[] funcNames = functions
                .Select(fd => fd.Name)
                .OrderBy(n => n)
                .ToArray();
            for (int i = 0; i < funcNames.Length; i++)
            {
                sb.Append(", ");
                sb.Append(funcNames[i]);
            }
            sb.Append("] = (() => {\n");
            sb.Append(code);
            sb.Append('\n');
            sb.Append("return [PST$registerExtensibleCallback");
            for (int i = 0; i < funcNames.Length; i++)
            {
                sb.Append(", $");
                sb.Append(funcNames[i]);
            }
            sb.Append("];\n");
            sb.Append("})();\n");
            return sb.ToString();
        }
    }
}
