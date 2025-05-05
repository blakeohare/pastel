using Pastel.Parser.ParseNodes;
using System;
using System.Text;

namespace Pastel.Transpilers.Go
{
    internal class GoExpressionTranslator : AbstractExpressionTranslator
    {
        public GoExpressionTranslator(TranspilerContext ctx)
            : base(ctx)
        { }

        private StringBuffer TranslateExpressionStringUnwrap(Expression expr, bool asPtr)
        {
            if (expr is InlineConstant ic && ic.Type.IsString)
            {
                return StringBuffer
                    .Of(CodeUtil.ConvertStringValueToCode((string)ic.Value))
                    .WithTightness(ExpressionTightness.ATOMIC);
            }

            StringBuffer output = this.TranslateExpression(expr)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".str")
                .WithTightness(ExpressionTightness.UNARY_SUFFIX);

            if (!asPtr)
            {
                output = StringBuffer
                    .Of("*")
                    .Push(output.EnsureTightness(ExpressionTightness.UNARY_PREFIX))
                    .WithTightness(ExpressionTightness.UNARY_PREFIX);
            }

            return output;
        }

        private StringBuffer TranslateArrayGetNoCast(Expression array, Expression index)
        {
            return this.TranslateExpression(array)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".items[")
                .Push(this.TranslateExpression(index))
                .Push("]")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateArrayGet(Expression array, Expression index)
        {
            StringBuffer sb = this.TranslateArrayGetNoCast(array, index);
            PType itemType = array.ResolvedType.Generics[0];
            if (itemType.RootValue != "object")
            {
                sb
                    .Push(".(")
                    .Push(this.TypeTranspiler.TranslateType(array.ResolvedType.Generics[0]))
                    .Push(")");
            }

            return sb.WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateArrayJoin(Expression array, Expression sep)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateArrayLength(Expression array)
        {
            return StringBuffer
                .Of("len(")
                .Push(this.TranslateExpression(array).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push(".items)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateArrayNew(PType arrayType, Expression lengthExpression)
        {
            string defaultVal;
            switch (arrayType.RootValue)
            {
                case "bool": defaultVal = "false"; break;
                case "byte": defaultVal = "byte(0)"; break;
                case "char": defaultVal = "0"; break;
                case "int": defaultVal = "0"; break;
                case "double": defaultVal = "0.0"; break;
                default: defaultVal = "nil"; break;
            }
            return StringBuffer
                .Of("PST_newList(")
                .Push(this.TranslateExpression(lengthExpression))
                .Push(", ")
                .Push(defaultVal)
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateArraySet(Expression array, Expression index, Expression value)
        {
            return this.TranslateExpression(array)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".items[")
                .Push(this.TranslateExpression(index))
                .Push("] = ")
                .Push(this.TranslateExpression(value));
        }

        public override StringBuffer TranslateBase64ToBytes(Expression base64String)
        {
            this.MarkFeatureAsUsed("IMPORT:encoding/base64");
            return StringBuffer
                .Of("PST_base64ToBytes(")
                .Push(this.TranslateExpressionStringUnwrap(base64String, false))
                .Push(")");
        }

        public override StringBuffer TranslateBase64ToString(Expression base64String)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateBooleanConstant(bool value)
        {
            return StringBuffer
                .Of(value ? "true" : "false")
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateBooleanNot(UnaryOp unaryOp)
        {
            return StringBuffer
                .Of("!")
                .Push(this.TranslateExpression(unaryOp.Expression).EnsureTightness(ExpressionTightness.UNARY_PREFIX));
        }

        public override StringBuffer TranslateBoolToString(Expression value)
        {
            return StringBuffer
                .Of("PST_boolToStr(")
                .Push(this.TranslateExpression(value))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateBytesToBase64(Expression byteArr)
        {
            this.MarkFeatureAsUsed("IMPORT:encoding/base64");
            return StringBuffer
                .Of("PST_bytesToBase64(")
                .Push(this.TranslateExpression(byteArr))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateCast(PType type, Expression expression)
        {
            return this.TranslateExpression(expression)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".(")
                .Push(this.TypeTranspiler.TranslateType(type))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateCharConstant(char value)
        {
            int num = (int)value;
            return StringBuffer
                .Of("" + num)
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateCharToString(Expression charValue)
        {
            return StringBuffer
                .Of("PST_charToStr(")
                .Push(this.TranslateExpression(charValue))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateChr(Expression charCode)
        {
            return this.TranslateExpression(charCode);
        }

        public override StringBuffer TranslateConstructorInvocation(ConstructorInvocation constructorInvocation)
        {
            StructDefinition sDef = constructorInvocation.StructDefinition;
            string name = sDef.NameToken.Value;
            StringBuffer buf = StringBuffer
                .Of("&S_")
                .Push(name)
                .Push("{ ");
            for (int i = 0; i < sDef.FieldNames.Length; i++)
            {
                if (i > 0) buf.Push(", ");
                buf
                    .Push("f_")
                    .Push(sDef.FieldNames[i].Value)
                    .Push(": ")
                    .Push(this.TranslateExpression(constructorInvocation.Args[i]));
            }

            return buf.Push(" }");
        }

        public override StringBuffer TranslateCurrentTimeSeconds()
        {
            this.MarkFeatureAsUsed("IMPORT:time");
            return StringBuffer
                .Of("float64(time.Now().UnixMicro()) / 1000000")
                .WithTightness(ExpressionTightness.MULTIPLICATION);
        }

        private bool IsStringDict(Expression dictExpr)
        {
            return dictExpr.ResolvedType.Generics[0].IsString;
        }

        public override StringBuffer TranslateDictionaryContainsKey(Expression dictionary, Expression key)
        {
            bool isString = this.IsStringDict(dictionary);
            return StringBuffer
                .Of(isString ? "PST_dictContainsStr(" : "PST_dictContainsInt(")
                .Push(this.TranslateExpression(dictionary))
                .Push(", ")
                .Push(this.TranslateExpressionStringUnwrap(key, false))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryGet(Expression dictionary, Expression key)
        {
            bool isString = this.IsStringDict(dictionary);
            PType valueType = dictionary.ResolvedType.Generics[1];
            StringBuffer sb = StringBuffer
                .Of(isString ? "PST_dictGetStr(" : "PST_dictGetInt(")
                .Push(this.TranslateExpression(dictionary))
                .Push(", ")
                .Push(this.TranslateExpressionStringUnwrap(key, false))
                .Push(")");
            if (valueType.RootValue != "object")
            {
                sb
                    .Push(".(")
                    .Push(this.TypeTranspiler.TranslateType(valueType))
                    .Push(")");
            }
            return sb.WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryKeys(Expression dictionary)
        {
            bool isString = this.IsStringDict(dictionary);
            return StringBuffer
                .Of(isString ? "PST_dictKeysStr(" : "PST_dictKeysInt(")
                .Push(this.TranslateExpression(dictionary))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryNew(PType keyType, PType valueType)
        {
            return StringBuffer
                .Of(keyType.IsString ? "PST_newDictStr()" : "PST_newDictInt()")
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateDictionaryRemove(Expression dictionary, Expression key)
        {
            bool isString = this.IsStringDict(dictionary);
            return StringBuffer
                .Of(isString ? "PST_dictRemoveStr(" : "PST_dictRemoveInt(")
                .Push(this.TranslateExpression(dictionary))
                .Push(", ")
                .Push(isString
                    ? this.TranslateExpressionStringUnwrap(key, false)
                    : this.TranslateExpression(key))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionarySet(Expression dictionary, Expression key, Expression value)
        {
            bool isString = this.IsStringDict(dictionary);
            return StringBuffer
                .Of(isString ? "PST_dictSetStr(" : "PST_dictSetInt")
                .Push(this.TranslateExpression(dictionary))
                .Push(", ")
                // String is consumed by the helper function directly in its wrapped form as it will need to be wrapped anyway
                .Push(this.TranslateExpression(key))
                .Push(", ")
                .Push(this.TranslateExpression(value))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionarySize(Expression dictionary)
        {
            return StringBuffer
                .Of("len(")
                .Push(this.TranslateExpression(dictionary).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push(".k)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryValues(Expression dictionary)
        {
            bool isString = this.IsStringDict(dictionary);
            return StringBuffer
                .Of("PST_wrapArray(")
                .Push(this.TranslateExpression(dictionary).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push(".v, true)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDivideFloat(Expression left, Expression right)
        {
            bool leftFloat = left.ResolvedType.IsFloat;
            bool rightFloat = right.ResolvedType.IsFloat;
            StringBuffer leftSb = this.TranslateExpression(left);
            StringBuffer rightSb = this.TranslateExpression(right);
            if (!leftFloat) leftSb.Prepend("float64(").Push(")").WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
            if (!rightFloat) rightSb.Prepend("float64(").Push(")").WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
            return
                leftSb.EnsureTightness(ExpressionTightness.MULTIPLICATION)
                .Push(" / ")
                .Push(rightSb.EnsureGreaterTightness(ExpressionTightness.MULTIPLICATION))
                .WithTightness(ExpressionTightness.MULTIPLICATION);
        }

        public override StringBuffer TranslateDivideInteger(Expression left, Expression right)
        {
            return this.TranslateExpression(left)
                .EnsureTightness(ExpressionTightness.MULTIPLICATION)
                .Push(" / ")
                .Push(this.TranslateExpression(right).EnsureGreaterTightness(ExpressionTightness.MULTIPLICATION))
                .WithTightness(ExpressionTightness.MULTIPLICATION);
        }

        public override StringBuffer TranslateEmitComment(string value)
        {
            return StringBuffer
                .Of("// ")
                .Push(value);
        }

        public override StringBuffer TranslateExtensibleCallbackInvoke(Expression name, Expression argsArray)
        {
            return StringBuffer.Of("PST_ExtCallbacks[")
                .Push(this.TranslateExpressionStringUnwrap(name, false))
                .Push("](")
                .Push(this.TranslateExpression(argsArray).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push(".items)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateFloatConstant(double value)
        {
            return StringBuffer
                .Of(CodeUtil.FloatToString(value))
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateFloatToString(Expression floatExpr)
        {
            this.MarkFeatureAsUsed("IMPORT:strconv");
            this.MarkFeatureAsUsed("IMPORT:strings");
            return StringBuffer.Of("PST_floatToStr(")
                .Push(this.TranslateExpression(floatExpr))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateFunctionInvocation(FunctionReference funcRef, Expression[] args)
        {
            StringBuffer buf = this.TranslateExpression(funcRef)
                .Push("(");
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0) buf.Push(", ");
                buf.Push(this.TranslateExpression(args[i]));
            }
            return buf.Push(")");
        }

        public override StringBuffer TranslateFunctionReference(FunctionReference funcRef)
        {
            return StringBuffer.Of("fn_").Push(funcRef.Function.Name);
        }

        public override StringBuffer TranslateInlineIncrement(Expression innerExpression, bool isPrefix, bool isAddition)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateIntegerConstant(int value)
        {
            return StringBuffer.Of(value.ToString());
        }

        public override StringBuffer TranslateIntToString(Expression integer)
        {
            this.MarkFeatureAsUsed("IMPORT:strconv");

            return StringBuffer.Of("PST_intToStr(")
                .Push(this.TranslateExpression(integer))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateIsValidInteger(Expression stringValue)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListAdd(Expression list, Expression item)
        {
            return this.TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".add(")
                .Push(this.TranslateExpression(item))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListClear(Expression list)
        {
            return StringBuffer.Of("PST_listClear(")
                .Push(this.TranslateExpression(list))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListConcat(Expression list, Expression items)
        {
            return StringBuffer.Of("PST_listConcat(")
                .Push(this.TranslateExpression(list))
                .Push(", ")
                .Push(this.TranslateExpression(items))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListGet(Expression list, Expression index)
        {
            return this.TranslateArrayGet(list, index);
        }

        public override StringBuffer TranslateListInsert(Expression list, Expression index, Expression item)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListJoinChars(Expression list)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListJoinStrings(Expression list, Expression sep)
        {
            this.MarkFeatureAsUsed("IMPORT:strings");
            return StringBuffer.Of("PST_listJoin(")
                .Push(this.TranslateExpression(list))
                .Push(", ")
                .Push(this.TranslateExpressionStringUnwrap(sep, false))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListNew(PType type)
        {
            return StringBuffer
                .Of("PST_newList(0, nil)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListPop(Expression list)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListRemoveAt(Expression list, Expression index)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListReverse(Expression list)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListSet(Expression list, Expression index, Expression value)
        {
            return this.TranslateArraySet(list, index, value);
        }

        public override StringBuffer TranslateListShuffle(Expression list)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListSize(Expression list)
        {
            return StringBuffer.Of("len(")
                .Push(this.TranslateExpression(list).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push(".items)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListToArray(Expression list)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMathAbs(Expression num)
        {
            string funcName;
            if (num.ResolvedType.IsFloat)
            {
                this.MarkFeatureAsUsed("IMPORT:math");
                funcName = "math.Abs";
            }
            else
            {
                funcName = "PST_mathAbsInt";
            }

            return StringBuffer
                .Of(funcName)
                .Push("(")
                .Push(this.TranslateExpression(num))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathArcCos(Expression ratio)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMathArcSin(Expression ratio)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMathArcTan(Expression yComponent, Expression xComponent)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMathCeil(Expression num)
        {
            this.MarkFeatureAsUsed("IMPORT:math");
            return StringBuffer
                .Of("int(math.Ceil(")
                .Push(this.TranslateExpression(num))
                .Push("))")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathCos(Expression thetaRadians)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMathFloor(Expression num)
        {
            this.MarkFeatureAsUsed("IMPORT:math");
            return StringBuffer
                .Of("int(math.Floor(")
                .Push(this.TranslateExpression(num))
                .Push("))")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathLog(Expression value)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMathPow(Expression expBase, Expression exponent)
        {
            this.MarkFeatureAsUsed("IMPORT:math");

            return StringBuffer.Of("math.Pow(")
                .Push(this.TranslateExpression(expBase))
                .Push(", ")
                .Push(this.TranslateExpression(exponent))
                .Push(")");
        }

        public override StringBuffer TranslateMathSin(Expression thetaRadians)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMathTan(Expression thetaRadians)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMultiplyList(Expression list, Expression n)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateNegative(UnaryOp unaryOp)
        {
            return StringBuffer.Of("-")
                .Push(this.TranslateExpression(unaryOp.Expression).EnsureTightness(ExpressionTightness.UNARY_PREFIX))
                .WithTightness(ExpressionTightness.UNARY_PREFIX);
        }

        public override StringBuffer TranslateNullConstant()
        {
            return StringBuffer.Of("nil").WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateOrd(Expression charValue)
        {
            return this.TranslateExpression(charValue);
        }

        private ExpressionTightness GetTightnessOfOp(string op)
        {
            switch (op)
            {
                case "&&":
                case "||":
                    return ExpressionTightness.BOOLEAN_LOGIC;

                case "+":
                case "-":
                    return ExpressionTightness.ADDITION;

                case "&":
                case "|":
                case "^":
                    return ExpressionTightness.BITWISE;

                case "<<":
                case ">>":
                    return ExpressionTightness.BITSHIFT;

                case "*":
                case "/":
                case "%":
                    return ExpressionTightness.MULTIPLICATION;

                case "==":
                case "!=":
                    return ExpressionTightness.EQUALITY;

                case "<":
                case ">":
                case ">=":
                case "<=":
                    return ExpressionTightness.INEQUALITY;

                default:
                    throw new System.NotImplementedException();
            }
        }

        public override StringBuffer TranslateOpPair(OpPair opPair)
        {
            Expression left = opPair.Left;
            Expression right = opPair.Right;
            StringBuffer leftSb = this.TranslateExpression(left);
            StringBuffer rightSb = this.TranslateExpression(right);
            ExpressionTightness opTightness = this.GetTightnessOfOp(opPair.Op);
            string leftType = left.ResolvedType.RootValue;
            string rightType = right.ResolvedType.RootValue;
            bool hasInt = leftType == "int" || rightType == "int";
            bool hasFloat = leftType == "double" || rightType == "double";
            if (hasFloat && hasInt)
            {
                if (leftType == "int")
                {
                    leftSb = StringBuffer
                        .Of("float64(")
                        .Push(leftSb)
                        .Push(")")
                        .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
                }
                else
                {
                    rightSb = StringBuffer
                        .Of("float64(")
                        .Push(rightSb)
                        .Push(")")
                        .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
                }
            }

            return leftSb
                .EnsureTightness(opTightness)
                .Push(" ")
                .Push(opPair.Op)
                .Push(" ")
                .Push(rightSb.EnsureGreaterTightness(opTightness));
        }

        public override StringBuffer TranslateParseFloatUnsafe(Expression stringValue)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateParseInt(Expression safeStringValue)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslatePrintStdErr(Expression value)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslatePrintStdOut(Expression value)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateRandomFloat()
        {
            this.MarkFeatureAsUsed("IMPORT:math/rand");
            return StringBuffer
                .Of("rand.Float64()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateSortedCopyOfIntArray(Expression intArray)
        {
            this.MarkFeatureAsUsed("IMPORT:sort");
            return StringBuffer
                .Of("PST_SortedIntArrayCopy(")
                .Push(this.TranslateExpression(intArray))
                .Push(")");
        }

        public override StringBuffer TranslateSortedCopyOfStringArray(Expression stringArray)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringAppend(Expression str1, Expression str2)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringCharAt(Expression str, Expression index)
        {
            return StringBuffer.Of("PST_strGetUChars(")
                .Push(this.TranslateExpression(str))
                .Push(")[")
                .Push(this.TranslateExpression(index))
                .Push("]")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringCharCodeAt(Expression str, Expression index)
        {
            return StringBuffer.Of("PST_strGetUChars(")
                .Push(this.TranslateExpression(str))
                .Push(")[")
                .Push(this.TranslateExpression(index))
                .Push("]")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringCompareIsReverse(Expression str1, Expression str2)
        {
            StringBuffer left = this.TranslateExpression(str1).EnsureTightness(ExpressionTightness.UNARY_PREFIX);
            StringBuffer right = this.TranslateExpression(str2).EnsureGreaterTightness(ExpressionTightness.SUFFIX_SEQUENCE);
            return StringBuffer
                .Of("*")
                .Push(left)
                .Push(".str > *")
                .Push(right)
                .Push(".str")
                .WithTightness(ExpressionTightness.INEQUALITY);
        }

        public override StringBuffer TranslateStringConcatAll(Expression[] strings)
        {
            this.MarkFeatureAsUsed("IMPORT:strings");

            StringBuffer sb = StringBuffer.Of("PST_strJoin([]*pstring{");
            for (int i = 0; i < strings.Length; i++)
            {
                if (i > 0) sb.Push(", ");
                sb.Push(this.TranslateExpression(strings[i]));
            }

            sb.Push("})");
            return sb.WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringConcatPair(Expression strLeft, Expression strRight)
        {
            return StringBuffer
                .Of("PST_str(")
                .Push(this.TranslateExpressionStringUnwrap(strLeft, false).EnsureTightness(ExpressionTightness.ADDITION))
                .Push(" + ")
                .Push(this.TranslateExpressionStringUnwrap(strRight, false).EnsureGreaterTightness(ExpressionTightness.ADDITION))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringConstant(string value)
        {
            return StringBuffer
                .Of("PST_str(")
                .Push(CodeUtil.ConvertStringValueToCode(value))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringContains(Expression haystack, Expression needle)
        {
            this.MarkFeatureAsUsed("IMPORT:strings");
            return StringBuffer
                .Of("strings.Contains(")
                .Push(this.TranslateExpressionStringUnwrap(haystack, false))
                .Push(", ")
                .Push(this.TranslateExpressionStringUnwrap(needle, false))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringEndsWith(Expression haystack, Expression needle)
        {
            this.MarkFeatureAsUsed("IMPORT:strings");
            return StringBuffer
                .Of("strings.HasSuffix(")
                .Push(this.TranslateExpressionStringUnwrap(haystack, false))
                .Push(", ")
                .Push(this.TranslateExpressionStringUnwrap(needle, false))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringEquals(Expression left, Expression right)
        {
            StringBuffer leftBuf = this.TranslateExpression(left)
                .EnsureTightness(ExpressionTightness.EQUALITY);
            StringBuffer rightBuf = this.TranslateExpression(right)
                .EnsureGreaterTightness(ExpressionTightness.EQUALITY);
            return StringBuffer
                .Of("PST_strEq(")
                .Push(this.TranslateExpression(left))
                .Push(", ")
                .Push(this.TranslateExpression(right))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringFromCharCode(Expression charCode)
        {
            return StringBuffer.Of("PST_strFromCharCode(")
                .Push(this.TranslateExpression(charCode))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringIndexOf(Expression haystack, Expression needle)
        {
            return StringBuffer
                .Of("PST_strFind(")
                .Push(this.TranslateExpression(haystack))
                .Push(", ")
                .Push(this.TranslateExpression(needle))
                .Push(", true, 0)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringIndexOfWithStart(Expression haystack, Expression needle, Expression startIndex)
        {
            return StringBuffer
                .Of("PST_strFind(")
                .Push(this.TranslateExpression(haystack))
                .Push(", ")
                .Push(this.TranslateExpression(needle))
                .Push(", true, ")
                .Push(this.TranslateExpression(startIndex))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringLastIndexOf(Expression haystack, Expression needle)
        {
            return StringBuffer
                .Of("PST_strFind(")
                .Push(this.TranslateExpression(haystack))
                .Push(", ")
                .Push(this.TranslateExpression(needle))
                .Push(", false, 0)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringLength(Expression str)
        {
            return StringBuffer
                .Of("PST_strLen(")
                .Push(this.TranslateExpression(str))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringReplace(Expression haystack, Expression needle, Expression newNeedle)
        {
            this.MarkFeatureAsUsed("IMPORT:strings");
            return StringBuffer
                .Of("PST_strReplace(")
                .Push(this.TranslateExpressionStringUnwrap(haystack, false))
                .Push(", ")
                .Push(this.TranslateExpressionStringUnwrap(needle, false))
                .Push(", ")
                .Push(this.TranslateExpressionStringUnwrap(newNeedle, false))
                .Push(")");
        }

        public override StringBuffer TranslateStringReverse(Expression str)
        {
            return StringBuffer
                .Of("PST_strReverse(")
                .Push(this.TranslateExpressionStringUnwrap(str, false))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringSplit(Expression haystack, Expression needle)
        {
            this.MarkFeatureAsUsed("IMPORT:strings");
            return StringBuffer
                .Of("PST_strSplit(")
                .Push(this.TranslateExpressionStringUnwrap(haystack, false))
                .Push(", ")
                .Push(this.TranslateExpressionStringUnwrap(needle, false))
                .Push(")");
        }

        public override StringBuffer TranslateStringStartsWith(Expression haystack, Expression needle)
        {
            this.MarkFeatureAsUsed("IMPORT:strings");
            return StringBuffer
                .Of("strings.HasPrefix(")
                .Push(this.TranslateExpressionStringUnwrap(haystack, false))
                .Push(", ")
                .Push(this.TranslateExpressionStringUnwrap(needle, false))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringSubstring(Expression str, Expression start, Expression length)
        {
            return StringBuffer.Of("PST_substr(")
                .Push(this.TranslateExpression(str))
                .Push(", ")
                .Push(this.TranslateExpression(start))
                .Push(", ")
                .Push(this.TranslateExpression(length))
                .Push(")")
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringSubstringIsEqualTo(Expression haystack, Expression startIndex, Expression needle)
        {
            return StringBuffer
                .Of("PST_strSubstringEquals(")
                .Push(this.TranslateExpression(haystack))
                .Push(", ")
                .Push(this.TranslateExpression(startIndex))
                .Push(", ")
                .Push(this.TranslateExpression(needle))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringToLower(Expression str)
        {
            this.MarkFeatureAsUsed("IMPORT:strings");
            return StringBuffer.Of("PST_strLower(")
                .Push(this.TranslateExpression(str))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringToUpper(Expression str)
        {
            this.MarkFeatureAsUsed("IMPORT:strings");
            return StringBuffer.Of("PST_strUpper(")
                .Push(this.TranslateExpression(str))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringToUtf8Bytes(Expression str)
        {
            return StringBuffer
                .Of("PST_strToUtf8Bytes(")
                .Push(this.TranslateExpressionStringUnwrap(str, false))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringTrim(Expression str)
        {
            this.MarkFeatureAsUsed("IMPORT:strings");
            return StringBuffer
                .Of("PST_strTrim(")
                .Push(this.TranslateExpressionStringUnwrap(str, false))
                .Push(", 3)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringTrimEnd(Expression str)
        {
            this.MarkFeatureAsUsed("IMPORT:strings");
            return StringBuffer
                .Of("PST_strTrim(")
                .Push(this.TranslateExpressionStringUnwrap(str, false))
                .Push(", 1)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringTrimStart(Expression str)
        {
            this.MarkFeatureAsUsed("IMPORT:strings");
            return StringBuffer
                .Of("PST_strTrim(")
                .Push(this.TranslateExpressionStringUnwrap(str, false))
                .Push(", 2)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringBuilderAdd(Expression sbInst, Expression obj)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringBuilderClear(Expression sbInst)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringBuilderNew()
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringBuilderToString(Expression sbInst)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStrongReferenceEquality(Expression left, Expression right)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStructFieldDereference(Expression root, StructDefinition structDef, string fieldName, int fieldIndex)
        {
            return this.TranslateExpression(root)
                .Push(".f_")
                .Push(fieldName);
        }

        public override StringBuffer TranslateToCodeString(Expression str)
        {
            this.MarkFeatureAsUsed("IMPORT:encoding/json");
            return StringBuffer.Of("PST_stringToCode(")
                .Push(this.TranslateExpressionStringUnwrap(str, false))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateTryParseFloat(Expression stringValue, Expression floatOutList)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateUtf8BytesToString(Expression bytes)
        {
            return StringBuffer
                .Of("PST_utf8BytesToStr(")
                .Push(this.TranslateExpression(bytes).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push(".items)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateVariable(Variable variable)
        {
            return StringBuffer
                .Of("v_")
                .Push(variable.Name)
                .WithTightness(ExpressionTightness.ATOMIC);
        }
    }
}
