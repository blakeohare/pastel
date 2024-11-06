using Pastel.Parser.ParseNodes;
using System;
using System.Collections.Generic;

namespace Pastel.Transpilers
{
    internal class CTranspiler : CurlyBraceTranspiler
    {
        public CTranspiler() : base(false)
        { }

        public override string CanonicalTab => "    ";

        public override string HelperCodeResourcePath { get { return "Transpilers/Resources/PastelHelper.c"; } }

        public override string TranslateType(PType type)
        {
            switch (type.RootValue)
            {
                case "int":
                case "char":
                case "double":
                case "void":
                    return type.RootValue;

                case "bool":
                    return "int";

                case "string":
                    return "PString";

                case "object":
                    return "void*";

                case "StringBuilder":
                    return "PStringBuilder";

                case "Array":
                case "List":
                    switch (type.Generics[0].RootValue)
                    {
                        case "int":
                        case "bool":
                            return "PIntList*";
                        case "double":
                            return "PFloatList*";
                        default:
                            return "PPtrList*";
                    }

                case "Dictionary":
                    string keyType = type.Generics[0].RootValue;
                    string valType = type.Generics[1].RootValue;
                    throw new NotImplementedException();

                case "Func":
                    throw new NotImplementedException();

                default:
                    if (type.Generics.Length > 0)
                    {
                        throw new NotImplementedException();
                    }
                    return type.TypeName + "*";
            }
        }

        protected override void WrapCodeImpl(TranspilerContext ctx, ProjectConfig config, List<string> lines, bool isForStruct)
        {
            throw new NotImplementedException();
        }

        public override void TranslatePrintStdErr(TranspilerContext sb, Expression value)
        {
            throw new NotImplementedException();
        }

        public override void TranslatePrintStdOut(TranspilerContext sb, Expression value)
        {
            throw new NotImplementedException();
        }

        public override void TranslateArrayGet(TranspilerContext sb, Expression array, Expression index)
        {
            throw new NotImplementedException();
        }

        public override void TranslateArrayJoin(TranspilerContext sb, Expression array, Expression sep)
        {
            throw new NotImplementedException();
        }

        public override void TranslateArrayLength(TranspilerContext sb, Expression array)
        {
            throw new NotImplementedException();
        }

        public override void TranslateArrayNew(TranspilerContext sb, PType arrayType, Expression lengthExpression)
        {
            throw new NotImplementedException();
        }

        public override void TranslateArraySet(TranspilerContext sb, Expression array, Expression index, Expression value)
        {
            throw new NotImplementedException();
        }

        public override void TranslateBase64ToBytes(TranspilerContext sb, Expression base64String)
        {
            throw new NotImplementedException();
        }

        public override void TranslateBase64ToString(TranspilerContext sb, Expression base64String)
        {
            throw new NotImplementedException();
        }

        public override void TranslateCast(TranspilerContext sb, PType type, Expression expression)
        {
            throw new NotImplementedException();
        }

        public override void TranslateCharConstant(TranspilerContext sb, char value)
        {
            throw new NotImplementedException();
        }

        public override void TranslateCharToString(TranspilerContext sb, Expression charValue)
        {
            throw new NotImplementedException();
        }

        public override void TranslateChr(TranspilerContext sb, Expression charCode)
        {
            throw new NotImplementedException();
        }

        public override void TranslateCurrentTimeSeconds(TranspilerContext sb)
        {
            throw new NotImplementedException();
        }

        public override void TranslateConstructorInvocation(TranspilerContext sb, ConstructorInvocation constructorInvocation)
        {
            throw new NotImplementedException();
        }

        public override void TranslateDictionaryContainsKey(TranspilerContext sb, Expression dictionary, Expression key)
        {
            throw new NotImplementedException();
        }

        public override void TranslateDictionaryGet(TranspilerContext sb, Expression dictionary, Expression key)
        {
            throw new NotImplementedException();
        }

        public override void TranslateDictionaryKeys(TranspilerContext sb, Expression dictionary)
        {
            throw new NotImplementedException();
        }

        public override void TranslateDictionaryNew(TranspilerContext sb, PType keyType, PType valueType)
        {
            throw new NotImplementedException();
        }

