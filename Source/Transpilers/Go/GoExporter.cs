using System.Collections.Generic;
using System.Linq;

namespace Pastel.Transpilers.Go
{
    internal class GoExporter : AbstractExporter
    {
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
            AbstractTranspiler transpiler = ctx.Transpiler;
            funcCode = transpiler.WrapCodeForFunctions(ctx.TranspilerContext, config, funcCode);
            funcCode = transpiler.WrapFinalExportedCode(funcCode, ctx.GetCompiler().GetFunctionDefinitions());
            funcCode = CodeUtil.ConvertWhitespaceFromCanonicalFormToPreferred(funcCode, transpiler);
            filesOut["@FUNC_FILE"] = funcCode;
        }

        private void GenerateStructBundleImplementation(Dictionary<string, string> filesOut, TranspilerContext ctx, ProjectConfig config, string[] structOrder, Dictionary<string, string> structCodeByName)
        {
            List<string> codeLines = [];
            foreach (string structName in structOrder)
            {
                codeLines.Add(ctx.Transpiler.WrapCodeForStructs(ctx, config, structCodeByName[structName]));
            }
            string path = "@STRUCT_DIR/genstructs.go";
            string codeString = string.Join('\n', codeLines);
            codeString = CodeUtil.ConvertWhitespaceFromCanonicalFormToPreferred(codeString, ctx.Transpiler);
            filesOut[path] = codeString;
        }
    }
}
