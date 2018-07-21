using System;
using System.Collections.Generic;

namespace Pastel.ParseNodes
{
    internal class InlineConstant : Expression
    {
        public object Value { get; set; }
        public PType Type { get; set; }

        public static InlineConstant Of(Token similarToken, object value)
        {
            TokenType type = TokenType.ALPHANUMS; // TODO: change this.
            Token token = new Token(similarToken.FileName, value.ToString(), similarToken.Index, similarToken.Line, similarToken.Column, type);
            return OfImpl(token, value);
        }

        public static InlineConstant Of(TokenStream tokens, object value)
        {
            return OfImpl(tokens.CreateDummyToken(value.ToString()), value);
        }

        private static InlineConstant OfImpl(Token dummyToken, object value)
        {
            if (value is int)
            {
                return (InlineConstant)new InlineConstant(PType.INT, dummyToken, value).ResolveType(null, null);
            }

            throw new NotImplementedException();
        }

        public InlineConstant(PType type, Token firstToken, object value) : base(firstToken)
        {
            this.Type = type;
            this.ResolvedType = type;
            this.Value = value;
        }

        public override Expression ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            return this;
        }

        public InlineConstant CloneWithNewToken(Token token)
        {
            return new InlineConstant(this.Type, token, this.Value);
        }

        internal override InlineConstant DoConstantResolution(HashSet<string> cycleDetection, PastelCompiler compiler)
        {
            return this;
        }

        internal override Expression ResolveType(VariableScope varScope, PastelCompiler compiler)
        {
            this.ResolvedType = this.Type;
            return this;
        }

        internal override Expression ResolveWithTypeContext(PastelCompiler compiler)
        {
            return this;
        }
    }
}