        public override void TranslateDictionaryRemove(TranspilerContext sb, Expression dictionary, Expression key)
        {
            throw new NotImplementedException();
        }

        public override void TranslateDictionarySet(TranspilerContext sb, Expression dictionary, Expression key, Expression value)
        {
            throw new NotImplementedException();
        }

        public override void TranslateDictionarySize(TranspilerContext sb, Expression dictionary)
        {
            throw new NotImplementedException();
        }

        public override void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut)
        {
            throw new NotImplementedException();
        }

        public override void TranslateDictionaryValues(TranspilerContext sb, Expression dictionary)
        {
            throw new NotImplementedException();
        }

        public override void TranslateExtensibleCallbackInvoke(TranspilerContext sb, Expression name, Expression argsArray)
        {
            throw new NotImplementedException();
        }

        public override void TranslateFloatBuffer16(TranspilerContext sb)
        {
            throw new NotImplementedException();
        }

        public override void TranslateFloatDivision(TranspilerContext sb, Expression floatNumerator, Expression floatDenominator)
        {
            throw new NotImplementedException();
        }

        public override void TranslateFloatToInt(TranspilerContext sb, Expression floatExpr)
        {
            throw new NotImplementedException();
        }

        public override void TranslateFloatToString(TranspilerContext sb, Expression floatExpr)
        {
            throw new NotImplementedException();
        }

        public override void TranslateGetFunction(TranspilerContext sb, Expression name)
        {
            throw new NotImplementedException();
        }

        public override void TranslateInstanceFieldDereference(TranspilerContext sb, Expression root, ClassDefinition classDef, string fieldName)
        {
            throw new NotImplementedException();
        }

        public override void TranslateIntBuffer16(TranspilerContext sb)
        {
            throw new NotImplementedException();
        }

        public override void TranslateIntegerDivision(TranspilerContext sb, Expression integerNumerator, Expression integerDenominator)
        {
            throw new NotImplementedException();
        }

        public override void TranslateIntToString(TranspilerContext sb, Expression integer)
        {
            throw new NotImplementedException();
        }

        public override void TranslateIsValidInteger(TranspilerContext sb, Expression stringValue)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListAdd(TranspilerContext sb, Expression list, Expression item)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListConcat(TranspilerContext sb, Expression list, Expression items)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListClear(TranspilerContext sb, Expression list)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListGet(TranspilerContext sb, Expression list, Expression index)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListInsert(TranspilerContext sb, Expression list, Expression index, Expression item)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListJoinChars(TranspilerContext sb, Expression list)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListJoinStrings(TranspilerContext sb, Expression list, Expression sep)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListNew(TranspilerContext sb, PType type)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListPop(TranspilerContext sb, Expression list)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListRemoveAt(TranspilerContext sb, Expression list, Expression index)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListReverse(TranspilerContext sb, Expression list)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListSet(TranspilerContext sb, Expression list, Expression index, Expression value)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListShuffle(TranspilerContext sb, Expression list)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListSize(TranspilerContext sb, Expression list)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListToArray(TranspilerContext sb, Expression list)
        {
            throw new NotImplementedException();
        }

        public override void TranslateMathArcCos(TranspilerContext sb, Expression ratio)
        {
            throw new NotImplementedException();
        }

        public override void TranslateMathArcSin(TranspilerContext sb, Expression ratio)
        {
            throw new NotImplementedException();
        }

        public override void TranslateMathArcTan(TranspilerContext sb, Expression yComponent, Expression xComponent)
        {
            throw new NotImplementedException();
        }

        public override void TranslateMathCos(TranspilerContext sb, Expression thetaRadians)
        {
            throw new NotImplementedException();
        }

        public override void TranslateMathLog(TranspilerContext sb, Expression value)
        {
            throw new NotImplementedException();
        }

        public override void TranslateMathPow(TranspilerContext sb, Expression expBase, Expression exponent)
        {
            throw new NotImplementedException();
        }

        public override void TranslateMathSin(TranspilerContext sb, Expression thetaRadians)
        {
            throw new NotImplementedException();
        }

        public override void TranslateMathTan(TranspilerContext sb, Expression thetaRadians)
        {
            throw new NotImplementedException();
        }

        public override void TranslateMultiplyList(TranspilerContext sb, Expression list, Expression n)
        {
            throw new NotImplementedException();
        }

