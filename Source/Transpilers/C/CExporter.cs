using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Transpilers.C
{
    internal class CExporter : AbstractExporter
    {
        protected override string PreferredTab => "    ";
        protected override string PreferredNewline => "\n";

        protected override Dictionary<string, string> GenerateFiles(ProjectConfig config, PastelContext context)
        {
            Dictionary<string, string> files = [];
            GenerateFunctionImplementation(files, context, config, context.GetCodeForFunctions());
            Dictionary<string, string> structDefinitions = context.GetCodeForStructs();
            string[] structOrder = [.. structDefinitions.Keys.OrderBy(k => k.ToLower())];

            foreach (string structName in structOrder)
            {
                GenerateStructImplementation(files, context, config, structName, structDefinitions[structName]);
            }

            foreach (string structName in structOrder)
            {
                string structDeclarationCode = context.GetCodeForStructDeclaration(structName);
                throw new NotImplementedException();
            }

            string funcDeclarations = context.GetCodeForFunctionDeclarations();
            throw new NotImplementedException();
        }

        private void GenerateFunctionImplementation(Dictionary<string, string> filesOut, PastelContext ctx, ProjectConfig config, string funcCode)
        {
            AbstractTranspiler transpiler = ctx.Transpiler;
            funcCode = transpiler.WrapCodeForFunctions(ctx.TranspilerContext, config, funcCode);
            funcCode = transpiler.WrapFinalExportedCode(funcCode, ctx.GetCompiler().GetFunctionDefinitions());
            filesOut["@FUNC_FILE"] = funcCode;
        }

        private void GenerateStructImplementation(Dictionary<string, string> filesOut, PastelContext ctx, ProjectConfig config, string structName, string structCode)
        {
            structCode = ctx.Transpiler.WrapCodeForStructs(ctx.TranspilerContext, config, structCode);
            filesOut["@STRUCT_DIR/" + structName + ".h"] = structCode;
        }
    }
}
