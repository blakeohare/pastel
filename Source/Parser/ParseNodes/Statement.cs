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

        public abstract Statement ResolveNamesAndCullUnusedCode(Resolver resolver);

        internal static IList<Statement> ResolveNamesAndCullUnusedCodeForBlock(IList<Statement> code, Resolver resolver)
        {
            List<Statement> output = new List<Statement>();
            for (int i = 0; i < code.Count; ++i)
            {
                Statement line = code[i].ResolveNamesAndCullUnusedCode(resolver);
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

        internal static void ResolveTypes(Statement[] statements, VariableScope varScope, Resolver resolver)
        {
            for (int i = 0; i < statements.Length; ++i)
            {
                statements[i].ResolveTypes(varScope, resolver);
            }
        }

        internal static void ResolveWithTypeContext(Resolver resolver, Statement[] statements)
        {
            for (int i = 0; i < statements.Length; ++i)
            {
                statements[i] = statements[i].ResolveWithTypeContext(resolver);
            }
        }

        internal abstract void ResolveTypes(VariableScope varScope, Resolver resolver);

        internal abstract Statement ResolveWithTypeContext(Resolver resolver);
    }
}