        public override void TranslateNullConstant(TranspilerContext sb)
        {
            throw new NotImplementedException();
        }

        public override void TranslateOrd(TranspilerContext sb, Expression charValue)
        {
            throw new NotImplementedException();
        }

        public override void TranslateParseFloatUnsafe(TranspilerContext sb, Expression stringValue)
        {
            throw new NotImplementedException();
        }

        public override void TranslateParseInt(TranspilerContext sb, Expression safeStringValue)
        {
            throw new NotImplementedException();
        }

        public override void TranslateRandomFloat(TranspilerContext sb)
        {
            throw new NotImplementedException();
        }

        public override void TranslateSortedCopyOfIntArray(TranspilerContext sb, Expression intArray)
        {
            throw new NotImplementedException();
        }

        public override void TranslateSortedCopyOfStringArray(TranspilerContext sb, Expression stringArray)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringAppend(TranspilerContext sb, Expression str1, Expression str2)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringBuffer16(TranspilerContext sb)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringCharAt(TranspilerContext sb, Expression str, Expression index)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringCharCodeAt(TranspilerContext sb, Expression str, Expression index)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringCompareIsReverse(TranspilerContext sb, Expression str1, Expression str2)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringConcatAll(TranspilerContext sb, Expression[] strings)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringConcatPair(TranspilerContext sb, Expression strLeft, Expression strRight)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringContains(TranspilerContext sb, Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringEndsWith(TranspilerContext sb, Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringEquals(TranspilerContext sb, Expression left, Expression right)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringFromCharCode(TranspilerContext sb, Expression charCode)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringIndexOf(TranspilerContext sb, Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringIndexOfWithStart(TranspilerContext sb, Expression haystack, Expression needle, Expression startIndex)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringLastIndexOf(TranspilerContext sb, Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringLength(TranspilerContext sb, Expression str)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringReplace(TranspilerContext sb, Expression haystack, Expression needle, Expression newNeedle)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringReverse(TranspilerContext sb, Expression str)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringSplit(TranspilerContext sb, Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringStartsWith(TranspilerContext sb, Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringSubstring(TranspilerContext sb, Expression str, Expression start, Expression length)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringSubstringIsEqualTo(TranspilerContext sb, Expression haystack, Expression startIndex, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringToLower(TranspilerContext sb, Expression str)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringToUpper(TranspilerContext sb, Expression str)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringToUtf8Bytes(TranspilerContext sb, Expression str)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringTrim(TranspilerContext sb, Expression str)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringTrimEnd(TranspilerContext sb, Expression str)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringTrimStart(TranspilerContext sb, Expression str)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringBuilderAdd(TranspilerContext sb, Expression sbInst, Expression obj)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringBuilderClear(TranspilerContext sb, Expression sbInst)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringBuilderNew(TranspilerContext sb)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringBuilderToString(TranspilerContext sb, Expression sbInst)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStrongReferenceEquality(TranspilerContext sb, Expression left, Expression right)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStructFieldDereference(TranspilerContext sb, Expression root, StructDefinition structDef, string fieldName, int fieldIndex)
        {
            throw new NotImplementedException();
        }

        public override void TranslateThis(TranspilerContext sb, ThisExpression thisExpr)
        {
            throw new NotImplementedException();
        }

        public override void TranslateToCodeString(TranspilerContext sb, Expression str)
        {
            throw new NotImplementedException();
        }

        public override void TranslateTryParseFloat(TranspilerContext sb, Expression stringValue, Expression floatOutList)
        {
            throw new NotImplementedException();
        }

        public override void TranslateUtf8BytesToString(TranspilerContext sb, Expression bytes)
        {
            throw new NotImplementedException();
        }

        public override void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl)
        {
            throw new NotImplementedException();
        }

        public override void GenerateCodeForClass(TranspilerContext sb, ClassDefinition classDef)
        {
            throw new NotImplementedException();
        }

        public override void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef)
        {
            throw new NotImplementedException();
        }

        public override void GenerateCodeForFunction(TranspilerContext output, FunctionDefinition funcDef, bool isStatic)
        {
            throw new NotImplementedException();
        }
    }
}
