using System;
using System.Collections.Generic;

namespace Pastel.Parser.ParseNodes
{
    internal class InlineConstant : Expression
    {
        public object Value { get; set; }
        public PType Type { get; set; }

        public static InlineConstant Of(object value, ICompilationEntity owner)
        {
            Token dummyToken = Token.CreateDummyToken(value.ToString());
            if (value is int)
            {
                return (InlineConstant)new InlineConstant(PType.INT, dummyToken, value, owner).ResolveType(null, null);
            }

            throw new NotImplementedException();
        }

        public InlineConstant(PType type, Token firstToken, object value, ICompilationEntity owner) : base(firstToken, owner)
        {
            Type = type;
            ResolvedType = type;
            Value = value;
        }

        public override Expression ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            return this;
        }

        public InlineConstant CloneWithNewToken(Token token)
        {
            return new InlineConstant(Type, token, Value, Owner);
        }

        public InlineConstant CloneWithNewTokenAndOwner(Token token, ICompilationEntity owner)
        {
            return new InlineConstant(Type, token, Value, owner);
        }

        internal override InlineConstant DoConstantResolution(HashSet<string> cycleDetection, PastelCompiler compiler)
        {
            return this;
        }

        internal override Expression ResolveType(VariableScope varScope, PastelCompiler compiler)
        {
            ResolvedType = Type;
            if (ResolvedType.RootValue == "char")
            {
                string strValue = Value.ToString();
                if (strValue.Length > 1)
                {
                    throw new ParserException(FirstToken, "Character constant with a coded value longer than 1 actual character.");
                }
                Value = strValue[0];
            }
            return this;
        }

        internal override Expression ResolveWithTypeContext(PastelCompiler compiler)
        {
            return this;
        }
    }
}
