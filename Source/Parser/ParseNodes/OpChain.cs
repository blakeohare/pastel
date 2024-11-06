﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser.ParseNodes
{
    internal class OpChain : Expression
    {
        public Expression[] Expressions { get; set; }
        public Token[] Ops { get; set; }

        public OpChain(
            IList<Expression> expressions,
            IList<Token> ops) : base(expressions[0].FirstToken, expressions[0].Owner)
        {
            Expressions = expressions.ToArray();
            Ops = ops.ToArray();
        }

        public bool IsStringConcatenation
        {
            get
            {
                if (Expressions[0].ResolvedType.RootValue == "string")
                {
                    for (int i = 0; i < Ops.Length; ++i)
                    {
                        if (Ops[i].Value != "+")
                        {
                            return false;
                        }
                    }
                    return true;
                }
                return false;
            }
        }

        public override Expression ResolveNamesAndCullUnusedCode(PastelCompiler compiler)
        {
            ResolveNamesAndCullUnusedCodeInPlace(Expressions, compiler);
            // Don't do short-circuiting yet for && and ||
            return this;
        }

        internal override InlineConstant DoConstantResolution(HashSet<string> cycleDetection, PastelCompiler compiler)
        {
            for (int i = 0; i < Expressions.Length; ++i)
            {
                Expressions[i] = Expressions[i].DoConstantResolution(cycleDetection, compiler);
            }

            InlineConstant current = (InlineConstant)Expressions[0];
            for (int i = 1; i < Expressions.Length; ++i)
            {
                InlineConstant next = (InlineConstant)Expressions[i];
                string lookup = current.Type.RootValue + Ops[i - 1].Value + next.Type.RootValue;
                switch (lookup)
                {
                    case "int+int":
                        current = new InlineConstant(PType.INT, current.FirstToken, (int)current.Value + (int)next.Value, next.Owner);
                        break;
                    case "int-int":
                        current = new InlineConstant(PType.INT, current.FirstToken, (int)current.Value - (int)next.Value, next.Owner);
                        break;
                    case "int*int":
                        current = new InlineConstant(PType.INT, current.FirstToken, (int)current.Value * (int)next.Value, next.Owner);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            return current;
        }

        internal override Expression ResolveType(VariableScope varScope, PastelCompiler compiler)
        {
            for (int i = 0; i < Expressions.Length; ++i)
            {
                Expressions[i] = Expressions[i].ResolveType(varScope, compiler);
            }

            ResolvedType = Expressions[0].ResolvedType;

            for (int i = 0; i < Ops.Length; ++i)
            {
                PType nextType = Expressions[i + 1].ResolvedType;
                string op = Ops[i].Value;
                if (op == "==" || op == "!=")
                {
                    if (nextType.RootValue == ResolvedType.RootValue ||
                        nextType.RootValue == "null" && ResolvedType.IsNullable ||
                        nextType.IsNullable && ResolvedType.RootValue == "null" ||
                        nextType.RootValue == "null" && ResolvedType.RootValue == "null")
                    {
                        ResolvedType = PType.BOOL;
                        continue;
                    }
                }
                string lookup = ResolvedType.RootValue + Ops[i].Value + nextType.RootValue;
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
                        ResolvedType = PType.INT;
                        break;

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
                        ResolvedType = PType.DOUBLE;
                        break;

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
                        ResolvedType = PType.BOOL;
                        break;

                    case "int/int":
                    case "int/double":
                    case "double/int":
                    case "double/double":
                        throw new ParserException(Ops[i], "Due to varying platform behavior of / use Core.IntegerDivision(numerator, denominator) or Core.FloatDivision(numerator, denominator)");

                    case "char+string":
                    case "string+char":
                        ResolvedType = PType.STRING;
                        break;

                    case "string+string":
                        ResolvedType = PType.STRING;
                        break;

                    default:
                        throw new ParserException(Ops[i], "The operator '" + Ops[i].Value + "' is not defined for types: " + ResolvedType + " and " + nextType + ".");
                }
            }
            return this;
        }

        internal override Expression ResolveWithTypeContext(PastelCompiler compiler)
        {
            for (int i = 0; i < Expressions.Length; ++i)
            {
                Expressions[i] = Expressions[i].ResolveWithTypeContext(compiler);
            }

            InlineConstant left = Expressions[0] as InlineConstant;
            InlineConstant right = Expressions[1] as InlineConstant;
            while (left != null && right != null)
            {
                object leftValue = left.Value;
                object rightValue = right.Value;
                string lookup = left.ResolvedType.RootValue + Ops[0].Value + right.ResolvedType.RootValue;
                switch (lookup)
                {
                    case "int+int": Expressions[0] = CreateInteger(left.FirstToken, (int)leftValue + (int)rightValue); break;
                    case "int-int": Expressions[0] = CreateInteger(left.FirstToken, (int)leftValue - (int)rightValue); break;
                    case "int*int": Expressions[0] = CreateInteger(left.FirstToken, (int)leftValue * (int)rightValue); break;
                    case "int/int": Expressions[0] = CreateInteger(left.FirstToken, (int)leftValue / (int)rightValue); break;
                    case "int&int": Expressions[0] = CreateInteger(left.FirstToken, (int)leftValue & (int)rightValue); break;
                    case "int|int": Expressions[0] = CreateInteger(left.FirstToken, (int)leftValue | (int)rightValue); break;
                    case "int^int": Expressions[0] = CreateInteger(left.FirstToken, (int)leftValue ^ (int)rightValue); break;
                    case "int<<int": Expressions[0] = CreateInteger(left.FirstToken, (int)leftValue << (int)rightValue); break;
                    case "int>>int": Expressions[0] = CreateInteger(left.FirstToken, (int)leftValue >> (int)rightValue); break;
                    case "int+double": Expressions[0] = CreateFloat(left.FirstToken, (int)leftValue + (double)rightValue); break;
                    case "int-double": Expressions[0] = CreateFloat(left.FirstToken, (int)leftValue - (double)rightValue); break;
                    case "int*double": Expressions[0] = CreateFloat(left.FirstToken, (int)leftValue * (double)rightValue); break;
                    case "int/double": Expressions[0] = CreateFloat(left.FirstToken, (int)leftValue / (double)rightValue); break;
                    case "double+int": Expressions[0] = CreateFloat(left.FirstToken, (double)leftValue + (int)rightValue); break;
                    case "double-int": Expressions[0] = CreateFloat(left.FirstToken, (double)leftValue - (int)rightValue); break;
                    case "double*int": Expressions[0] = CreateFloat(left.FirstToken, (double)leftValue * (int)rightValue); break;
                    case "double/int": Expressions[0] = CreateFloat(left.FirstToken, (double)leftValue / (int)rightValue); break;
                    case "double+double": Expressions[0] = CreateFloat(left.FirstToken, (double)leftValue + (double)rightValue); break;
                    case "double-double": Expressions[0] = CreateFloat(left.FirstToken, (double)leftValue - (double)rightValue); break;
                    case "double*double": Expressions[0] = CreateFloat(left.FirstToken, (double)leftValue * (double)rightValue); break;
                    case "double/double": Expressions[0] = CreateFloat(left.FirstToken, (double)leftValue / (double)rightValue); break;
                    case "bool&&bool": Expressions[0] = CreateBoolean(left.FirstToken, (bool)leftValue && (bool)rightValue); break;
                    case "bool||bool": Expressions[0] = CreateBoolean(left.FirstToken, (bool)leftValue || (bool)rightValue); break;
                    case "string+string": Expressions[0] = CreateString(left.FirstToken, (string)leftValue + (string)rightValue); break;
                    case "string+char": Expressions[0] = CreateString(left.FirstToken, (string)leftValue + (char)rightValue); break;
                    case "char+string": Expressions[0] = CreateString(left.FirstToken, (char)leftValue + (string)rightValue); break;
                    default:
                        if (Ops[0].Value == "%")
                        {
                            throw new NotImplementedException("Remember when you implement this to prevent negatives.");
                        }
                        throw new ParserException(Ops[0], "The operator is not defined for these two constants.");

                }
                List<Expression> expressions = new List<Expression>(Expressions);
                expressions.RemoveAt(1); // I know, I know...
                Expressions = expressions.ToArray();

                if (Expressions.Length == 1)
                {
                    return Expressions[0];
                }
                List<Token> ops = new List<Token>(Ops);
                ops.RemoveAt(0);
                Ops = ops.ToArray();
                left = Expressions[0] as InlineConstant;
                right = Expressions[1] as InlineConstant;
            }

            // TODO(pastel-split): This is just a quick and dirty short-circuit logic for && and ||
            // Do full logic later. Currently this is causing problems in specific snippets in Crayon libraries.
            string opValue = Ops[0].Value;
            if (left != null)
            {
                if (opValue == "&&" && left.Value is bool)
                {
                    return (bool)left.Value ? this : left;
                }
                if (opValue == "||" && left.Value is bool)
                {
                    return (bool)left.Value ? left : this;
                }
            }

            return this;
        }

        private InlineConstant CreateBoolean(Token originalFirstToken, bool value)
        {
            return new InlineConstant(PType.BOOL, originalFirstToken, value, Owner) { ResolvedType = PType.BOOL };
        }

        private InlineConstant CreateInteger(Token originalFirstToken, int value)
        {
            return new InlineConstant(PType.INT, originalFirstToken, value, Owner) { ResolvedType = PType.INT };
        }

        private InlineConstant CreateFloat(Token originalFirstToken, double value)
        {
            return new InlineConstant(PType.DOUBLE, originalFirstToken, value, Owner) { ResolvedType = PType.DOUBLE };
        }

        private InlineConstant CreateString(Token originalFirstToken, string value)
        {
            return new InlineConstant(PType.STRING, originalFirstToken, value, Owner) { ResolvedType = PType.STRING };
        }
    }
}