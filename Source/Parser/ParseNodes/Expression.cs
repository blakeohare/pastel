using System.Collections.Generic;

namespace Pastel.Parser.ParseNodes
{
    internal abstract class Expression
    {
        public Token FirstToken { get; private set; }

        public PType ResolvedType { get; set; }

        public ICompilationEntity Owner { get; private set; }

        public Expression(Token firstToken, ICompilationEntity owner)
        {
            FirstToken = firstToken;
            Owner = owner;
        }

        public abstract Expression ResolveNamesAndCullUnusedCode(PastelCompiler compiler);

        public static void ResolveNamesAndCullUnusedCodeInPlace(Expression[] expressions, PastelCompiler compiler)
        {
            int length = expressions.Length;
            for (int i = 0; i < length; ++i)
            {
                expressions[i] = expressions[i].ResolveNamesAndCullUnusedCode(compiler);
            }
        }

        internal virtual InlineConstant DoConstantResolution(HashSet<string> cycleDetection, PastelCompiler compiler)
        {
            // override this for expressions that are expected to return constants.
            throw new ParserException(FirstToken, "This expression does not resolve into a constant.");
        }

        internal abstract Expression ResolveType(VariableScope varScope, PastelCompiler compiler);
        internal abstract Expression ResolveWithTypeContext(PastelCompiler compiler);
    }
}
