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
            this.Condition = condition;
            this.IfCode = ifCode.ToArray();
            this.ElseToken = elseToken;
            this.ElseCode = elseCode.ToArray();
        }

        public override Statement ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            this.Condition = this.Condition.ResolveNamesAndCullUnusedCode(resolver);

            if (this.Condition is InlineConstant ic)
            {
                if (ic.Value is bool boolCondVal)
                {
                    return new StatementBatch(
                        this.FirstToken, 
                        Statement.ResolveNamesAndCullUnusedCodeForBlock(
                            boolCondVal ? this.IfCode : this.ElseCode,
                            resolver));
                }
            }
            this.IfCode = Statement.ResolveNamesAndCullUnusedCodeForBlock(this.IfCode, resolver).ToArray();
            this.ElseCode = Statement.ResolveNamesAndCullUnusedCodeForBlock(this.ElseCode, resolver).ToArray();

            return this;
        }

        internal override void ResolveTypes(VariableScope varScope, Resolver resolver)
        {
            this.Condition = this.Condition.ResolveType(varScope, resolver);
            if (!this.Condition.ResolvedType.IsBoolean)
            {
                throw new UNTESTED_ParserException(
                    this.Condition.FirstToken,
                    "Only booleans can be used in if statements.");
            }

            Statement.ResolveTypes(this.IfCode, new VariableScope(varScope), resolver);
            Statement.ResolveTypes(this.ElseCode, new VariableScope(varScope), resolver);
        }

        internal override Statement ResolveWithTypeContext(Resolver resolver)
        {
            this.Condition = this.Condition.ResolveWithTypeContext(resolver);
            Statement.ResolveWithTypeContext(resolver, this.IfCode);
            if (this.ElseCode.Length > 0) Statement.ResolveWithTypeContext(resolver, this.ElseCode);

            if (this.Condition is InlineConstant condIc)
            {
                bool condition = (bool)condIc.Value;
                return new StatementBatch(this.FirstToken, condition ? this.IfCode : this.ElseCode);
            }

            return this;
        }
    }
}
