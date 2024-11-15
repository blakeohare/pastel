using System.Collections.Generic;

namespace Pastel.Transpilers.JavaScript
{
    internal class JavaScriptExporter : AbstractExporter
    {
        protected override Dictionary<string, string> GenerateFiles(ProjectConfig config, PastelContext context)
        {
            Dictionary<string, string> files = [];
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
    }
}
