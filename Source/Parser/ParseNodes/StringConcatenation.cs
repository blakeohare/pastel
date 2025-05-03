using System.Collections.Generic;

namespace Pastel.Parser.ParseNodes
{
    internal class StringConcatenation : Expression 
    {
        public List<Expression> Expressions { get; private set; }
    
        public StringConcatenation(Expression left, Expression right)
            : base(left.FirstToken, left.Owner)
        {
            this.Expressions = [left, right];
            this.ResolvedType = PType.STRING;
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            throw new System.InvalidOperationException();
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            for (int i = 0; i < this.Expressions.Count; i++)
            {
                Expression current = this.Expressions[i].ResolveWithTypeContext(resolver);
                this.Expressions[i] = current;
                
                InlineConstant? inlineConst = this.Expressions[i] as InlineConstant;
                if (inlineConst != null)
                {
                    Token token = inlineConst.FirstToken;
                    
                    switch (inlineConst.Type.RootValue)
                    {
                        case "int":
                            current = InlineConstant.Of(inlineConst.Value + "", token, this.Owner); break;
                        case "bool":
                            current = InlineConstant.Of((bool)inlineConst.Value ? "true" : "false", token, this.Owner);
                            break;
                        case "float":
                            current = InlineConstant.Of(CodeUtil.FloatToString((double)inlineConst.Value), token, this.Owner);
                            break;
                        case "char":
                            current = InlineConstant.Of("" + (char)inlineConst.Value, token, this.Owner);
                            break;
                        case "string":
                            if ((string)inlineConst.Value == "") current = null;
                            break;
                        case "null":
                            throw new ParserException(current.FirstToken, "Cannot concatenate null to string.");
                        default:
                            throw new ParserException(
                                current.FirstToken,
                                "No string conversion available for type: " + inlineConst.Type.RootValue);
                    }
                }

                if (current != null)
                {
                    PType curType = current.ResolvedType;
                    switch (curType.RootValue)
                    {
                        case "int":
                            current = new CoreFunctionInvocation(current.FirstToken, CoreFunction.INT_TO_STRING, null,
                                [current], this.Owner);
                            break;
                        case "float":
                            current = new CoreFunctionInvocation(
                                current.FirstToken, CoreFunction.FLOAT_TO_STRING, null, [current], this.Owner);
                            break;
                        case "bool":
                            current = new CoreFunctionInvocation(
                                current.FirstToken, CoreFunction.BOOL_TO_STRING, null, [current], this.Owner);
                            break;
                        case "char":
                            current = new CoreFunctionInvocation(
                                current.FirstToken, CoreFunction.CHAR_TO_STRING, null, [current], this.Owner);
                            break;
                        case "string":
                            break;
                        default:
                            throw new ParserException(
                                current.FirstToken,
                                "There is no default conversion from " + curType.RootValue + " to string.");
                    }
                }

                this.Expressions[i] = current;
                if (current == null)
                {
                    this.Expressions.RemoveAt(i--);
                }
            }

            if (this.Expressions.Count == 0)
            {
                return InlineConstant.Of("", this.FirstToken, this.Owner);
            }

            List<Expression> newExpressions = [this.Expressions[0]];

            for (int i = 1; i < this.Expressions.Count; i++)
            {
                Expression lastExpression = newExpressions[newExpressions.Count - 1];
                Expression currentExpr = this.Expressions[i];

                if (lastExpression is InlineConstant leftConst && currentExpr is InlineConstant rightConst)
                {
                    leftConst.Value = (string)leftConst.Value + (string)rightConst.Value;
                }
                else
                {
                    newExpressions.Add(currentExpr);
                }
            }

            this.Expressions = newExpressions;

            if (this.Expressions.Count == 0) return InlineConstant.Of("", this.FirstToken, this.Owner);
            if (this.Expressions.Count == 1) return this.Expressions[0];

            return this;
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            throw new System.InvalidOperationException();
        }
    }
}