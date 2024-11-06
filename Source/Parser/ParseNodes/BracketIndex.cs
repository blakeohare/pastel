using System;

namespace Pastel.Parser.ParseNodes
{
    internal class BracketIndex : Expression
    {
        public Expression Root { get; set; }
        public Token BracketToken { get; set; }
        public Expression Index { get; set; }

        public BracketIndex(Expression root, Token bracketToken, Expression index) : base(root.FirstToken, root.Owner)
        {
            Root = root;
            BracketToken = bracketToken;
            Index = index;
        }

        public override Expression ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            Root = Root.ResolveNamesAndCullUnusedCode(compiler);
            Index = Index.ResolveNamesAndCullUnusedCode(compiler);
            return this;
        }

        internal override Expression ResolveType(VariableScope varScope, PastelCompiler compiler)
        {
            Root = Root.ResolveType(varScope, compiler);
            Index = Index.ResolveType(varScope, compiler);

            PType rootType = Root.ResolvedType;
            PType indexType = Index.ResolvedType;

            bool badIndex = false;
            if (rootType.RootValue == "List" || rootType.RootValue == "Array")
            {
                badIndex = !indexType.IsIdentical(compiler, PType.INT);
                ResolvedType = rootType.Generics[0];
            }
            else if (rootType.RootValue == "Dictionary")
            {
                badIndex = !indexType.IsIdentical(compiler, rootType.Generics[0]);
                ResolvedType = rootType.Generics[1];
            }
            else if (rootType.RootValue == "string")
            {
                badIndex = !indexType.IsIdentical(compiler, PType.INT);
                ResolvedType = PType.CHAR;
                if (Root is InlineConstant && Index is InlineConstant)
                {
                    string c = ((string)((InlineConstant)Root).Value)[(int)((InlineConstant)Index).Value].ToString();
                    InlineConstant newValue = new InlineConstant(PType.CHAR, FirstToken, c, Owner);
                    newValue.ResolveType(varScope, compiler);
                    return newValue;
                }
            }
            else
            {
                badIndex = true;
            }

            if (badIndex)
            {
                throw new ParserException(BracketToken, "Cannot index into a " + rootType + " with a " + indexType + ".");
            }

            return this;
        }

        internal override Expression ResolveWithTypeContext(PastelCompiler compiler)
        {
            Root = Root.ResolveWithTypeContext(compiler);
            Index = Index.ResolveWithTypeContext(compiler);

            Expression[] args = new Expression[] { Root, Index };
            CoreFunction nf;
            switch (Root.ResolvedType.RootValue)
            {
                case "string": nf = CoreFunction.STRING_CHAR_AT; break;
                case "List": nf = CoreFunction.LIST_GET; break;
                case "Dictionary": nf = CoreFunction.DICTIONARY_GET; break;
                case "Array": nf = CoreFunction.ARRAY_GET; break;
                default: throw new InvalidOperationException(); // this should have been caught earlier in ResolveType()
            }
            return new CoreFunctionInvocation(FirstToken, nf, args, Owner) { ResolvedType = ResolvedType };
        }
    }
}
