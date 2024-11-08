using Pastel.Parser;
using Pastel.Parser.ParseNodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Transpilers
{
    internal class PhpTranspiler : CurlyBraceTranspiler
    {
        public PhpTranspiler(TranspilerContext transpilerCtx)
            : base(transpilerCtx, true)
        {
            this.HasNewLineAtEndOfFile = false;
            this.HasStructsInSeparateFiles = false;
        }

        public override string PreferredTab => "\t";
        public override string PreferredNewline => "\n";

        public override string HelperCodeResourcePath { get { return "Transpilers/Resources/PastelHelper.php"; } }

        public override string TranslateType(PType type)
        {
            throw new Exception(); // PHP doesn't have strict types.
        }

        protected override void WrapCodeImpl(TranspilerContext ctx, ProjectConfig config, List<string> lines, bool isForStruct)
        {
            if (!isForStruct)
            {
                CodeUtil.IndentLines(2, lines);

                List<string> prefixes = new List<string>();

                string className = config.WrappingClassNameForFunctions ?? "PastelGeneratedCode";

                prefixes.Add("class " + className + " {");

                CodeUtil.IndentLines(prefixes);

                prefixes.InsertRange(0, new string[] {
                    "<?php",
                    ""
                });

                lines.InsertRange(0, prefixes);

                bool hasIntBuffer = false;
                foreach (string line in lines)
                {
                    if (line.Contains("PST_intBuffer16"))
                    {
                        hasIntBuffer = true;
                    }
                }

                lines.Add("\t}");

                if (hasIntBuffer)
                {
                    lines.Add("\tPastelGeneratedCode::$PST_intBuffer16 = pastelWrapList(array_fill(0, 16, 0));");
                }

                lines.Add("");
                lines.Add("?>");
            }
        }

        private IList<string> GetPhpLinesWithoutWrapper(string filename, string fileContents)
        {
            List<string> lines = new List<string>(fileContents.Trim().Split('\n'));
            if (lines.Count >= 2)
            {
                string first = lines[0].Trim();
                string last = lines[lines.Count - 1].Trim();
                if ((first == "<?php" || first == "<?") &&
                    last == "?>")
                {
                    lines.RemoveAt(lines.Count - 1);
                    lines.RemoveAt(0);

                    while (lines.Count > 0 && lines[lines.Count - 1].Trim().Length == 0)
                    {
                        lines.RemoveAt(lines.Count - 1);
                    }
                    return lines;
                }
            }
            throw new UserErrorException(filename + " must begin with '<?php'/'<?' and end with '?>' on their own lines.");
        }

        public override StringBuffer TranslateFunctionInvocation(FunctionReference funcRef, Expression[] args)
        {
            return StringBuffer.Of("self::")
                .Push(base.TranslateFunctionInvocation(funcRef, args));
        }

        public override StringBuffer TranslateFunctionPointerInvocation(FunctionPointerInvocation fpi)
        {
            StringBuffer buf = this.TranslateExpression(fpi.Root)
                .Push('(');
            Expression[] args = fpi.Args;
            for (int i = 0; i < args.Length; ++i)
            {
                if (i > 0) buf.Push(", ");
                buf.Push(this.TranslateExpression(args[i]));
            }
            return buf.Push(')');
        }

        public override StringBuffer TranslatePrintStdErr(Expression value)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslatePrintStdOut(Expression value)
        {
            return StringBuffer
                .Of("echo ")
                .Push(this.TranslateExpression(value));
        }

        public override StringBuffer TranslateArrayGet(Expression array, Expression index)
        {
            return this.TranslateExpression(array)
                .Push("->arr[")
                .Push(this.TranslateExpression(index))
                .Push(']');
        }

        public override StringBuffer TranslateArrayJoin(Expression array, Expression sep)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateArrayLength(Expression array)
        {
            return StringBuffer
                .Of("count(")
                .Push(this.TranslateExpression(array))
                .Push("->arr)");
        }

        public override StringBuffer TranslateArrayNew(PType arrayType, Expression lengthExpression)
        {
            StringBuffer buf = StringBuffer
                .Of("pastelWrapList(array_fill(0, ")
                .Push(this.TranslateExpression(lengthExpression))
                .Push(", ");
            switch (arrayType.RootValue)
            {
                case "int": buf.Push("0"); break;
                case "bool": buf.Push("false"); break;
                case "float": buf.Push("0.0"); break;
                case "double": buf.Push("0.0"); break;
                default: buf.Push("null"); break;
            }
            return buf.Push("))");
        }

        public override StringBuffer TranslateArraySet(Expression array, Expression index, Expression value)
        {
            return this.TranslateListSet(array, index, value);
        }

        public override StringBuffer TranslateBase64ToBytes(Expression base64String)
        {
            return StringBuffer
                .Of("self::PST_bytesToIntArray(base64_decode(")
                .Push(this.TranslateExpression(base64String))
                .Push(", true))");
        }

        public override StringBuffer TranslateBase64ToString(Expression base64String)
        {
            return StringBuffer
                .Of("base64_decode(")
                .Push(this.TranslateExpression(base64String))
                .Push(", true)");
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
                .Of("chr(")
                .Push(this.TranslateExpression(charCode))
                .Push(')');
        }

        public override StringBuffer TranslateConstructorInvocation(ConstructorInvocation constructorInvocation)
        {
            if (constructorInvocation.ClassDefinition != null)
            {
                throw new NotImplementedException();
            }
            else
            {
                StringBuffer buf = StringBuffer
                    .Of("new ")
                    .Push(constructorInvocation.StructDefinition.NameToken.Value)
                    .Push('(');
                Expression[] args = constructorInvocation.Args;
                for (int i = 0; i < args.Length; ++i)
                {
                    if (i > 0) buf.Push(", ");
                    buf.Push(this.TranslateExpression(args[i]));
                }
                return buf.Push(')');
            }
        }

        public override StringBuffer TranslateCurrentTimeSeconds()
        {
            return StringBuffer.Of("microtime(true)");
        }

        private StringBuffer TranslateDictionaryKeyExpression(Expression keyExpr)
        {
            if (keyExpr.ResolvedType.RootValue != "int")
            {
                return this.TranslateExpression(keyExpr);
            }

            if (keyExpr is InlineConstant)
            {
                int key = (int)((InlineConstant)keyExpr).Value;
                return StringBuffer
                    .Of("'i")
                    .Push(key + "'");
            }

            return StringBuffer
                .Of("'i'.")
                .Push(this.TranslateExpressionAsString(keyExpr));
        }

        public override StringBuffer TranslateDictionaryContainsKey(Expression dictionary, Expression key)
        {
            return StringBuffer
                .Of("isset(")
                .Push(this.TranslateExpression(dictionary))
                .Push("->arr[")
                .Push(TranslateDictionaryKeyExpression(key))
                .Push("])");
        }

        public override StringBuffer TranslateDictionaryGet(Expression dictionary, Expression key)
        {
            return this.TranslateExpression(dictionary)
                .Push("->arr[")
                .Push(TranslateDictionaryKeyExpression(key))
                .Push(']');
        }

        public override StringBuffer TranslateDictionaryKeys(Expression dictionary)
        {
            return StringBuffer.Of("self::PST_dictGetKeys(")
                .Push(this.TranslateExpression(dictionary))
                .Push(", ")
                .Push(dictionary.ResolvedType.Generics[0].RootValue == "int" ? "true" : "false")
                .Push(')');
        }

        public override StringBuffer TranslateDictionaryNew(PType keyType, PType valueType)
        {
            return StringBuffer.Of("new PastelPtrArray()");
        }

        public override StringBuffer TranslateDictionaryRemove(Expression dictionary, Expression key)
        {
            return StringBuffer
                .Of("unset(")
                .Push(this.TranslateExpression(dictionary))
                .Push("->arr[")
                .Push(TranslateDictionaryKeyExpression(key))
                .Push("])");
        }

        public override StringBuffer TranslateDictionarySet(Expression dictionary, Expression key, Expression value)
        {
            return this.TranslateExpression(dictionary)
                .Push("->arr[")
                .Push(TranslateDictionaryKeyExpression(key))
                .Push("] = ")
                .Push(this.TranslateExpression(value));
        }

        public override StringBuffer TranslateDictionarySize(Expression dictionary)
        {
            return StringBuffer
                .Of("count(")
                .Push(this.TranslateExpression(dictionary))
                .Push("->arr)");
        }

        public override void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut)
        {
            bool keyExpressionIsSimple = key is Variable || key is InlineConstant;
            string keyVar = null;
            sb.Append(sb.CurrentTab);
            if (!keyExpressionIsSimple)
            {
                keyVar = "$_PST_dictKey" + this.transpilerCtx.SwitchCounter++;
                sb.Append(keyVar);
                sb.Append(" = ");
                sb.Append(StringBuffer.Flatten(TranslateDictionaryKeyExpression(key)));
                sb.Append(";\n");
                sb.Append(sb.CurrentTab);
            }

            sb.Append('$');
            sb.Append(varOut.Name);
            sb.Append(" = isset(");
            sb.Append(this.TranslateExpressionAsString(dictionary));
            sb.Append("->arr[");
            if (keyExpressionIsSimple)
            {
                sb.Append(StringBuffer.Flatten(TranslateDictionaryKeyExpression(key)));
            }
            else
            {
                sb.Append(keyVar);
            }
            sb.Append("]) ? ");
            sb.Append(this.TranslateExpressionAsString(dictionary));
            sb.Append("->arr[");
            if (keyExpressionIsSimple)
            {
                sb.Append(StringBuffer.Flatten(TranslateDictionaryKeyExpression(key)));
            }
            else
            {
                sb.Append(keyVar);
            }
            sb.Append("] : (");
            sb.Append(this.TranslateExpressionAsString(fallbackValue));
            sb.Append(");\n");
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
            return StringBuffer
                .Of("((")
                .Push(this.TranslateExpression(floatNumerator))
                .Push(") / (")
                .Push(this.TranslateExpression(floatDenominator))
                .Push("))");
        }

        public override StringBuffer TranslateFloatToInt(Expression floatExpr)
        {
            return StringBuffer
                .Of("intval(")
                .Push(this.TranslateExpression(floatExpr))
                .Push(')');
        }

        public override StringBuffer TranslateFloatToString(Expression floatExpr)
        {
            return StringBuffer
                .Of("('' . (")
                .Push(this.TranslateExpression(floatExpr))
                .Push("))");
        }

        public override StringBuffer TranslateGetFunction(Expression name)
        {
            return StringBuffer
                .Of("TranslationHelper_getFunction(")
                .Push(this.TranslateExpression(name))
                .Push(')');
        }

        public override StringBuffer TranslateInstanceFieldDereference(Expression root, ClassDefinition classDef, string fieldName)
        {
            return this.TranslateExpression(root)
                .Push("->")
                .Push(fieldName);
        }

        public override StringBuffer TranslateIntBuffer16()
        {
            return StringBuffer.Of("(self::PST_intBuffer16)");
        }

        public override StringBuffer TranslateIntegerDivision(Expression integerNumerator, Expression integerDenominator)
        {
            return StringBuffer
                .Of("intval((")
                .Push(this.TranslateExpression(integerNumerator))
                .Push(") / (")
                .Push(this.TranslateExpression(integerDenominator))
                .Push("))");
        }

        public override StringBuffer TranslateIntToString(Expression integer)
        {
            return StringBuffer
                .Of("('' . (")
                .Push(this.TranslateExpression(integer))
                .Push("))");
        }

        public override StringBuffer TranslateIsValidInteger(Expression stringValue)
        {
            return StringBuffer
                .Of("self::PST_isValidInteger(")
                .Push(this.TranslateExpression(stringValue))
                .Push(')');
        }

        public override StringBuffer TranslateListAdd(Expression list, Expression item)
        {
            return StringBuffer
                .Of("array_push(")
                .Push(this.TranslateExpression(list))
                .Push("->arr, ")
                .Push(this.TranslateExpression(item))
                .Push(')');
        }

        public override StringBuffer TranslateListClear(Expression list)
        {
            return this.TranslateExpression(list)
                .Push("->arr = array()");
        }

        public override StringBuffer TranslateListConcat(Expression list, Expression items)
        {
            return StringBuffer
                .Of("pastelWrapList(array_merge(")
                .Push(this.TranslateExpression(list))
                .Push("->arr, ")
                .Push(this.TranslateExpression(items))
                .Push("->arr))");
        }

        public override StringBuffer TranslateListGet(Expression list, Expression index)
        {
            return this.TranslateExpression(list)
                .Push("->arr[")
                .Push(this.TranslateExpression(index))
                .Push(']');
        }

        public override StringBuffer TranslateListInsert(Expression list, Expression index, Expression item)
        {
            return StringBuffer
                .Of("array_splice(")
                .Push(this.TranslateExpression(list))
                .Push("->arr, ")
                .Push(this.TranslateExpression(index))
                .Push(", 0, array(")
                .Push(this.TranslateExpression(item))
                .Push("))");
        }

        public override StringBuffer TranslateListJoinChars(Expression list)
        {
            return StringBuffer
                .Of("implode(")
                .Push(this.TranslateExpression(list))
                .Push("->arr)");
        }

        public override StringBuffer TranslateListJoinStrings(Expression list, Expression sep)
        {
            return StringBuffer
                .Of("implode(")
                .Push(this.TranslateExpression(sep))
                .Push(", ")
                .Push(this.TranslateExpression(list))
                .Push("->arr)");
        }

        public override StringBuffer TranslateListNew(PType type)
        {
            return StringBuffer.Of("new PastelPtrArray()");
        }

        public override StringBuffer TranslateListPop(Expression list)
        {
            return StringBuffer
                .Of("array_pop(")
                .Push(this.TranslateExpression(list))
                .Push("->arr)");
        }

        public override StringBuffer TranslateListRemoveAt(Expression list, Expression index)
        {
            return StringBuffer
                .Of("array_splice(")
                .Push(this.TranslateExpression(list))
                .Push("->arr, ")
                .Push(this.TranslateExpression(index))
                .Push(", 1)");
        }

        public override StringBuffer TranslateListReverse(Expression list)
        {
            return StringBuffer
                .Of("self::PST_reverseArray(")
                .Push(this.TranslateExpression(list))
                .Push(')');
        }

        public override StringBuffer TranslateListSet(Expression list, Expression index, Expression value)
        {
            if (list is Variable)
            {
                return this.TranslateExpression(list)
                    .Push("->arr[")
                    .Push(this.TranslateExpression(index))
                    .Push("] = ")
                    .Push(this.TranslateExpression(value));
            }

            return StringBuffer
                .Of("self::PST_assignIndexHack(")
                .Push(this.TranslateExpression(list))
                .Push(", ")
                .Push(this.TranslateExpression(index))
                .Push(", ")
                .Push(this.TranslateExpression(value))
                .Push(')');
        }

        public override StringBuffer TranslateListShuffle(Expression list)
        {
            return StringBuffer
                .Of("shuffle(")
                .Push(this.TranslateExpression(list))
                .Push("->arr)");
        }

        public override StringBuffer TranslateListSize(Expression list)
        {
            return StringBuffer
                .Of("count(")
                .Push(this.TranslateExpression(list))
                .Push("->arr)");
        }

        public override StringBuffer TranslateListToArray(Expression list)
        {
            return StringBuffer
                .Of("pastelWrapList(")
                .Push(this.TranslateExpression(list))
                .Push("->arr)");
        }

        public override StringBuffer TranslateMathArcCos(Expression ratio)
        {
            return StringBuffer
                .Of("acos(")
                .Push(this.TranslateExpression(ratio))
                .Push(')');
        }

        public override StringBuffer TranslateMathArcSin(Expression ratio)
        {
            return StringBuffer
                .Of("asin(")
                .Push(this.TranslateExpression(ratio))
                .Push(')');
        }

        public override StringBuffer TranslateMathArcTan(Expression yComponent, Expression xComponent)
        {
            return StringBuffer
                .Of("atan2(")
                .Push(this.TranslateExpression(yComponent))
                .Push(", ")
                .Push(this.TranslateExpression(xComponent))
                .Push(')');
        }

        public override StringBuffer TranslateMathCos(Expression thetaRadians)
        {
            return StringBuffer
                .Of("cos(")
                .Push(this.TranslateExpression(thetaRadians))
                .Push(')');
        }

        public override StringBuffer TranslateMathLog(Expression value)
        {
            return StringBuffer
                .Of("log(")
                .Push(this.TranslateExpression(value))
                .Push(')');
        }

        public override StringBuffer TranslateMathPow(Expression expBase, Expression exponent)
        {
            return StringBuffer
                .Of("pow(")
                .Push(this.TranslateExpression(expBase))
                .Push(", ")
                .Push(this.TranslateExpression(exponent))
                .Push(')');
        }

        public override StringBuffer TranslateMathSin(Expression thetaRadians)
        {
            return StringBuffer
                .Of("sin(")
                .Push(this.TranslateExpression(thetaRadians))
                .Push(')');
        }

        public override StringBuffer TranslateMathTan(Expression thetaRadians)
        {
            return StringBuffer
                .Of("tan(")
                .Push(this.TranslateExpression(thetaRadians))
                .Push(')');
        }

        public override StringBuffer TranslateMultiplyList(Expression list, Expression n)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateNullConstant()
        {
            return StringBuffer.Of("null");
        }

        public override StringBuffer TranslateOrd(Expression charValue)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateParseFloatUnsafe(Expression stringValue)
        {
            return StringBuffer
                .Of("floatval(")
                .Push(this.TranslateExpression(stringValue))
                .Push(')');
        }

        public override StringBuffer TranslateParseInt(Expression safeStringValue)
        {
            return StringBuffer
                .Of("intval(")
                .Push(this.TranslateExpression(safeStringValue))
                .Push(')');
        }

        public override StringBuffer TranslateRandomFloat()
        {
            return StringBuffer.Of("(random_int(0, PHP_INT_MAX - 1) / PHP_INT_MAX)");
        }

        public override StringBuffer TranslateSortedCopyOfIntArray(Expression intArray)
        {
            return StringBuffer
                .Of("self::PST_sortedCopyOfIntArray(")
                .Push(this.TranslateExpression(intArray))
                .Push(')');
        }

        public override StringBuffer TranslateSortedCopyOfStringArray(Expression stringArray)
        {
            return StringBuffer
                .Of("self::PST_sortedCopyOfStringArray(")
                .Push(this.TranslateExpression(stringArray))
                .Push(')');
        }

        public override StringBuffer TranslateStringAppend(Expression str1, Expression str2)
        {
            return this.TranslateExpression(str1)
                .Push(" .= ")
                .Push(this.TranslateExpression(str2));
        }

        public override StringBuffer TranslateStringBuffer16()
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringCharAt(Expression str, Expression index)
        {
            return this.TranslateExpression(str)
                .Push('[')
                .Push(this.TranslateExpression(index))
                .Push(']');
        }

        public override StringBuffer TranslateStringCharCodeAt(Expression str, Expression index)
        {
            return StringBuffer
                .Of("chr(")
                .Push(this.TranslateExpression(str))
                .Push('[')
                .Push(this.TranslateExpression(index))
                .Push("])");
        }

        public override StringBuffer TranslateStringCompareIsReverse(Expression str1, Expression str2)
        {
            return StringBuffer
                .Of("(strcmp(")
                .Push(this.TranslateExpression(str1))
                .Push(", ")
                .Push(this.TranslateExpression(str2))
                .Push(") > 0)");
        }

        public override StringBuffer TranslateStringConcatAll(Expression[] strings)
        {
            StringBuffer buf = StringBuffer.Of("implode(array(");
            for (int i = 0; i < strings.Length; ++i)
            {
                if (i > 0) buf.Push(", ");
                buf.Push(this.TranslateExpression(strings[i]));
            }
            return buf.Push("))");
        }

        public override StringBuffer TranslateStringConcatPair(Expression strLeft, Expression strRight)
        {
            return StringBuffer
                .Of("((")
                .Push(this.TranslateExpression(strLeft))
                .Push(") . (")
                .Push(this.TranslateExpression(strRight))
                .Push("))");
        }

        public override StringBuffer TranslateStringContains(Expression haystack, Expression needle)
        {
            return StringBuffer
                .Of("(strpos(")
                .Push(this.TranslateExpression(haystack))
                .Push(", ")
                .Push(this.TranslateExpression(needle))
                .Push(") !== false)");
        }

        public override StringBuffer TranslateStringEndsWith(Expression haystack, Expression needle)
        {
            return StringBuffer
                .Of("self::PST_stringEndsWith(")
                .Push(this.TranslateExpression(haystack))
                .Push(", ")
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
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringIndexOf(Expression haystack, Expression needle)
        {
            return StringBuffer
                .Of("self::PST_stringIndexOf(")
                .Push(this.TranslateExpression(haystack))
                .Push(", ")
                .Push(this.TranslateExpression(needle))
                .Push(", 0)");
        }

        public override StringBuffer TranslateStringIndexOfWithStart(Expression haystack, Expression needle, Expression startIndex)
        {
            return StringBuffer
                .Of("self::PST_stringIndexOf(")
                .Push(this.TranslateExpression(haystack))
                .Push(", ")
                .Push(this.TranslateExpression(needle))
                .Push(", ")
                .Push(this.TranslateExpression(startIndex))
                .Push(")");
        }

        public override StringBuffer TranslateStringLastIndexOf(Expression haystack, Expression needle)
        {
            return StringBuffer
                .Of("self::PST_stringLastIndexOf(")
                .Push(this.TranslateExpression(haystack))
                .Push(", ")
                .Push(this.TranslateExpression(needle))
                .Push(", 0)");
        }

        public override StringBuffer TranslateStringLength(Expression str)
        {
            return StringBuffer
                .Of("strlen(")
                .Push(this.TranslateExpression(str))
                .Push(')');
        }

        public override StringBuffer TranslateStringReplace(Expression haystack, Expression needle, Expression newNeedle)
        {
            return StringBuffer
                .Of("str_replace(")
                .Push(this.TranslateExpression(needle))
                .Push(", ")
                .Push(this.TranslateExpression(newNeedle))
                .Push(", ")
                .Push(this.TranslateExpression(haystack))
                .Push(')');
        }

        public override StringBuffer TranslateStringReverse(Expression str)
        {
            return StringBuffer
                .Of("strrev(")
                .Push(this.TranslateExpression(str))
                .Push(')');
        }

        public override StringBuffer TranslateStringSplit(Expression haystack, Expression needle)
        {
            return StringBuffer
                .Of("pastelWrapList(explode(")
                .Push(this.TranslateExpression(needle))
                .Push(", ")
                .Push(this.TranslateExpression(haystack))
                .Push("))");
        }

        public override StringBuffer TranslateStringStartsWith(Expression haystack, Expression needle)
        {
            return StringBuffer
                .Of("self::PST_stringStartsWith(")
                .Push(this.TranslateExpression(haystack))
                .Push(", ")
                .Push(this.TranslateExpression(needle))
                .Push(')');
        }

        public override StringBuffer TranslateStringSubstring(Expression str, Expression start, Expression length)
        {
            return StringBuffer
                .Of("substr(")
                .Push(this.TranslateExpression(str))
                .Push(", ")
                .Push(this.TranslateExpression(start))
                .Push(", ")
                .Push(this.TranslateExpression(length))
                .Push(')');
        }

        public override StringBuffer TranslateStringSubstringIsEqualTo(Expression haystack, Expression startIndex, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringToLower(Expression str)
        {
            return StringBuffer
                .Of("strtolower(")
                .Push(this.TranslateExpression(str))
                .Push(')');
        }

        public override StringBuffer TranslateStringToUpper(Expression str)
        {
            return StringBuffer
                .Of("strtoupper(")
                .Push(this.TranslateExpression(str))
                .Push(')');
        }

        public override StringBuffer TranslateStringToUtf8Bytes(Expression str)
        {
            return StringBuffer
                .Of("self::PST_stringToUtf8Bytes(")
                .Push(this.TranslateExpression(str))
                .Push(')');
        }

        public override StringBuffer TranslateStringTrim(Expression str)
        {
            return StringBuffer
                .Of("trim(")
                .Push(this.TranslateExpression(str))
                .Push(')');
        }

        public override StringBuffer TranslateStringTrimEnd(Expression str)
        {
            return StringBuffer
                .Of("rtrim(")
                .Push(this.TranslateExpression(str))
                .Push(')');
        }

        public override StringBuffer TranslateStringTrimStart(Expression str)
        {
            return StringBuffer
                .Of("ltrim(")
                .Push(this.TranslateExpression(str))
                .Push(')');
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
            return this.TranslateExpression(left)
                .Push(" === ")
                .Push(this.TranslateExpression(right));
        }

        public override StringBuffer TranslateStructFieldDereference(Expression root, StructDefinition structDef, string fieldName, int fieldIndex)
        {
            return this.TranslateExpression(root)
                .Push("->")
                .Push(fieldName);
        }

        public override StringBuffer TranslateThis(ThisExpression thisExpr)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateToCodeString(Expression str)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateTryParseFloat(Expression stringValue, Expression floatOutList)
        {
            return StringBuffer
                .Of("self::PST_tryParseFloat(")
                .Push(this.TranslateExpression(stringValue))
                .Push(", ")
                .Push(this.TranslateExpression(floatOutList))
                .Push(')');
        }

        public override StringBuffer TranslateUtf8BytesToString(Expression bytes)
        {
            return StringBuffer
                .Of("self::PST_utf8BytesToString(")
                .Push(this.TranslateExpression(bytes))
                .Push(')');
        }

        public override StringBuffer TranslateVariable(Variable variable)
        {
            return StringBuffer
                .Of("$")
                .Push(variable.Name);
        }

        public override void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl)
        {
            sb.Append(sb.CurrentTab);
            sb.Append('$');
            sb.Append(varDecl.VariableNameToken.Value);
            sb.Append(" = ");
            sb.Append(this.TranslateExpressionAsString(varDecl.Value));
            sb.Append(";\n");
        }

        public override void GenerateCodeForFunction(TranspilerContext sb, FunctionDefinition funcDef, bool isStatic)
        {
            sb.Append("\n");
            sb.Append(sb.CurrentTab);
            sb.Append("public static function ");
            sb.Append(funcDef.NameToken.Value);
            sb.Append('(');
            for (int i = 0; i < funcDef.ArgNames.Length; ++i)
            {
                Token arg = funcDef.ArgNames[i];
                if (i > 0) sb.Append(", ");
                sb.Append('$');
                sb.Append(arg.Value);
            }
            sb.Append(") {\n");
            sb.TabDepth++;

            this.TranslateStatements(sb, funcDef.Code);

            sb.TabDepth--;
            sb.Append(sb.CurrentTab);
            sb.Append("}\n\n");
        }

        public override void GenerateCodeForClass(TranspilerContext sb, ClassDefinition classDef)
        {
            throw new NotImplementedException();
        }

        public override void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef)
        {
            string name = structDef.NameToken.Value;
            sb.Append("class ");
            sb.Append(name);
            sb.Append(" {\n");
            sb.TabDepth++;

            string[] localNames = structDef.LocalFieldNames.Select(a => a.Value).ToArray();
            string[] flatNames = structDef.FlatFieldNames.Select(a => a.Value).ToArray();

            foreach (string fieldName in flatNames)
            {
                sb.Append(sb.CurrentTab);
                sb.Append("var $");
                sb.Append(fieldName);
                sb.Append(";\n");
            }
            sb.Append(sb.CurrentTab);
            sb.Append("function __construct(");
            for (int i = 0; i < flatNames.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                sb.Append("$a");
                sb.Append(i);
            }
            sb.Append(") {\n");
            sb.TabDepth++;
            for (int i = 0; i < flatNames.Length; ++i)
            {
                sb.Append(sb.CurrentTab);
                sb.Append("$this->");
                sb.Append(flatNames[i]);
                sb.Append(" = $a");
                sb.Append(i);
                sb.Append(";\n");
            }
            sb.TabDepth--;
            sb.Append(sb.CurrentTab);
            sb.Append("}\n");

            sb.TabDepth--;
            sb.Append(sb.CurrentTab);
            sb.Append("}\n");
        }
    }
}
