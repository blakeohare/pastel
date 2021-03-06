﻿using System.Collections.Generic;
using System.Linq;

namespace Pastel.Nodes
{
    internal class SwitchStatement : Executable
    {
        public Expression Condition { get; set; }
        public SwitchChunk[] Chunks { get; set; }

        public SwitchStatement(Token switchToken, Expression condition, IList<SwitchChunk> chunks) : base(switchToken)
        {
            this.Condition = condition;
            this.Chunks = chunks.ToArray();
        }

        public class SwitchChunk
        {
            public Token[] CaseAndDefaultTokens { get; set; }
            public Expression[] Cases { get; set; }
            public bool HasDefault { get; set; }
            public Executable[] Code { get; set; }

            public SwitchChunk(IList<Token> caseAndDefaultTokens, IList<Expression> caseExpressionsOrNullForDefault, IList<Executable> code)
            {
                this.CaseAndDefaultTokens = caseAndDefaultTokens.ToArray();
                this.Cases = caseExpressionsOrNullForDefault.ToArray();
                this.Code = code.ToArray();

                for (int i = 0; i < this.Cases.Length - 1; ++i)
                {
                    if (this.Cases[i] == null)
                    {
                        throw new ParserException(caseAndDefaultTokens[i], "default cannot appear before other cases.");
                    }
                }

                this.HasDefault = this.Cases[this.Cases.Length - 1] == null;
            }
        }

        public override Executable ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            this.Condition = this.Condition.ResolveNamesAndCullUnusedCode(compiler);
            for (int i = 0; i < this.Chunks.Length; ++i)
            {
                SwitchChunk chunk = this.Chunks[i];
                for (int j = 0; j < chunk.Cases.Length; ++j)
                {
                    if (chunk.Cases[j] != null)
                    {
                        chunk.Cases[j] = chunk.Cases[j].ResolveNamesAndCullUnusedCode(compiler);
                    }
                }

                chunk.Code = Executable.ResolveNamesAndCullUnusedCodeForBlock(chunk.Code, compiler).ToArray();
            }
            return this;
        }

        internal override void ResolveTypes(VariableScope varScope, PastelCompiler compiler)
        {
            this.Condition = this.Condition.ResolveType(varScope, compiler);
            PType conditionType = this.Condition.ResolvedType;
            bool isInt = conditionType.IsIdentical(compiler, PType.INT);
            bool isChar = !isInt && conditionType.IsIdentical(compiler, PType.CHAR);
            if (!isInt && !isChar)
            {
                throw new ParserException(this.Condition.FirstToken, "Only ints and chars can be used in switch statements.");
            }

            // consider it all one scope
            for (int i = 0; i < this.Chunks.Length; ++i)
            {
                SwitchChunk chunk = this.Chunks[i];
                for (int j = 0; j < chunk.Cases.Length; ++j)
                {
                    Expression ex = chunk.Cases[j];
                    if (ex != null)
                    {
                        ex = ex.ResolveType(varScope, compiler);
                        chunk.Cases[j] = ex;
                        if ((isInt && ex.ResolvedType.RootValue != "int") ||
                            (isChar && ex.ResolvedType.RootValue != "char"))
                        {
                            throw new ParserException(ex.FirstToken, isInt ? "Only ints may be used." : "Only chars may be used.");
                        }
                    }
                }

                Executable.ResolveTypes(chunk.Code, varScope, compiler);
            }
        }

        internal override Executable ResolveWithTypeContext(PastelCompiler compiler)
        {
            this.Condition = this.Condition.ResolveWithTypeContext(compiler);
            HashSet<int> values = new HashSet<int>();
            for (int i = 0; i < this.Chunks.Length; ++i)
            {
                SwitchChunk chunk = this.Chunks[i];
                for (int j = 0; j < chunk.Cases.Length; ++j)
                {
                    if (chunk.Cases[j] != null)
                    {
                        chunk.Cases[j] = chunk.Cases[j].ResolveWithTypeContext(compiler);
                        InlineConstant ic = chunk.Cases[j] as InlineConstant;
                        if (ic == null)
                        {
                            throw new ParserException(chunk.Cases[j].FirstToken, "Only constants may be used as switch cases.");
                        }
                        int value;
                        if (ic.ResolvedType.RootValue == "char")
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
                Executable.ResolveWithTypeContext(compiler, chunk.Code);
            }
            return this;
        }
    }
}
