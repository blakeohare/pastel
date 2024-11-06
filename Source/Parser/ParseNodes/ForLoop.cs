using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser.ParseNodes
{
    internal class ForLoop : Executable
    {
        public Executable[] InitCode { get; set; }
        public Expression Condition { get; set; }
        public Executable[] StepCode { get; set; }
        public Executable[] Code { get; set; }

        public ForLoop(
            Token forToken,
            IList<Executable> initCode,
            Expression condition,
            IList<Executable> stepCode,
            IList<Executable> code) : base(forToken)
        {
            InitCode = initCode.ToArray();
            Condition = condition;
            StepCode = stepCode.ToArray();
            Code = code.ToArray();
        }

        public override Executable ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            InitCode = ResolveNamesAndCullUnusedCodeForBlock(InitCode, compiler).ToArray();
            Condition = Condition.ResolveNamesAndCullUnusedCode(compiler);
            StepCode = ResolveNamesAndCullUnusedCodeForBlock(StepCode, compiler).ToArray();

            // TODO: check Condition for falseness

            Code = ResolveNamesAndCullUnusedCodeForBlock(Code, compiler).ToArray();

            return this;
        }

        internal override void ResolveTypes(VariableScope varScope, PastelCompiler compiler)
        {
            // This gets compiled as a wihle loop with the init added before the loop, so it should go in the same variable scope.
            // The implication is that multiple declarations in the init for successive loops will collide.
            ResolveTypes(InitCode, varScope, compiler);
            Condition = Condition.ResolveType(varScope, compiler);
            ResolveTypes(StepCode, varScope, compiler);
            VariableScope innerScope = new VariableScope(varScope);
            ResolveTypes(Code, innerScope, compiler);
        }

        internal override Executable ResolveWithTypeContext(PastelCompiler compiler)
        {
            ResolveWithTypeContext(compiler, InitCode);
            Condition = Condition.ResolveWithTypeContext(compiler);
            ResolveWithTypeContext(compiler, StepCode);
            ResolveWithTypeContext(compiler, Code);

            // Canonialize the for loop into a while loop.
            List<Executable> loopCode = new List<Executable>(Code);
            loopCode.AddRange(StepCode);
            WhileLoop whileLoop = new WhileLoop(FirstToken, Condition, loopCode);
            loopCode = new List<Executable>(InitCode);
            loopCode.Add(whileLoop);
            return new ExecutableBatch(FirstToken, loopCode);
        }
    }
}
