using System.Collections.Generic;

namespace Pastel.Transpilers.Java
{
    internal class JavaExporter : AbstractExporter
    {
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
            AbstractTranspiler transpiler = ctx.Transpiler;
            funcCode = transpiler.WrapCodeForFunctions(ctx.TranspilerContext, config, funcCode);
            funcCode = transpiler.WrapFinalExportedCode(funcCode, ctx.GetCompiler().GetFunctionDefinitions());
            funcCode = CodeUtil.ConvertWhitespaceFromCanonicalFormToPreferred(funcCode, transpiler);
            filesOut["@FUNC_FILE"] = funcCode;
        }

        private void GenerateStructImplementation(Dictionary<string, string> filesOut, PastelContext ctx, ProjectConfig config, string structName, string structCode)
        {
            structCode = ctx.Transpiler.WrapCodeForStructs(ctx.TranspilerContext, config, structCode);
            string fileExtension = LanguageUtil.GetFileExtension(config.Language);
            string path = "@STRUCT_DIR/" + structName + ".java";
            structCode = CodeUtil.ConvertWhitespaceFromCanonicalFormToPreferred(structCode, ctx.Transpiler);
            filesOut[path] = structCode;
        }
    }
}
