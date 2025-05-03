using Pastel.Parser;
using Pastel.Parser.ParseNodes;
using System;

namespace Pastel.Transpilers.Python
{
    internal class PythonExpressionTranslator : AbstractExpressionTranslator
    {
        public PythonExpressionTranslator(TranspilerContext ctx)
            : base(ctx)
        { }

        private string TranslateOp(string originalOp)
        {
            switch (originalOp)
            {
                case "&&": return "and";
                case "||": return "or";
                default: return originalOp;
            }
        }

        private ExpressionTightness GetOpTightness(string op)
        {
            switch (op)
            {
                case "&&": return ExpressionTightness.PYTHON_AND;
                case "||": return ExpressionTightness.PYTHON_OR;
                case "&": return ExpressionTightness.PYTHON_BITWISE_AND;
                case "|": return ExpressionTightness.PYTHON_BITWISE_OR;
                case "^": return ExpressionTightness.PYTHON_BITWISE_XOR;
                case "+":
                case "-":
                    return ExpressionTightness.ADDITION;
                case "*":
                case "/":
                case "%":
                    return ExpressionTightness.MULTIPLICATION;
                case "<":
                case "<=":
                case ">":
                case ">=":
                case "==":
                case "!=":
                    return ExpressionTightness.PYTHON_COMPARE;
                default:
                    return ExpressionTightness.UNKNOWN;
            }
        }

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
            return TranslateExpression(sep)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".join(")
                .Push(TranslateExpression(array))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateArrayLength(Expression array)
        {
            return StringBuffer
                .Of("len(")
                .Push(TranslateExpression(array))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateArrayNew(PType arrayType, Expression lengthExpression)
        {
            if (lengthExpression is InlineConstant)
            {
                InlineConstant ic = (InlineConstant)lengthExpression;
                int length = (int)ic.Value;
                switch (length)
                {
                    case 0: return StringBuffer.Of("[]").WithTightness(ExpressionTightness.ATOMIC);
                    case 1: return StringBuffer.Of("[None]").WithTightness(ExpressionTightness.ATOMIC);
                    case 2: return StringBuffer.Of("[None, None]").WithTightness(ExpressionTightness.ATOMIC);
                    default: break;
                }
            }

            return StringBuffer
                .Of("PST_NoneListOfOne * ")
                .Push(TranslateExpression(lengthExpression).EnsureGreaterTightness(ExpressionTightness.MULTIPLICATION))
                .WithTightness(ExpressionTightness.MULTIPLICATION);
        }

        public override StringBuffer TranslateArraySet(Expression array, Expression index, Expression value)
        {
            return TranslateExpression(array)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("[")
                .Push(TranslateExpression(index))
                .Push("] = ")
                .Push(TranslateExpression(value));
        }

        public override StringBuffer TranslateBase64ToBytes(Expression base64String)
        {
            this.MarkFeatureAsUsed("IMPORT:base64");
            return StringBuffer
                .Of("list(base64.b64decode(")
                .Push(this.TranslateExpression(base64String))
                .Push("))")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateBase64ToString(Expression base64String)
        {
            this.MarkFeatureAsUsed("IMPORT:base64");
            return StringBuffer
                .Of("base64.b64decode(")
                .Push(this.TranslateExpression(base64String))
                .Push(").decode('utf-8')")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateBooleanConstant(bool value)
        {
            return StringBuffer
                .Of(value ? "True" : "False")
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateBooleanNot(UnaryOp unaryOp)
        {
            return StringBuffer
                .Of("not ")
                .Push(this.TranslateExpression(unaryOp.Expression).EnsureTightness(ExpressionTightness.PYTHON_NOT))
                .WithTightness(ExpressionTightness.PYTHON_NOT);
        }

        public override StringBuffer TranslateBytesToBase64(Expression byteArr)
        {
            this.MarkFeatureAsUsed("IMPORT:base64");
            return StringBuffer
                .Of("PST_bytesToBase64(")
                .Push(this.TranslateExpression(byteArr))
                .Push(")")
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
            return TranslateExpression(charValue);
        }

        public override StringBuffer TranslateChr(Expression charCode)
        {
            return StringBuffer
                .Of("chr(")
                .Push(TranslateExpression(charCode))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateConstructorInvocation(ConstructorInvocation constructorInvocation)
        {
            StructDefinition structDef = constructorInvocation.StructDefinition;
            StringBuffer buf = StringBuffer.Of("[");
            int args = structDef.FieldNames.Length;
            for (int i = 0; i < args; ++i)
            {
                if (i > 0) buf.Push(", ");
                buf.Push(TranslateExpression(constructorInvocation.Args[i]));
            }
            return buf
                .Push("]")
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateCurrentTimeSeconds()
        {
            this.MarkFeatureAsUsed("IMPORT:time");
            return StringBuffer
                .Of("time.time()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryContainsKey(Expression dictionary, Expression key)
        {
            return TranslateExpression(key)
                .EnsureTightness(ExpressionTightness.PYTHON_COMPARE)
                .Push(" in ")
                .Push(TranslateExpression(dictionary).EnsureGreaterTightness(ExpressionTightness.PYTHON_COMPARE))
                .WithTightness(ExpressionTightness.PYTHON_COMPARE);
        }

        public override StringBuffer TranslateDictionaryGet(Expression dictionary, Expression key)
        {
            return TranslateExpression(dictionary)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("[")
                .Push(TranslateExpression(key))
                .Push("]")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryKeys(Expression dictionary)
        {
            return StringBuffer
                .Of("list(")
                .Push(TranslateExpression(dictionary))
                .Push(".keys())")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryNew(PType keyType, PType valueType)
        {
            return StringBuffer
                .Of("{}")
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateDictionaryRemove(Expression dictionary, Expression key)
        {
            return TranslateExpression(dictionary)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".pop(")
                .Push(TranslateExpression(key))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionarySet(Expression dictionary, Expression key, Expression value)
        {
            return TranslateExpression(dictionary)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("[")
                .Push(TranslateExpression(key))
                .Push("] = ")
                .Push(TranslateExpression(value));
        }

        public override StringBuffer TranslateDictionarySize(Expression dictionary)
        {
            return StringBuffer
                .Of("len(")
                .Push(TranslateExpression(dictionary))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryValues(Expression dictionary)
        {
            return StringBuffer
                .Of("list(")
                .Push(TranslateExpression(dictionary).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push(".values())")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateEmitComment(string value)
        {
            return StringBuffer
                .Of("# ")
                .Push(value);
        }

        public override StringBuffer TranslateExtensibleCallbackInvoke(Expression name, Expression argsArray)
        {
            return StringBuffer.Of("PST_ExtCallbacks[")
                .Push(this.TranslateExpression(name))
                .Push("](")
                .Push(this.TranslateExpression(argsArray))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateFloatBuffer16()
        {
            return StringBuffer
                .Of("PST_FloatBuffer16")
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateFloatConstant(double value)
        {
            return StringBuffer
                .Of(CodeUtil.FloatToString(value))
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateFloatDivision(Expression floatNumerator, Expression floatDenominator)
        {
            return TranslateExpression(floatNumerator)
                .EnsureTightness(ExpressionTightness.MULTIPLICATION)
                .Push(" / ")
                .Push(TranslateExpression(floatDenominator).EnsureGreaterTightness(ExpressionTightness.MULTIPLICATION))
                .WithTightness(ExpressionTightness.MULTIPLICATION);
        }

        public override StringBuffer TranslateFloatToInt(Expression floatExpr)
        {
            return StringBuffer
                .Of("int(")
                .Push(TranslateExpression(floatExpr))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateFloatToString(Expression floatExpr)
        {
            return StringBuffer
                .Of("str(")
                .Push(TranslateExpression(floatExpr))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateFunctionInvocation(FunctionReference funcRef, Expression[] args)
        {
            StringBuffer buf = TranslateFunctionReference(funcRef)
                .Push("(");
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0) buf.Push(", ");
                buf.Push(this.TranslateExpression(args[i]));
            }
            return buf
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateFunctionReference(FunctionReference funcRef)
        {
            return StringBuffer
                .Of(funcRef.Function.NameToken.Value)
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateGetFunction(Expression name)
        {
            return StringBuffer.Of("TranslationHelper_getFunction(")
                .Push(TranslateExpression(name))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateInlineIncrement(Expression innerExpression, bool isPrefix, bool isAddition)
        {
            throw new ParserException(
                innerExpression.FirstToken,
                "Python does not support ++ or --. Please check all usages with if (@ext_boolean(\"HAS_INCREMENT\")) { ... }");
        }

        public override StringBuffer TranslateIntBuffer16()
        {
            return StringBuffer
                .Of("PST_IntBuffer16")
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateIntegerConstant(int value)
        {
            return StringBuffer
                .Of(value.ToString())
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateIntegerDivision(Expression integerNumerator, Expression integerDenominator)
        {
            return TranslateExpression(integerNumerator)
                .EnsureTightness(ExpressionTightness.MULTIPLICATION)
                .Push(" // ")
                .Push(TranslateExpression(integerDenominator).EnsureGreaterTightness(ExpressionTightness.MULTIPLICATION))
                .WithTightness(ExpressionTightness.MULTIPLICATION);
        }

        public override StringBuffer TranslateIntToString(Expression integer)
        {
            return StringBuffer
                .Of("str(")
                .Push(TranslateExpression(integer))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateIsValidInteger(Expression stringValue)
        {
            return StringBuffer
                .Of("PST_isValidInteger(")
                .Push(TranslateExpression(stringValue))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListAdd(Expression list, Expression item)
        {
            return TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".append(")
                .Push(TranslateExpression(item))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListClear(Expression list)
        {
            return StringBuffer
                .Of("del ")
                .Push(TranslateExpression(list).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push("[:]")
                .WithTightness(ExpressionTightness.UNARY_PREFIX);
        }

        public override StringBuffer TranslateListConcat(Expression list, Expression items)
        {
            return TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.ADDITION)
                .Push(" + ")
                .Push(TranslateExpression(items).EnsureGreaterTightness(ExpressionTightness.ADDITION))
                .WithTightness(ExpressionTightness.ADDITION);
        }

        public override StringBuffer TranslateListGet(Expression list, Expression index)
        {
            return TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("[")
                .Push(TranslateExpression(index))
                .Push("]")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListInsert(Expression list, Expression index, Expression item)
        {
            return TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".insert(")
                .Push(TranslateExpression(index))
                .Push(", ")
                .Push(TranslateExpression(item))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListJoinChars(Expression list)
        {
            return StringBuffer
                .Of("''.join(")
                .Push(TranslateExpression(list))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListJoinStrings(Expression list, Expression sep)
        {
            return TranslateExpression(sep)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".join(")
                .Push(TranslateExpression(list))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListNew(PType type)
        {
            return StringBuffer
                .Of("[]")
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateListPop(Expression list)
        {
            return TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".pop()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListRemoveAt(Expression list, Expression index)
        {
            return StringBuffer
                .Of("del ")
                .Push(TranslateExpression(list).EnsureTightness(ExpressionTightness.UNARY_PREFIX))
                .Push("[")
                .Push(TranslateExpression(index))
                .Push("]")
                .WithTightness(ExpressionTightness.UNARY_PREFIX);
        }

        public override StringBuffer TranslateListReverse(Expression list)
        {
            return TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".reverse()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListSet(Expression list, Expression index, Expression value)
        {
            return TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("[")
                .Push(TranslateExpression(index))
                .Push("] = ")
                .Push(TranslateExpression(value));
        }

        public override StringBuffer TranslateListShuffle(Expression list)
        {
            return StringBuffer
                .Of("random.shuffle(")
                .Push(TranslateExpression(list))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListSize(Expression list)
        {
            return StringBuffer
                .Of("len(")
                .Push(TranslateExpression(list))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListToArray(Expression list)
        {
            return TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("[:]")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathArcCos(Expression ratio)
        {
            return StringBuffer
                .Of("math.acos(")
                .Push(TranslateExpression(ratio))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathArcSin(Expression ratio)
        {
            return StringBuffer
                .Of("math.asin(")
                .Push(TranslateExpression(ratio))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathArcTan(Expression yComponent, Expression xComponent)
        {
            return StringBuffer
                .Of("math.atan2(")
                .Push(TranslateExpression(yComponent))
                .Push(", ")
                .Push(TranslateExpression(xComponent))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathCos(Expression thetaRadians)
        {
            return StringBuffer
                .Of("math.cos(")
                .Push(TranslateExpression(thetaRadians))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathLog(Expression value)
        {
            return StringBuffer
                .Of("math.log(")
                .Push(TranslateExpression(value))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathPow(Expression expBase, Expression exponent)
        {
            return TranslateExpression(expBase)
                .EnsureTightness(ExpressionTightness.PYTHON_EXPONENT)
                .Push(" ** ")
                .Push(TranslateExpression(exponent).EnsureGreaterTightness(ExpressionTightness.PYTHON_EXPONENT))
                .WithTightness(ExpressionTightness.PYTHON_EXPONENT);
        }

        public override StringBuffer TranslateMathSin(Expression thetaRadians)
        {
            return StringBuffer
                .Of("math.sin(")
                .Push(TranslateExpression(thetaRadians))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathTan(Expression thetaRadians)
        {
            return StringBuffer
                .Of("math.tan(")
                .Push(TranslateExpression(thetaRadians))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMultiplyList(Expression list, Expression n)
        {
            return TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.MULTIPLICATION)
                .Push(" * ")
                .Push(TranslateExpression(n).EnsureGreaterTightness(ExpressionTightness.MULTIPLICATION))
                .WithTightness(ExpressionTightness.MULTIPLICATION);
        }

        public override StringBuffer TranslateNegative(UnaryOp unaryOp)
        {
            return TranslateExpression(unaryOp.Expression)
                .EnsureTightness(ExpressionTightness.UNARY_PREFIX)
                .Prepend("-")
                .WithTightness(ExpressionTightness.UNARY_PREFIX);
        }

        public override StringBuffer TranslateNullConstant()
        {
            return StringBuffer
                .Of("None")
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateOrd(Expression charValue)
        {
            return StringBuffer
                .Of("ord(")
                .Push(TranslateExpression(charValue))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateOpChain(OpChain opChain)
        {
            int expressionCount = opChain.Expressions.Length;
            string firstOp = opChain.Ops[0].Value;
            bool shortCircuit = firstOp == "&&" || firstOp == "||";

            int exprLeftIndex = shortCircuit ? expressionCount - 2 : 0;
            StringBuffer acc = TranslateExpression(opChain.Expressions[shortCircuit ? expressionCount - 1 : 0]);
            for (int i = 1; i < expressionCount; i++)
            {
                int operatorIndex = exprLeftIndex; // in lock step but naming is clear
                int exprRightIndex = exprLeftIndex + 1;
                string op = opChain.Ops[operatorIndex].Value;
                string pyOp = this.TranslateOp(op);
                ExpressionTightness opTightness = GetOpTightness(op);

                if (shortCircuit)
                {
                    acc
                        .EnsureGreaterTightness(opTightness)
                        .Prepend(" ")
                        .Prepend(pyOp)
                        .Prepend(" ")
                        .Prepend(TranslateExpression(opChain.Expressions[exprLeftIndex]).EnsureGreaterTightness(opTightness))
                        .WithTightness(opTightness);
                }
                else
                {
                    acc
                        .EnsureTightness(opTightness)
                        .Push(" ")
                        .Push(pyOp)
                        .Push(" ")
                        .Push(TranslateExpression(opChain.Expressions[exprRightIndex]).EnsureGreaterTightness(opTightness))
                        .WithTightness(opTightness);
                }

                exprLeftIndex += shortCircuit ? -1 : 1;
            }

            return acc;
        }

        public override StringBuffer TranslateParseFloatUnsafe(Expression stringValue)
        {
            return StringBuffer
                .Of("float(")
                .Push(TranslateExpression(stringValue))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateParseInt(Expression safeStringValue)
        {
            return StringBuffer
                .Of("int(")
                .Push(TranslateExpression(safeStringValue))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslatePrintStdErr(Expression value)
        {
            return StringBuffer
                .Of("print(")
                .Push(TranslateExpression(value))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslatePrintStdOut(Expression value)
        {
            return StringBuffer
                .Of("print(")
                .Push(this.TranslateExpression(value))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateRandomFloat()
        {
            this.MarkFeatureAsUsed("IMPORT:random");
            return StringBuffer
                .Of("random.random()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateSortedCopyOfIntArray(Expression intArray)
        {
            return StringBuffer
                .Of("PST_sortedCopyOfList(")
                .Push(this.TranslateExpression(intArray))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateSortedCopyOfStringArray(Expression stringArray)
        {
            return StringBuffer
                .Of("PST_sortedCopyOfList(")
                .Push(TranslateExpression(stringArray))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringAppend(Expression str1, Expression str2)
        {
            return TranslateExpression(str1)
                .Push(" += ")
                .Push(TranslateExpression(str2));
        }

        public override StringBuffer TranslateStringBuffer16()
        {
            return StringBuffer.Of("PST_StringBuffer16");
        }

        public override StringBuffer TranslateStringCharAt(Expression str, Expression index)
        {
            return TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("[")
                .Push(TranslateExpression(index))
                .Push("]")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringCharCodeAt(Expression str, Expression index)
        {
            return StringBuffer
                .Of("ord(")
                .Push(TranslateExpression(str).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push("[")
                .Push(TranslateExpression(index))
                .Push("])")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringCompareIsReverse(Expression str1, Expression str2)
        {
            return TranslateExpression(str1)
                .EnsureTightness(ExpressionTightness.PYTHON_COMPARE)
                .Push(" > ")
                .Push(TranslateExpression(str2).EnsureGreaterTightness(ExpressionTightness.PYTHON_COMPARE))
                .WithTightness(ExpressionTightness.PYTHON_COMPARE);
        }

        public override StringBuffer TranslateStringConcatAll(Expression[] strings)
        {
            StringBuffer buf = StringBuffer.Of("''.join([");
            for (int i = 0; i < strings.Length; ++i)
            {
                if (i > 0) buf.Push(", ");
                buf.Push(TranslateExpression(strings[i]));
            }
            return buf
                .Push("])")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringConcatPair(Expression strLeft, Expression strRight)
        {
            return TranslateExpression(strLeft)
                .EnsureTightness(ExpressionTightness.ADDITION)
                .Push(" + ")
                .Push(TranslateExpression(strRight).EnsureGreaterTightness(ExpressionTightness.ADDITION))
                .WithTightness(ExpressionTightness.ADDITION);
        }

        public override StringBuffer TranslateStringConstant(string value)
        {
            return StringBuffer
                .Of(CodeUtil.ConvertStringValueToCode(value))
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateStringContains(Expression haystack, Expression needle)
        {
            return TranslateExpression(needle)
                .EnsureTightness(ExpressionTightness.PYTHON_COMPARE)
                .Push(" in ")
                .Push(TranslateExpression(haystack).EnsureGreaterTightness(ExpressionTightness.PYTHON_COMPARE))
                .WithTightness(ExpressionTightness.PYTHON_COMPARE);
        }

        public override StringBuffer TranslateStringEndsWith(Expression haystack, Expression needle)
        {
            return TranslateExpression(haystack)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".endswith(")
                .Push(TranslateExpression(needle))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringEquals(Expression left, Expression right)
        {
            return TranslateExpression(left)
                .EnsureTightness(ExpressionTightness.PYTHON_COMPARE)
                .Push(" == ")
                .Push(TranslateExpression(right).EnsureGreaterTightness(ExpressionTightness.PYTHON_COMPARE))
                .WithTightness(ExpressionTightness.PYTHON_COMPARE);
        }

        public override StringBuffer TranslateStringFromCharCode(Expression charCode)
        {
            return StringBuffer
                .Of("chr(")
                .Push(TranslateExpression(charCode))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringIndexOf(Expression haystack, Expression needle)
        {
            return TranslateExpression(haystack)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".find(")
                .Push(TranslateExpression(needle))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringIndexOfWithStart(Expression haystack, Expression needle, Expression startIndex)
        {
            return TranslateExpression(haystack)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".find(")
                .Push(TranslateExpression(needle))
                .Push(", ")
                .Push(TranslateExpression(startIndex))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringLastIndexOf(Expression haystack, Expression needle)
        {
            return TranslateExpression(haystack)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".rfind(")
                .Push(TranslateExpression(needle))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringLength(Expression str)
        {
            return StringBuffer
                .Of("len(")
                .Push(TranslateExpression(str))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringReplace(Expression haystack, Expression needle, Expression newNeedle)
        {
            return TranslateExpression(haystack)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".replace(")
                .Push(TranslateExpression(needle))
                .Push(", ")
                .Push(TranslateExpression(newNeedle))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringReverse(Expression str)
        {
            return TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("[::-1]")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringSplit(Expression haystack, Expression needle)
        {
            return TranslateExpression(haystack)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".split(")
                .Push(TranslateExpression(needle))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringStartsWith(Expression haystack, Expression needle)
        {
            return TranslateExpression(haystack)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".startswith(")
                .Push(TranslateExpression(needle))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringSubstring(Expression str, Expression start, Expression length)
        {
            return TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push("[")
                .Push(TranslateExpression(start))
                .Push(":")
                .Push(
                    TranslateExpression(start).EnsureTightness(ExpressionTightness.ADDITION)
                    .Push(" + ")
                    .Push(TranslateExpression(length).EnsureGreaterTightness(ExpressionTightness.ADDITION))
                )
                .Push("]")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringSubstringIsEqualTo(Expression haystack, Expression startIndex, Expression needle)
        {
            return StringBuffer
                .Of("PST_stringCheckSlice(")
                .Push(TranslateExpression(haystack))
                .Push(", ")
                .Push(TranslateExpression(startIndex))
                .Push(", ")
                .Push(TranslateExpression(needle))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringToLower(Expression str)
        {
            return TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".lower()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringToUpper(Expression str)
        {
            return TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".upper()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringToUtf8Bytes(Expression str)
        {
            return StringBuffer
                .Of("PST_stringToUtf8Bytes(")
                .Push(TranslateExpression(str))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringTrim(Expression str)
        {
            return TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".strip()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringTrimEnd(Expression str)
        {
            return TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".rstrip()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringTrimStart(Expression str)
        {
            return TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".lstrip()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringBuilderAdd(Expression sbInst, Expression obj)
        {
            StringBuffer buf = TranslateExpression(sbInst)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".append(");
            string t = obj.ResolvedType.RootValue;
            bool isString = t == "string" || t == "char";
            if (isString)
            {
                buf.Push(TranslateExpression(obj));
            }
            else
            {
                buf
                    .Push("str(")
                    .Push(TranslateExpression(obj))
                    .Push(")");
            }
            return buf
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringBuilderClear(Expression sbInst)
        {
            return TranslateListClear(sbInst);
        }

        public override StringBuffer TranslateStringBuilderNew()
        {
            return StringBuffer
                .Of("[]")
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateStringBuilderToString(Expression sbInst)
        {
            return StringBuffer
                .Of("''.join(")
                .Push(TranslateExpression(sbInst))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStrongReferenceEquality(Expression left, Expression right)
        {
            return TranslateExpression(left)
                .EnsureTightness(ExpressionTightness.PYTHON_COMPARE)
                .Push(" is ")
                .Push(TranslateExpression(right).EnsureGreaterTightness(ExpressionTightness.PYTHON_COMPARE))
                .WithTightness(ExpressionTightness.PYTHON_COMPARE);
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
            this.MarkFeatureAsUsed("IMPORT:json");
            return StringBuffer
                .Of("json.dumps(")
                .Push(this.TranslateExpression(str))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateTryParseFloat(Expression stringValue, Expression floatOutList)
        {
            return StringBuffer
                .Of("PST_tryParseFloat(")
                .Push(TranslateExpression(stringValue))
                .Push(", ")
                .Push(TranslateExpression(floatOutList))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateUtf8BytesToString(Expression bytes)
        {
            return StringBuffer
                .Of("bytes(")
                .Push(TranslateExpression(bytes))
                .Push(").decode('utf-8')")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateVariable(Variable variable)
        {
            return StringBuffer
                .Of(variable.Name)
                .WithTightness(ExpressionTightness.ATOMIC);
        }
    }
}
