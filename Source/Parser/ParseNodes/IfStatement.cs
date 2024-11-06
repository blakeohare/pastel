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

        public override Statement ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            Condition = Condition.ResolveNamesAndCullUnusedCode(resolver);

            if (Condition is InlineConstant)
            {
                object value = ((InlineConstant)Condition).Value;
                if (value is bool)
                {
                    return new StatementBatch(FirstToken, ResolveNamesAndCullUnusedCodeForBlock(
                        (bool)value ? IfCode : ElseCode,
                        resolver));
                }
            }
            IfCode = ResolveNamesAndCullUnusedCodeForBlock(IfCode, resolver).ToArray();
            ElseCode = ResolveNamesAndCullUnusedCodeForBlock(ElseCode, resolver).ToArray();

            return this;
        }

        internal override void ResolveTypes(VariableScope varScope, Resolver resolver)
        {
            this.Condition = this.Condition.ResolveType(varScope, resolver);
            if (this.Condition.ResolvedType.RootValue != "bool")
            {
                throw new ParserException(this.Condition.FirstToken, "Only booleans can be used in if statements.");
            }

            ResolveTypes(this.IfCode, new VariableScope(varScope), resolver);
            ResolveTypes(this.ElseCode, new VariableScope(varScope), resolver);
        }

        internal override Statement ResolveWithTypeContext(Resolver resolver)
        {
            this.Condition = this.Condition.ResolveWithTypeContext(resolver);
            ResolveWithTypeContext(resolver, this.IfCode);
            if (this.ElseCode.Length > 0) ResolveWithTypeContext(resolver, this.ElseCode);

            if (this.Condition is InlineConstant)
            {
                bool condition = (bool)((InlineConstant)Condition).Value;
                return new StatementBatch(FirstToken, condition ? this.IfCode : this.ElseCode);
            }

            return this;
        }
    }
}
