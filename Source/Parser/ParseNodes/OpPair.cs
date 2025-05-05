using System;

namespace Pastel.Parser.ParseNodes
{
    internal class OpPair : Expression
    {
        public Expression Left { get; private set; }
        public Expression Right { get; private set; }
        public Token OpToken { get; private set; }
        public string Op { get; private set; }
        
        public OpPair(Expression left, Token opToken, Expression right)
            : base(left.FirstToken, left.Owner)
        {
            this.Left = left;
            this.Right = right;
            this.OpToken = opToken;
            this.Op = opToken.Value;
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            this.Left = this.Left.ResolveType(varScope, resolver);
            this.Right = this.Right.ResolveType(varScope, resolver);

            this.ResolvedType = this.DetermineResolvedType(this.Left.ResolvedType, this.Op, this.Right.ResolvedType);

            bool isLeftStr = this.Left.ResolvedType.IsString;
            bool isRightStr = this.Right.ResolvedType.IsString;
            if (this.Op == "+" && (isLeftStr || isRightStr))
            {
                StringConcatenation? leftStrConcat = this.Left as StringConcatenation;
                StringConcatenation? rightStrConcat = this.Right as StringConcatenation;
                if (leftStrConcat != null && rightStrConcat != null)
                {
                    leftStrConcat.Expressions.AddRange(rightStrConcat.Expressions);
                    return leftStrConcat;
                }

                if (leftStrConcat != null)
                {
                    leftStrConcat.Expressions.Add(this.Right);
                    return leftStrConcat;
                }

                if (rightStrConcat != null)
                {
                    rightStrConcat.Expressions.Insert(0, this.Left);
                    return rightStrConcat;
                }

                return new StringConcatenation(this.Left, this.Right);
            }

            return this;
        }

