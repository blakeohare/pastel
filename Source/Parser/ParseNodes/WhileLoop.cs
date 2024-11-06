﻿using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser.ParseNodes
{
    internal class WhileLoop : Statement
    {
        public Expression Condition { get; set; }
        public Statement[] Code { get; set; }

        public WhileLoop(
            Token whileToken,
            Expression condition,
            IList<Statement> code) : base(whileToken)
        {
            Condition = condition;
            Code = code.ToArray();
        }

        public override Statement ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            Condition = Condition.ResolveNamesAndCullUnusedCode(compiler);
            Code = ResolveNamesAndCullUnusedCodeForBlock(Code, compiler).ToArray();
            return this;
        }

        internal override void ResolveTypes(VariableScope varScope, PastelCompiler compiler)
        {
            Condition = Condition.ResolveType(varScope, compiler);
            if (!Condition.ResolvedType.IsIdentical(compiler, PType.BOOL))
            {
                throw new ParserException(Condition.FirstToken, "While loop must have a boolean condition.");
            }

            ResolveTypes(Code, varScope, compiler);
        }

        internal override Statement ResolveWithTypeContext(PastelCompiler compiler)
        {
            Condition = Condition.ResolveWithTypeContext(compiler);
            ResolveWithTypeContext(compiler, Code);
            return this;
        }
    }
}
