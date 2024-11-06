using System.Collections.Generic;

namespace Pastel.Parser.ParseNodes
{
    internal abstract class Statement
    {
        public Token FirstToken { get; set; }

        public Statement(Token firstToken)
        {
            FirstToken = firstToken;
        }

        public abstract Statement ResolveNamesAndCullUnusedCode(PastelCompiler compiler);

        internal static IList<Statement> ResolveNamesAndCullUnusedCodeForBlock(IList<Statement> code, PastelCompiler compiler)
        {
            List<Statement> output = new List<Statement>();
            for (int i = 0; i < code.Count; ++i)
            {
                Statement line = code[i].ResolveNamesAndCullUnusedCode(compiler);
                if (line is StatementBatch)
                {
                    // StatementBatch is always flattened
                    output.AddRange(((StatementBatch)line).Statements);
                }
                else
                {
                    output.Add(line);
                }
            }

            for (int i = 0; i < output.Count - 1; i++)
            {
                Statement ex = output[i];
                if (ex is ReturnStatement || ex is BreakStatement)
                {
                    throw new ParserException(output[i + 1].FirstToken, "Unreachable code detected");
                }

                if (ex is ExpressionAsStatement)
                {
                    Expression innerExpression = ((ExpressionAsStatement)ex).Expression;
                    if (!(innerExpression is FunctionInvocation))
                    {
                        throw new ParserException(ex.FirstToken, "This expression isn't allowed here.");
                    }
                }
            }
            return output;
        }

        internal static void ResolveTypes(Statement[] statements, VariableScope varScope, PastelCompiler compiler)
        {
            for (int i = 0; i < statements.Length; ++i)
            {
                statements[i].ResolveTypes(varScope, compiler);
            }
        }

        internal static void ResolveWithTypeContext(PastelCompiler compiler, Statement[] statements)
        {
            for (int i = 0; i < statements.Length; ++i)
            {
                statements[i] = statements[i].ResolveWithTypeContext(compiler);
            }
        }

        internal abstract void ResolveTypes(VariableScope varScope, PastelCompiler compiler);

        internal abstract Statement ResolveWithTypeContext(PastelCompiler compiler);
    }
}
