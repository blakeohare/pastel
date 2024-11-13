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
                .Push(base.TranslateFunctionInvocation(funcRef, args))
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateFunctionPointerInvocation(FunctionPointerInvocation fpi)
        {
            StringBuffer buf = this.TranslateExpression(fpi.Root)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("(");
            Expression[] args = fpi.Args;
            for (int i = 0; i < args.Length; ++i)
            {
                if (i > 0) buf.Push(", ");
                buf.Push(this.TranslateExpression(args[i]));
            }
            return buf
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
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
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("->arr[")
                .Push(this.TranslateExpression(index))
                .Push("]")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateArrayJoin(Expression array, Expression sep)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateArrayLength(Expression array)
        {
            return StringBuffer
                .Of("count(")
                .Push(this.TranslateExpression(array).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
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
            return buf
                .Push("))")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
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
                .Push(", true))")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateBase64ToString(Expression base64String)
        {
            return StringBuffer
                .Of("base64_decode(")
                .Push(this.TranslateExpression(base64String))
                .Push(", true)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateCast(PType type, Expression expression)
        {
            return this.TranslateExpression(expression);
        }

        public override StringBuffer TranslateCharConstant(char value)
        {
            return StringBuffer
                .Of(CodeUtil.ConvertStringValueToCode(value.ToString()))
                .WithTightness(ExpressionTightness.ATOMIC);
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
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateConstructorInvocation(ConstructorInvocation constructorInvocation)
        {
            StringBuffer buf = StringBuffer
                .Of("new ")
                .Push(constructorInvocation.StructDefinition.NameToken.Value)
                .Push("(");
            Expression[] args = constructorInvocation.Args;
            for (int i = 0; i < args.Length; ++i)
            {
                if (i > 0) buf.Push(", ");
                buf.Push(this.TranslateExpression(args[i]));
            }
            return buf
                .Push(")")
                // The PHP 'new' keyword has a slightly weaker operator precedence than other curly brace languages
                .WithTightness(ExpressionTightness.UNARY_PREFIX);
        }

        public override StringBuffer TranslateCurrentTimeSeconds()
        {
            return StringBuffer
                .Of("microtime(true)")
                .WithTightness(ExpressionTightness.ATOMIC);
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
                    .Push(key + "'")
                    .WithTightness(ExpressionTightness.ATOMIC);
            }

            return StringBuffer
                .Of("'i' . ")
                // Technically this should be PHP_STRING_CONCAT instead of ADDITION, but 'i' . a + b just looks
                // really weird instead of 'i' . (a * b)
                .Push(this.TranslateExpression(keyExpr).EnsureGreaterTightness(ExpressionTightness.MULTIPLICATION))
                .WithTightness(ExpressionTightness.PHP_STRING_CONCAT);
        }

        public override StringBuffer TranslateDictionaryContainsKey(Expression dictionary, Expression key)
        {
            return StringBuffer
                .Of("isset(")
                .Push(this.TranslateExpression(dictionary).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push("->arr[")
                .Push(TranslateDictionaryKeyExpression(key))
                .Push("])")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryGet(Expression dictionary, Expression key)
        {
            return this.TranslateExpression(dictionary)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("->arr[")
                .Push(TranslateDictionaryKeyExpression(key))
                .Push("]")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryKeys(Expression dictionary)
        {
            return StringBuffer.Of("self::PST_dictGetKeys(")
                .Push(this.TranslateExpression(dictionary))
                .Push(", ")
                .Push(dictionary.ResolvedType.Generics[0].RootValue == "int" ? "true" : "false")
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryNew(PType keyType, PType valueType)
        {
            return StringBuffer
                .Of("new PastelPtrArray()")
                .WithTightness(ExpressionTightness.UNARY_PREFIX);
        }

        public override StringBuffer TranslateDictionaryRemove(Expression dictionary, Expression key)
        {
            return StringBuffer
                .Of("unset(")
                .Push(this.TranslateExpression(dictionary))
                .Push("->arr[")
                .Push(TranslateDictionaryKeyExpression(key))
                .Push("])")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionarySet(Expression dictionary, Expression key, Expression value)
        {
            return this.TranslateExpression(dictionary)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("->arr[")
                .Push(TranslateDictionaryKeyExpression(key))
                .Push("] = ")
                .Push(this.TranslateExpression(value));
        }

        public override StringBuffer TranslateDictionarySize(Expression dictionary)
        {
            return StringBuffer
                .Of("count(")
                .Push(this.TranslateExpression(dictionary).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push("->arr)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
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
                sb.Append(this.TranslateDictionaryKeyExpression(key).Flatten());
                sb.Append(";\n");
                sb.Append(sb.CurrentTab);
            }

            sb.Append('$');
            sb.Append(varOut.Name);
            sb.Append(" = isset(");
            sb.Append(this.TranslateExpression(dictionary).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE).Flatten());
            sb.Append("->arr[");
            if (keyExpressionIsSimple)
            {
                sb.Append(this.TranslateDictionaryKeyExpression(key).Flatten());
            }
            else
            {
                sb.Append(keyVar);
            }
            sb.Append("]) ? ");
            sb.Append(this.TranslateExpression(dictionary).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE).Flatten());
            sb.Append("->arr[");
            if (keyExpressionIsSimple)
            {
                sb.Append(this.TranslateDictionaryKeyExpression(key).Flatten());
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
            return this.TranslateExpression(floatNumerator)
                .EnsureTightness(ExpressionTightness.MULTIPLICATION)
                .Push(" / ")
                .Push(this.TranslateExpression(floatDenominator).EnsureGreaterTightness(ExpressionTightness.MULTIPLICATION));
        }

        public override StringBuffer TranslateFloatToInt(Expression floatExpr)
        {
            return StringBuffer
                .Of("intval(")
                .Push(this.TranslateExpression(floatExpr))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateFloatToString(Expression floatExpr)
        {
            return this.TranslateExpression(floatExpr)
                // not correct, just an aesthetic choice to over-insert to make it not look weird
                .EnsureGreaterTightness(ExpressionTightness.MULTIPLICATION)
                .Push(" . ''")
                .WithTightness(ExpressionTightness.PHP_STRING_CONCAT);
        }

        public override StringBuffer TranslateGetFunction(Expression name)
        {
            return StringBuffer
                .Of("TranslationHelper_getFunction(")
                .Push(this.TranslateExpression(name))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateIntBuffer16()
        {
            return StringBuffer
                .Of("self::PST_intBuffer16")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateIntegerDivision(Expression integerNumerator, Expression integerDenominator)
        {
            return StringBuffer
                .Of("intval(")
                .Push(this.TranslateExpression(integerNumerator).EnsureTightness(ExpressionTightness.MULTIPLICATION))
                .Push(" / ")
                .Push(this.TranslateExpression(integerDenominator).EnsureGreaterTightness(ExpressionTightness.MULTIPLICATION))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateIntToString(Expression integer)
        {
            return this.TranslateFloatToString(integer); // same
        }

        public override StringBuffer TranslateIsValidInteger(Expression stringValue)
        {
            return StringBuffer
                .Of("self::PST_isValidInteger(")
                .Push(this.TranslateExpression(stringValue))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListAdd(Expression list, Expression item)
        {
            return StringBuffer
                .Of("array_push(")
                .Push(this.TranslateExpression(list).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push("->arr, ")
                .Push(this.TranslateExpression(item))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListClear(Expression list)
        {
            return this.TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("->arr = array()");
        }

        public override StringBuffer TranslateListConcat(Expression list, Expression items)
        {
            return StringBuffer
                .Of("pastelWrapList(array_merge(")
                .Push(this.TranslateExpression(list).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push("->arr, ")
                .Push(this.TranslateExpression(items).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push("->arr))");
        }

        public override StringBuffer TranslateListGet(Expression list, Expression index)
        {
            return this.TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("->arr[")
                .Push(this.TranslateExpression(index))
                .Push("]")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListInsert(Expression list, Expression index, Expression item)
        {
            return StringBuffer
                .Of("array_splice(")
                .Push(this.TranslateExpression(list).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push("->arr, ")
                .Push(this.TranslateExpression(index))
                .Push(", 0, array(")
                .Push(this.TranslateExpression(item))
                .Push("))")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListJoinChars(Expression list)
        {
            return StringBuffer
                .Of("implode(")
                .Push(this.TranslateExpression(list))
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("->arr)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListJoinStrings(Expression list, Expression sep)
        {
            return StringBuffer
                .Of("implode(")
                .Push(this.TranslateExpression(sep))
                .Push(", ")
                .Push(this.TranslateExpression(list).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push("->arr)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListNew(PType type)
        {
            return StringBuffer
                .Of("new PastelPtrArray()")
                .WithTightness(ExpressionTightness.UNARY_PREFIX);
        }

        public override StringBuffer TranslateListPop(Expression list)
        {
            return StringBuffer
                .Of("array_pop(")
                .Push(this.TranslateExpression(list).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push("->arr)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListRemoveAt(Expression list, Expression index)
        {
            return StringBuffer
                .Of("array_splice(")
                .Push(this.TranslateExpression(list).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push("->arr, ")
                .Push(this.TranslateExpression(index))
                .Push(", 1)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListReverse(Expression list)
        {
            return StringBuffer
                .Of("self::PST_reverseArray(")
                .Push(this.TranslateExpression(list))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListSet(Expression list, Expression index, Expression value)
        {
            if (list is Variable)
            {
                return this.TranslateExpression(list)
                    .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
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
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListShuffle(Expression list)
        {
            return StringBuffer
                .Of("shuffle(")
                .Push(this.TranslateExpression(list).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push("->arr)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListSize(Expression list)
        {
            return StringBuffer
                .Of("count(")
                .Push(this.TranslateExpression(list).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push("->arr)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListToArray(Expression list)
        {
            return StringBuffer
                .Of("pastelWrapList(")
                .Push(this.TranslateExpression(list).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push("->arr)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathArcCos(Expression ratio)
        {
            return StringBuffer
                .Of("acos(")
                .Push(this.TranslateExpression(ratio))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathArcSin(Expression ratio)
        {
            return StringBuffer
                .Of("asin(")
                .Push(this.TranslateExpression(ratio))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathArcTan(Expression yComponent, Expression xComponent)
        {
            return StringBuffer
                .Of("atan2(")
                .Push(this.TranslateExpression(yComponent))
                .Push(", ")
                .Push(this.TranslateExpression(xComponent))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathCos(Expression thetaRadians)
        {
            return StringBuffer
                .Of("cos(")
                .Push(this.TranslateExpression(thetaRadians))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathLog(Expression value)
        {
            return StringBuffer
                .Of("log(")
                .Push(this.TranslateExpression(value))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathPow(Expression expBase, Expression exponent)
        {
            return StringBuffer
                .Of("pow(")
                .Push(this.TranslateExpression(expBase))
                .Push(", ")
                .Push(this.TranslateExpression(exponent))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathSin(Expression thetaRadians)
        {
            return StringBuffer
                .Of("sin(")
                .Push(this.TranslateExpression(thetaRadians))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathTan(Expression thetaRadians)
        {
            return StringBuffer
                .Of("tan(")
                .Push(this.TranslateExpression(thetaRadians))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
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
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateParseInt(Expression safeStringValue)
        {
            return StringBuffer
                .Of("intval(")
                .Push(this.TranslateExpression(safeStringValue))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateRandomFloat()
        {
            return StringBuffer
                .Of("random_int(0, PHP_INT_MAX - 1) / PHP_INT_MAX")
                .WithTightness(ExpressionTightness.MULTIPLICATION);
        }

        public override StringBuffer TranslateSortedCopyOfIntArray(Expression intArray)
        {
            return StringBuffer
                .Of("self::PST_sortedCopyOfIntArray(")
                .Push(this.TranslateExpression(intArray))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateSortedCopyOfStringArray(Expression stringArray)
        {
            return StringBuffer
                .Of("self::PST_sortedCopyOfStringArray(")
                .Push(this.TranslateExpression(stringArray))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
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
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("[")
                .Push(this.TranslateExpression(index))
                .Push("]")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringCharCodeAt(Expression str, Expression index)
        {
            return StringBuffer
                .Of("chr(")
                .Push(this.TranslateExpression(str).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push("[")
                .Push(this.TranslateExpression(index))
                .Push("])")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringCompareIsReverse(Expression str1, Expression str2)
        {
            return StringBuffer
                .Of("strcmp(")
                .Push(this.TranslateExpression(str1))
                .Push(", ")
                .Push(this.TranslateExpression(str2))
                .Push(") > 0")
                .WithTightness(ExpressionTightness.INEQUALITY);
        }

        public override StringBuffer TranslateStringConcatAll(Expression[] strings)
        {
            StringBuffer buf = StringBuffer.Of("implode(array(");
            for (int i = 0; i < strings.Length; ++i)
            {
                if (i > 0) buf.Push(", ");
                buf.Push(this.TranslateExpression(strings[i]));
            }
            return buf
                .Push("))")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringConcatPair(Expression strLeft, Expression strRight)
        {
            return this.TranslateExpression(strLeft)
                .EnsureTightness(ExpressionTightness.PHP_STRING_CONCAT)
                .Push(" . ")
                .Push(this.TranslateExpression(strRight).EnsureGreaterTightness(ExpressionTightness.PHP_STRING_CONCAT))
                .WithTightness(ExpressionTightness.PHP_STRING_CONCAT);
        }

        public override StringBuffer TranslateStringContains(Expression haystack, Expression needle)
        {
            return StringBuffer
                .Of("strpos(")
                .Push(this.TranslateExpression(haystack))
                .Push(", ")
                .Push(this.TranslateExpression(needle))
                .Push(") !== false")
                .WithTightness(ExpressionTightness.EQUALITY);
        }

        public override StringBuffer TranslateStringEndsWith(Expression haystack, Expression needle)
        {
            return StringBuffer
                .Of("self::PST_stringEndsWith(")
                .Push(this.TranslateExpression(haystack))
                .Push(", ")
                .Push(this.TranslateExpression(needle))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringEquals(Expression left, Expression right)
        {
            return this.TranslateExpression(left)
                .EnsureGreaterTightness(ExpressionTightness.EQUALITY)
                .Push(" === ")
                .Push(this.TranslateExpression(right).EnsureGreaterTightness(ExpressionTightness.EQUALITY))
                .WithTightness(ExpressionTightness.EQUALITY);
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
                .Push(", 0)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
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
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringLastIndexOf(Expression haystack, Expression needle)
        {
            return StringBuffer
                .Of("self::PST_stringLastIndexOf(")
                .Push(this.TranslateExpression(haystack))
                .Push(", ")
                .Push(this.TranslateExpression(needle))
                .Push(", 0)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringLength(Expression str)
        {
            return StringBuffer
                .Of("strlen(")
                .Push(this.TranslateExpression(str))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
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
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringReverse(Expression str)
        {
            return StringBuffer
                .Of("strrev(")
                .Push(this.TranslateExpression(str))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringSplit(Expression haystack, Expression needle)
        {
            return StringBuffer
                .Of("pastelWrapList(explode(")
                .Push(this.TranslateExpression(needle))
                .Push(", ")
                .Push(this.TranslateExpression(haystack))
                .Push("))")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringStartsWith(Expression haystack, Expression needle)
        {
            return StringBuffer
                .Of("self::PST_stringStartsWith(")
                .Push(this.TranslateExpression(haystack))
                .Push(", ")
                .Push(this.TranslateExpression(needle))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
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
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
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
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringToUpper(Expression str)
        {
            return StringBuffer
                .Of("strtoupper(")
                .Push(this.TranslateExpression(str))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringToUtf8Bytes(Expression str)
        {
            return StringBuffer
                .Of("self::PST_stringToUtf8Bytes(")
                .Push(this.TranslateExpression(str))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringTrim(Expression str)
        {
            return StringBuffer
                .Of("trim(")
                .Push(this.TranslateExpression(str))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringTrimEnd(Expression str)
        {
            return StringBuffer
                .Of("rtrim(")
                .Push(this.TranslateExpression(str))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringTrimStart(Expression str)
        {
            return StringBuffer
                .Of("ltrim(")
                .Push(this.TranslateExpression(str))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
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
                .EnsureGreaterTightness(ExpressionTightness.EQUALITY)
                .Push(" === ")
                .Push(this.TranslateExpression(right).EnsureGreaterTightness(ExpressionTightness.EQUALITY))
                .WithTightness(ExpressionTightness.EQUALITY);
        }

        public override StringBuffer TranslateStructFieldDereference(Expression root, StructDefinition structDef, string fieldName, int fieldIndex)
        {
            return this.TranslateExpression(root)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("->")
                .Push(fieldName)
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
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
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateUtf8BytesToString(Expression bytes)
        {
            return StringBuffer
                .Of("self::PST_utf8BytesToString(")
                .Push(this.TranslateExpression(bytes))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateVariable(Variable variable)
        {
            return StringBuffer
                .Of("$")
                .Push(variable.Name)
                .WithTightness(ExpressionTightness.ATOMIC);
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
