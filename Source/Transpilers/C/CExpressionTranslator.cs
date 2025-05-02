using Pastel.Parser.ParseNodes;
using System;

namespace Pastel.Transpilers.C
{
    internal class CExpressionTranslator : CurlyBraceExpressionTranslator
    {
        public CExpressionTranslator(TranspilerContext ctx)
            : base(ctx)
        { }

        public override StringBuffer TranslatePrintStdErr(Expression value)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslatePrintStdOut(Expression value)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateArrayGet(Expression array, Expression index)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateArrayJoin(Expression array, Expression sep)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateArrayLength(Expression array)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateArrayNew(PType arrayType, Expression lengthExpression)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateArraySet(Expression array, Expression index, Expression value)
        {
            throw new NotImplementedException();
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

        public override StringBuffer TranslateCurrentTimeSeconds()
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateConstructorInvocation(ConstructorInvocation constructorInvocation)
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

        public override StringBuffer TranslateExtensibleCallbackInvoke(Expression name, Expression argsArray)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateFloatBuffer16()
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateFloatDivision(Expression floatNumerator, Expression floatDenominator)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateFloatToInt(Expression floatExpr)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateFloatToString(Expression floatExpr)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateGetFunction(Expression name)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateIntBuffer16()
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateIntegerDivision(Expression integerNumerator, Expression integerDenominator)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateIntToString(Expression integer)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateIsValidInteger(Expression stringValue)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListAdd(Expression list, Expression item)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListConcat(Expression list, Expression items)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListClear(Expression list)
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
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListNew(PType type)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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

        public override StringBuffer TranslateRandomFloat()
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateSortedCopyOfIntArray(Expression intArray)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringConcatPair(Expression strLeft, Expression strRight)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
