﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Transpilers
{
    internal class AbstractExporter
    {
        protected Dictionary<string, string> GenerateFiles(ProjectConfig config, PastelContext context)
        {
            Dictionary<string, string> files = [];

            if (context.UsesStructDefinitions)
            {
                Dictionary<string, string> structDefinitions = context.GetCodeForStructs();
                string[] structOrder = [.. structDefinitions.Keys.OrderBy(k => k.ToLower())];
                if (context.HasStructsInSeparateFiles)
                {
                    foreach (string structName in structOrder)
                    {
                        GenerateStructImplementation(files, context, config, structName, structDefinitions[structName]);
                    }
                }
                else
                {
                    GenerateStructBundleImplementation(files, context.TranspilerContext, config, structOrder, structDefinitions);
                }

                if (context.UsesStructDeclarations)
                {
                    foreach (string structName in structOrder)
                    {
                        string structDeclarationCode = context.GetCodeForStructDeclaration(structName);
                        throw new NotImplementedException();
                    }
                }
            }

            if (context.UsesFunctionDeclarations)
            {
                string funcDeclarations = context.GetCodeForFunctionDeclarations();
                throw new NotImplementedException();
            }

            GenerateFunctionImplementation(files, context, config, context.GetCodeForFunctions());
            return files;
        }

        public void DoExport(ProjectConfig config, PastelContext context)
        {
            Dictionary<string, string> files = GenerateFiles(config, context);

            foreach (string path in files.Keys)
            {
                string actualPath = path
                    .Replace("@FUNC_FILE", config.OutputFileFunctions)
                    .Replace("@STRUCT_DIR", config.OutputDirStructs)
                    .Replace('/', System.IO.Path.DirectorySeparatorChar);
                System.IO.File.WriteAllText(actualPath, files[path]);
            }
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
            string path = "@STRUCT_DIR/" + structName + fileExtension;
            structCode = CodeUtil.ConvertWhitespaceFromCanonicalFormToPreferred(structCode, ctx.Transpiler);
            filesOut[path] = structCode;
        }

        private void GenerateStructBundleImplementation(Dictionary<string, string> filesOut, TranspilerContext ctx, ProjectConfig config, string[] structOrder, Dictionary<string, string> structCodeByName)
        {
            List<string> codeLines = [];
            foreach (string structName in structOrder)
            {
                codeLines.Add(ctx.Transpiler.WrapCodeForStructs(ctx, config, structCodeByName[structName]));
            }
            string path;
            switch (config.Language)
            {
                case Language.PHP:
                    codeLines = [
                        "<?php",
                        ..codeLines,
                        "?>",
                    ];
                    path = "@STRUCT_DIR/gen_classes.php";
                    break;

                case Language.GO:
                    path = "@STRUCT_DIR/genstructs.go";
                    break;

                default:
                    throw new NotImplementedException();
            }

            string codeString = string.Join('\n', codeLines);
            codeString = CodeUtil.ConvertWhitespaceFromCanonicalFormToPreferred(codeString, ctx.Transpiler);
            filesOut[path] = codeString;
        }
    }
}
