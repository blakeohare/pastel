﻿using Pastel.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Transpilers
{
    internal class CSharpTranspiler : CurlyBraceTranspiler
    {
        public CSharpTranspiler() : base("    ", "\r\n", false)
        { }

        public override string HelperCodeResourcePath { get { return "Transpilers/Resources/PastelHelper.cs"; } }

        public override string TranslateType(PType type)
        {
            switch (type.RootValue)
            {
                case "int":
                case "char":
                case "bool":
                case "double":
                case "string":
                case "object":
                case "void":
                    return type.RootValue;

                case "StringBuilder":
                    return "System.Text.StringBuilder";

                case "List":
                    return "System.Collections.Generic.List<" + this.TranslateType(type.Generics[0]) + ">";

                case "Dictionary":
                    return "System.Collections.Generic.Dictionary<" + this.TranslateType(type.Generics[0]) + ", " + this.TranslateType(type.Generics[1]) + ">";

                case "Array":
                    return this.TranslateType(type.Generics[0]) + "[]";

                case "Func":
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append("System.Func<");
                    for (int i = 0; i < type.Generics.Length - 1; ++i)
                    {
                        sb.Append(this.TranslateType(type.Generics[i + 1]));
                        sb.Append(", ");
                    }
                    sb.Append(this.TranslateType(type.Generics[0]));
                    sb.Append('>');
                    return sb.ToString();

                default:
                    if (type.Generics.Length > 0)
                    {
                        throw new NotImplementedException();
                    }
                    return type.TypeName;
            }
        }

        protected override void WrapCodeImpl(TranspilerContext ctx, ProjectConfig config, List<string> lines, bool isForStruct)
        {
            if (!isForStruct)
            {
                lines.InsertRange(0, new string[] {
                    "#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.",
                    "#pragma warning disable CS8602 // Dereference of a possibly null reference.",
                    "#pragma warning disable CS8603 // Possible null reference return.",
                    "#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.",
                });
            }

            if (!isForStruct && config.WrappingClassNameForFunctions != null)
            {
                PastelUtil.IndentLines(this.TabChar, lines);
                lines.InsertRange(0, new string[] { "public static class " + config.WrappingClassNameForFunctions, "{" });
                lines.Add("}");
            }

            string nsValue = isForStruct ? config.NamespaceForStructs : config.NamespaceForFunctions;
            if (nsValue != null)
            {
                PastelUtil.IndentLines(this.TabChar, lines);
                lines.InsertRange(0, new string[] { "namespace " + nsValue, "{" });
                lines.Add("}");
            }

            HashSet<string> importSet = new HashSet<string>(config.Imports);
            if (!isForStruct) importSet.Add("System.Linq");
            string[] imports = importSet.OrderBy(t => t).ToArray();
            if (imports.Length > 0)
            {
                lines.InsertRange(0,
                    imports
                        .Select(t => "using " + t + ";")
                        .Concat(new string[] { "" }));
            }
        }

        public override void TranslatePrintStdErr(TranspilerContext sb, Expression value)
        {
            sb.Append("PlatformTranslationHelper.PrintStdErr(");
            this.TranslateExpression(sb, value);
            sb.Append(')');
        }

        public override void TranslatePrintStdOut(TranspilerContext sb, Expression value)
        {
            sb.Append("PlatformTranslationHelper.PrintStdOut(");
            this.TranslateExpression(sb, value);
            sb.Append(')');
        }

        public override void TranslateArrayGet(TranspilerContext sb, Expression array, Expression index)
        {
            this.TranslateExpression(sb, array);
            sb.Append('[');
            this.TranslateExpression(sb, index);
            sb.Append(']');
        }

        public override void TranslateArrayJoin(TranspilerContext sb, Expression array, Expression sep)
        {
            sb.Append("string.Join(");
            this.TranslateExpression(sb, sep);
            sb.Append(", ");
            this.TranslateExpression(sb, array);
            sb.Append(')');
        }

        public override void TranslateArrayLength(TranspilerContext sb, Expression array)
        {
            this.TranslateExpression(sb, array);
            sb.Append(".Length");
        }

        public override void TranslateArrayNew(TranspilerContext sb, PType arrayType, Expression lengthExpression)
        {
            int nestingLevel = 0;
            while (arrayType.RootValue == "Array")
            {
                nestingLevel++;
                arrayType = arrayType.Generics[0];
            }
            sb.Append("new ");
            sb.Append(this.TranslateType(arrayType));
            sb.Append('[');
            this.TranslateExpression(sb, lengthExpression);
            sb.Append(']');
            while (nestingLevel-- > 0)
            {
                sb.Append("[]");
            }
        }

        public override void TranslateArraySet(TranspilerContext sb, Expression array, Expression index, Expression value)
        {
            this.TranslateExpression(sb, array);
            sb.Append('[');
            this.TranslateExpression(sb, index);
            sb.Append("] = ");
            this.TranslateExpression(sb, value);
        }

        public override void TranslateBase64ToBytes(TranspilerContext sb, Expression base64String)
        {
            sb.Append("System.Convert.FromBase64String(");
            this.TranslateExpression(sb, base64String);
            sb.Append(").Cast<int>().ToArray()");
        }

        public override void TranslateBase64ToString(TranspilerContext sb, Expression base64String)
        {
            sb.Append("PST_Base64ToString(");
            this.TranslateExpression(sb, base64String);
            sb.Append(')');
        }

        public override void TranslateCast(TranspilerContext sb, PType type, Expression expression)
        {
            sb.Append('(');
            sb.Append(this.TranslateType(type));
            sb.Append(')');
            this.TranslateExpression(sb, expression);
        }

        public override void TranslateCharConstant(TranspilerContext sb, char value)
        {
            sb.Append(PastelUtil.ConvertCharToCharConstantCode(value));
        }

        public override void TranslateCharToString(TranspilerContext sb, Expression charValue)
        {
            this.TranslateExpression(sb, charValue);
            sb.Append(".ToString()");
        }

        public override void TranslateChr(TranspilerContext sb, Expression charCode)
        {
            sb.Append("((char) ");
            this.TranslateExpression(sb, charCode);
            sb.Append(")");
        }

        public override void TranslateCurrentTimeSeconds(TranspilerContext sb)
        {
            sb.Append("PST_CurrentTime");
        }

        public override void TranslateConstructorInvocation(TranspilerContext sb, ConstructorInvocation constructorInvocation)
        {
            sb.Append("new ");
            sb.Append(this.TranslateType(constructorInvocation.Type));
            sb.Append('(');
            Expression[] args = constructorInvocation.Args;
            for (int i = 0; i < args.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                this.TranslateExpression(sb, args[i]);
            }
            sb.Append(')');
        }

        public override void TranslateDictionaryContainsKey(TranspilerContext sb, Expression dictionary, Expression key)
        {
            this.TranslateExpression(sb, dictionary);
            sb.Append(".ContainsKey(");
            this.TranslateExpression(sb, key);
            sb.Append(")");
        }

        public override void TranslateDictionaryGet(TranspilerContext sb, Expression dictionary, Expression key)
        {
            this.TranslateExpression(sb, dictionary);
            sb.Append('[');
            this.TranslateExpression(sb, key);
            sb.Append(']');
        }

        public override void TranslateDictionaryKeys(TranspilerContext sb, Expression dictionary)
        {
            this.TranslateExpression(sb, dictionary);
            sb.Append(".Keys.ToArray()");
        }

        public override void TranslateDictionaryNew(TranspilerContext sb, PType keyType, PType valueType)
        {
            sb.Append("new Dictionary<");
            sb.Append(this.TranslateType(keyType));
            sb.Append(", ");
            sb.Append(this.TranslateType(valueType));
            sb.Append(">()");
        }

        public override void TranslateDictionaryRemove(TranspilerContext sb, Expression dictionary, Expression key)
        {
            this.TranslateExpression(sb, dictionary);
            sb.Append(".Remove(");
            this.TranslateExpression(sb, key);
            sb.Append(')');
        }

        public override void TranslateDictionarySet(TranspilerContext sb, Expression dictionary, Expression key, Expression value)
        {
            this.TranslateExpression(sb, dictionary);
            sb.Append('[');
            this.TranslateExpression(sb, key);
            sb.Append("] = ");
            this.TranslateExpression(sb, value);
        }

        public override void TranslateDictionarySize(TranspilerContext sb, Expression dictionary)
        {
            this.TranslateExpression(sb, dictionary);
            sb.Append(".Count");
        }

        public override void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("if (!");
            this.TranslateExpression(sb, dictionary);
            sb.Append(".TryGetValue(");
            this.TranslateExpression(sb, key);
            sb.Append(", out ");
            sb.Append(varOut.Name);
            sb.Append(")) ");
            sb.Append(varOut.Name);
            sb.Append(" = ");
            this.TranslateExpression(sb, fallbackValue);
            sb.Append(";");
            sb.Append(this.NewLine);
        }

        public override void TranslateDictionaryValues(TranspilerContext sb, Expression dictionary)
        {
            this.TranslateExpression(sb, dictionary);
            sb.Append(".Values.ToArray()");
        }

        public override void TranslateExtensibleCallbackInvoke(TranspilerContext sb, Expression name, Expression argsArray)
        {
            sb.Append("(PST_ExtCallbacks.ContainsKey(");
            this.TranslateExpression(sb, name);
            sb.Append(") ? PST_ExtCallbacks[");
            this.TranslateExpression(sb, name);
            sb.Append("].Invoke(");
            this.TranslateExpression(sb, argsArray);
            sb.Append(") : null)");
        }

        public override void TranslateFloatBuffer16(TranspilerContext sb)
        {
            sb.Append("PST_FloatBuffer16");
        }

        public override void TranslateFloatDivision(TranspilerContext sb, Expression floatNumerator, Expression floatDenominator)
        {
            sb.Append("(");
            this.TranslateExpression(sb, floatNumerator);
            sb.Append(") / (");
            this.TranslateExpression(sb, floatDenominator);
            sb.Append(')');
        }

        public override void TranslateFloatToInt(TranspilerContext sb, Expression floatExpr)
        {
            sb.Append("(int)");
            this.TranslateExpression(sb, floatExpr);
        }

        public override void TranslateFloatToString(TranspilerContext sb, Expression floatExpr)
        {
            sb.Append("PST_FloatToString(");
            this.TranslateExpression(sb, floatExpr);
            sb.Append(')');
        }

        public override void TranslateGetFunction(TranspilerContext sb, Expression name)
        {
            sb.Append("TranslationHelper.GetFunctionPointer(");
            this.TranslateExpression(sb, name);
            sb.Append(')');
        }

        public override void TranslateInstanceFieldDereference(TranspilerContext sb, Expression root, ClassDefinition classDef, string fieldName)
        {
            this.TranslateExpression(sb, root);
            sb.Append('.');
            sb.Append(fieldName);
        }

        public override void TranslateIntBuffer16(TranspilerContext sb)
        {
            sb.Append("PST_IntBuffer16");
        }

        public override void TranslateIntegerDivision(TranspilerContext sb, Expression integerNumerator, Expression integerDenominator)
        {
            sb.Append("(");
            this.TranslateExpression(sb, integerNumerator);
            sb.Append(") / (");
            this.TranslateExpression(sb, integerDenominator);
            sb.Append(')');
        }

        public override void TranslateIntToString(TranspilerContext sb, Expression integer)
        {
            sb.Append("(");
            this.TranslateExpression(sb, integer);
            sb.Append(").ToString()");
        }

        public override void TranslateIsValidInteger(TranspilerContext sb, Expression stringValue)
        {
            sb.Append("PST_IsValidInteger(");
            this.TranslateExpression(sb, stringValue);
            sb.Append(')');
        }

        public override void TranslateListAdd(TranspilerContext sb, Expression list, Expression item)
        {
            this.TranslateExpression(sb, list);
            sb.Append(".Add(");
            this.TranslateExpression(sb, item);
            sb.Append(')');
        }

        public override void TranslateListConcat(TranspilerContext sb, Expression list, Expression items)
        {
            sb.Append("PST_ListConcat(");
            this.TranslateExpression(sb, list);
            sb.Append(", ");
            this.TranslateExpression(sb, items);
            sb.Append(")");
        }

        public override void TranslateListClear(TranspilerContext sb, Expression list)
        {
            this.TranslateExpression(sb, list);
            sb.Append(".Clear()");
        }

        public override void TranslateListGet(TranspilerContext sb, Expression list, Expression index)
        {
            this.TranslateExpression(sb, list);
            sb.Append('[');
            this.TranslateExpression(sb, index);
            sb.Append(']');
        }

        public override void TranslateListInsert(TranspilerContext sb, Expression list, Expression index, Expression item)
        {
            this.TranslateExpression(sb, list);
            sb.Append(".Insert(");
            this.TranslateExpression(sb, index);
            sb.Append(", ");
            this.TranslateExpression(sb, item);
            sb.Append(')');
        }

        public override void TranslateListJoinChars(TranspilerContext sb, Expression list)
        {
            sb.Append("new string(");
            this.TranslateExpression(sb, list);
            sb.Append(".ToArray())");
        }

        public override void TranslateListJoinStrings(TranspilerContext sb, Expression list, Expression sep)
        {
            sb.Append("string.Join(");
            this.TranslateExpression(sb, sep);
            sb.Append(", ");
            this.TranslateExpression(sb, list);
            sb.Append(')');
        }

        public override void TranslateListNew(TranspilerContext sb, PType type)
        {
            sb.Append("new List<");
            sb.Append(this.TranslateType(type));
            sb.Append(">()");
        }

        public override void TranslateListPop(TranspilerContext sb, Expression list)
        {
            sb.Append("PST_ListPop(");
            this.TranslateExpression(sb, list);
            sb.Append(')');
        }

        public override void TranslateListRemoveAt(TranspilerContext sb, Expression list, Expression index)
        {
            this.TranslateExpression(sb, list);
            sb.Append(".RemoveAt(");
            this.TranslateExpression(sb, index);
            sb.Append(')');
        }

        public override void TranslateListReverse(TranspilerContext sb, Expression list)
        {
            this.TranslateExpression(sb, list);
            sb.Append(".Reverse()");
        }

        public override void TranslateListSet(TranspilerContext sb, Expression list, Expression index, Expression value)
        {
            this.TranslateExpression(sb, list);
            sb.Append('[');
            this.TranslateExpression(sb, index);
            sb.Append("] = ");
            this.TranslateExpression(sb, value);
        }

        public override void TranslateListShuffle(TranspilerContext sb, Expression list)
        {
            sb.Append("PST_ShuffleInPlace(");
            this.TranslateExpression(sb, list);
            sb.Append(')');
        }

        public override void TranslateListSize(TranspilerContext sb, Expression list)
        {
            this.TranslateExpression(sb, list);
            sb.Append(".Count");
        }

        public override void TranslateListToArray(TranspilerContext sb, Expression list)
        {
            this.TranslateExpression(sb, list);
            sb.Append(".ToArray()");
        }

        public override void TranslateMathArcCos(TranspilerContext sb, Expression ratio)
        {
            sb.Append("System.Math.Acos(");
            this.TranslateExpression(sb, ratio);
            sb.Append(')');
        }

        public override void TranslateMathArcSin(TranspilerContext sb, Expression ratio)
        {
            sb.Append("System.Math.Asin(");
            this.TranslateExpression(sb, ratio);
            sb.Append(')');
        }

        public override void TranslateMathArcTan(TranspilerContext sb, Expression yComponent, Expression xComponent)
        {
            sb.Append("System.Math.Atan2(");
            this.TranslateExpression(sb, yComponent);
            sb.Append(", ");
            this.TranslateExpression(sb, xComponent);
            sb.Append(')');
        }

        public override void TranslateMathCos(TranspilerContext sb, Expression thetaRadians)
        {
            sb.Append("System.Math.Cos(");
            this.TranslateExpression(sb, thetaRadians);
            sb.Append(')');
        }

        public override void TranslateMathLog(TranspilerContext sb, Expression value)
        {
            sb.Append("System.Math.Log(");
            this.TranslateExpression(sb, value);
            sb.Append(')');
        }

        public override void TranslateMathPow(TranspilerContext sb, Expression expBase, Expression exponent)
        {
            sb.Append("System.Math.Pow(");
            this.TranslateExpression(sb, expBase);
            sb.Append(", ");
            this.TranslateExpression(sb, exponent);
            sb.Append(")");
        }

        public override void TranslateMathSin(TranspilerContext sb, Expression thetaRadians)
        {
            sb.Append("System.Math.Sin(");
            this.TranslateExpression(sb, thetaRadians);
            sb.Append(')');
        }

        public override void TranslateMathTan(TranspilerContext sb, Expression thetaRadians)
        {
            sb.Append("System.Math.Tan(");
            this.TranslateExpression(sb, thetaRadians);
            sb.Append(')');
        }

        public override void TranslateMultiplyList(TranspilerContext sb, Expression list, Expression n)
        {
            sb.Append("PST_MultiplyList(");
            this.TranslateExpression(sb, list);
            sb.Append(", ");
            this.TranslateExpression(sb, n);
            sb.Append(")");
        }

        public override void TranslateNullConstant(TranspilerContext sb)
        {
            sb.Append("null");
        }

        public override void TranslateOrd(TranspilerContext sb, Expression charValue)
        {
            if (charValue is InlineConstant)
            {
                // this should have been optimized out.
                // throw new Exception(); // TODO: but it isn't quite ye
            }
            sb.Append("((int)(");
            this.TranslateExpression(sb, charValue);
            sb.Append("))");
        }

        public override void TranslateParseFloatUnsafe(TranspilerContext sb, Expression stringValue)
        {
            sb.Append("double.Parse(");
            this.TranslateExpression(sb, stringValue);
            sb.Append(')');
        }

        public override void TranslateParseInt(TranspilerContext sb, Expression safeStringValue)
        {
            sb.Append("int.Parse(");
            this.TranslateExpression(sb, safeStringValue);
            sb.Append(')');
        }

        public override void TranslateRandomFloat(TranspilerContext sb)
        {
            sb.Append("PST_Random.NextDouble()");
        }

        public override void TranslateSortedCopyOfIntArray(TranspilerContext sb, Expression intArray)
        {
            this.TranslateExpression(sb, intArray);
            sb.Append(".OrderBy<int, int>(_PST_GEN_arg => _PST_GEN_arg).ToArray()");
        }

        public override void TranslateSortedCopyOfStringArray(TranspilerContext sb, Expression stringArray)
        {
            this.TranslateExpression(sb, stringArray);
            sb.Append(".OrderBy<string, string>(_PST_GEN_arg => _PST_GEN_arg).ToArray()");
        }

        public override void TranslateStringAppend(TranspilerContext sb, Expression str1, Expression str2)
        {
            this.TranslateExpression(sb, str1);
            sb.Append(" += ");
            this.TranslateExpression(sb, str2);
        }

        public override void TranslateStringBuffer16(TranspilerContext sb)
        {
            sb.Append("PST_StringBuffer16");
        }

        public override void TranslateStringCharAt(TranspilerContext sb, Expression str, Expression index)
        {
            this.TranslateExpression(sb, str);
            sb.Append('[');
            this.TranslateExpression(sb, index);
            sb.Append(']');
        }

        public override void TranslateStringCharCodeAt(TranspilerContext sb, Expression str, Expression index)
        {
            sb.Append("((int) ");
            this.TranslateExpression(sb, str);
            sb.Append('[');
            this.TranslateExpression(sb, index);
            sb.Append("])");
        }

        public override void TranslateStringCompareIsReverse(TranspilerContext sb, Expression str1, Expression str2)
        {
            sb.Append('(');
            this.TranslateExpression(sb, str1);
            sb.Append(".CompareTo(");
            this.TranslateExpression(sb, str2);
            sb.Append(") == 1)");
        }

        public override void TranslateStringConcatAll(TranspilerContext sb, Expression[] strings)
        {
            sb.Append("string.Join(\"\", new string[] { ");
            for (int i = 0; i < strings.Length; ++i)
            {
                if (i > 0) sb.Append(',');
                this.TranslateExpression(sb, strings[i]);
            }
            sb.Append(" })");
        }

        public override void TranslateStringConcatPair(TranspilerContext sb, Expression strLeft, Expression strRight)
        {
            this.TranslateExpression(sb, strLeft);
            sb.Append(" + ");
            this.TranslateExpression(sb, strRight);
        }

        public override void TranslateStringContains(TranspilerContext sb, Expression haystack, Expression needle)
        {
            this.TranslateExpression(sb, haystack);
            sb.Append(".Contains(");
            this.TranslateExpression(sb, needle);
            sb.Append(')');
        }

        public override void TranslateStringEndsWith(TranspilerContext sb, Expression haystack, Expression needle)
        {
            this.TranslateExpression(sb, haystack);
            sb.Append(".EndsWith(");
            this.TranslateExpression(sb, needle);
            sb.Append(')');
        }

        public override void TranslateStringEquals(TranspilerContext sb, Expression left, Expression right)
        {
            this.TranslateExpression(sb, left);
            sb.Append(" == ");
            this.TranslateExpression(sb, right);
        }

        public override void TranslateStringFromCharCode(TranspilerContext sb, Expression charCode)
        {
            sb.Append("((char) ");
            this.TranslateExpression(sb, charCode);
            sb.Append(").ToString()");
        }

        public override void TranslateStringIndexOf(TranspilerContext sb, Expression haystack, Expression needle)
        {
            this.TranslateExpression(sb, haystack);
            sb.Append(".IndexOf(");
            this.TranslateExpression(sb, needle);
            sb.Append(')');
        }

        public override void TranslateStringIndexOfWithStart(TranspilerContext sb, Expression haystack, Expression needle, Expression startIndex)
        {
            this.TranslateExpression(sb, haystack);
            sb.Append(".IndexOf(");
            this.TranslateExpression(sb, needle);
            sb.Append(", ");
            this.TranslateExpression(sb, startIndex);
            sb.Append(')');
        }

        public override void TranslateStringLastIndexOf(TranspilerContext sb, Expression haystack, Expression needle)
        {
            this.TranslateExpression(sb, haystack);
            sb.Append(".LastIndexOf(");
            this.TranslateExpression(sb, needle);
            sb.Append(')');
        }

        public override void TranslateStringLength(TranspilerContext sb, Expression str)
        {
            this.TranslateExpression(sb, str);
            sb.Append(".Length");
        }

        public override void TranslateStringReplace(TranspilerContext sb, Expression haystack, Expression needle, Expression newNeedle)
        {
            this.TranslateExpression(sb, haystack);
            sb.Append(".Replace(");
            this.TranslateExpression(sb, needle);
            sb.Append(", ");
            this.TranslateExpression(sb, newNeedle);
            sb.Append(")");
        }

        public override void TranslateStringReverse(TranspilerContext sb, Expression str)
        {
            sb.Append("PST_StringReverse(");
            this.TranslateExpression(sb, str);
            sb.Append(')');
        }

        public override void TranslateStringSplit(TranspilerContext sb, Expression haystack, Expression needle)
        {
            sb.Append("PST_StringSplit(");
            this.TranslateExpression(sb, haystack);
            sb.Append(", ");
            this.TranslateExpression(sb, needle);
            sb.Append(')');
        }

        public override void TranslateStringStartsWith(TranspilerContext sb, Expression haystack, Expression needle)
        {
            this.TranslateExpression(sb, haystack);
            sb.Append(".StartsWith(");
            this.TranslateExpression(sb, needle);
            sb.Append(')');
        }

        public override void TranslateStringSubstring(TranspilerContext sb, Expression str, Expression start, Expression length)
        {
            this.TranslateExpression(sb, str);
            sb.Append(".Substring(");
            this.TranslateExpression(sb, start);
            sb.Append(", ");
            this.TranslateExpression(sb, length);
            sb.Append(')');
        }

        public override void TranslateStringSubstringIsEqualTo(TranspilerContext sb, Expression haystack, Expression startIndex, Expression needle)
        {
            sb.Append("PST_SubstringIsEqualTo(");
            this.TranslateExpression(sb, haystack);
            sb.Append(", ");
            this.TranslateExpression(sb, startIndex);
            sb.Append(", ");
            this.TranslateExpression(sb, needle);
            sb.Append(')');
        }

        public override void TranslateStringToLower(TranspilerContext sb, Expression str)
        {
            this.TranslateExpression(sb, str);
            sb.Append(".ToLower()");
        }

        public override void TranslateStringToUpper(TranspilerContext sb, Expression str)
        {
            this.TranslateExpression(sb, str);
            sb.Append(".ToUpper()");
        }

        public override void TranslateStringToUtf8Bytes(TranspilerContext sb, Expression str)
        {
            sb.Append("PST_stringToUtf8Bytes(");
            this.TranslateExpression(sb, str);
            sb.Append(')');
        }

        public override void TranslateStringTrim(TranspilerContext sb, Expression str)
        {
            this.TranslateExpression(sb, str);
            sb.Append(".Trim()");
        }

        public override void TranslateStringTrimEnd(TranspilerContext sb, Expression str)
        {
            this.TranslateExpression(sb, str);
            sb.Append(".TrimEnd()");
        }

        public override void TranslateStringTrimStart(TranspilerContext sb, Expression str)
        {
            this.TranslateExpression(sb, str);
            sb.Append(".TrimStart()");
        }

        public override void TranslateStringBuilderAdd(TranspilerContext sb, Expression sbInst, Expression obj)
        {
            this.TranslateExpression(sb, sbInst);
            sb.Append(".Append(");
            this.TranslateExpression(sb, obj);
            sb.Append(')');
        }

        public override void TranslateStringBuilderClear(TranspilerContext sb, Expression sbInst)
        {
            this.TranslateExpression(sb, sbInst);
            sb.Append(".Clear()");
        }

        public override void TranslateStringBuilderNew(TranspilerContext sb)
        {
            sb.Append("new System.Text.StringBuilder()");
        }

        public override void TranslateStringBuilderToString(TranspilerContext sb, Expression sbInst)
        {
            this.TranslateExpression(sb, sbInst);
            sb.Append(".ToString()");
        }

        public override void TranslateStrongReferenceEquality(TranspilerContext sb, Expression left, Expression right)
        {
            this.TranslateExpression(sb, left);
            sb.Append(" == ");
            this.TranslateExpression(sb, right);
        }

        public override void TranslateStructFieldDereference(TranspilerContext sb, Expression root, StructDefinition structDef, string fieldName, int fieldIndex)
        {
            this.TranslateExpression(sb, root);
            sb.Append('.');
            sb.Append(fieldName);
        }

        public override void TranslateThis(TranspilerContext sb, ThisExpression thisExpr)
        {
            sb.Append("this");
        }

        public override void TranslateToCodeString(TranspilerContext sb, Expression str)
        {
            sb.Append("PST_ToCodeString(");
            this.TranslateExpression(sb, str);
            sb.Append(')');
        }

        public override void TranslateTryParseFloat(TranspilerContext sb, Expression stringValue, Expression floatOutList)
        {
            sb.Append("PST_ParseFloat(");
            this.TranslateExpression(sb, stringValue);
            sb.Append(", ");
            this.TranslateExpression(sb, floatOutList);
            sb.Append(')');
        }

        public override void TranslateUtf8BytesToString(TranspilerContext sb, Expression bytes)
        {
            sb.Append("System.Text.Encoding.UTF8.GetString((");
            this.TranslateExpression(sb, bytes);
            sb.Append(").Select(v => (byte)v).ToArray())");
        }

        public override void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(this.TranslateType(varDecl.Type));
            sb.Append(' ');
            sb.Append(varDecl.VariableNameToken.Value);
            if (varDecl.Value != null)
            {
                sb.Append(" = ");
                this.TranslateExpression(sb, varDecl.Value);
            }
            sb.Append(';');
            sb.Append(this.NewLine);
        }

        public override void GenerateCodeForClass(TranspilerContext sb, ClassDefinition classDef)
        {
            string name = classDef.NameToken.Value;

            sb.Append("public class " + name);
            if (classDef.ParentClass != null)
            {
                sb.Append(" : " + classDef.ParentClass.NameToken.Value);
            }
            sb.Append(this.NewLine);

            sb.Append("{");
            sb.Append(this.NewLine);
            sb.TabDepth++;
            foreach (FieldDefinition fd in classDef.Fields)
            {
                System.Text.StringBuilder line = new System.Text.StringBuilder();
                sb.Append(sb.CurrentTab);
                sb.Append("public ");
                sb.Append(this.TranslateType(fd.FieldType));
                sb.Append(' ');
                sb.Append(fd.NameToken.Value);
                sb.Append(" = ");
                this.TranslateExpression(sb, fd.Value);
                sb.Append(";");
                sb.Append(this.NewLine);
                sb.Append(this.NewLine);
            }

            ConstructorDefinition constructorDef = classDef.Constructor;
            sb.Append("    public ");
            sb.Append(name);
            sb.Append('(');
            for (int i = 0; i < constructorDef.ArgNames.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(this.TranslateType(constructorDef.ArgTypes[i]));
                sb.Append(' ');
                sb.Append(constructorDef.ArgNames[i].Value);
            }
            sb.Append(")");
            sb.Append(this.NewLine);
            sb.Append(sb.CurrentTab);
            sb.Append("{");
            sb.Append(this.NewLine);
            sb.TabDepth++;
            this.TranslateExecutables(sb, constructorDef.Code);
            sb.TabDepth--;
            sb.Append(sb.CurrentTab);
            sb.Append("}");
            sb.Append(this.NewLine);
            sb.Append(this.NewLine);

            foreach (FunctionDefinition fd in classDef.Methods)
            {
                this.GenerateCodeForFunction(sb, fd, false);
                sb.Append(this.NewLine);
            }

            sb.TabDepth--;
            sb.Append(this.NewLine);
            sb.Append("}");
            sb.Append(this.NewLine);
        }

        public override void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef)
        {
            PType[] localTypes = structDef.LocalFieldTypes;
            Token[] localNames = structDef.LocalFieldNames;
            PType[] flatTypes = structDef.FlatFieldTypes;
            Token[] flatNames = structDef.FlatFieldNames;

            string name = structDef.NameToken.Value;
            List<string> lines = new List<string>();

            string defline = "public class " + name;
            if (structDef.Parent != null)
            {
                defline += " : " + structDef.ParentName.Value;
            }
            lines.Add(defline);
            lines.Add("{");
            for (int i = 0; i < localNames.Length; ++i)
            {
                lines.Add("    public " + this.TranslateType(localTypes[i]) + " " + localNames[i].Value + ";");
            }
            lines.Add("");

            System.Text.StringBuilder constructorDeclaration = new System.Text.StringBuilder();
            constructorDeclaration.Append("    public ");
            constructorDeclaration.Append(name);
            constructorDeclaration.Append('(');
            for (int i = 0; i < flatTypes.Length; ++i)
            {
                if (i > 0) constructorDeclaration.Append(", ");
                constructorDeclaration.Append(this.TranslateType(flatTypes[i]));
                constructorDeclaration.Append(' ');
                constructorDeclaration.Append(flatNames[i].Value);
            }
            constructorDeclaration.Append(')');

            if (structDef.Parent != null)
            {
                Token[] parentFieldNames = structDef.Parent.FlatFieldNames;
                constructorDeclaration.Append(" : base(");
                for (int i = 0; i < parentFieldNames.Length; ++i)
                {
                    if (i > 0) constructorDeclaration.Append(", ");
                    constructorDeclaration.Append(flatNames[i].Value);
                }
                constructorDeclaration.Append(')');
            }

            lines.Add(constructorDeclaration.ToString());
            lines.Add("    {");
            for (int i = 0; i < localTypes.Length; ++i)
            {
                string fieldName = localNames[i].Value;
                lines.Add("        this." + fieldName + " = " + fieldName + ";");
            }
            lines.Add("    }");

            lines.Add("}");
            lines.Add("");

            // TODO: rewrite this function to use the string builder inline and use this.NL
            sb.Append(string.Join("\r\n", lines));
        }

        public override void GenerateCodeForFunction(TranspilerContext output, FunctionDefinition funcDef, bool isStatic)
        {
            PType returnType = funcDef.ReturnType;
            string funcName = funcDef.NameToken.Value;
            PType[] argTypes = funcDef.ArgTypes;
            Token[] argNames = funcDef.ArgNames;

            output.Append(output.CurrentTab);
            output.Append("public ");
            if (isStatic) output.Append("static ");
            output.Append(this.TranslateType(returnType));
            output.Append(' ');
            output.Append(funcName);
            output.Append("(");
            for (int i = 0; i < argTypes.Length; ++i)
            {
                if (i > 0) output.Append(", ");
                output.Append(this.TranslateType(argTypes[i]));
                output.Append(' ');
                output.Append(argNames[i].Value);
            }
            output.Append(")");
            output.Append(this.NewLine);
            output.Append(output.CurrentTab);
            output.Append("{");
            output.Append(this.NewLine);
            output.TabDepth++;
            this.TranslateExecutables(output, funcDef.Code);
            output.TabDepth--;
            output.Append(output.CurrentTab);
            output.Append("}");
        }
    }
}