        private PType DetermineResolvedType(PType tleft, string op, PType tright)
        {
            if (op == "==" || op == "!=")
            {
                if (tright.RootValue == tleft.RootValue ||
                    (tright.IsNull && tleft.IsNullable) ||
                    (tright.IsNullable && tleft.IsNull) ||
                    (tright.IsNull && tleft.IsNull))
                {
                    return PType.BOOL;
                }
            }

            string lookup = tleft.RootValue + op + tright.RootValue;

            switch (lookup)
            {
                case "int+int":
                case "int-int":
                case "int*int":
                case "int%int":
                case "int&int":
                case "int|int":
                case "int^int":
                case "int<<int":
                case "int>>int":
                    return PType.INT;

                case "int+double":
                case "double+int":
                case "double+double":
                case "int-double":
                case "double-int":
                case "double-double":
                case "int*double":
                case "double*int":
                case "double*double":
                case "double%int":
                case "int%double":
                case "double%double":
                    return PType.DOUBLE;

                case "int>int":
                case "int<int":
                case "int>=int":
                case "int<=int":
                case "double<int":
                case "double>int":
                case "double<=int":
                case "double>=int":
                case "int<double":
                case "int>double":
                case "int<=double":
                case "int>=double":
                case "double<double":
                case "double>double":
                case "double<=double":
                case "double>=double":
                case "int==int":
                case "double==double":
                case "int==double":
                case "double==int":
                case "int!=int":
                case "double!=double":
                case "int!=double":
                case "double!=int":
                case "bool&&bool":
                case "bool||bool":
                case "char>char":
                case "char<char":
                case "char>=char":
                case "char<=char":
                    return PType.BOOL;

                case "int/int":
                    return PType.INT;

                case "int/double":
                case "double/int":
                case "double/double":
                    return PType.DOUBLE;

                case "string+string":
                case "string+bool":
                case "string+int":
                case "string+double":
                case "string+char":
                case "bool+string":
                case "int+string":
                case "double+string":
                case "char+string":
                    return PType.STRING;
            }

            throw new ParserException(
                this.OpToken,
                "The operator '" + op + "' is not defined for types: " +
                tleft.TypeName + " and " + tright.TypeName + ".");
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            // OpPair is converted from OpChain after this phase.
            throw new System.InvalidOperationException();
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            this.Left = this.Left.ResolveWithTypeContext(resolver);
            this.Right = this.Right.ResolveWithTypeContext(resolver);
            InlineConstant? left = this.Left as InlineConstant;
            InlineConstant? right = this.Right as InlineConstant;
            if (left == null || right == null)
            {
                return this;
            }

            object leftValue = left.Value;
            object rightValue = right.Value;
            string lookup = left.ResolvedType.RootValue + this.Op + right.ResolvedType.RootValue;
            
            switch (lookup)
            {
                case "int+int": return this.CreateInteger((int)leftValue + (int)rightValue);
                case "int-int": return this.CreateInteger((int)leftValue - (int)rightValue);
                case "int*int": return this.CreateInteger((int)leftValue * (int)rightValue);
                case "int/int": return this.CreateInteger((int)leftValue / (int)rightValue); 
                case "int&int": return this.CreateInteger((int)leftValue & (int)rightValue);
                case "int|int": return this.CreateInteger((int)leftValue | (int)rightValue); 
                case "int^int": return this.CreateInteger((int)leftValue ^ (int)rightValue);
                case "int<<int": return this.CreateInteger((int)leftValue << (int)rightValue);
                case "int>>int": return this.CreateInteger((int)leftValue >> (int)rightValue);
                case "int+double": return this.CreateFloat((int)leftValue + (double)rightValue);
                case "int-double": return this.CreateFloat((int)leftValue - (double)rightValue);
                case "int*double": return this.CreateFloat((int)leftValue * (double)rightValue);
                case "int/double": return this.CreateFloat((int)leftValue / (double)rightValue);
                case "double+int": return this.CreateFloat((double)leftValue + (int)rightValue);
                case "double-int": return this.CreateFloat((double)leftValue - (int)rightValue);
                case "double*int": return this.CreateFloat((double)leftValue * (int)rightValue);
                case "double/int": return this.CreateFloat((double)leftValue / (int)rightValue);
                case "double+double": return this.CreateFloat((double)leftValue + (double)rightValue);
                case "double-double": return this.CreateFloat((double)leftValue - (double)rightValue);
                case "double*double": return this.CreateFloat((double)leftValue * (double)rightValue);
                case "double/double": return this.CreateFloat((double)leftValue / (double)rightValue);
                case "bool&&bool": return this.CreateBoolean((bool)leftValue && (bool)rightValue);
                case "bool||bool": return this.CreateBoolean((bool)leftValue || (bool)rightValue);
                case "string+string": return this.CreateString((string)leftValue + (string)rightValue);
                case "string+char": return this.CreateString((string)leftValue + (char)rightValue);
                case "char+string": return this.CreateString((char)leftValue + (string)rightValue);
                default:
                    if (this.Op == "%")
                    {
                        throw new System.NotImplementedException("Remember when you implement this to prevent negatives.");
                    }
                    throw new ParserException(this.OpToken, "The operator is not defined for these two constants.");
            }
        }
        
        private InlineConstant CreateBoolean(bool value)
        {
            return new InlineConstant(PType.BOOL, this.FirstToken, value, Owner) { ResolvedType = PType.BOOL };
        }

        private InlineConstant CreateInteger(int value)
        {
            return new InlineConstant(PType.INT, this.FirstToken, value, Owner) { ResolvedType = PType.INT };
        }

        private InlineConstant CreateFloat(double value)
        {
            return new InlineConstant(PType.DOUBLE, this.FirstToken, value, Owner) { ResolvedType = PType.DOUBLE };
        }

        private InlineConstant CreateString(string value)
        {
            return new InlineConstant(PType.STRING, this.FirstToken, value, Owner) { ResolvedType = PType.STRING };
        }
    }
}
