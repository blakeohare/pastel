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

        public override StringBuffer TranslateArrayGet(Expression array, Expression index)
        {
            return TranslateExpression(array)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".items[")
                .Push(TranslateExpression(index))
                .Push("].(")
                .Push(this.TypeTranspiler.TranslateType(array.ResolvedType.Generics[0]))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
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
            return StringBuffer
                .Of("PST_newList(")
                .Push(this.TranslateExpression(lengthExpression))
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
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateBase64ToString(Expression base64String)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateBooleanConstant(bool value)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateBooleanNot(UnaryOp unaryOp)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateCast(PType type, Expression expression)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateCharConstant(char value)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateCharToString(Expression charValue)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateChr(Expression charCode)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateConstructorInvocation(ConstructorInvocation constructorInvocation)
        {
            StructDefinition sDef = constructorInvocation.StructDefinition;
            string name = sDef.NameToken.Value;
            StringBuffer buf = StringBuffer
                .Of("PtrBox_")
                .Push(name)
                .Push("{ o: ")
                .Push("&S_")
                .Push(name)
                .Push("{ ");
            for (int i = 0; i < sDef.FieldNames.Length; i++)
            {
                if (i > 0) buf.Push(", ");
                buf
                    .Push("f_")
                    .Push(sDef.FieldNames[i].Value)
                    .Push(": ")
                    .Push(TranslateExpression(constructorInvocation.Args[i]));
            }
            return buf.Push(" } }");
        }

        public override StringBuffer TranslateCurrentTimeSeconds()
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateDictionaryContainsKey(Expression dictionary, Expression key)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateDictionaryGet(Expression dictionary, Expression key)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateDictionaryKeys(Expression dictionary)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateDictionaryNew(PType keyType, PType valueType)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateDictionaryRemove(Expression dictionary, Expression key)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateDictionarySet(Expression dictionary, Expression key, Expression value)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateDictionarySize(Expression dictionary)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateDictionaryValues(Expression dictionary)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateEmitComment(string value)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateExtensibleCallbackInvoke(Expression name, Expression argsArray)
        {
            // TODO: if name is a string constant, then use directly
            return StringBuffer.Of("PST_ExtCallbacks[*")
                .Push(this.TranslateExpression(name).EnsureGreaterTightness(ExpressionTightness.UNARY_PREFIX))
                .Push("](")
                .Push(this.TranslateExpression(argsArray).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push(".items)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateFloatBuffer16()
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateFloatConstant(double value)
        {
            return StringBuffer.Of(CodeUtil.FloatToString(value));
        }

        public override StringBuffer TranslateFloatDivision(Expression floatNumerator, Expression floatDenominator)
        {
            bool wrapA = floatNumerator.ResolvedType.RootValue == "int";
            bool wrapB = floatDenominator.ResolvedType.RootValue == "int";
            return StringBuffer
                .Of("(")
                .Push(wrapA ? "float64(" : "(")
                .Push(TranslateExpression(floatNumerator))
                .Push(") / ")
                .Push(wrapB ? "float64(" : "(")
                .Push(TranslateExpression(floatDenominator))
                .Push("))");
        }

        public override StringBuffer TranslateFloatToInt(Expression floatExpr)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateFloatToString(Expression floatExpr)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateFunctionInvocation(FunctionReference funcRef, Expression[] args)
        {
            StringBuffer buf = TranslateExpression(funcRef)
                .Push("(");
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0) buf.Push(", ");
                buf.Push(TranslateExpression(args[i]));
            }
            return buf.Push(")");
        }

        public override StringBuffer TranslateFunctionReference(FunctionReference funcRef)
        {
            return StringBuffer.Of("fn_").Push(funcRef.Function.Name);
        }

        public override StringBuffer TranslateGetFunction(Expression name)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateInlineIncrement(Expression innerExpression, bool isPrefix, bool isAddition)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateIntBuffer16()
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateIntegerConstant(int value)
        {
            return StringBuffer.Of(value.ToString());
        }

        public override StringBuffer TranslateIntegerDivision(Expression integerNumerator, Expression integerDenominator)
        {
            return StringBuffer
                .Of("((")
                .Push(TranslateExpression(integerNumerator))
                .Push(") / (")
                .Push(TranslateExpression(integerDenominator))
                .Push("))");
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
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListConcat(Expression list, Expression items)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListGet(Expression list, Expression index)
        {
            throw new NotImplementedException();
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
                .Push(this.TranslateExpression(sep))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListNew(PType type)
        {
            return StringBuffer
                .Of("PST_newList(0)")
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
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListShuffle(Expression list)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListSize(Expression list)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListToArray(Expression list)
        {
            throw new NotImplementedException();
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

        public override StringBuffer TranslateMathCos(Expression thetaRadians)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMathLog(Expression value)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMathPow(Expression expBase, Expression exponent)
        {
            this.MarkFeatureAsUsed("IMPORT:math");

            return StringBuffer.Of("math.Pow(")
                .Push(TranslateExpression(expBase))
                .Push(", ")
                .Push(TranslateExpression(exponent))
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
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateNullConstant()
        {
            return StringBuffer.Of("nil").WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateOrd(Expression charValue)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateOpChain(OpChain opChain)
        {
            bool isFloat = false;
            bool containsInt = false;
            Expression[] expressions = opChain.Expressions;
            for (int i = 0; i < expressions.Length; i++)
            {
                string type = expressions[i].ResolvedType.RootValue;
                if (type == "double")
                {
                    isFloat = true;
                }
                else if (type == "int")
                {
                    containsInt = true;
                }
            }

            bool doIntToFloatConversion = isFloat && containsInt;

            StringBuffer buf = StringBuffer.Of("(");
            for (int i = 0; i < opChain.Expressions.Length; i++)
            {
                if (i > 0)
                {
                    buf.Push(" ").Push(opChain.Ops[i - 1].Value).Push(" ");
                }
                Expression expr = opChain.Expressions[i];
                bool convertToFloat = doIntToFloatConversion && expr.ResolvedType.RootValue == "int";
                buf
                    .Push(convertToFloat ? "float64(" : "(")
                    .Push(TranslateExpression(opChain.Expressions[i]))
                    .Push(")");
            }
            return buf.Push(")");
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
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateSortedCopyOfIntArray(Expression intArray)
        {
            this.MarkFeatureAsUsed("IMPORT:sort");
            return StringBuffer
                .Of("PST_SortedIntArrayCopy(")
                .Push(TranslateExpression(intArray))
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

        public override StringBuffer TranslateStringBuffer16()
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringCharAt(Expression str, Expression index)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringCharCodeAt(Expression str, Expression index)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringCompareIsReverse(Expression str1, Expression str2)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringConcatAll(Expression[] strings)
        {
            this.MarkFeatureAsUsed("IMPORT:strings");

            StringBuffer sb = StringBuffer.Of("PST_strPtr(strings.Join([]string{");
            for (int i = 0; i < strings.Length; i++)
            {
                if (i > 0) sb.Push(", ");
                sb.Push("*");
                sb.Push(this.TranslateExpression(strings[i]).EnsureTightness(ExpressionTightness.UNARY_PREFIX));
            }

            sb.Push("}, \"\"))");
            return sb.WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateStringConcatPair(Expression strLeft, Expression strRight)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringConstant(string value)
        {
            return StringBuffer
                .Of("PST_strPtr(")
                .Push(CodeUtil.ConvertStringValueToCode(value))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringContains(Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringEndsWith(Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringIndexOf(Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringIndexOfWithStart(Expression haystack, Expression needle, Expression startIndex)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringLastIndexOf(Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringLength(Expression str)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringReplace(Expression haystack, Expression needle, Expression newNeedle)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringReverse(Expression str)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringSplit(Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringStartsWith(Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringSubstring(Expression str, Expression start, Expression length)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringSubstringIsEqualTo(Expression haystack, Expression startIndex, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringToLower(Expression str)
        {
            this.MarkFeatureAsUsed("IMPORT:strings");
            return StringBuffer.Of("PST_strPtr(strings.ToLower(*")
                .Push(this.TranslateExpression(str))
                .Push("))")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringToUpper(Expression str)
        {
            this.MarkFeatureAsUsed("IMPORT:strings");
            return StringBuffer.Of("PST_strPtr(strings.ToUpper(*")
                .Push(this.TranslateExpression(str))
                .Push("))")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringToUtf8Bytes(Expression str)
        {
            return StringBuffer
                .Of("PST_strToUtf8Bytes(")
                .Push(this.TranslateExpression(str))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringTrim(Expression str)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringTrimEnd(Expression str)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringTrimStart(Expression str)
        {
            throw new NotImplementedException();
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
            return TranslateExpression(root)
                .Push(".o.f_")
                .Push(fieldName);
        }

        public override StringBuffer TranslateToCodeString(Expression str)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateTryParseFloat(Expression stringValue, Expression floatOutList)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateUtf8BytesToString(Expression bytes)
        {
            throw new NotImplementedException();
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
