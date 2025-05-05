using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser.ParseNodes
{
    internal class SwitchStatement : Statement
    {
        public Expression Condition { get; set; }
        public SwitchChunk[] Chunks { get; set; }

        public SwitchStatement(Token switchToken, Expression condition, IList<SwitchChunk> chunks) : base(switchToken)
        {
            Condition = condition;
            Chunks = chunks.ToArray();
        }

        public class SwitchChunk
        {
            public Token[] CaseAndDefaultTokens { get; set; }
            public Expression[] Cases { get; set; }
            public bool HasDefault { get; set; }
            public Statement[] Code { get; set; }

            public SwitchChunk(IList<Token> caseAndDefaultTokens, IList<Expression> caseExpressionsOrNullForDefault, IList<Statement> code)
            {
                CaseAndDefaultTokens = caseAndDefaultTokens.ToArray();
                Cases = caseExpressionsOrNullForDefault.ToArray();
                Code = code.ToArray();

                for (int i = 0; i < Cases.Length - 1; ++i)
                {
                    if (Cases[i] == null)
                    {
                        throw new ParserException(caseAndDefaultTokens[i], "default cannot appear before other cases.");
                    }
                }

                HasDefault = Cases[Cases.Length - 1] == null;
            }
        }

        public override Statement ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            Condition = Condition.ResolveNamesAndCullUnusedCode(resolver);
            for (int i = 0; i < Chunks.Length; ++i)
            {
                SwitchChunk chunk = Chunks[i];
                for (int j = 0; j < chunk.Cases.Length; ++j)
                {
                    if (chunk.Cases[j] != null)
                    {
                        chunk.Cases[j] = chunk.Cases[j].ResolveNamesAndCullUnusedCode(resolver);
                    }
                }

                chunk.Code = ResolveNamesAndCullUnusedCodeForBlock(chunk.Code, resolver).ToArray();
            }
            return this;
        }

        internal override void ResolveTypes(VariableScope varScope, Resolver resolver)
        {
            Condition = Condition.ResolveType(varScope, resolver);
            PType conditionType = Condition.ResolvedType;
            bool isInt = conditionType.IsIdentical(resolver, PType.INT);
            bool isChar = !isInt && conditionType.IsIdentical(resolver, PType.CHAR);
            if (!isInt && !isChar)
            {
                throw new ParserException(Condition.FirstToken, "Only ints and chars can be used in switch statements.");
            }

            // consider it all one scope
            for (int i = 0; i < Chunks.Length; ++i)
            {
                SwitchChunk chunk = Chunks[i];
                for (int j = 0; j < chunk.Cases.Length; ++j)
                {
                    Expression ex = chunk.Cases[j];
                    if (ex != null)
                    {
                        ex = ex.ResolveType(varScope, resolver);
                        chunk.Cases[j] = ex;
                        if (isInt && ex.ResolvedType.RootValue != "int" ||
                            isChar && ex.ResolvedType.RootValue != "char")
                        {
                            throw new ParserException(ex.FirstToken, isInt ? "Only ints may be used." : "Only chars may be used.");
                        }
                    }
                }

                ResolveTypes(chunk.Code, varScope, resolver);
            }
        }

        internal override Statement ResolveWithTypeContext(Resolver resolver)
        {
            Condition = Condition.ResolveWithTypeContext(resolver);
            HashSet<int> values = new HashSet<int>();
            for (int i = 0; i < Chunks.Length; ++i)
            {
                SwitchChunk chunk = Chunks[i];
                for (int j = 0; j < chunk.Cases.Length; ++j)
                {
                    if (chunk.Cases[j] != null)
                    {
                        chunk.Cases[j] = chunk.Cases[j].ResolveWithTypeContext(resolver);
                        InlineConstant ic = chunk.Cases[j] as InlineConstant;
                        if (ic == null)
                        {
                            throw new ParserException(chunk.Cases[j].FirstToken, "Only constants may be used as switch cases.");
                        }
                        int value;
                        if (ic.ResolvedType.IsChar)
                        {
                            value = (char)ic.Value;
                        }
                        else
                        {
                            value = (int)ic.Value;
                        }
                        if (values.Contains(value))
                        {
                            throw new ParserException(chunk.Cases[j].FirstToken, "This cases appears multiple times.");
                        }
                        values.Add(value);
                    }
                }
                ResolveWithTypeContext(resolver, chunk.Code);
            }
            return this;
        }
    }
}
