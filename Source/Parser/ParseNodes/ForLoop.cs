using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser.ParseNodes
{
    internal class ForLoop : Statement
    {
        public Statement[] InitCode { get; set; }
        public Expression Condition { get; set; }
        public Statement[] StepCode { get; set; }
        public Statement[] Code { get; set; }

        public ForLoop(
            Token forToken,
            IList<Statement> initCode,
            Expression condition,
            IList<Statement> stepCode,
            IList<Statement> code) : base(forToken)
        {
            InitCode = initCode.ToArray();
            Condition = condition;
            StepCode = stepCode.ToArray();
            Code = code.ToArray();
        }

        public override Statement ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            this.InitCode = ResolveNamesAndCullUnusedCodeForBlock(this.InitCode, resolver).ToArray();
            this.Condition = this.Condition.ResolveNamesAndCullUnusedCode(resolver);
            this.StepCode = ResolveNamesAndCullUnusedCodeForBlock(this.StepCode, resolver).ToArray();

            // TODO: check Condition for falseness

            this.Code = ResolveNamesAndCullUnusedCodeForBlock(this.Code, resolver).ToArray();

            return this;
        }

        internal override void ResolveTypes(VariableScope varScope, Resolver resolver)
        {
            // This gets compiled as a wihle loop with the init added before the loop, so it should go in the same variable scope.
            // The implication is that multiple declarations in the init for successive loops will collide.
            ResolveTypes(InitCode, varScope, resolver);
            Condition = Condition.ResolveType(varScope, resolver);
            ResolveTypes(StepCode, varScope, resolver);
            VariableScope innerScope = new VariableScope(varScope);
            ResolveTypes(Code, innerScope, resolver);
        }

        internal override Statement ResolveWithTypeContext(Resolver resolver)
        {
            ResolveWithTypeContext(resolver, InitCode);
            this.Condition = this.Condition.ResolveWithTypeContext(resolver);
            ResolveWithTypeContext(resolver, StepCode);
            ResolveWithTypeContext(resolver, Code);

            // Canonialize the for loop into a while loop.
            List<Statement> loopCode = new List<Statement>(Code);
            loopCode.AddRange(StepCode);
            WhileLoop whileLoop = new WhileLoop(FirstToken, Condition, loopCode);
            loopCode = new List<Statement>(InitCode);
            loopCode.Add(whileLoop);
            return new StatementBatch(FirstToken, loopCode);
        }
    }
}
