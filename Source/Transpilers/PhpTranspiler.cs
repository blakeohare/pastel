using Pastel.Parser;
using Pastel.Parser.ParseNodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Transpilers
{
    internal class PhpTranspiler : CurlyBraceTranspiler
    {
        public PhpTranspiler() : base(true)
        {
            this.HasNewLineAtEndOfFile = false;
            this.HasStructsInSeparateFiles = false;
        }

        public override string CanonicalTab => "\t";

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

        public override void TranslateFunctionInvocation(TranspilerContext sb, FunctionReference funcRef, Expression[] args)
        {
            sb.Append("self::");
            base.TranslateFunctionInvocation(sb, funcRef, args);
        }

        public override void TranslateFunctionPointerInvocation(TranspilerContext sb, FunctionPointerInvocation fpi)
        {
            this.TranslateExpression(sb, fpi.Root);
            sb.Append('(');
            Expression[] args = fpi.Args;
            for (int i = 0; i < args.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                this.TranslateExpression(sb, args[i]);
            }
            sb.Append(')');
        }

        public override void TranslatePrintStdErr(TranspilerContext sb, Expression value)
        {
            throw new NotImplementedException();
        }

        public override void TranslatePrintStdOut(TranspilerContext sb, Expression value)
        {
            sb.Append("echo ");
            this.TranslateExpression(sb, value);
        }

        public override void TranslateArrayGet(TranspilerContext sb, Expression array, Expression index)
        {
            this.TranslateExpression(sb, array);
            sb.Append("->arr[");
            this.TranslateExpression(sb, index);
            sb.Append(']');
        }

        public override void TranslateArrayJoin(TranspilerContext sb, Expression array, Expression sep)
        {
            throw new NotImplementedException();
        }

        public override void TranslateArrayLength(TranspilerContext sb, Expression array)
        {
            sb.Append("count(");
            this.TranslateExpression(sb, array);
            sb.Append("->arr)");
        }

        public override void TranslateArrayNew(TranspilerContext sb, PType arrayType, Expression lengthExpression)
        {
            sb.Append("pastelWrapList(array_fill(0, ");
            this.TranslateExpression(sb, lengthExpression);
            sb.Append(", ");
            switch (arrayType.RootValue)
            {
                case "int": sb.Append("0"); break;
                case "bool": sb.Append("false"); break;
                case "float": sb.Append("0.0"); break;
                case "double": sb.Append("0.0"); break;
                default: sb.Append("null"); break;
            }
            sb.Append("))");
        }

        public override void TranslateArraySet(TranspilerContext sb, Expression array, Expression index, Expression value)
        {
            this.TranslateListSet(sb, array, index, value);
        }

        public override void TranslateBase64ToBytes(TranspilerContext sb, Expression base64String)
        {
            sb.Append("self::PST_bytesToIntArray(base64_decode(");
            this.TranslateExpression(sb, base64String);
            sb.Append(", true))");
        }

        public override void TranslateBase64ToString(TranspilerContext sb, Expression base64String)
        {
            sb.Append("base64_decode(");
            this.TranslateExpression(sb, base64String);
            sb.Append(", true)");
        }

        public override void TranslateCast(TranspilerContext sb, PType type, Expression expression)
        {
            this.TranslateExpression(sb, expression);
        }

        public override void TranslateCharConstant(TranspilerContext sb, char value)
        {
            sb.Append(CodeUtil.ConvertStringValueToCode(value.ToString()));
        }

        public override void TranslateCharToString(TranspilerContext sb, Expression charValue)
        {
            this.TranslateExpression(sb, charValue);
        }

        public override void TranslateChr(TranspilerContext sb, Expression charCode)
        {
            sb.Append("chr(");
            this.TranslateExpression(sb, charCode);
            sb.Append(')');
        }

        public override void TranslateConstructorInvocation(TranspilerContext sb, ConstructorInvocation constructorInvocation)
        {
            if (constructorInvocation.ClassDefinition != null)
            {
                throw new NotImplementedException();
            }
            else
            {
                sb.Append("new ");
                sb.Append(constructorInvocation.StructDefinition.NameToken.Value);
                sb.Append('(');
                Expression[] args = constructorInvocation.Args;
                for (int i = 0; i < args.Length; ++i)
                {
                    if (i > 0) sb.Append(", ");
                    this.TranslateExpression(sb, args[i]);
                }
                sb.Append(')');
            }
        }

        public override void TranslateCurrentTimeSeconds(TranspilerContext sb)
        {
            sb.Append("microtime(true)");
        }

        private void TranslateDictionaryKeyExpression(TranspilerContext sb, Expression keyExpr)
        {
            if (keyExpr.ResolvedType.RootValue == "int")
            {
                if (keyExpr is InlineConstant)
                {
                    sb.Append("'i");
                    int key = (int)((InlineConstant)keyExpr).Value;
                    sb.Append(key);
                    sb.Append("'");
                }
                else
                {
                    sb.Append("'i'.");
                    this.TranslateExpression(sb, keyExpr);
                }
            }
            else
            {
                this.TranslateExpression(sb, keyExpr);
            }
        }

        public override void TranslateDictionaryContainsKey(TranspilerContext sb, Expression dictionary, Expression key)
        {
            sb.Append("isset(");
            this.TranslateExpression(sb, dictionary);
            sb.Append("->arr[");
            TranslateDictionaryKeyExpression(sb, key);
            sb.Append("])");
        }

        public override void TranslateDictionaryGet(TranspilerContext sb, Expression dictionary, Expression key)
        {
            this.TranslateExpression(sb, dictionary);
            sb.Append("->arr[");
            TranslateDictionaryKeyExpression(sb, key);
            sb.Append(']');
        }

        public override void TranslateDictionaryKeys(TranspilerContext sb, Expression dictionary)
        {
            sb.Append("self::PST_dictGetKeys(");
            this.TranslateExpression(sb, dictionary);
            sb.Append(", ");
            sb.Append(dictionary.ResolvedType.Generics[0].RootValue == "int" ? "true" : "false");
            sb.Append(')');
        }

        public override void TranslateDictionaryNew(TranspilerContext sb, PType keyType, PType valueType)
        {
            sb.Append("new PastelPtrArray()");
        }

        public override void TranslateDictionaryRemove(TranspilerContext sb, Expression dictionary, Expression key)
        {
            sb.Append("unset(");
            this.TranslateExpression(sb, dictionary);
            sb.Append("->arr[");
            TranslateDictionaryKeyExpression(sb, key);
            sb.Append("])");
        }

        public override void TranslateDictionarySet(TranspilerContext sb, Expression dictionary, Expression key, Expression value)
        {
            this.TranslateExpression(sb, dictionary);
            sb.Append("->arr[");

            TranslateDictionaryKeyExpression(sb, key);
            sb.Append("] = ");
            this.TranslateExpression(sb, value);
        }

        public override void TranslateDictionarySize(TranspilerContext sb, Expression dictionary)
        {
            sb.Append("count(");
            this.TranslateExpression(sb, dictionary);
            sb.Append("->arr)");
        }

        public override void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut)
        {
            bool keyExpressionIsSimple = key is Variable || key is InlineConstant;
            string keyVar = null;
            sb.Append(sb.CurrentTab);
            if (!keyExpressionIsSimple)
            {
                keyVar = "$_PST_dictKey" + sb.SwitchCounter++;
                sb.Append(keyVar);
                sb.Append(" = ");
                TranslateDictionaryKeyExpression(sb, key);
                sb.Append(";\n");
                sb.Append(sb.CurrentTab);
            }

            sb.Append('$');
            sb.Append(varOut.Name);
            sb.Append(" = isset(");
            this.TranslateExpression(sb, dictionary);
            sb.Append("->arr[");
            if (keyExpressionIsSimple)
            {
                TranslateDictionaryKeyExpression(sb, key);
            }
            else
            {
                sb.Append(keyVar);
            }
            sb.Append("]) ? ");
            this.TranslateExpression(sb, dictionary);
            sb.Append("->arr[");
            if (keyExpressionIsSimple)
            {
                TranslateDictionaryKeyExpression(sb, key);
            }
            else
            {
                sb.Append(keyVar);
            }
            sb.Append("] : (");
            this.TranslateExpression(sb, fallbackValue);
            sb.Append(");\n");
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
            sb.Append("((");
            this.TranslateExpression(sb, floatNumerator);
            sb.Append(") / (");
            this.TranslateExpression(sb, floatDenominator);
            sb.Append("))");
        }

        public override void TranslateFloatToInt(TranspilerContext sb, Expression floatExpr)
        {
            sb.Append("intval(");
            this.TranslateExpression(sb, floatExpr);
            sb.Append(')');
        }

        public override void TranslateFloatToString(TranspilerContext sb, Expression floatExpr)
        {
            sb.Append("('' . (");
            this.TranslateExpression(sb, floatExpr);
            sb.Append("))");
        }

        public override void TranslateGetFunction(TranspilerContext sb, Expression name)
        {
            sb.Append("TranslationHelper_getFunction(");
            this.TranslateExpression(sb, name);
            sb.Append(')');
        }

        public override void TranslateInstanceFieldDereference(TranspilerContext sb, Expression root, ClassDefinition classDef, string fieldName)
        {
            this.TranslateExpression(sb, root);
            sb.Append("->");
            sb.Append(fieldName);
        }

        public override void TranslateIntBuffer16(TranspilerContext sb)
        {
            sb.Append("(self::PST_intBuffer16)");
        }

        public override void TranslateIntegerDivision(TranspilerContext sb, Expression integerNumerator, Expression integerDenominator)
        {
            sb.Append("intval((");
            this.TranslateExpression(sb, integerNumerator);
            sb.Append(") / (");
            this.TranslateExpression(sb, integerDenominator);
            sb.Append("))");
        }

        public override void TranslateIntToString(TranspilerContext sb, Expression integer)
        {
            sb.Append("('' . (");
            this.TranslateExpression(sb, integer);
            sb.Append("))");
        }

        public override void TranslateIsValidInteger(TranspilerContext sb, Expression stringValue)
        {
            sb.Append("self::PST_isValidInteger(");
            this.TranslateExpression(sb, stringValue);
            sb.Append(')');
        }

        public override void TranslateListAdd(TranspilerContext sb, Expression list, Expression item)
        {
            sb.Append("array_push(");
            this.TranslateExpression(sb, list);
            sb.Append("->arr, ");
            this.TranslateExpression(sb, item);
            sb.Append(')');
        }

        public override void TranslateListClear(TranspilerContext sb, Expression list)
        {
            this.TranslateExpression(sb, list);
            sb.Append("->arr = array()");
        }

        public override void TranslateListConcat(TranspilerContext sb, Expression list, Expression items)
        {
            sb.Append("pastelWrapList(array_merge(");
            this.TranslateExpression(sb, list);
            sb.Append("->arr, ");
            this.TranslateExpression(sb, items);
            sb.Append("->arr))");
        }

        public override void TranslateListGet(TranspilerContext sb, Expression list, Expression index)
        {
            this.TranslateExpression(sb, list);
            sb.Append("->arr[");
            this.TranslateExpression(sb, index);
            sb.Append(']');
        }

        public override void TranslateListInsert(TranspilerContext sb, Expression list, Expression index, Expression item)
        {
            sb.Append("array_splice(");
            this.TranslateExpression(sb, list);
            sb.Append("->arr, ");
            this.TranslateExpression(sb, index);
            sb.Append(", 0, array(");
            this.TranslateExpression(sb, item);
            sb.Append("))");
        }

        public override void TranslateListJoinChars(TranspilerContext sb, Expression list)
        {
            sb.Append("implode(");
            this.TranslateExpression(sb, list);
            sb.Append("->arr)");
        }

        public override void TranslateListJoinStrings(TranspilerContext sb, Expression list, Expression sep)
        {
            sb.Append("implode(");
            this.TranslateExpression(sb, sep);
            sb.Append(", ");
            this.TranslateExpression(sb, list);
            sb.Append("->arr)");
        }

        public override void TranslateListNew(TranspilerContext sb, PType type)
        {
            sb.Append("new PastelPtrArray()");
        }

        public override void TranslateListPop(TranspilerContext sb, Expression list)
        {
            sb.Append("array_pop(");
            this.TranslateExpression(sb, list);
            sb.Append("->arr)");
        }

        public override void TranslateListRemoveAt(TranspilerContext sb, Expression list, Expression index)
        {
            sb.Append("array_splice(");
            this.TranslateExpression(sb, list);
            sb.Append("->arr, ");
            this.TranslateExpression(sb, index);
            sb.Append(", 1)");
        }

        public override void TranslateListReverse(TranspilerContext sb, Expression list)
        {
            sb.Append("self::PST_reverseArray(");
            this.TranslateExpression(sb, list);
            sb.Append(')');
        }

        public override void TranslateListSet(TranspilerContext sb, Expression list, Expression index, Expression value)
        {
            if (list is Variable)
            {
                this.TranslateExpression(sb, list);
                sb.Append("->arr[");
                this.TranslateExpression(sb, index);
                sb.Append("] = ");
                this.TranslateExpression(sb, value);
            }
            else
            {
                sb.Append("self::PST_assignIndexHack(");
                this.TranslateExpression(sb, list);
                sb.Append(", ");
                this.TranslateExpression(sb, index);
                sb.Append(", ");
                this.TranslateExpression(sb, value);
                sb.Append(')');
            }
        }

        public override void TranslateListShuffle(TranspilerContext sb, Expression list)
        {
            sb.Append("shuffle(");
            this.TranslateExpression(sb, list);
            sb.Append("->arr)");
        }

        public override void TranslateListSize(TranspilerContext sb, Expression list)
        {
            sb.Append("count(");
            this.TranslateExpression(sb, list);
            sb.Append("->arr)");
        }

        public override void TranslateListToArray(TranspilerContext sb, Expression list)
        {
            sb.Append("pastelWrapList(");
            this.TranslateExpression(sb, list);
            sb.Append("->arr)");
        }

        public override void TranslateMathArcCos(TranspilerContext sb, Expression ratio)
        {
            sb.Append("acos(");
            this.TranslateExpression(sb, ratio);
            sb.Append(')');
        }

        public override void TranslateMathArcSin(TranspilerContext sb, Expression ratio)
        {
            sb.Append("asin(");
            this.TranslateExpression(sb, ratio);
            sb.Append(')');
        }

        public override void TranslateMathArcTan(TranspilerContext sb, Expression yComponent, Expression xComponent)
        {
            sb.Append("atan2(");
            this.TranslateExpression(sb, yComponent);
            sb.Append(", ");
            this.TranslateExpression(sb, xComponent);
            sb.Append(')');
        }

        public override void TranslateMathCos(TranspilerContext sb, Expression thetaRadians)
        {
            sb.Append("cos(");
            this.TranslateExpression(sb, thetaRadians);
            sb.Append(')');
        }

        public override void TranslateMathLog(TranspilerContext sb, Expression value)
        {
            sb.Append("log(");
            this.TranslateExpression(sb, value);
            sb.Append(')');
        }

        public override void TranslateMathPow(TranspilerContext sb, Expression expBase, Expression exponent)
        {
            sb.Append("pow(");
            this.TranslateExpression(sb, expBase);
            sb.Append(", ");
            this.TranslateExpression(sb, exponent);
            sb.Append(')');

        }

        public override void TranslateMathSin(TranspilerContext sb, Expression thetaRadians)
        {
            sb.Append("sin(");
            this.TranslateExpression(sb, thetaRadians);
            sb.Append(')');
        }

        public override void TranslateMathTan(TranspilerContext sb, Expression thetaRadians)
        {
            sb.Append("tan(");
            this.TranslateExpression(sb, thetaRadians);
            sb.Append(')');
        }

        public override void TranslateMultiplyList(TranspilerContext sb, Expression list, Expression n)
        {
            throw new NotImplementedException();
        }

        public override void TranslateNullConstant(TranspilerContext sb)
        {
            sb.Append("null");
        }

        public override void TranslateOrd(TranspilerContext sb, Expression charValue)
        {
            throw new NotImplementedException();
        }

        public override void TranslateParseFloatUnsafe(TranspilerContext sb, Expression stringValue)
        {
            sb.Append("floatval(");
            this.TranslateExpression(sb, stringValue);
            sb.Append(')');
        }

        public override void TranslateParseInt(TranspilerContext sb, Expression safeStringValue)
        {
            sb.Append("intval(");
            this.TranslateExpression(sb, safeStringValue);
            sb.Append(')');
        }

        public override void TranslateRandomFloat(TranspilerContext sb)
        {
            sb.Append("(random_int(0, PHP_INT_MAX - 1) / PHP_INT_MAX)");
        }

        public override void TranslateSortedCopyOfIntArray(TranspilerContext sb, Expression intArray)
        {
            sb.Append("self::PST_sortedCopyOfIntArray(");
            this.TranslateExpression(sb, intArray);
            sb.Append(')');
        }

        public override void TranslateSortedCopyOfStringArray(TranspilerContext sb, Expression stringArray)
        {
            sb.Append("self::PST_sortedCopyOfStringArray(");
            this.TranslateExpression(sb, stringArray);
            sb.Append(')');
        }

        public override void TranslateStringAppend(TranspilerContext sb, Expression str1, Expression str2)
        {
            this.TranslateExpression(sb, str1);
            sb.Append(" .= ");
            this.TranslateExpression(sb, str2);
        }

        public override void TranslateStringBuffer16(TranspilerContext sb)
        {
            throw new NotImplementedException();
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
            sb.Append("chr(");
            this.TranslateExpression(sb, str);
            sb.Append('[');
            this.TranslateExpression(sb, index);
            sb.Append("])");
        }

        public override void TranslateStringCompareIsReverse(TranspilerContext sb, Expression str1, Expression str2)
        {
            sb.Append("(strcmp(");
            this.TranslateExpression(sb, str1);
            sb.Append(", ");
            this.TranslateExpression(sb, str2);
            sb.Append(") > 0)");
        }

        public override void TranslateStringConcatAll(TranspilerContext sb, Expression[] strings)
        {
            sb.Append("implode(array(");
            for (int i = 0; i < strings.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                this.TranslateExpression(sb, strings[i]);
            }
            sb.Append("))");
        }

        public override void TranslateStringConcatPair(TranspilerContext sb, Expression strLeft, Expression strRight)
        {
            sb.Append("((");
            this.TranslateExpression(sb, strLeft);
            sb.Append(") . (");
            this.TranslateExpression(sb, strRight);
            sb.Append("))");
        }

        public override void TranslateStringContains(TranspilerContext sb, Expression haystack, Expression needle)
        {
            sb.Append("(strpos(");
            this.TranslateExpression(sb, haystack);
            sb.Append(", ");
            this.TranslateExpression(sb, needle);
            sb.Append(") !== false)");
        }

        public override void TranslateStringEndsWith(TranspilerContext sb, Expression haystack, Expression needle)
        {
            sb.Append("self::PST_stringEndsWith(");
            this.TranslateExpression(sb, haystack);
            sb.Append(", ");
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
            throw new NotImplementedException();
        }

        public override void TranslateStringIndexOf(TranspilerContext sb, Expression haystack, Expression needle)
        {
            sb.Append("self::PST_stringIndexOf(");
            this.TranslateExpression(sb, haystack);
            sb.Append(", ");
            this.TranslateExpression(sb, needle);
            sb.Append(", 0)");
        }

        public override void TranslateStringIndexOfWithStart(TranspilerContext sb, Expression haystack, Expression needle, Expression startIndex)
        {
            sb.Append("self::PST_stringIndexOf(");
            this.TranslateExpression(sb, haystack);
            sb.Append(", ");
            this.TranslateExpression(sb, needle);
            sb.Append(", ");
            this.TranslateExpression(sb, startIndex);
            sb.Append(")");
        }

        public override void TranslateStringLastIndexOf(TranspilerContext sb, Expression haystack, Expression needle)
        {
            sb.Append("self::PST_stringLastIndexOf(");
            this.TranslateExpression(sb, haystack);
            sb.Append(", ");
            this.TranslateExpression(sb, needle);
            sb.Append(", 0)");
        }

        public override void TranslateStringLength(TranspilerContext sb, Expression str)
        {
            sb.Append("strlen(");
            this.TranslateExpression(sb, str);
            sb.Append(')');
        }

        public override void TranslateStringReplace(TranspilerContext sb, Expression haystack, Expression needle, Expression newNeedle)
        {
            sb.Append("str_replace(");
            this.TranslateExpression(sb, needle);
            sb.Append(", ");
            this.TranslateExpression(sb, newNeedle);
            sb.Append(", ");
            this.TranslateExpression(sb, haystack);
            sb.Append(')');
        }

        public override void TranslateStringReverse(TranspilerContext sb, Expression str)
        {
            sb.Append("strrev(");
            this.TranslateExpression(sb, str);
            sb.Append(')');
        }

        public override void TranslateStringSplit(TranspilerContext sb, Expression haystack, Expression needle)
        {
            sb.Append("pastelWrapList(explode(");
            this.TranslateExpression(sb, needle);
            sb.Append(", ");
            this.TranslateExpression(sb, haystack);
            sb.Append("))");
        }

        public override void TranslateStringStartsWith(TranspilerContext sb, Expression haystack, Expression needle)
        {
            sb.Append("self::PST_stringStartsWith(");
            this.TranslateExpression(sb, haystack);
            sb.Append(", ");
            this.TranslateExpression(sb, needle);
            sb.Append(')');
        }

        public override void TranslateStringSubstring(TranspilerContext sb, Expression str, Expression start, Expression length)
        {
            sb.Append("substr(");
            this.TranslateExpression(sb, str);
            sb.Append(", ");
            this.TranslateExpression(sb, start);
            sb.Append(", ");
            this.TranslateExpression(sb, length);
            sb.Append(')');
        }

        public override void TranslateStringSubstringIsEqualTo(TranspilerContext sb, Expression haystack, Expression startIndex, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringToLower(TranspilerContext sb, Expression str)
        {
            sb.Append("strtolower(");
            this.TranslateExpression(sb, str);
            sb.Append(')');
        }

        public override void TranslateStringToUpper(TranspilerContext sb, Expression str)
        {
            sb.Append("strtoupper(");
            this.TranslateExpression(sb, str);
            sb.Append(')');
        }

        public override void TranslateStringToUtf8Bytes(TranspilerContext sb, Expression str)
        {
            sb.Append("self::PST_stringToUtf8Bytes(");
            this.TranslateExpression(sb, str);
            sb.Append(')');
        }

        public override void TranslateStringTrim(TranspilerContext sb, Expression str)
        {
            sb.Append("trim(");
            this.TranslateExpression(sb, str);
            sb.Append(')');
        }

        public override void TranslateStringTrimEnd(TranspilerContext sb, Expression str)
        {
            sb.Append("rtrim(");
            this.TranslateExpression(sb, str);
            sb.Append(')');
        }

        public override void TranslateStringTrimStart(TranspilerContext sb, Expression str)
        {
            sb.Append("ltrim(");
            this.TranslateExpression(sb, str);
            sb.Append(')');
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
            this.TranslateExpression(sb, left);
            sb.Append(" === ");
            this.TranslateExpression(sb, right);
        }

        public override void TranslateStructFieldDereference(TranspilerContext sb, Expression root, StructDefinition structDef, string fieldName, int fieldIndex)
        {
            this.TranslateExpression(sb, root);
            sb.Append("->");
            sb.Append(fieldName);
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
            sb.Append("self::PST_tryParseFloat(");
            this.TranslateExpression(sb, stringValue);
            sb.Append(", ");
            this.TranslateExpression(sb, floatOutList);
            sb.Append(')');
        }

        public override void TranslateUtf8BytesToString(TranspilerContext sb, Expression bytes)
        {
            sb.Append("self::PST_utf8BytesToString(");
            this.TranslateExpression(sb, bytes);
            sb.Append(')');
        }

        public override void TranslateVariable(TranspilerContext sb, Variable variable)
        {
            sb.Append('$');
            sb.Append(variable.Name);
        }

        public override void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl)
        {
            sb.Append(sb.CurrentTab);
            sb.Append('$');
            sb.Append(varDecl.VariableNameToken.Value);
            sb.Append(" = ");
            this.TranslateExpression(sb, varDecl.Value);
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

            this.TranslateExecutables(sb, funcDef.Code);

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
