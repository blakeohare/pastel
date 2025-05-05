using System;

namespace Pastel.Parser.ParseNodes
{
    internal class BracketIndex : Expression
    {
        public Expression Root { get; set; }
        public Token BracketToken { get; set; }
        public Expression Index { get; set; }

        public BracketIndex(Expression root, Token bracketToken, Expression index) 
            : base(ExpressionType.BRACKET_INDEX, root.FirstToken, root.Owner)
        {
            this.Root = root;
            this.BracketToken = bracketToken;
            this.Index = index;
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            this.Root = this.Root.ResolveNamesAndCullUnusedCode(resolver);
            this.Index = this.Index.ResolveNamesAndCullUnusedCode(resolver);
            return this;
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            this.Root = this.Root.ResolveType(varScope, resolver);
            this.Index = this.Index.ResolveType(varScope, resolver);

            PType rootType = this.Root.ResolvedType;
            PType indexType = this.Index.ResolvedType;

            bool badIndex = false;
            if (rootType.IsList || rootType.IsArray)
            {
                badIndex = !indexType.IsIdentical(resolver, PType.INT);
                this.ResolvedType = rootType.Generics[0];
            }
            else if (rootType.IsDictionary)
            {
                badIndex = !indexType.IsIdentical(resolver, rootType.Generics[0]);
                this.ResolvedType = rootType.Generics[1];
            }
            else if (rootType.IsString)
            {
                badIndex = !indexType.IsInteger;
                this.ResolvedType = PType.CHAR;
                // TODO(cleanup): shouldn't badIndex be checked here?
                if (this.Root is InlineConstant rootIc && this.Index is InlineConstant indexIc)
                {
                    string rootStr = (string)rootIc.Value; 
                    string c = rootStr[(int)indexIc.Value].ToString();
                    InlineConstant newValue = InlineConstant.OfCharacter(c, this.FirstToken, this.Owner);
                    newValue.ResolveType(varScope, resolver);
                    return newValue;
                }
            }
            else
            {
                badIndex = true;
            }

            if (badIndex)
            {
                throw new ParserException(this.BracketToken, "Cannot index into a " + rootType + " with a " + indexType + ".");
            }

            return this;
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            this.Root = this.Root.ResolveWithTypeContext(resolver);
            this.Index = this.Index.ResolveWithTypeContext(resolver);

            Expression[] args = [ Root, Index ];
            CoreFunction nf;
            switch (this.Root.ResolvedType.RootValue)
            {
                case "string": nf = CoreFunction.STRING_CHAR_AT; break;
                case "List": nf = CoreFunction.LIST_GET; break;
                case "Dictionary": nf = CoreFunction.DICTIONARY_GET; break;
                case "Array": nf = CoreFunction.ARRAY_GET; break;
                default: throw new InvalidOperationException(); // this should have been caught earlier in ResolveType()
            }

            return new CoreFunctionInvocation(this.FirstToken, nf, args, this.Owner) { ResolvedType = this.ResolvedType };
        }
    }
}
