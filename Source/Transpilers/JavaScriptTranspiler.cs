using Pastel.Parser;
using Pastel.Parser.ParseNodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pastel.Transpilers
{
    internal class JavaScriptTranspiler : CurlyBraceTranspiler
    {
        public JavaScriptTranspiler(TranspilerContext transpilerCtx) : base(transpilerCtx, true)
        {
            this.UsesStructDefinitions = false;
            this.ClassDefinitionsInSeparateFiles = false;
        }

        public override string PreferredTab => "\t";
        public override string PreferredNewline => "\n";

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

        public override StringBuffer TranslateArrayGet(Expression array, Expression index)
        {
            return this.TranslateExpression(array)
                .Push('[')
                .Push(this.TranslateExpression(index))
                .Push(']');
        }

        public override StringBuffer TranslateArrayJoin(Expression array, Expression sep)
        {
            return this.TranslateExpression(array)
                .Push(".join(")
                .Push(this.TranslateExpression(sep))
                .Push(')');
        }

        public override StringBuffer TranslateArrayLength(Expression array)
        {
            return this.TranslateExpression(array)
                .Push(".length");
        }

        public override StringBuffer TranslateArrayNew(PType arrayType, Expression lengthExpression)
        {
            return StringBuffer
                .Of("PST$createNewArray(")
                .Push(this.TranslateExpression(lengthExpression))
                .Push(')');
        }

        public override StringBuffer TranslateArraySet(Expression array, Expression index, Expression value)
        {
            return this.TranslateExpression(array)
                .Push('[')
                .Push(this.TranslateExpression(index))
                .Push("] = ")
                .Push(this.TranslateExpression(value));
        }

        public override StringBuffer TranslateBase64ToBytes(Expression base64String)
        {
            return StringBuffer.Of("atob(")
                .Push(this.TranslateExpression(base64String))
                .Push(").split(',').map(n => parseInt(n))");
        }

        public override StringBuffer TranslateBase64ToString(Expression base64String)
        {
            return StringBuffer.Of("decodeURIComponent(Array.prototype.map.call(atob(")
                .Push(this.TranslateExpression(base64String))
                .Push("), function(c) { return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2); }).join(''))");
        }

        public override StringBuffer TranslateCast(PType type, Expression expression)
        {
            return this.TranslateExpression(expression);
        }

        public override StringBuffer TranslateCharConstant(char value)
        {
            return StringBuffer.Of(CodeUtil.ConvertStringValueToCode(value.ToString()));
        }

        public override StringBuffer TranslateCharToString(Expression charValue)
        {
            return this.TranslateExpression(charValue);
        }

        public override StringBuffer TranslateChr(Expression charCode)
        {
            return StringBuffer
                .Of("String.fromCharCode(")
                .Push(this.TranslateExpression(charCode))
                .Push(")");
        }

        public override StringBuffer TranslateConstructorInvocation(ConstructorInvocation constructorInvocation)
        {
            StringBuffer buf = StringBuffer.Of("[");
            Expression[] args = constructorInvocation.Args;
            for (int i = 0; i < args.Length; ++i)
            {
                if (i > 0) buf.Push(", ");
                buf.Push(this.TranslateExpression(args[i]));
            }
            return buf.Push(']');
        }

        public override StringBuffer TranslateCurrentTimeSeconds()
        {
            return StringBuffer.Of("((Date.now ? Date.now() : new Date().getTime()) / 1000.0)");
        }

        public override StringBuffer TranslateDictionaryContainsKey(Expression dictionary, Expression key)
        {
            return StringBuffer
                .Of("(")
                .Push(this.TranslateExpression(dictionary))
                .Push('[')
                .Push(this.TranslateExpression(key))
                .Push("] !== undefined)");
        }

        public override StringBuffer TranslateDictionaryGet(Expression dictionary, Expression key)
        {
            return this.TranslateExpression(dictionary)
                .Push('[')
                .Push(this.TranslateExpression(key))
                .Push(']');
        }

        public override StringBuffer TranslateDictionaryKeys(Expression dictionary)
        {
            return StringBuffer
                .Of("Object.keys(")
                .Push(this.TranslateExpression(dictionary))
                .Push(")");
        }

        public override StringBuffer TranslateDictionaryNew(PType keyType, PType valueType)
        {
            return StringBuffer.Of("{}");
        }

        public override StringBuffer TranslateDictionaryRemove(Expression dictionary, Expression key)
        {
            return StringBuffer
                .Of("delete ")
                .Push(this.TranslateExpression(dictionary))
                .Push('[')
                .Push(this.TranslateExpression(key))
                .Push(']');
        }

        public override StringBuffer TranslateDictionarySet(Expression dictionary, Expression key, Expression value)
        {
            return this.TranslateExpression(dictionary)
                .Push('[')
                .Push(this.TranslateExpression(key))
                .Push("] = ")
                .Push(this.TranslateExpression(value));
        }

        public override StringBuffer TranslateDictionarySize(Expression dictionary)
        {
            return StringBuffer
                .Of("Object.keys(")
                .Push(this.TranslateExpression(dictionary))
                .Push(").length");
        }

        public override void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(varOut.Name);
            sb.Append(" = ");
            sb.Append(this.TranslateExpressionAsString(dictionary));
            sb.Append('[');
            sb.Append(this.TranslateExpressionAsString(key));
            sb.Append("];\n");
            sb.Append(sb.CurrentTab);
            sb.Append("if (");
            sb.Append(varOut.Name);
            sb.Append(" === undefined) ");
            sb.Append(varOut.Name);
            sb.Append(" = ");
            sb.Append(this.TranslateExpressionAsString(fallbackValue));
            sb.Append(";\n");
        }

        public override StringBuffer TranslateDictionaryValues(Expression dictionary)
        {
            return StringBuffer
                .Of("Object.values(")
                .Push(this.TranslateExpression(dictionary))
                .Push(')');
        }

        public override StringBuffer TranslateExtensibleCallbackInvoke(Expression name, Expression argsArray)
        {
            return StringBuffer
                .Of("(PST$extCallbacks[")
                .Push(this.TranslateExpression(name))
                .Push("] || ((o) => null))(")
                .Push(this.TranslateExpression(argsArray))
                .Push(")");
        }

        public override StringBuffer TranslateFloatBuffer16()
        {
            return StringBuffer.Of("PST$floatBuffer16");
        }

        public override StringBuffer TranslateFloatDivision(Expression floatNumerator, Expression floatDenominator)
        {
            return StringBuffer
                .Of("(")
                .Push(this.TranslateExpression(floatNumerator))
                .Push(" / ")
                .Push(this.TranslateExpression(floatDenominator))
                .Push(')');
        }

        public override StringBuffer TranslateFloatToInt(Expression floatExpr)
        {
            return StringBuffer
                .Of("Math.floor(")
                .Push(this.TranslateExpression(floatExpr))
                .Push(')');
        }

        public override StringBuffer TranslateFloatToString(Expression floatExpr)
        {
            return StringBuffer
                .Of("'' + ")
                .Push(this.TranslateExpression(floatExpr));
        }

        public override StringBuffer TranslateGetFunction(Expression name)
        {
            return StringBuffer
                .Of("PST$getFunction(")
                .Push(this.TranslateExpression(name))
                .Push(')');
        }

        public override StringBuffer TranslateInstanceFieldDereference(Expression root, ClassDefinition classDef, string fieldName)
        {
            return this.TranslateExpression(root)
                .Push('.')
                .Push(fieldName);
        }

        public override StringBuffer TranslateIntBuffer16()
        {
            return StringBuffer.Of("PST$intBuffer16");
        }

        public override StringBuffer TranslateIntegerDivision(Expression integerNumerator, Expression integerDenominator)
        {
            return StringBuffer
                .Of("Math.floor(")
                .Push(this.TranslateExpression(integerNumerator))
                .Push(" / ")
                .Push(this.TranslateExpression(integerDenominator))
                .Push(')');
        }

        public override StringBuffer TranslateIntToString(Expression integer)
        {
            return StringBuffer
                .Of("('' + ")
                .Push(this.TranslateExpression(integer))
                .Push(')');
        }

        public override StringBuffer TranslateIsValidInteger(Expression stringValue)
        {
            return StringBuffer
                .Of("!isNaN(parseInt(")
                .Push(this.TranslateExpression(stringValue))
                .Push("))");
        }

        public override StringBuffer TranslateListAdd(Expression list, Expression item)
        {
            return this.TranslateExpression(list)
                .Push(".push(")
                .Push(this.TranslateExpression(item))
                .Push(')');
        }

        public override StringBuffer TranslateListClear(Expression list)
        {
            return StringBuffer.Of("PST$clearList(")
                .Push(this.TranslateExpression(list))
                .Push(')');
        }

        public override StringBuffer TranslateListConcat(Expression list, Expression items)
        {
            return this.TranslateExpression(list)
                .Push(".concat(")
                .Push(this.TranslateExpression(items))
                .Push(")");
        }

        public override StringBuffer TranslateListGet(Expression list, Expression index)
        {
            return this.TranslateExpression(list)
                .Push('[')
                .Push(this.TranslateExpression(index))
                .Push(']');
        }

        public override StringBuffer TranslateListInsert(Expression list, Expression index, Expression item)
        {
            return this.TranslateExpression(list)
                .Push(".splice(")
                .Push(this.TranslateExpression(index))
                .Push(", 0, ")
                .Push(this.TranslateExpression(item))
                .Push(')');
        }

        public override StringBuffer TranslateListJoinChars(Expression list)
        {
            return this.TranslateExpression(list)
                .Push(".join('')");
        }

        public override StringBuffer TranslateListJoinStrings(Expression list, Expression sep)
        {
            return this.TranslateExpression(list)
                .Push(".join(")
                .Push(this.TranslateExpression(sep))
                .Push(')');
        }

        public override StringBuffer TranslateListNew(PType type)
        {
            return StringBuffer.Of("[]");
        }

        public override StringBuffer TranslateListPop(Expression list)
        {
            return this.TranslateExpression(list)
                .Push(".pop()");
        }

        public override StringBuffer TranslateListRemoveAt(Expression list, Expression index)
        {
            return this.TranslateExpression(list)
                .Push(".splice(")
                .Push(this.TranslateExpression(index))
                .Push(", 1)");
        }

        public override StringBuffer TranslateListReverse(Expression list)
        {
            return this.TranslateExpression(list)
                .Push(".reverse()");
        }

        public override StringBuffer TranslateListSet(Expression list, Expression index, Expression value)
        {
            return this.TranslateExpression(list)
                .Push('[')
                .Push(this.TranslateExpression(index))
                .Push("] = ")
                .Push(this.TranslateExpression(value));
        }

        public override StringBuffer TranslateListShuffle(Expression list)
        {
            return StringBuffer.Of("PST$shuffle(")
                .Push(this.TranslateExpression(list))
                .Push(')');
        }

        public override StringBuffer TranslateListSize(Expression list)
        {
            return this.TranslateExpression(list)
                .Push(".length");
        }

        public override StringBuffer TranslateListToArray(Expression list)
        {
            // TODO: go through and figure out which list to array conversions are necessary to copy and which ones are just ensuring that the type is compatible
            // For example, JS and Python can just no-op in situations where a throwaway list builder is being made.
            return StringBuffer
                .Of("[...(")
                .Push(this.TranslateExpression(list))
                .Push(")]");
        }

        public override StringBuffer TranslateMathArcCos(Expression ratio)
        {
            return StringBuffer
                .Of("Math.acos(")
                .Push(this.TranslateExpression(ratio))
                .Push(')');
        }

        public override StringBuffer TranslateMathArcSin(Expression ratio)
        {
            return StringBuffer
                .Of("Math.asin(")
                .Push(this.TranslateExpression(ratio))
                .Push(')');
        }

        public override StringBuffer TranslateMathArcTan(Expression yComponent, Expression xComponent)
        {
            return StringBuffer
                .Of("Math.atan2(")
                .Push(this.TranslateExpression(yComponent))
                .Push(", ")
                .Push(this.TranslateExpression(xComponent))
                .Push(')');
        }

        public override StringBuffer TranslateMathCos(Expression thetaRadians)
        {
            return StringBuffer
                .Of("Math.cos(")
                .Push(this.TranslateExpression(thetaRadians))
                .Push(')');
        }

        public override StringBuffer TranslateMathLog(Expression value)
        {
            return StringBuffer
                .Of("Math.log(")
                .Push(this.TranslateExpression(value))
                .Push(')');
        }

        public override StringBuffer TranslateMathPow(Expression expBase, Expression exponent)
        {
            return StringBuffer
                .Of("Math.pow(")
                .Push(this.TranslateExpression(expBase))
                .Push(", ")
                .Push(this.TranslateExpression(exponent))
                .Push(')');
        }

        public override StringBuffer TranslateMathSin(Expression thetaRadians)
        {
            return StringBuffer
                .Of("Math.sin(")
                .Push(this.TranslateExpression(thetaRadians))
                .Push(')');
        }

        public override StringBuffer TranslateMathTan(Expression thetaRadians)
        {
            return StringBuffer
                .Of("Math.tan(")
                .Push(this.TranslateExpression(thetaRadians))
                .Push(')');
        }

        public override StringBuffer TranslateMultiplyList(Expression list, Expression n)
        {
            return StringBuffer
                .Of("PST$multiplyList(")
                .Push(this.TranslateExpression(list))
                .Push(", ")
                .Push(this.TranslateExpression(n))
                .Push(')');
        }

        public override StringBuffer TranslateNullConstant()
        {
            return StringBuffer.Of("null");
        }

        public override StringBuffer TranslateOrd(Expression charValue)
        {
            return StringBuffer
                .Of("(")
                .Push(this.TranslateExpression(charValue))
                .Push(").charCodeAt(0)");
        }

        public override StringBuffer TranslateParseFloatUnsafe(Expression stringValue)
        {
            return StringBuffer
                .Of("parseFloat(")
                .Push(this.TranslateExpression(stringValue))
                .Push(')');
        }

        public override StringBuffer TranslateParseInt(Expression safeStringValue)
        {
            return StringBuffer
                .Of("parseInt(")
                .Push(this.TranslateExpression(safeStringValue))
                .Push(')');
        }

        public override StringBuffer TranslatePrintStdErr(Expression value)
        {
            return StringBuffer
                .Of("PST$stderr(")
                .Push(this.TranslateExpression(value))
                .Push(')');
        }

        public override StringBuffer TranslatePrintStdOut(Expression value)
        {
            return StringBuffer
                .Of("PST$stdout(")
                .Push(this.TranslateExpression(value))
                .Push(')');
        }

        public override StringBuffer TranslateRandomFloat()
        {
            return StringBuffer.Of("Math.random()");
        }

        public override StringBuffer TranslateSortedCopyOfIntArray(Expression intArray)
        {
            return StringBuffer
                .Of("PST$sortedCopyOfArray(")
                .Push(this.TranslateExpression(intArray))
                .Push(')');
        }

        public override StringBuffer TranslateSortedCopyOfStringArray(Expression stringArray)
        {
            return StringBuffer
                .Of("PST$sortedCopyOfArray(")
                .Push(this.TranslateExpression(stringArray))
                .Push(')');
        }

        public override StringBuffer TranslateStringAppend(Expression str1, Expression str2)
        {
            return this.TranslateExpression(str1)
                .Push(" += ")
                .Push(this.TranslateExpression(str2));
        }

        public override StringBuffer TranslateStringBuffer16()
        {
            return StringBuffer.Of("PST$stringBuffer16");
        }

        public override StringBuffer TranslateStringCharAt(Expression str, Expression index)
        {
            return this.TranslateExpression(str)
                .Push(".charAt(")
                .Push(this.TranslateExpression(index))
                .Push(')');
        }

        public override StringBuffer TranslateStringCharCodeAt(Expression str, Expression index)
        {
            return this.TranslateExpression(str)
                .Push(".charCodeAt(")
                .Push(this.TranslateExpression(index))
                .Push(')');
        }

        public override StringBuffer TranslateStringCompareIsReverse(Expression str1, Expression str2)
        {
            return StringBuffer.Of("(")
                .Push(this.TranslateExpression(str1))
                .Push(".localeCompare(")
                .Push(this.TranslateExpression(str2))
                .Push(") > 0)");
        }

        public override StringBuffer TranslateStringConcatAll(Expression[] strings)
        {
            // TODO: because all major JavaScript engines use a string builder inherently,
            // this should be simplified to simple concats.
            StringBuffer buf = StringBuffer.Of("[");
            for (int i = 0; i < strings.Length; ++i)
            {
                if (i > 0) buf.Push(", ");
                buf.Push(this.TranslateExpression(strings[i]));
            }
            return buf.Push("].join('')");
        }

        public override StringBuffer TranslateStringConcatPair(Expression strLeft, Expression strRight)
        {
            return this.TranslateExpression(strLeft)
                .Push(" + ")
                .Push(this.TranslateExpression(strRight));
        }

        public override StringBuffer TranslateStringContains(Expression haystack, Expression needle)
        {
            return StringBuffer
                .Of("(")
                .Push(this.TranslateExpression(haystack))
                .Push(".indexOf(")
                .Push(this.TranslateExpression(needle))
                .Push(") != -1)");
        }

        public override StringBuffer TranslateStringEndsWith(Expression haystack, Expression needle)
        {
            return StringBuffer
                .Of("(")
                .Push(this.TranslateExpression(haystack))
                .Push(").endsWith(")
                .Push(this.TranslateExpression(needle))
                .Push(')');
        }

        public override StringBuffer TranslateStringEquals(Expression left, Expression right)
        {
            return this.TranslateExpression(left)
                .Push(" === ")
                .Push(this.TranslateExpression(right));
        }

        public override StringBuffer TranslateStringFromCharCode(Expression charCode)
        {
            return StringBuffer.Of("String.fromCharCode(")
                .Push(this.TranslateExpression(charCode))
                .Push(')');
        }

        public override StringBuffer TranslateStringIndexOf(Expression haystack, Expression needle)
        {
            return this.TranslateExpression(haystack)
                .Push(".indexOf(")
                .Push(this.TranslateExpression(needle))
                .Push(')');
        }

        public override StringBuffer TranslateStringIndexOfWithStart(Expression haystack, Expression needle, Expression startIndex)
        {
            return this.TranslateExpression(haystack)
                .Push(".indexOf(")
                .Push(this.TranslateExpression(needle))
                .Push(", ")
                .Push(this.TranslateExpression(startIndex))
                .Push(')');
        }

        public override StringBuffer TranslateStringLastIndexOf(Expression haystack, Expression needle)
        {
            return this.TranslateExpression(haystack)
                .Push(".lastIndexOf(")
                .Push(this.TranslateExpression(needle))
                .Push(')');
        }

        public override StringBuffer TranslateStringLength(Expression str)
        {
            return this.TranslateExpression(str)
                .Push(".length");
        }

        public override StringBuffer TranslateStringReplace(Expression haystack, Expression needle, Expression newNeedle)
        {
            return this.TranslateExpression(haystack)
                .Push(".split(")
                .Push(this.TranslateExpression(needle))
                .Push(").join(")
                .Push(this.TranslateExpression(newNeedle))
                .Push(')');
        }

        public override StringBuffer TranslateStringReverse(Expression str)
        {
            return this.TranslateExpression(str)
                .Push(".split('').reverse().join('')");
        }

        public override StringBuffer TranslateStringSplit(Expression haystack, Expression needle)
        {
            return this.TranslateExpression(haystack)
                .Push(".split(")
                .Push(this.TranslateExpression(needle))
                .Push(')');
        }

        public override StringBuffer TranslateStringStartsWith(Expression haystack, Expression needle)
        {
            return StringBuffer
                .Of("(")
                .Push(this.TranslateExpression(haystack))
                .Push(").startsWith(")
                .Push(this.TranslateExpression(needle))
                .Push(")");
        }

        public override StringBuffer TranslateStringSubstring(Expression str, Expression start, Expression length)
        {
            return this.TranslateExpression(str)
                .Push(".substring(")
                .Push(this.TranslateExpression(start))
                .Push(", (")
                .Push(this.TranslateExpression(start))
                .Push(") + (")
                .Push(this.TranslateExpression(length))
                .Push("))");
        }

        public override StringBuffer TranslateStringSubstringIsEqualTo(Expression haystack, Expression startIndex, Expression needle)
        {
            return StringBuffer
                .Of("PST$checksubstring(")
                .Push(this.TranslateExpression(haystack))
                .Push(", ")
                .Push(this.TranslateExpression(startIndex))
                .Push(", ")
                .Push(this.TranslateExpression(needle))
                .Push(')');
        }

        public override StringBuffer TranslateStringToLower(Expression str)
        {
            return this.TranslateExpression(str)
                .Push(".toLowerCase()");
        }

        public override StringBuffer TranslateStringToUpper(Expression str)
        {
            return this.TranslateExpression(str)
                .Push(".toUpperCase()");
        }

        public override StringBuffer TranslateStringToUtf8Bytes(Expression str)
        {
            return StringBuffer
                .Of("PST$stringToUtf8Bytes(")
                .Push(this.TranslateExpression(str))
                .Push(')');
        }

        public override StringBuffer TranslateStringTrim(Expression str)
        {
            return this.TranslateExpression(str)
                .Push(".trim()");
        }

        public override StringBuffer TranslateStringTrimEnd(Expression str)
        {
            return StringBuffer
                .Of("(")
                .Push(this.TranslateExpression(str))
                .Push(").trimEnd()");
        }

        public override StringBuffer TranslateStringTrimStart(Expression str)
        {
            return StringBuffer
                .Of("(")
                .Push(this.TranslateExpression(str))
                .Push(").trimStart()");
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
                .Push("[" + fieldIndex + "]");
        }

        public override StringBuffer TranslateThis(ThisExpression thisExpr)
        {
            return StringBuffer.Of("_PST_this");
        }

        public override StringBuffer TranslateToCodeString(Expression str)
        {
            return StringBuffer
                .Of("JSON.stringify(")
                .Push(this.TranslateExpression(str))
                .Push(')');
        }

        public override StringBuffer TranslateTryParseFloat(Expression stringValue, Expression floatOutList)
        {
            return StringBuffer
                .Of("PST$floatParseHelper(")
                .Push(this.TranslateExpression(floatOutList))
                .Push(", ")
                .Push(this.TranslateExpression(stringValue))
                .Push(')');
        }

        public override StringBuffer TranslateUtf8BytesToString(Expression bytes)
        {
            return StringBuffer.Of("new TextDecoder().decode(new Uint8Array(")
                .Push(this.TranslateExpression(bytes))
                .Push("))");
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
                sb.Append(this.TranslateExpressionAsString(varDecl.Value));
            }
            sb.Append(";\n");
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
            sb.Append(") {\n");

            sb.TabDepth = 1;
            this.TranslateStatements(sb, funcDef.Code);
            sb.TabDepth = 0;

            sb.Append("};\n\n");
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
                sb.Append(this.TranslateExpressionAsString(fd.Value));
                sb.Append(";\n");
            }
            this.TranslateStatements(sb, ctor.Code);
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
                this.TranslateStatements(sb, func.Code);
                sb.TabDepth--;
                sb.Append(sb.CurrentTab);
                sb.Append("};\n");
            }
            sb.TabDepth--;
            sb.Append("}\n");
        }
    }
}
