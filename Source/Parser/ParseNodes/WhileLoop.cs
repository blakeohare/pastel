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
            Condition = condition;
            this.Code = code.ToArray();
        }

        public override Statement ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            Condition = Condition.ResolveNamesAndCullUnusedCode(resolver);
            this.Code = ResolveNamesAndCullUnusedCodeForBlock(this.Code, resolver).ToArray();
            return this;
        }

        internal override void ResolveTypes(VariableScope varScope, Resolver resolver)
        {
            Condition = Condition.ResolveType(varScope, resolver);
            if (!Condition.ResolvedType.IsIdentical(resolver, PType.BOOL))
            {
                throw new ParserException(Condition.FirstToken, "While loop must have a boolean condition.");
            }

            ResolveTypes(this.Code, varScope, resolver);
        }

        internal override Statement ResolveWithTypeContext(Resolver resolver)
        {
            Condition = Condition.ResolveWithTypeContext(resolver);
            ResolveWithTypeContext(resolver, this.Code);
            return this;
        }
    }
}
