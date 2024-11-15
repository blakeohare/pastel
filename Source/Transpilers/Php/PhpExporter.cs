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
            filesOut["@FUNC_FILE"] = funcCode;
        }

        private void GenerateStructImplementation(Dictionary<string, string> filesOut, PastelContext ctx, ProjectConfig config, string structName, string structCode)
        {
            structCode = ctx.Transpiler.WrapCodeForStructs(ctx.TranspilerContext, config, structCode);
            filesOut["@STRUCT_DIR/" + structName + ".php"] = structCode;
        }

        private void GenerateStructBundleImplementation(Dictionary<string, string> filesOut, TranspilerContext ctx, ProjectConfig config, string[] structOrder, Dictionary<string, string> structCodeByName)
        {
            List<string> codeLines = ["<?php"];
            foreach (string structName in structOrder)
            {
                codeLines.Add(ctx.Transpiler.WrapCodeForStructs(ctx, config, structCodeByName[structName]));
            }
            codeLines.Add("?>");

            filesOut["@STRUCT_DIR/gen_classes.php"] = string.Join('\n', codeLines);
        }
    }
}
