﻿using System.Collections.Generic;
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
            this.InitCode = initCode.ToArray();
            this.Condition = condition;
            this.StepCode = stepCode.ToArray();
            this.Code = code.ToArray();
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
            Statement.ResolveTypes(this.InitCode, varScope, resolver);
            this.Condition = this.Condition.ResolveType(varScope, resolver);
            Statement.ResolveTypes(this.StepCode, varScope, resolver);
            VariableScope innerScope = new VariableScope(varScope);
            Statement.ResolveTypes(this.Code, innerScope, resolver);
        }

        internal override Statement ResolveWithTypeContext(Resolver resolver)
        {
            Statement.ResolveWithTypeContext(resolver, this.InitCode);
            this.Condition = this.Condition.ResolveWithTypeContext(resolver);
            Statement.ResolveWithTypeContext(resolver, this.StepCode);
            Statement.ResolveWithTypeContext(resolver, this.Code);
            
            // Canonicalize the for loop into a while loop.
            return new StatementBatch(this.FirstToken, [
                ..this.InitCode, 
                new WhileLoop(
                    this.FirstToken, 
                    this.Condition, [
                        ..this.Code, 
                        ..this.StepCode
                    ])
            ]);
        }
    }
}
