﻿using Pastel.Nodes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pastel.Transpilers
{
    internal class JavaScriptTranspiler : CurlyBraceTranspiler
    {
        public JavaScriptTranspiler() : base("\t", "\n", true)
        {
            this.UsesStructDefinitions = false;
            this.ClassDefinitionsInSeparateFiles = false;
        }

        public override string HelperCodeResourcePath { get { return "Transpilers/Resources/PastelHelper.js"; } }

        public override string WrapFinalExportedCode(string code, FunctionDefinition[] functions)
        {
            // TODO: public annotation to only export certain functions.

            // TODO: internally minify names. As this is being exported with a list, the order
            // is the only important thing to assign it to the proper external alias.
            StringBuilder sb = new StringBuilder();
            sb.Append("const [");
            string[] funcNames = functions
                .Select(fd => fd.Name)
                .OrderBy(n => n)
                .ToArray();
            for (int i = 0; i < funcNames.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(funcNames[i]);
            }
            sb.Append("] = (() => {\n");
            sb.Append(code);
            sb.Append('\n');
            sb.Append("return [");
            for (int i = 0; i < funcNames.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(funcNames[i]);
            }
            sb.Append("];\n");
            sb.Append("})();\n");
            return sb.ToString();
        }

        protected override void WrapCodeImpl(TranspilerContext ctx, ProjectConfig config, List<string> lines, bool isForStruct)
        {
            // do nothing
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
            this.TranslateExpression(sb, array);
            sb.Append(".join(");
            this.TranslateExpression(sb, sep);
            sb.Append(')');
        }

        public override void TranslateArrayLength(TranspilerContext sb, Expression array)
        {
            this.TranslateExpression(sb, array);
            sb.Append(".length");
        }

        public override void TranslateArrayNew(TranspilerContext sb, PType arrayType, Expression lengthExpression)
        {
            sb.Append("PST$createNewArray(");
            this.TranslateExpression(sb, lengthExpression);
            sb.Append(')');
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
            sb.Append("atob(");
            this.TranslateExpression(sb, base64String);
            sb.Append(").split(',').map(n => parseInt(n))");
        }

        public override void TranslateBase64ToString(TranspilerContext sb, Expression base64String)
        {
            sb.Append("decodeURIComponent(Array.prototype.map.call(atob(");
            this.TranslateExpression(sb, base64String);
            sb.Append("), function(c) { return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2); }).join(''))");
        }

        public override void TranslateCast(TranspilerContext sb, PType type, Expression expression)
        {
            this.TranslateExpression(sb, expression);
        }

        public override void TranslateCharConstant(TranspilerContext sb, char value)
        {
            sb.Append(PastelUtil.ConvertStringValueToCode(value.ToString()));
        }

        public override void TranslateCharToString(TranspilerContext sb, Expression charValue)
        {
            this.TranslateExpression(sb, charValue);
        }

        public override void TranslateChr(TranspilerContext sb, Expression charCode)
        {
            sb.Append("String.fromCharCode(");
            this.TranslateExpression(sb, charCode);
            sb.Append(')');
        }

        public override void TranslateConstructorInvocation(TranspilerContext sb, ConstructorInvocation constructorInvocation)
        {
            sb.Append('[');
            Expression[] args = constructorInvocation.Args;
            for (int i = 0; i < args.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                this.TranslateExpression(sb, args[i]);
            }
            sb.Append(']');
        }

        public override void TranslateCurrentTimeSeconds(TranspilerContext sb)
        {
            sb.Append("((Date.now ? Date.now() : new Date().getTime()) / 1000.0)");
        }

        public override void TranslateDictionaryContainsKey(TranspilerContext sb, Expression dictionary, Expression key)
        {
            sb.Append("(");
            this.TranslateExpression(sb, dictionary);
            sb.Append('[');
            this.TranslateExpression(sb, key);
            sb.Append("] !== undefined)");
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
            sb.Append("Object.keys(");
            this.TranslateExpression(sb, dictionary);
            sb.Append(')');
        }

        public override void TranslateDictionaryNew(TranspilerContext sb, PType keyType, PType valueType)
        {
            sb.Append("{}");
        }

        public override void TranslateDictionaryRemove(TranspilerContext sb, Expression dictionary, Expression key)
        {
            sb.Append("delete ");
            this.TranslateExpression(sb, dictionary);
            sb.Append('[');
            this.TranslateExpression(sb, key);
            sb.Append(']');
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
            sb.Append("Object.keys(");
            this.TranslateExpression(sb, dictionary);
            sb.Append(").length");
        }

        public override void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(varOut.Name);
            sb.Append(" = ");
            this.TranslateExpression(sb, dictionary);
            sb.Append('[');
            this.TranslateExpression(sb, key);
            sb.Append("];");
            sb.Append(this.NewLine);
            sb.Append(sb.CurrentTab);
            sb.Append("if (");
            sb.Append(varOut.Name);
            sb.Append(" === undefined) ");
            sb.Append(varOut.Name);
            sb.Append(" = ");
            this.TranslateExpression(sb, fallbackValue);
            sb.Append(";");
            sb.Append(this.NewLine);
        }

        public override void TranslateDictionaryValues(TranspilerContext sb, Expression dictionary)
        {
            sb.Append("Object.values(");
            this.TranslateExpression(sb, dictionary);
            sb.Append(')');
        }

        public override void TranslateExtensibleCallbackInvoke(TranspilerContext sb, Expression name, Expression argsArray)
        {
            sb.Append("(PST$extCallbacks[");
            this.TranslateExpression(sb, name);
            sb.Append("] || ((o) => null))(");
            this.TranslateExpression(sb, argsArray);
            sb.Append(")");
        }

        public override void TranslateFloatBuffer16(TranspilerContext sb)
        {
            sb.Append("PST$floatBuffer16");
        }

        public override void TranslateFloatDivision(TranspilerContext sb, Expression floatNumerator, Expression floatDenominator)
        {
            sb.Append('(');
            this.TranslateExpression(sb, floatNumerator);
            sb.Append(" / ");
            this.TranslateExpression(sb, floatDenominator);
            sb.Append(')');
        }

        public override void TranslateFloatToInt(TranspilerContext sb, Expression floatExpr)
        {
            sb.Append("Math.floor(");
            this.TranslateExpression(sb, floatExpr);
            sb.Append(')');
        }

        public override void TranslateFloatToString(TranspilerContext sb, Expression floatExpr)
        {
            sb.Append("'' + ");
            this.TranslateExpression(sb, floatExpr);
        }

        public override void TranslateGetFunction(TranspilerContext sb, Expression name)
        {
            sb.Append("PST$getFunction(");
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
            sb.Append("PST$intBuffer16");
        }

        public override void TranslateIntegerDivision(TranspilerContext sb, Expression integerNumerator, Expression integerDenominator)
        {
            sb.Append("Math.floor(");
            this.TranslateExpression(sb, integerNumerator);
            sb.Append(" / ");
            this.TranslateExpression(sb, integerDenominator);
            sb.Append(')');
        }

        public override void TranslateIntToString(TranspilerContext sb, Expression integer)
        {
            sb.Append("('' + ");
            this.TranslateExpression(sb, integer);
            sb.Append(')');
        }

        public override void TranslateIsValidInteger(TranspilerContext sb, Expression stringValue)
        {
            sb.Append("!isNaN(parseInt(");
            this.TranslateExpression(sb, stringValue);
            sb.Append("))");
        }

        public override void TranslateListAdd(TranspilerContext sb, Expression list, Expression item)
        {
            this.TranslateExpression(sb, list);
            sb.Append(".push(");
            this.TranslateExpression(sb, item);
            sb.Append(')');
        }

        public override void TranslateListClear(TranspilerContext sb, Expression list)
        {
            sb.Append("PST$clearList(");
            this.TranslateExpression(sb, list);
            sb.Append(')');
        }

        public override void TranslateListConcat(TranspilerContext sb, Expression list, Expression items)
        {
            this.TranslateExpression(sb, list);
            sb.Append(".concat(");
            this.TranslateExpression(sb, items);
            sb.Append(")");
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
            sb.Append(".splice(");
            this.TranslateExpression(sb, index);
            sb.Append(", 0, ");
            this.TranslateExpression(sb, item);
            sb.Append(')');
        }

        public override void TranslateListJoinChars(TranspilerContext sb, Expression list)
        {
            this.TranslateExpression(sb, list);
            sb.Append(".join('')");
        }

        public override void TranslateListJoinStrings(TranspilerContext sb, Expression list, Expression sep)
        {
            this.TranslateExpression(sb, list);
            sb.Append(".join(");
            this.TranslateExpression(sb, sep);
            sb.Append(')');
        }

        public override void TranslateListNew(TranspilerContext sb, PType type)
        {
            sb.Append("[]");
        }

        public override void TranslateListPop(TranspilerContext sb, Expression list)
        {
            this.TranslateExpression(sb, list);
            sb.Append(".pop()");
        }

        public override void TranslateListRemoveAt(TranspilerContext sb, Expression list, Expression index)
        {
            this.TranslateExpression(sb, list);
            sb.Append(".splice(");
            this.TranslateExpression(sb, index);
            sb.Append(", 1)");
        }

        public override void TranslateListReverse(TranspilerContext sb, Expression list)
        {
            this.TranslateExpression(sb, list);
            sb.Append(".reverse()");
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
            sb.Append("PST$shuffle(");
            this.TranslateExpression(sb, list);
            sb.Append(')');
        }

        public override void TranslateListSize(TranspilerContext sb, Expression list)
        {
            this.TranslateExpression(sb, list);
            sb.Append(".length");
        }

        public override void TranslateListToArray(TranspilerContext sb, Expression list)
        {
            // TODO: go through and figure out which list to array conversions are necessary to copy and which ones are just ensuring that the type is compatible
            // For example, JS and Python can just no-op in situations where a throwaway list builder is being made.
            sb.Append("[...(");
            this.TranslateExpression(sb, list);
            sb.Append(")]");
        }

        public override void TranslateMathArcCos(TranspilerContext sb, Expression ratio)
        {
            sb.Append("Math.acos(");
            this.TranslateExpression(sb, ratio);
            sb.Append(')');
        }

        public override void TranslateMathArcSin(TranspilerContext sb, Expression ratio)
        {
            sb.Append("Math.asin(");
            this.TranslateExpression(sb, ratio);
            sb.Append(')');
        }

        public override void TranslateMathArcTan(TranspilerContext sb, Expression yComponent, Expression xComponent)
        {
            sb.Append("Math.atan2(");
            this.TranslateExpression(sb, yComponent);
            sb.Append(", ");
            this.TranslateExpression(sb, xComponent);
            sb.Append(')');
        }

        public override void TranslateMathCos(TranspilerContext sb, Expression thetaRadians)
        {
            sb.Append("Math.cos(");
            this.TranslateExpression(sb, thetaRadians);
            sb.Append(')');
        }

        public override void TranslateMathLog(TranspilerContext sb, Expression value)
        {
            sb.Append("Math.log(");
            this.TranslateExpression(sb, value);
            sb.Append(')');
        }

        public override void TranslateMathPow(TranspilerContext sb, Expression expBase, Expression exponent)
        {
            sb.Append("Math.pow(");
            this.TranslateExpression(sb, expBase);
            sb.Append(", ");
            this.TranslateExpression(sb, exponent);
            sb.Append(')');
        }

        public override void TranslateMathSin(TranspilerContext sb, Expression thetaRadians)
        {
            sb.Append("Math.sin(");
            this.TranslateExpression(sb, thetaRadians);
            sb.Append(')');
        }

        public override void TranslateMathTan(TranspilerContext sb, Expression thetaRadians)
        {
            sb.Append("Math.tan(");
            this.TranslateExpression(sb, thetaRadians);
            sb.Append(')');
        }

        public override void TranslateMultiplyList(TranspilerContext sb, Expression list, Expression n)
        {
            sb.Append("PST$multiplyList(");
            this.TranslateExpression(sb, list);
            sb.Append(", ");
            this.TranslateExpression(sb, n);
            sb.Append(')');
        }

        public override void TranslateNullConstant(TranspilerContext sb)
        {
            sb.Append("null");
        }

        public override void TranslateOrd(TranspilerContext sb, Expression charValue)
        {
            sb.Append('(');
            this.TranslateExpression(sb, charValue);
            sb.Append(").charCodeAt(0)");
        }

        public override void TranslateParseFloatUnsafe(TranspilerContext sb, Expression stringValue)
        {
            sb.Append("parseFloat(");
            this.TranslateExpression(sb, stringValue);
            sb.Append(')');
        }

        public override void TranslateParseInt(TranspilerContext sb, Expression safeStringValue)
        {
            sb.Append("parseInt(");
            this.TranslateExpression(sb, safeStringValue);
            sb.Append(')');
        }

        public override void TranslatePrintStdErr(TranspilerContext sb, Expression value)
        {
            sb.Append("PST$stderr(");
            this.TranslateExpression(sb, value);
            sb.Append(')');
        }

        public override void TranslatePrintStdOut(TranspilerContext sb, Expression value)
        {
            sb.Append("PST$stdout(");
            this.TranslateExpression(sb, value);
            sb.Append(')');
        }

        public override void TranslateRandomFloat(TranspilerContext sb)
        {
            sb.Append("Math.random()");
        }

        public override void TranslateSortedCopyOfIntArray(TranspilerContext sb, Expression intArray)
        {
            sb.Append("PST$sortedCopyOfArray(");
            this.TranslateExpression(sb, intArray);
            sb.Append(')');
        }

        public override void TranslateSortedCopyOfStringArray(TranspilerContext sb, Expression stringArray)
        {
            sb.Append("PST$sortedCopyOfArray(");
            this.TranslateExpression(sb, stringArray);
            sb.Append(')');
        }

        public override void TranslateStringAppend(TranspilerContext sb, Expression str1, Expression str2)
        {
            this.TranslateExpression(sb, str1);
            sb.Append(" += ");
            this.TranslateExpression(sb, str2);
        }

        public override void TranslateStringBuffer16(TranspilerContext sb)
        {
            sb.Append("PST$stringBuffer16");
        }

        public override void TranslateStringCharAt(TranspilerContext sb, Expression str, Expression index)
        {
            this.TranslateExpression(sb, str);
            sb.Append(".charAt(");
            this.TranslateExpression(sb, index);
            sb.Append(')');
        }

        public override void TranslateStringCharCodeAt(TranspilerContext sb, Expression str, Expression index)
        {
            this.TranslateExpression(sb, str);
            sb.Append(".charCodeAt(");
            this.TranslateExpression(sb, index);
            sb.Append(')');
        }

        public override void TranslateStringCompareIsReverse(TranspilerContext sb, Expression str1, Expression str2)
        {
            sb.Append("(");
            this.TranslateExpression(sb, str1);
            sb.Append(".localeCompare(");
            this.TranslateExpression(sb, str2);
            sb.Append(") > 0)");
        }

        public override void TranslateStringConcatAll(TranspilerContext sb, Expression[] strings)
        {
            sb.Append("[");
            for (int i = 0; i < strings.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                this.TranslateExpression(sb, strings[i]);
            }
            sb.Append("].join('')");
        }

        public override void TranslateStringConcatPair(TranspilerContext sb, Expression strLeft, Expression strRight)
        {
            this.TranslateExpression(sb, strLeft);
            sb.Append(" + ");
            this.TranslateExpression(sb, strRight);
        }

        public override void TranslateStringContains(TranspilerContext sb, Expression haystack, Expression needle)
        {
            sb.Append('(');
            this.TranslateExpression(sb, haystack);
            sb.Append(".indexOf(");
            this.TranslateExpression(sb, needle);
            sb.Append(") != -1)");
        }

        public override void TranslateStringEndsWith(TranspilerContext sb, Expression haystack, Expression needle)
        {
            sb.Append("(");
            this.TranslateExpression(sb, haystack);
            sb.Append(").endsWith(");
            this.TranslateExpression(sb, needle);
            sb.Append(')');
        }

        public override void TranslateStringEquals(TranspilerContext sb, Expression left, Expression right)
        {
            this.TranslateExpression(sb, left);
            sb.Append(" === ");
            this.TranslateExpression(sb, right);
        }

        public override void TranslateStringFromCharCode(TranspilerContext sb, Expression charCode)
        {
            sb.Append("String.fromCharCode(");
            this.TranslateExpression(sb, charCode);
            sb.Append(')');
        }

        public override void TranslateStringIndexOf(TranspilerContext sb, Expression haystack, Expression needle)
        {
            this.TranslateExpression(sb, haystack);
            sb.Append(".indexOf(");
            this.TranslateExpression(sb, needle);
            sb.Append(')');
        }

        public override void TranslateStringIndexOfWithStart(TranspilerContext sb, Expression haystack, Expression needle, Expression startIndex)
        {
            this.TranslateExpression(sb, haystack);
            sb.Append(".indexOf(");
            this.TranslateExpression(sb, needle);
            sb.Append(", ");
            this.TranslateExpression(sb, startIndex);
            sb.Append(')');
        }

        public override void TranslateStringLastIndexOf(TranspilerContext sb, Expression haystack, Expression needle)
        {
            this.TranslateExpression(sb, haystack);
            sb.Append(".lastIndexOf(");
            this.TranslateExpression(sb, needle);
            sb.Append(')');
        }

        public override void TranslateStringLength(TranspilerContext sb, Expression str)
        {
            this.TranslateExpression(sb, str);
            sb.Append(".length");
        }

        public override void TranslateStringReplace(TranspilerContext sb, Expression haystack, Expression needle, Expression newNeedle)
        {
            this.TranslateExpression(sb, haystack);
            sb.Append(".split(");
            this.TranslateExpression(sb, needle);
            sb.Append(").join(");
            this.TranslateExpression(sb, newNeedle);
            sb.Append(')');
        }

        public override void TranslateStringReverse(TranspilerContext sb, Expression str)
        {
            this.TranslateExpression(sb, str);
            sb.Append(".split('').reverse().join('')");
        }

        public override void TranslateStringSplit(TranspilerContext sb, Expression haystack, Expression needle)
        {
            this.TranslateExpression(sb, haystack);
            sb.Append(".split(");
            this.TranslateExpression(sb, needle);
            sb.Append(')');
        }

        public override void TranslateStringStartsWith(TranspilerContext sb, Expression haystack, Expression needle)
        {
            sb.Append('(');
            this.TranslateExpression(sb, haystack);
            sb.Append(").startsWith(");
            this.TranslateExpression(sb, needle);
            sb.Append(")");
        }

        public override void TranslateStringSubstring(TranspilerContext sb, Expression str, Expression start, Expression length)
        {
            this.TranslateExpression(sb, str);
            sb.Append(".substring(");
            this.TranslateExpression(sb, start);
            sb.Append(", (");
            this.TranslateExpression(sb, start);
            sb.Append(") + (");
            this.TranslateExpression(sb, length);
            sb.Append("))");
        }

        public override void TranslateStringSubstringIsEqualTo(TranspilerContext sb, Expression haystack, Expression startIndex, Expression needle)
        {
            sb.Append("PST$checksubstring(");
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
            sb.Append(".toLowerCase()");
        }

        public override void TranslateStringToUpper(TranspilerContext sb, Expression str)
        {
            this.TranslateExpression(sb, str);
            sb.Append(".toUpperCase()");
        }

        public override void TranslateStringToUtf8Bytes(TranspilerContext sb, Expression str)
        {
            sb.Append("PST$stringToUtf8Bytes(");
            this.TranslateExpression(sb, str);
            sb.Append(')');
        }

        public override void TranslateStringTrim(TranspilerContext sb, Expression str)
        {
            this.TranslateExpression(sb, str);
            sb.Append(".trim()");
        }

        public override void TranslateStringTrimEnd(TranspilerContext sb, Expression str)
        {
            sb.Append('(');
            this.TranslateExpression(sb, str);
            sb.Append(").trimEnd()");
        }

        public override void TranslateStringTrimStart(TranspilerContext sb, Expression str)
        {
            sb.Append('(');
            this.TranslateExpression(sb, str);
            sb.Append(").trimStart()");
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
            this.TranslateExpression(sb, root);
            sb.Append('[');
            sb.Append(fieldIndex);
            sb.Append(']');
        }

        public override void TranslateThis(TranspilerContext sb, ThisExpression thisExpr)
        {
            sb.Append("_PST_this");
        }

        public override void TranslateToCodeString(TranspilerContext sb, Expression str)
        {
            sb.Append("JSON.stringify(");
            this.TranslateExpression(sb, str);
            sb.Append(')');
        }

        public override void TranslateTryParseFloat(TranspilerContext sb, Expression stringValue, Expression floatOutList)
        {
            sb.Append("PST$floatParseHelper(");
            this.TranslateExpression(sb, floatOutList);
            sb.Append(", ");
            this.TranslateExpression(sb, stringValue);
            sb.Append(')');
        }

        public override void TranslateUtf8BytesToString(TranspilerContext sb, Expression bytes)
        {
            sb.Append("new TextDecoder().decode(new Uint8Array(");
            this.TranslateExpression(sb, bytes);
            sb.Append("))");
        }

        public override void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("let ");
            sb.Append(varDecl.VariableNameToken.Value);
            sb.Append(" = ");
            if (varDecl.Value == null)
            {
                sb.Append("null");
            }
            else
            {
                this.TranslateExpression(sb, varDecl.Value);
            }
            sb.Append(';');
            sb.Append(this.NewLine);
        }

        public override void GenerateCodeForFunction(TranspilerContext sb, FunctionDefinition funcDef, bool isStatic)
        {
            sb.Append("let ");
            sb.Append(funcDef.NameToken.Value);
            sb.Append(" = function(");
            Token[] args = funcDef.ArgNames;
            for (int i = 0; i < args.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(args[i].Value);
            }
            sb.Append(") {");
            sb.Append(this.NewLine);

            sb.TabDepth = 1;
            this.TranslateExecutables(sb, funcDef.Code);
            sb.TabDepth = 0;

            sb.Append("};");
            sb.Append(this.NewLine);
            sb.Append(this.NewLine);
        }

        public override void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef)
        {
            throw new NotImplementedException();
        }

        public override void GenerateCodeForClass(TranspilerContext sb, ClassDefinition classDef)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("function ");
            sb.Append(classDef.NameToken.Value);
            sb.Append("(");
            ConstructorDefinition ctor = classDef.Constructor;
            for (int i = 0; i < ctor.ArgNames.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(ctor.ArgNames[i].Value);
            }
            sb.Append(") {\n");
            sb.TabDepth++;
            sb.Append(sb.CurrentTab);
            sb.Append("let _PST_this = this;\n");
            foreach (FieldDefinition fd in classDef.Fields)
            {
                sb.Append(sb.CurrentTab);
                sb.Append("this.");
                sb.Append(fd.NameToken.Value);
                sb.Append(" = ");
                this.TranslateExpression(sb, fd.Value);
                sb.Append(";\n");
            }
            this.TranslateExecutables(sb, ctor.Code);
            foreach (FunctionDefinition func in classDef.Methods)
            {
                sb.Append(sb.CurrentTab);
                sb.Append(classDef.NameToken.Value);
                sb.Append(".prototype.");
                sb.Append(func.Name);
                sb.Append(" = function(");
                for (int i = 0; i < func.ArgNames.Length; ++i)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(func.ArgNames[i].Value);
                }
                sb.Append(") {\n");
                sb.TabDepth++;
                this.TranslateExecutables(sb, func.Code);
                sb.TabDepth--;
                sb.Append(sb.CurrentTab);
                sb.Append("};\n");
            }
            sb.TabDepth--;
            sb.Append("}\n");
        }
    }
}
