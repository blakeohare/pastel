using Pastel.Parser.ParseNodes;
using System;

namespace Pastel.Transpilers.CSharp
{
    internal class CSharpExpressionTranslator : CurlyBraceExpressionTranslator
    {
        public CSharpExpressionTranslator(TranspilerContext ctx)
            : base(ctx)
        { }

        public override StringBuffer TranslatePrintStdErr(Expression value)
        {
            return StringBuffer
                .Of("PlatformTranslationHelper.PrintStdErr(")
                .Push(this.TranslateExpression(value))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslatePrintStdOut(Expression value)
        {
            return StringBuffer
                .Of("PlatformTranslationHelper.PrintStdOut(")
                .Push(this.TranslateExpression(value))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateArrayGet(Expression array, Expression index)
        {
            return this.TranslateExpression(array)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("[")
                .Push(this.TranslateExpression(index))
                .Push("]")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateArrayJoin(Expression array, Expression sep)
        {
            return StringBuffer
                .Of("string.Join(")
                .Push(this.TranslateExpression(sep))
                .Push(", ")
                .Push(this.TranslateExpression(array))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateArrayLength(Expression array)
        {
            return this.TranslateExpression(array)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".Length")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateArrayNew(PType arrayType, Expression lengthExpression)
        {
            int nestingLevel = 0;
            while (arrayType.IsArray)
            {
                nestingLevel++;
                arrayType = arrayType.Generics[0];
            }
            StringBuffer buf = StringBuffer
                .Of("new ")
                .Push(this.TypeTranspiler.TranslateType(arrayType))
                .Push("[")
                .Push(this.TranslateExpression(lengthExpression))
                .Push("]");
            while (nestingLevel-- > 0)
            {
                buf.Push("[]");
            }
            return buf
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
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
            return StringBuffer
                .Of("System.Convert.FromBase64String(")
                .Push(this.TranslateExpression(base64String))
                .Push(").Select(b => (int)b).ToArray()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateBase64ToString(Expression base64String)
        {
            return StringBuffer
                .Of("PST_Base64ToString(")
                .Push(this.TranslateExpression(base64String))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateBytesToBase64(Expression byteArr)
        {
            return StringBuffer
                .Of("PST_BytesToBase64(")
                .Push(this.TranslateExpression(byteArr))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateCast(PType type, Expression expression)
        {
            return StringBuffer
                .Of("(")
                .Push(this.TypeTranspiler.TranslateType(type))
                .Push(")")
                .Push(this.TranslateExpression(expression).EnsureTightness(ExpressionTightness.UNARY_PREFIX))
                .WithTightness(ExpressionTightness.UNARY_PREFIX);
        }

        public override StringBuffer TranslateCharConstant(char value)
        {
            return StringBuffer
                .Of(CodeUtil.ConvertCharToCharConstantCode(value))
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateCharToString(Expression charValue)
        {
            return this.TranslateExpression(charValue)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".ToString()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateChr(Expression charCode)
        {
            return this.TranslateExpression(charCode)
                .EnsureTightness(ExpressionTightness.UNARY_PREFIX)
                .Prepend("(char)")
                .WithTightness(ExpressionTightness.UNARY_PREFIX);
        }

        public override StringBuffer TranslateCurrentTimeSeconds()
        {
            return StringBuffer
                .Of("PST_CurrentTime")
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateConstructorInvocation(ConstructorInvocation constructorInvocation)
        {
            StringBuffer buf = StringBuffer
                .Of("new ")
                .Push(this.TypeTranspiler.TranslateType(constructorInvocation.Type))
                .Push("(");
            Expression[] args = constructorInvocation.Args;
            for (int i = 0; i < args.Length; ++i)
            {
                if (i > 0) buf.Push(", ");
                buf.Push(this.TranslateExpression(args[i]));
            }
            return buf
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryContainsKey(Expression dictionary, Expression key)
        {
            return this.TranslateExpression(dictionary)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".ContainsKey(")
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
                .Push(".Keys.ToArray()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryNew(PType keyType, PType valueType)
        {
            return StringBuffer
                .Of("new Dictionary<")
                .Push(this.TypeTranspiler.TranslateType(keyType))
                .Push(", ")
                .Push(this.TypeTranspiler.TranslateType(valueType))
                .Push(">()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryRemove(Expression dictionary, Expression key)
        {
            return this.TranslateExpression(dictionary)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".Remove(")
                .Push(this.TranslateExpression(key))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionarySet(Expression dictionary, Expression key, Expression value)
        {
            return this.TranslateExpression(dictionary)
                .Push("[")
                .Push(this.TranslateExpression(key))
                .Push("] = ")
                .Push(this.TranslateExpression(value));
        }

        public override StringBuffer TranslateDictionarySize(Expression dictionary)
        {
            return this.TranslateExpression(dictionary)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".Count")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryValues(Expression dictionary)
        {
            return this.TranslateExpression(dictionary)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".Values.ToArray()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
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
                .Of("PST_ExtCallbacks[")
                .Push(this.TranslateExpression(name))
                .Push("].Invoke(")
                .Push(this.TranslateExpression(argsArray))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateFloatToString(Expression floatExpr)
        {
            return StringBuffer.Of("PST_FloatToString(")
                .Push(this.TranslateExpression(floatExpr))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateIntToString(Expression integer)
        {
            // NOTE: string constants will have been already converted at this point so no
            // need to worry about things like "42.ToString()"
            if (integer is InlineConstant) throw new InvalidOperationException(); // and assert as such

            return this.TranslateExpression(integer)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".ToString()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateIsValidInteger(Expression stringValue)
        {
            return StringBuffer
                .Of("PST_IsValidInteger(")
                .Push(this.TranslateExpression(stringValue))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListAdd(Expression list, Expression item)
        {
            return this.TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".Add(")
                .Push(this.TranslateExpression(item))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListConcat(Expression list, Expression items)
        {
            return StringBuffer
                .Of("PST_ListConcat(")
                .Push(this.TranslateExpression(list))
                .Push(", ")
                .Push(this.TranslateExpression(items))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListClear(Expression list)
        {
            return this.TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".Clear()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
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
                .Push(".Insert(")
                .Push(this.TranslateExpression(index))
                .Push(", ")
                .Push(this.TranslateExpression(item))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListJoinChars(Expression list)
        {
            return StringBuffer
                .Of("new string(")
                .Push(this.TranslateExpression(list))
                .Push(".ToArray())")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListJoinStrings(Expression list, Expression sep)
        {
            return StringBuffer
                .Of("string.Join(")
                .Push(this.TranslateExpression(sep))
                .Push(", ")
                .Push(this.TranslateExpression(list))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListNew(PType type)
        {
            return StringBuffer
                .Of("new List<")
                .Push(this.TypeTranspiler.TranslateType(type))
                .Push(">()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListPop(Expression list)
        {
            return StringBuffer
                .Of("PST_ListPop(")
                .Push(this.TranslateExpression(list))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListRemoveAt(Expression list, Expression index)
        {
            return this.TranslateExpression(list)
                .Push(".RemoveAt(")
                .Push(this.TranslateExpression(index))
                .Push(")");
        }

        public override StringBuffer TranslateListReverse(Expression list)
        {
            return this.TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".Reverse()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
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
            return StringBuffer
                .Of("PST_ShuffleInPlace(")
                .Push(TranslateExpression(list))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListSize(Expression list)
        {
            return this.TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".Count")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListToArray(Expression list)
        {
            return this.TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".ToArray()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathAbs(Expression num)
        {
            return StringBuffer
                .Of("System.Math.Abs(")
                .Push(this.TranslateExpression(num))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathArcCos(Expression ratio)
        {
            return StringBuffer
                .Of("System.Math.Acos(")
                .Push(this.TranslateExpression(ratio))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathArcSin(Expression ratio)
        {
            return StringBuffer
                .Of("System.Math.Asin(")
                .Push(this.TranslateExpression(ratio))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathArcTan(Expression yComponent, Expression xComponent)
        {
            return StringBuffer
                .Of("System.Math.Atan2(")
                .Push(this.TranslateExpression(yComponent))
                .Push(", ")
                .Push(this.TranslateExpression(xComponent))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathCeil(Expression num)
        {
            return StringBuffer
                .Of("(int) System.Math.Ceiling(")
                .Push(this.TranslateExpression(num))
                .Push(")")
                .WithTightness(ExpressionTightness.UNARY_PREFIX);
        }

        public override StringBuffer TranslateMathCos(Expression thetaRadians)
        {
            return StringBuffer
                .Of("System.Math.Cos(")
                .Push(this.TranslateExpression(thetaRadians))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathFloor(Expression num)
        {
            return StringBuffer
                .Of("(int) System.Math.Floor(")
                .Push(this.TranslateExpression(num))
                .Push(")")
                .WithTightness(ExpressionTightness.UNARY_PREFIX);
        }

        public override StringBuffer TranslateMathLog(Expression value)
        {
            return StringBuffer
                .Of("System.Math.Log(")
                .Push(this.TranslateExpression(value))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathPow(Expression expBase, Expression exponent)
        {
            return StringBuffer
                .Of("System.Math.Pow(")
                .Push(this.TranslateExpression(expBase))
                .Push(", ")
                .Push(this.TranslateExpression(exponent))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathSin(Expression thetaRadians)
        {
            return StringBuffer
                .Of("System.Math.Sin(")
                .Push(this.TranslateExpression(thetaRadians))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathTan(Expression thetaRadians)
        {
            return StringBuffer
                .Of("System.Math.Tan(")
                .Push(this.TranslateExpression(thetaRadians))
                .Push(")");
        }

        public override StringBuffer TranslateMultiplyList(Expression list, Expression n)
        {
            return StringBuffer
                .Of("PST_MultiplyList(")
                .Push(this.TranslateExpression(list))
                .Push(", ")
                .Push(this.TranslateExpression(n))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateNullConstant()
        {
            return StringBuffer
                .Of("null")
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateOrd(Expression charValue)
        {
            if (charValue is InlineConstant)
            {
                // this should have been optimized out.
                // throw new Exception(); // TODO: but it isn't quite yet
            }

            return this.TranslateExpression(charValue)
                .EnsureTightness(ExpressionTightness.UNARY_PREFIX)
                .Prepend("(int)")
                .WithTightness(ExpressionTightness.UNARY_PREFIX);
        }

        public override StringBuffer TranslateParseFloatUnsafe(Expression stringValue)
        {
            return StringBuffer
                .Of("double.Parse(")
                .Push(this.TranslateExpression(stringValue))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateParseInt(Expression safeStringValue)
        {
            return StringBuffer
                .Of("int.Parse(")
                .Push(this.TranslateExpression(safeStringValue))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateRandomFloat()
        {
            return StringBuffer
                .Of("PST_Random.NextDouble()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateSortedCopyOfIntArray(Expression intArray)
        {
            return this.TranslateExpression(intArray)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".OrderBy<int, int>(_PST_GEN_arg => _PST_GEN_arg).ToArray()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateSortedCopyOfStringArray(Expression stringArray)
        {
            return this.TranslateExpression(stringArray)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".OrderBy<string, string>(_PST_GEN_arg => _PST_GEN_arg).ToArray()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringAppend(Expression str1, Expression str2)
        {
            return this.TranslateExpression(str1)
                .Push(" += ")
                .Push(this.TranslateExpression(str2));
        }

        public override StringBuffer TranslateStringCharAt(Expression str, Expression index)
        {
            return this.TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("[")
                .Push(this.TranslateExpression(index))
                .Push("]")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringCharCodeAt(Expression str, Expression index)
        {
            return StringBuffer
                .Of("(int)")
                .Push(this.TranslateExpression(str).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push("[")
                .Push(this.TranslateExpression(index))
                .Push("]")
                .WithTightness(ExpressionTightness.UNARY_PREFIX);
        }

        public override StringBuffer TranslateStringCompareIsReverse(Expression str1, Expression str2)
        {
            return this.TranslateExpression(str1)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".CompareTo(")
                .Push(this.TranslateExpression(str2))
                .Push(") == 1")
                .WithTightness(ExpressionTightness.EQUALITY);
        }

        public override StringBuffer TranslateStringConcatAll(Expression[] strings)
        {
            StringBuffer buf = StringBuffer.Of("string.Join(\"\", new string[] { ");
            for (int i = 0; i < strings.Length; ++i)
            {
                if (i > 0) buf.Push(", ");
                buf.Push(this.TranslateExpression(strings[i]));
            }
            return buf
                .Push(" })")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringConcatPair(Expression strLeft, Expression strRight)
        {
            return this.TranslateExpression(strLeft)
                .EnsureTightness(ExpressionTightness.ADDITION)
                .Push(" + ")
                .Push(this.TranslateExpression(strRight).EnsureGreaterTightness(ExpressionTightness.ADDITION))
                .WithTightness(ExpressionTightness.ADDITION);
        }

        public override StringBuffer TranslateStringContains(Expression haystack, Expression needle)
        {
            return this.TranslateExpression(haystack)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".Contains(")
                .Push(this.TranslateExpression(needle))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringEndsWith(Expression haystack, Expression needle)
        {
            return this.TranslateExpression(haystack)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".EndsWith(")
                .Push(this.TranslateExpression(needle))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringEquals(Expression left, Expression right)
        {
            return this.TranslateExpression(left)
                .EnsureGreaterTightness(ExpressionTightness.EQUALITY)
                .Push(" == ")
                .Push(this.TranslateExpression(right).EnsureGreaterTightness(ExpressionTightness.EQUALITY))
                .WithTightness(ExpressionTightness.EQUALITY);
        }

        public override StringBuffer TranslateStringFromCharCode(Expression charCode)
        {
            return StringBuffer
                .Of("((char)")
                .Push(this.TranslateExpression(charCode).EnsureTightness(ExpressionTightness.UNARY_PREFIX))
                .Push(").ToString()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringIndexOf(Expression haystack, Expression needle)
        {
            return this.TranslateExpression(haystack)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".IndexOf(")
                .Push(this.TranslateExpression(needle))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringIndexOfWithStart(Expression haystack, Expression needle, Expression startIndex)
        {
            return this.TranslateExpression(haystack)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".IndexOf(")
                .Push(this.TranslateExpression(needle))
                .Push(", ")
                .Push(this.TranslateExpression(startIndex))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringLastIndexOf(Expression haystack, Expression needle)
        {
            return this.TranslateExpression(haystack)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".LastIndexOf(")
                .Push(this.TranslateExpression(needle))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringLength(Expression str)
        {
            return this.TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".Length")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringReplace(Expression haystack, Expression needle, Expression newNeedle)
        {
            return this.TranslateExpression(haystack)
                .Push(".Replace(")
                .Push(this.TranslateExpression(needle))
                .Push(", ")
                .Push(this.TranslateExpression(newNeedle))
                .Push(")");
        }

        public override StringBuffer TranslateStringReverse(Expression str)
        {
            return StringBuffer.Of("PST_StringReverse(")
                .Push(this.TranslateExpression(str))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringSplit(Expression haystack, Expression needle)
        {
            return StringBuffer.Of("PST_StringSplit(")
                .Push(this.TranslateExpression(haystack))
                .Push(", ")
                .Push(this.TranslateExpression(needle))
                .Push(")");
        }

        public override StringBuffer TranslateStringStartsWith(Expression haystack, Expression needle)
        {
            return this.TranslateExpression(haystack)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".StartsWith(")
                .Push(this.TranslateExpression(needle))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringSubstring(Expression str, Expression start, Expression length)
        {
            return this.TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".Substring(")
                .Push(this.TranslateExpression(start))
                .Push(", ")
                .Push(this.TranslateExpression(length))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringSubstringIsEqualTo(Expression haystack, Expression startIndex, Expression needle)
        {
            return StringBuffer.Of("PST_SubstringIsEqualTo(")
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
            return this.TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".ToLower()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringToUpper(Expression str)
        {
            return this.TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".ToUpper()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringToUtf8Bytes(Expression str)
        {
            return StringBuffer
                .Of("PST_stringToUtf8Bytes(")
                .Push(this.TranslateExpression(str))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringTrim(Expression str)
        {
            return this.TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".Trim()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringTrimEnd(Expression str)
        {
            return this.TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".TrimEnd()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringTrimStart(Expression str)
        {
            return this.TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".TrimStart()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringBuilderAdd(Expression sbInst, Expression obj)
        {
            return this.TranslateExpression(sbInst)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".Append(")
                .Push(this.TranslateExpression(obj))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringBuilderClear(Expression sbInst)
        {
            return this.TranslateExpression(sbInst)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".Clear()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringBuilderNew()
        {
            return StringBuffer
                .Of("new System.Text.StringBuilder()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringBuilderToString(Expression sbInst)
        {
            return this.TranslateExpression(sbInst)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".ToString()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStrongReferenceEquality(Expression left, Expression right)
        {
            return this.TranslateExpression(left)
                .EnsureGreaterTightness(ExpressionTightness.EQUALITY)
                .Push(" == ")
                .Push(this.TranslateExpression(right).EnsureGreaterTightness(ExpressionTightness.EQUALITY))
                .WithTightness(ExpressionTightness.EQUALITY);
        }

        public override StringBuffer TranslateStructFieldDereference(Expression root, StructDefinition structDef, string fieldName, int fieldIndex)
        {
            return this.TranslateExpression(root)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".")
                .Push(fieldName)
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateToCodeString(Expression str)
        {
            return StringBuffer
                .Of("PST_ToCodeString(")
                .Push(this.TranslateExpression(str))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateTryParseFloat(Expression stringValue, Expression floatOutList)
        {
            return StringBuffer.Of("PST_ParseFloat(")
                .Push(this.TranslateExpression(stringValue))
                .Push(", ")
                .Push(this.TranslateExpression(floatOutList))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateUtf8BytesToString(Expression bytes)
        {
            return StringBuffer
                .Of("System.Text.Encoding.UTF8.GetString((")
                .Push(this.TranslateExpression(bytes))
                .Push(").Select(v => (byte)v).ToArray())")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }
    }
}
