using System.Collections.Generic;

namespace Pastel.Parser.ParseNodes
{
    internal abstract class Expression
    {
        public Token FirstToken { get; private set; }
        public PType ResolvedType { get; set; }
        public ICompilationEntity Owner { get; private set; }
        internal ExpressionType Type { get; private set; }
        
        public Expression(ExpressionType type, Token firstToken, ICompilationEntity owner)
        {
            this.Type = type;
            this.FirstToken = firstToken;
            this.Owner = owner;
        }

        public abstract Expression ResolveNamesAndCullUnusedCode(Resolver resolver);

        public static void ResolveNamesAndCullUnusedCodeInPlace(Expression[] expressions, Resolver resolver)
        {
            int length = expressions.Length;
            for (int i = 0; i < length; ++i)
            {
                expressions[i] = expressions[i].ResolveNamesAndCullUnusedCode(resolver);
            }
        }

        internal virtual InlineConstant DoConstantResolution(HashSet<string> cycleDetection, Resolver resolver)
        {
            // override this for expressions that are expected to return constants.
            throw new ParserException(FirstToken, "This expression does not resolve into a constant.");
        }

        internal abstract Expression ResolveType(VariableScope varScope, Resolver resolver);
        internal abstract Expression ResolveWithTypeContext(Resolver resolver);
    }
}
