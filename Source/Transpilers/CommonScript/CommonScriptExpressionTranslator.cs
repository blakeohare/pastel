using Pastel.Parser.ParseNodes;
using System;

namespace Pastel.Transpilers.CommonScript
{
    internal class CommonScriptExpressionTranslator : CurlyBraceExpressionTranslator
    {
        public CommonScriptExpressionTranslator(TranspilerContext ctx) : base(ctx) { }

        public override StringBuffer TranslateArrayGet(Expression array, Expression index)
        {
            return TranslateExpression(array)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("[")
                .Push(TranslateExpression(index))
                .Push("]")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateArrayJoin(Expression array, Expression sep)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateArrayLength(Expression array)
        {
            return this.TranslateExpression(array)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".length");
        }

        public override StringBuffer TranslateArrayNew(PType arrayType, Expression lengthExpression)
        {
            if (lengthExpression is InlineConstant && lengthExpression.ResolvedType.IsInteger)
            {
                int length = (int)((InlineConstant)lengthExpression).Value;
                StringBuffer buf = StringBuffer.Of("[");
                if (length <= 3)
                {
                    for (int i = 0; i < length; i++)
                    {
                        if (i > 0) buf.Push(", ");
                        buf.Push("null");
                    }
                    buf.Push("]");
                    return buf.WithTightness(ExpressionTightness.ATOMIC);
                }
            }
            return StringBuffer
                .Of("[null] * ")
                .Push(this.TranslateExpression(lengthExpression).EnsureGreaterTightness(ExpressionTightness.MULTIPLICATION))
                .WithTightness(ExpressionTightness.MULTIPLICATION);
        }

        public override StringBuffer TranslateArraySet(Expression array, Expression index, Expression value)
        {
            return this.TranslateExpression(array)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("[")
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

        public override StringBuffer TranslateBytesToBase64(Expression byteArr)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateCast(PType type, Expression expression)
        {
            return this.TranslateExpression(expression);
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
            StringBuffer buf = StringBuffer.Of("[");
            Expression[] args = constructorInvocation.Args;
            for (int i = 0; i < args.Length; ++i)
            {
                if (i > 0) buf.Push(", ");
                buf.Push(TranslateExpression(args[i]));
            }
            return buf
                .Push("]")
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateCurrentTimeSeconds()
        {
            return StringBuffer.Of("getUnixTimeFloat()").WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryContainsKey(Expression dictionary, Expression key)
        {
            return this.TranslateExpression(dictionary)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".contains(")
                .Push(this.TranslateExpression(key))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryGet(Expression dictionary, Expression key)
        {
            return this.TranslateExpression(dictionary)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("[")
                .Push(this.TranslateExpression(key))
                .Push("]")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryKeys(Expression dictionary)
        {
            return this.TranslateExpression(dictionary)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".keys()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryNew(PType keyType, PType valueType)
        {
            return StringBuffer.Of("{}").WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateDictionaryRemove(Expression dictionary, Expression key)
        {
            return this.TranslateExpression(dictionary)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".remove(")
                .Push(this.TranslateExpression(key))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionarySet(Expression dictionary, Expression key, Expression value)
        {
            return this.TranslateExpression(dictionary)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("[")
                .Push(this.TranslateExpression(key))
                .Push("] = ")
                .Push(this.TranslateExpression(value));
        }

        public override StringBuffer TranslateDictionarySize(Expression dictionary)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateDictionaryValues(Expression dictionary)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateDivideFloat(Expression left, Expression right)
        {
            return this.TranslateExpression(left)
                .EnsureTightness(ExpressionTightness.MULTIPLICATION)
                .Push(" / ")
                .Push(this.TranslateExpression(right).EnsureGreaterTightness(ExpressionTightness.MULTIPLICATION))
                .WithTightness(ExpressionTightness.MULTIPLICATION);
        }

        public override StringBuffer TranslateDivideInteger(Expression left, Expression right)
        {
            return this.TranslateDivideFloat(left, right);
        }

        public override StringBuffer TranslateExtensibleCallbackInvoke(Expression name, Expression argsArray)
        {
            return StringBuffer
                .Of("PST_ExtCallbacks.ext[")
                .Push(this.TranslateExpression(name))
                .Push("].invoke(")
                .Push(this.TranslateExpression(argsArray))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateFloatToString(Expression floatExpr)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateIntToString(Expression integer)
        {
            return this.TranslateExpression(integer)
                .EnsureTightness(ExpressionTightness.ADDITION)
                .Push(" + ''")
                .WithTightness(ExpressionTightness.ADDITION);
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
            return this.TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("[")
                .Push(this.TranslateExpression(index))
                .Push("]")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListInsert(Expression list, Expression index, Expression item)
        {
            return this.TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".insert(")
                .Push(this.TranslateExpression(item))
                .Push(", ")
                .Push(this.TranslateExpression(index))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListJoinChars(Expression list)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListJoinStrings(Expression list, Expression sep)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListNew(PType type)
        {
            return StringBuffer.Of("[]").WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateListPop(Expression list)
        {
            return this.TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".pop()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
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
            return this.TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("[")
                .Push(this.TranslateExpression(index))
                .Push("] = ")
                .Push(this.TranslateExpression(value));
        }

        public override StringBuffer TranslateListShuffle(Expression list)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListSize(Expression list)
        {
            return this.TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".length")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListToArray(Expression list)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMathAbs(Expression num)
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

        public override StringBuffer TranslateMathCeil(Expression num)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMathCos(Expression thetaRadians)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMathFloor(Expression num)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMathLog(Expression value)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMathPow(Expression expBase, Expression exponent)
        {
            // The tightness is over-aggressive for both nested expressions and for its own reported tightness.
            return TranslateExpression(expBase)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(" ** ")
                .Push(TranslateExpression(exponent).EnsureGreaterTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .WithTightness(ExpressionTightness.MULTIPLICATION);
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

        public override StringBuffer TranslateNullConstant()
        {
            return StringBuffer.Of("null").WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateStringBuilderNew()
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateOrd(Expression charValue)
        {
            throw new NotImplementedException();
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
            return this.TranslateExpression(intArray)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("[:].sort()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateSortedCopyOfStringArray(Expression stringArray)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringAppend(Expression str1, Expression str2)
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

        public override StringBuffer TranslateStringBuilderToString(Expression sbInst)
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
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringConcatPair(Expression strLeft, Expression strRight)
        {
            return this.TranslateExpression(strLeft)
                .WithTightness(ExpressionTightness.ADDITION)
                .Push(" + ")
                .Push(this.TranslateExpression(strRight).EnsureGreaterTightness(ExpressionTightness.ADDITION))
                .WithTightness(ExpressionTightness.ADDITION);
        }

        public override StringBuffer TranslateStringConstant(string value)
        {
            return base.TranslateStringConstant(value);
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringToUpper(Expression str)
        {
            return this.TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".upper()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringToUtf8Bytes(Expression str)
        {
            throw new NotImplementedException();
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

        public override StringBuffer TranslateStrongReferenceEquality(Expression left, Expression right)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStructFieldDereference(Expression root, StructDefinition structDef, string fieldName, int fieldIndex)
        {
            return TranslateExpression(root)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("[" + fieldIndex + "]")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
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
    }
}
