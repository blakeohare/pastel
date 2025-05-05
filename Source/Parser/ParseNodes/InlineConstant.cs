using System;
using System.Collections.Generic;

namespace Pastel.Parser.ParseNodes
{
    internal class InlineConstant : Expression
    {
        public object Value { get; set; }
        public PType Type { get; set; }

        public static InlineConstant Of(object value, Token token, ICompilationEntity owner)
        {
            Token dummyToken = token;
            PType type;
            if (value == null) type = PType.NULL;
            else if (value is int) type = PType.INT;
            else if (value is double) type = PType.DOUBLE;
            else if (value is string) type = PType.STRING;
            else if (value is bool) type = PType.BOOL;
            else if (value is char) type = PType.CHAR;
            else throw new NotImplementedException();

            return new InlineConstant(type, dummyToken, value, owner) { ResolvedType = type };
        }

        public InlineConstant(PType type, Token firstToken, object value, ICompilationEntity owner)
            : base(firstToken, owner)
        {
            this.Type = type;
            this.ResolvedType = type;
            this.Value = value;
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
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

        internal override InlineConstant DoConstantResolution(HashSet<string> cycleDetection, Resolver resolver)
        {
            return this;
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            this.ResolvedType = this.Type;
            if (this.ResolvedType.IsChar)
            {
                string strValue = this.Value.ToString();
                if (strValue.Length > 1)
                {
                    throw new ParserException(FirstToken, "Character constant with a coded value longer than 1 actual character.");
                }
                this.Value = strValue[0];
            }
            return this;
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            return this;
        }
    }
}
