using Pastel.Parser.ParseNodes;
using System;

namespace Pastel.Transpilers.Python
{
    internal class PythonTranspiler : AbstractTranspiler
    {
        public PythonTranspiler(TranspilerContext transpilerCtx)
            : base(
                transpilerCtx,
                new PythonExporter(),
                null,
                new PythonExpressionTranslator(transpilerCtx),
                new PythonStatementTranslator(transpilerCtx))
        { }

        public override string HelperCodeResourcePath { get { return "Transpilers/Python/PastelHelper.py"; } }

        public override void GenerateCodeForFunction(TranspilerContext sb, FunctionDefinition funcDef, bool isStatic)
        {
            this.transpilerCtx.CurrentFunctionDefinition = funcDef;

            sb.Append(sb.CurrentTab);
            sb.Append("def ");
            sb.Append(funcDef.NameToken.Value);
            sb.Append('(');
            int argCount = funcDef.ArgNames.Length;
            for (int i = 0; i < argCount; ++i)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(funcDef.ArgNames[i].Value);
            }
            sb.Append("):\n");
            sb.TabDepth++;
            this.StatementTranslator.TranslateStatements(sb, funcDef.Code);
            sb.TabDepth--;
            sb.Append("\n");

            foreach (PythonFakeSwitchStatement switchStatement in this.transpilerCtx.SwitchStatements)
            {
                sb.Append(sb.CurrentTab);
                sb.Append(switchStatement.GenerateGlobalDictionaryLookup());
                sb.Append("\n");
            }
            this.transpilerCtx.SwitchStatements.Clear();
            this.transpilerCtx.CurrentFunctionDefinition = null;
        }

        public override void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef)
        {
            throw new InvalidOperationException(); // This function should not be called. Python uses lists as structs.
        }
    }
}
