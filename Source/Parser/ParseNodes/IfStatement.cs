using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser.ParseNodes
{
    internal class IfStatement : Statement
    {
        public Expression Condition { get; set; }
        public Statement[] IfCode { get; set; }
        public Token ElseToken { get; set; }
        public Statement[] ElseCode { get; set; }

        public IfStatement(
            Token ifToken,
            Expression condition,
            IList<Statement> ifCode,
            Token elseToken,
            IList<Statement> elseCode) : base(ifToken)
        {
            Condition = condition;
            IfCode = ifCode.ToArray();
            ElseToken = elseToken;
            ElseCode = elseCode.ToArray();
        }

        public override Statement ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            Condition = Condition.ResolveNamesAndCullUnusedCode(compiler);

            if (Condition is InlineConstant)
            {
                object value = ((InlineConstant)Condition).Value;
                if (value is bool)
                {
                    return new StatementBatch(FirstToken, ResolveNamesAndCullUnusedCodeForBlock(
                        (bool)value ? IfCode : ElseCode,
                        compiler));
                }
            }
            IfCode = ResolveNamesAndCullUnusedCodeForBlock(IfCode, compiler).ToArray();
            ElseCode = ResolveNamesAndCullUnusedCodeForBlock(ElseCode, compiler).ToArray();

            return this;
        }

        internal override void ResolveTypes(VariableScope varScope, PastelCompiler compiler)
        {
            Condition = Condition.ResolveType(varScope, compiler);
            if (Condition.ResolvedType.RootValue != "bool")
            {
                throw new ParserException(Condition.FirstToken, "Only booleans can be used in if statements.");
            }

            ResolveTypes(IfCode, new VariableScope(varScope), compiler);
            ResolveTypes(ElseCode, new VariableScope(varScope), compiler);
        }

        internal override Statement ResolveWithTypeContext(PastelCompiler compiler)
        {
            Condition = Condition.ResolveWithTypeContext(compiler);
            ResolveWithTypeContext(compiler, IfCode);
            if (ElseCode.Length > 0) ResolveWithTypeContext(compiler, ElseCode);

            if (Condition is InlineConstant)
            {
                bool condition = (bool)((InlineConstant)Condition).Value;
                return new StatementBatch(FirstToken, condition ? IfCode : ElseCode);
            }

            return this;
        }
    }
}
