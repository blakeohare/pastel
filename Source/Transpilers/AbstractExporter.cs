﻿using System.Collections.Generic;

namespace Pastel.Transpilers
{
    internal abstract class AbstractExporter
    {
        protected abstract string PreferredTab { get; }
        protected abstract string PreferredNewline { get; }

        protected abstract Dictionary<string, string> GenerateFiles(ProjectConfig config, PastelContext context);

        public void DoExport(ProjectConfig config, PastelContext context)
        {
            Dictionary<string, string> files = GenerateFiles(config, context);

            foreach (string path in files.Keys)
            {
                string actualPath = path
                    .Replace("@FUNC_FILE", config.OutputFileFunctions)
                    .Replace("@STRUCT_DIR", config.OutputDirStructs)
                    .Replace('/', System.IO.Path.DirectorySeparatorChar);
                string code = files[path]
                    .Replace("\n", this.PreferredNewline)
                    .Replace("\t", this.PreferredTab);
                string parent = System.IO.Path.GetDirectoryName(actualPath);
                if (!DiskUtil.EnsureDirectoryExists(parent))
                {
                    throw new UserErrorException("Cannot export file: " + actualPath);
                }
                System.IO.File.WriteAllText(actualPath, code);
            }
        }

        protected string[] SplitAndIndent(string code, string indentStr)
        {
            string[] lines = code.TrimEnd().Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] != "") lines[i] = indentStr + lines[i];
            }
            return lines;
        }
    }
}
