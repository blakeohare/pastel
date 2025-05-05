using System.Collections.Generic;
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
            this.Condition = condition;
            this.Code = code.ToArray();
        }

        public override Statement ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            this.Condition = this.Condition.ResolveNamesAndCullUnusedCode(resolver);
            this.Code = Statement.ResolveNamesAndCullUnusedCodeForBlock(this.Code, resolver).ToArray();
            return this;
        }

        internal override void ResolveTypes(VariableScope varScope, Resolver resolver)
        {
            this.Condition = this.Condition.ResolveType(varScope, resolver);
            if (!this.Condition.ResolvedType.IsIdentical(resolver, PType.BOOL))
            {
                throw new ParserException(this.Condition.FirstToken, "While loop must have a boolean condition.");
            }

            Statement.ResolveTypes(this.Code, varScope, resolver);
        }

        internal override Statement ResolveWithTypeContext(Resolver resolver)
        {
            this.Condition = this.Condition.ResolveWithTypeContext(resolver);
            Statement.ResolveWithTypeContext(resolver, this.Code);
            return this;
        }
    }
}
