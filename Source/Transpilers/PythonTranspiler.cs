using Pastel.Parser;
using Pastel.Parser.ParseNodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Transpilers
{
    internal class PythonTranspiler : AbstractTranspiler
    {
        private string TranslateOp(string originalOp)
        {
            switch (originalOp)
            {
                case "&&": return "and";
                case "||": return "or";
                default: return originalOp;
            }
        }

        public PythonTranspiler(TranspilerContext transpilerCtx)
            : base(transpilerCtx)
        {
            this.UsesStructDefinitions = false;
        }

        public override string PreferredTab => "  ";
        public override string PreferredNewline => "\n";

        public override string HelperCodeResourcePath { get { return "Transpilers/Resources/PastelHelper.py"; } }

        public override string TranslateType(PType type)
        {
            throw new InvalidOperationException(); // Python does not support types.
        }

        protected override void WrapCodeImpl(TranspilerContext ctx, ProjectConfig config, List<string> lines, bool isForStruct)
        {
            if (config.Imports.Count > 0)
            {
                lines.InsertRange(0,
                    config.Imports
                        .OrderBy(t => t)
                        .Select(t => "import " + t)
                        .Append(""));
            }
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
            return StringBuffer
                .Of("(")
                .Push(this.TranslateExpression(sep))
                .Push(").join(")
                .Push(this.TranslateExpression(array))
                .Push(')');
        }

        public override StringBuffer TranslateArrayLength(Expression array)
        {
            return StringBuffer
                .Of("len(")
                .Push(this.TranslateExpression(array))
                .Push(')');
        }

        public override StringBuffer TranslateArrayNew(PType arrayType, Expression lengthExpression)
        {
            if (lengthExpression is InlineConstant)
            {
                InlineConstant ic = (InlineConstant)lengthExpression;
                int length = (int)ic.Value;
                switch (length)
                {
                    case 0: return StringBuffer.Of("[]");
                    case 1: return StringBuffer.Of("[None]");
                    case 2: return StringBuffer.Of("[None, None]");
                    default: break;
                }
            }

            return StringBuffer.Of("(PST_NoneListOfOne * ")
                .Push(this.TranslateExpression(lengthExpression))
                .Push(")");
        }

        public override StringBuffer TranslateArraySet(Expression array, Expression index, Expression value)
        {
            return this.TranslateExpression(array)
                .Push('[')
                .Push(this.TranslateExpression(index))
                .Push("] = ")
                .Push(this.TranslateExpression(value));
        }

        public override void TranslateAssignment(TranspilerContext sb, Assignment assignment)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(this.TranslateExpressionAsString(assignment.Target));
            sb.Append(' ');
            sb.Append(assignment.OpToken.Value);
            sb.Append(' ');
            sb.Append(this.TranslateExpressionAsString(assignment.Value));
            sb.Append('\n');
        }

        public override StringBuffer TranslateBase64ToBytes(Expression base64String)
        {
            return StringBuffer
                .Of("PST_base64ToBytes(")
                .Push(this.TranslateExpression(base64String))
                .Push(')');
        }

        public override StringBuffer TranslateBase64ToString(Expression base64String)
        {
            return StringBuffer
                .Of("PST_base64ToString(")
                .Push(this.TranslateExpression(base64String))
                .Push(')');
        }

        public override StringBuffer TranslateBooleanConstant(bool value)
        {
            return StringBuffer.Of(value ? "True" : "False");
        }

        public override StringBuffer TranslateBooleanNot(UnaryOp unaryOp)
        {
            return StringBuffer
                .Of("not (")
                .Push(this.TranslateExpression(unaryOp.Expression))
                .Push(')');
        }

        public override void TranslateBreak(TranspilerContext sb)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("break\n");
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
            StructDefinition structDef = constructorInvocation.StructDefinition;
            ClassDefinition classDef = constructorInvocation.ClassDefinition;
            if (structDef == null)
            {
                throw new NotImplementedException();
            }
            else
            {
                StringBuffer buf = StringBuffer.Of("[");
                int args = structDef.FlatFieldNames.Length;
                for (int i = 0; i < args; ++i)
                {
                    if (i > 0)
                    {
                        buf.Push(", ");
                    }
                    buf.Push(this.TranslateExpression(constructorInvocation.Args[i]));
                }
                return buf.Push(']');
            }
        }

        public override StringBuffer TranslateCurrentTimeSeconds()
        {
            return StringBuffer.Of("time.time()");
        }

        public override StringBuffer TranslateDictionaryContainsKey(Expression dictionary, Expression key)
        {
            return StringBuffer
                .Of("(")
                .Push(this.TranslateExpression(key))
                .Push(" in ")
                .Push(this.TranslateExpression(dictionary))
                .Push(')');
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
                .Of("list(")
                .Push(this.TranslateExpression(dictionary))
                .Push(".keys())");
        }

        public override StringBuffer TranslateDictionaryNew(PType keyType, PType valueType)
        {
            return StringBuffer.Of("{}");
        }

        public override StringBuffer TranslateDictionaryRemove(Expression dictionary, Expression key)
        {
            return this.TranslateExpression(dictionary)
                .Push(".pop(")
                .Push(this.TranslateExpression(key))
                .Push(')');
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
                .Of("len(")
                .Push(this.TranslateExpression(dictionary))
                .Push(')');
        }

        public override void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(varOut.Name);
            sb.Append(" = ");
            sb.Append(this.TranslateExpressionAsString(dictionary));
            sb.Append(".get(");
            sb.Append(this.TranslateExpressionAsString(key));
            sb.Append(", ");
            sb.Append(this.TranslateExpressionAsString(fallbackValue));
            sb.Append(")\n");
        }

        public override StringBuffer TranslateDictionaryValues(Expression dictionary)
        {
            return StringBuffer
                .Of("list(")
                .Push(this.TranslateExpression(dictionary))
                .Push(".values())");
        }

        public override StringBuffer TranslateEmitComment(string value)
        {
            return StringBuffer
                .Of("# ")
                .Push(value);
        }

        public override void TranslateStatements(TranspilerContext sb, Statement[] statements)
        {
            if (statements.Length == 0)
            {
                sb.Append(sb.CurrentTab);
                sb.Append("pass\n");
            }
            else
            {
                base.TranslateStatements(sb, statements);
            }
        }

        public override void TranslateExpressionAsStatement(TranspilerContext sb, Expression expression)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(this.TranslateExpressionAsString(expression));
            sb.Append("\n");
        }

        public override StringBuffer TranslateExtensibleCallbackInvoke(Expression name, Expression argsArray)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateFloatBuffer16()
        {
            return StringBuffer.Of("PST_FloatBuffer16");
        }

        public override StringBuffer TranslateFloatConstant(double value)
        {
            return StringBuffer.Of(CodeUtil.FloatToString(value));
        }

        public override StringBuffer TranslateFloatDivision(Expression floatNumerator, Expression floatDenominator)
        {
            return StringBuffer
                .Of("(1.0 * (")
                .Push(this.TranslateExpression(floatNumerator))
                .Push(") / (")
                .Push(this.TranslateExpression(floatDenominator))
                .Push("))");
        }

        public override StringBuffer TranslateFloatToInt(Expression floatExpr)
        {
            return StringBuffer
                .Of("int(")
                .Push(this.TranslateExpression(floatExpr))
                .Push(")");
        }

        public override StringBuffer TranslateFloatToString(Expression floatExpr)
        {
            return StringBuffer
                .Of("str(")
                .Push(this.TranslateExpression(floatExpr))
                .Push(')');
        }

        public override StringBuffer TranslateFunctionInvocation(FunctionReference funcRef, Expression[] args)
        {
            return this.TranslateFunctionReference(funcRef)
                .Push('(')
                .Push(this.TranslateCommaDelimitedExpressions(args))
                .Push(')');
        }

        public override StringBuffer TranslateFunctionReference(FunctionReference funcRef)
        {
            return StringBuffer.Of(funcRef.Function.NameToken.Value);
        }

        public override StringBuffer TranslateGetFunction(Expression name)
        {
            return StringBuffer.Of("TranslationHelper_getFunction(")
                .Push(this.TranslateExpression(name))
                .Push(')');
        }

        public override void TranslateIfStatement(TranspilerContext sb, IfStatement ifStatement)
        {
            sb.Append(sb.CurrentTab);
            this.TranslateIfStatementNoIndent(sb, ifStatement);
        }

        private void TranslateIfStatementNoIndent(TranspilerContext sb, IfStatement ifStatement)
        {
            sb.Append("if ");
            sb.Append(this.TranslateExpressionAsString(ifStatement.Condition));
            sb.Append(":\n");
            sb.TabDepth++;
            if (ifStatement.IfCode.Length == 0)
            {
                // ideally this should be optimized out at compile-time. TODO: throw instead and do that
                sb.Append(sb.CurrentTab);
                sb.Append("pass\n");
            }
            else
            {
                this.TranslateStatements(sb, ifStatement.IfCode);
            }
            sb.TabDepth--;

            Statement[] elseCode = ifStatement.ElseCode;

            if (elseCode.Length == 0) return;

            if (elseCode.Length == 1 && elseCode[0] is IfStatement)
            {
                sb.Append(sb.CurrentTab);
                sb.Append("el");
                this.TranslateIfStatementNoIndent(sb, (IfStatement)elseCode[0]);
            }
            else
            {
                sb.Append(sb.CurrentTab);
                sb.Append("else:\n");
                sb.TabDepth++;
                this.TranslateStatements(sb, elseCode);
                sb.TabDepth--;
            }
        }

        public override StringBuffer TranslateInlineIncrement(Expression innerExpression, bool isPrefix, bool isAddition)
        {
            throw new ParserException(
                innerExpression.FirstToken,
                "Python does not support ++ or --. Please check all usages with if (@ext_boolean(\"HAS_INCREMENT\")) { ... }");
        }

        public override StringBuffer TranslateInstanceFieldDereference(Expression root, ClassDefinition classDef, string fieldName)
        {
            return this.TranslateExpression(root)
                .Push('.')
                .Push(fieldName);
        }

        public override StringBuffer TranslateIntBuffer16()
        {
            return StringBuffer.Of("PST_IntBuffer16");
        }

        public override StringBuffer TranslateIntegerConstant(int value)
        {
            return StringBuffer.Of("" + value);
        }

        public override StringBuffer TranslateIntegerDivision(Expression integerNumerator, Expression integerDenominator)
        {
            return StringBuffer
                .Of("(")
                .Push(this.TranslateExpression(integerNumerator))
                .Push(") // (")
                .Push(this.TranslateExpression(integerDenominator))
                .Push(')');
        }

        public override StringBuffer TranslateIntToString(Expression integer)
        {
            return StringBuffer
                .Of("str(")
                .Push(this.TranslateExpression(integer))
                .Push(')');
        }

        public override StringBuffer TranslateIsValidInteger(Expression stringValue)
        {
            return StringBuffer
                .Of("PST_isValidInteger(")
                .Push(this.TranslateExpression(stringValue))
                .Push(')');
        }

        public override StringBuffer TranslateListAdd(Expression list, Expression item)
        {
            return this.TranslateExpression(list)
                .Push(".append(")
                .Push(this.TranslateExpression(item))
                .Push(')');
        }

        public override StringBuffer TranslateListClear(Expression list)
        {
            return StringBuffer
                .Of("del ")
                .Push(this.TranslateExpression(list))
                .Push("[:]");
        }

        public override StringBuffer TranslateListConcat(Expression list, Expression items)
        {
            return this.TranslateExpression(list)
                .Push(" + ")
                .Push(this.TranslateExpression(items));
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
                .Push(".insert(")
                .Push(this.TranslateExpression(index))
                .Push(", ")
                .Push(this.TranslateExpression(item))
                .Push(')');
        }

        public override StringBuffer TranslateListJoinChars(Expression list)
        {
            return StringBuffer
                .Of("''.join(")
                .Push(this.TranslateExpression(list))
                .Push(')');
        }

        public override StringBuffer TranslateListJoinStrings(Expression list, Expression sep)
        {
            return this.TranslateExpression(sep)
                .Push(".join(")
                .Push(this.TranslateExpression(list))
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
            return StringBuffer
                .Of("del ")
                .Push(this.TranslateExpression(list))
                .Push('[')
                .Push(this.TranslateExpression(index))
                .Push(']');
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
            return StringBuffer
                .Of("random.shuffle(")
                .Push(this.TranslateExpression(list))
                .Push(')');
        }

        public override StringBuffer TranslateListSize(Expression list)
        {
            return StringBuffer
                .Of("len(")
                .Push(this.TranslateExpression(list))
                .Push(')');
        }

        public override StringBuffer TranslateListToArray(Expression list)
        {
            return this.TranslateExpression(list)
                .Push("[:]");
        }

        public override StringBuffer TranslateMathArcCos(Expression ratio)
        {
            return StringBuffer
                .Of("math.acos(")
                .Push(this.TranslateExpression(ratio))
                .Push(')');
        }

        public override StringBuffer TranslateMathArcSin(Expression ratio)
        {
            return StringBuffer
                .Of("math.asin(")
                .Push(this.TranslateExpression(ratio))
                .Push(')');
        }

        public override StringBuffer TranslateMathArcTan(Expression yComponent, Expression xComponent)
        {
            return StringBuffer
                .Of("math.atan2(")
                .Push(this.TranslateExpression(yComponent))
                .Push(", ")
                .Push(this.TranslateExpression(xComponent))
                .Push(')');
        }

        public override StringBuffer TranslateMathCos(Expression thetaRadians)
        {
            return StringBuffer
                .Of("math.cos(")
                .Push(this.TranslateExpression(thetaRadians))
                .Push(')');
        }

        public override StringBuffer TranslateMathLog(Expression value)
        {
            return StringBuffer
                .Of("math.log(")
                .Push(this.TranslateExpression(value))
                .Push(')');
        }

        public override StringBuffer TranslateMathPow(Expression expBase, Expression exponent)
        {
            return StringBuffer
                .Of("(")
                .Push(this.TranslateExpression(expBase))
                .Push(" ** ")
                .Push(this.TranslateExpression(exponent))
                .Push(')');
        }

        public override StringBuffer TranslateMathSin(Expression thetaRadians)
        {
            return StringBuffer
                .Of("math.sin(")
                .Push(this.TranslateExpression(thetaRadians))
                .Push(')');
        }

        public override StringBuffer TranslateMathTan(Expression thetaRadians)
        {
            return StringBuffer
                .Of("math.tan(")
                .Push(this.TranslateExpression(thetaRadians))
                .Push(')');
        }

        public override StringBuffer TranslateMultiplyList(Expression list, Expression n)
        {
            return StringBuffer
                .Of("(")
                .Push(this.TranslateExpression(list))
                .Push(" * (")
                .Push(this.TranslateExpression(n))
                .Push("))");
        }

        public override StringBuffer TranslateNegative(UnaryOp unaryOp)
        {
            Expression expr = unaryOp.Expression;
            if (expr is InlineConstant || expr is Variable)
            {
                return StringBuffer
                    .Of("-")
                    .Push(this.TranslateExpression(expr));
            }

            return StringBuffer
                .Of("-(")
                .Push(this.TranslateExpression(expr))
                .Push(')');
        }

        public override StringBuffer TranslateNullConstant()
        {
            return StringBuffer.Of("None");
        }

        public override StringBuffer TranslateOrd(Expression charValue)
        {
            return StringBuffer
                .Of("ord(")
                .Push(this.TranslateExpression(charValue))
                .Push(')');
        }

        public override StringBuffer TranslateOpChain(OpChain opChain)
        {
            StringBuffer buf = StringBuffer.Of("(");
            Expression[] expressions = opChain.Expressions;
            Token[] ops = opChain.Ops;
            for (int i = 0; i < expressions.Length; ++i)
            {
                if (i > 0)
                {
                    // TODO: platform should have an op translator, which would just be a pass-through function for most ops.
                    buf
                        .Push(' ')
                        .Push(this.TranslateOp(ops[i - 1].Value))
                        .Push(' ');
                }
                buf.Push(this.TranslateExpression(expressions[i]));
            }
            return buf.Push(')');
        }

        public override StringBuffer TranslateParseFloatUnsafe(Expression stringValue)
        {
            return StringBuffer
                .Of("float(")
                .Push(this.TranslateExpression(stringValue))
                .Push(")");
        }

        public override StringBuffer TranslateParseInt(Expression safeStringValue)
        {
            return StringBuffer
                .Of("int(")
                .Push(this.TranslateExpression(safeStringValue))
                .Push(')');
        }

        public override StringBuffer TranslatePrintStdErr(Expression value)
        {
            return StringBuffer
                .Of("print(")
                .Push(this.TranslateExpression(value))
                .Push(')');
        }

        public override StringBuffer TranslatePrintStdOut(Expression value)
        {
            return StringBuffer
                .Of("print(")
                .Push(this.TranslateExpression(value))
                .Push(')');
        }

        public override StringBuffer TranslateRandomFloat()
        {
            return StringBuffer.Of("random.random()");
        }

        public override void TranslateReturnStatemnt(TranspilerContext sb, ReturnStatement returnStatement)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("return ");
            sb.Append(this.TranslateExpressionAsString(returnStatement.Expression));
            sb.Append("\n");
        }

        public override StringBuffer TranslateSortedCopyOfIntArray(Expression intArray)
        {
            return StringBuffer
                .Of("PST_sortedCopyOfList(")
                .Push(this.TranslateExpression(intArray))
                .Push(')');
        }

        public override StringBuffer TranslateSortedCopyOfStringArray(Expression stringArray)
        {
            return StringBuffer
                .Of("PST_sortedCopyOfList(")
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
            return StringBuffer.Of("PST_StringBuffer16");
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
                .Of("ord(")
                .Push(this.TranslateExpression(str))
                .Push('[')
                .Push(this.TranslateExpression(index))
                .Push("])");
        }

        public override StringBuffer TranslateStringCompareIsReverse(Expression str1, Expression str2)
        {
            return StringBuffer
                .Of("(")
                .Push(this.TranslateExpression(str1))
                .Push(" > ")
                .Push(this.TranslateExpression(str2))
                .Push(')');
        }

        public override StringBuffer TranslateStringConcatAll(Expression[] strings)
        {
            StringBuffer buf = StringBuffer.Of("''.join([");
            for (int i = 0; i < strings.Length; ++i)
            {
                if (i > 0) buf.Push(", ");
                buf.Push(this.TranslateExpression(strings[i]));
            }
            return buf.Push("])");
        }

        public override StringBuffer TranslateStringConcatPair(Expression strLeft, Expression strRight)
        {
            return this.TranslateExpression(strLeft)
                .Push(" + ")
                .Push(this.TranslateExpression(strRight));
        }

        public override StringBuffer TranslateStringConstant(string value)
        {
            return StringBuffer.Of(CodeUtil.ConvertStringValueToCode(value));
        }

        public override StringBuffer TranslateStringContains(Expression haystack, Expression needle)
        {
            return StringBuffer
                .Of("(")
                .Push(this.TranslateExpression(needle))
                .Push(" in ")
                .Push(this.TranslateExpression(haystack))
                .Push(')');
        }

        public override StringBuffer TranslateStringEndsWith(Expression haystack, Expression needle)
        {
            return this.TranslateExpression(haystack)
                .Push(".endswith(")
                .Push(this.TranslateExpression(needle))
                .Push(')');
        }

        public override StringBuffer TranslateStringEquals(Expression left, Expression right)
        {
            return this.TranslateExpression(left)
                .Push(" == ")
                .Push(this.TranslateExpression(right));
        }

        public override StringBuffer TranslateStringFromCharCode(Expression charCode)
        {
            return StringBuffer
                .Of("chr(")
                .Push(this.TranslateExpression(charCode))
                .Push(')');
        }

        public override StringBuffer TranslateStringIndexOf(Expression haystack, Expression needle)
        {
            return this.TranslateExpression(haystack)
                .Push(".find(")
                .Push(this.TranslateExpression(needle))
                .Push(')');
        }

        public override StringBuffer TranslateStringIndexOfWithStart(Expression haystack, Expression needle, Expression startIndex)
        {
            return this.TranslateExpression(haystack)
                .Push(".find(")
                .Push(this.TranslateExpression(needle))
                .Push(", ")
                .Push(this.TranslateExpression(startIndex))
                .Push(')');
        }

        public override StringBuffer TranslateStringLastIndexOf(Expression haystack, Expression needle)
        {
            return this.TranslateExpression(haystack)
                .Push(".rfind(")
                .Push(this.TranslateExpression(needle))
                .Push(')');
        }

        public override StringBuffer TranslateStringLength(Expression str)
        {
            return StringBuffer
                .Of("len(")
                .Push(this.TranslateExpression(str))
                .Push(')');
        }

        public override StringBuffer TranslateStringReplace(Expression haystack, Expression needle, Expression newNeedle)
        {
            return this.TranslateExpression(haystack)
                .Push(".replace(")
                .Push(this.TranslateExpression(needle))
                .Push(", ")
                .Push(this.TranslateExpression(newNeedle))
                .Push(')');
        }

        public override StringBuffer TranslateStringReverse(Expression str)
        {
            return this.TranslateExpression(str)
                .Push("[::-1]");
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
            return this.TranslateExpression(haystack)
                .Push(".startswith(")
                .Push(this.TranslateExpression(needle))
                .Push(')');
        }

        public override StringBuffer TranslateStringSubstring(Expression str, Expression start, Expression length)
        {
            return this.TranslateExpression(str)
                .Push('[')
                .Push(this.TranslateExpression(start))
                .Push(':')
                .Push(this.TranslateExpression(start))
                .Push(" + ")
                .Push(this.TranslateExpression(length))
                .Push(']');
        }

        public override StringBuffer TranslateStringSubstringIsEqualTo(Expression haystack, Expression startIndex, Expression needle)
        {
            return StringBuffer
                .Of("PST_stringCheckSlice(")
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
                .Push(".lower()");
        }

        public override StringBuffer TranslateStringToUpper(Expression str)
        {
            return this.TranslateExpression(str)
                .Push(".upper()");
        }

        public override StringBuffer TranslateStringToUtf8Bytes(Expression str)
        {
            return StringBuffer
                .Of("PST_stringToUtf8Bytes(")
                .Push(this.TranslateExpression(str))
                .Push(')');
        }

        public override StringBuffer TranslateStringTrim(Expression str)
        {
            return this.TranslateExpression(str)
                .Push(".strip()");
        }

        public override StringBuffer TranslateStringTrimEnd(Expression str)
        {
            return this.TranslateExpression(str)
                .Push(".rstrip()");
        }

        public override StringBuffer TranslateStringTrimStart(Expression str)
        {
            return this.TranslateExpression(str)
                .Push(".lstrip()");
        }

        public override StringBuffer TranslateStringBuilderAdd(Expression sbInst, Expression obj)
        {
            StringBuffer buf = this.TranslateExpression(sbInst)
                .Push(".append(");
            string t = obj.ResolvedType.RootValue;
            bool isString = t == "string" || t == "char";
            if (isString)
            {
                buf.Push(this.TranslateExpression(obj));
            }
            else
            {
                buf
                    .Push("str(")
                    .Push(this.TranslateExpression(obj))
                    .Push(')');
            }
            return buf.Push(')');
        }

        public override StringBuffer TranslateStringBuilderClear(Expression sbInst)
        {
            return this.TranslateListClear(sbInst);
        }

        public override StringBuffer TranslateStringBuilderNew()
        {
            return StringBuffer.Of("[]");
        }

        public override StringBuffer TranslateStringBuilderToString(Expression sbInst)
        {
            return StringBuffer
                .Of("''.join(")
                .Push(this.TranslateExpression(sbInst))
                .Push(')');
        }

        public override StringBuffer TranslateStrongReferenceEquality(Expression left, Expression right)
        {
            return this.TranslateExpression(left)
                .Push(" is ")
                .Push(this.TranslateExpression(right));
        }

        public override StringBuffer TranslateStructFieldDereference(Expression root, StructDefinition structDef, string fieldName, int fieldIndex)
        {
            return this.TranslateExpression(root)
                .Push("[" + fieldIndex + "]");
        }

        public override void TranslateSwitchStatement(TranspilerContext sb, SwitchStatement switchStatement)
        {
            string functionName = this.transpilerCtx.CurrentFunctionDefinition.NameToken.Value;
            int switchId = this.transpilerCtx.SwitchCounter++;
            PythonFakeSwitchStatement fakeSwitchStatement = PythonFakeSwitchStatement.Build(switchStatement, switchId, functionName);

            sb.Append(sb.CurrentTab);
            sb.Append(fakeSwitchStatement.ConditionVariableName);
            sb.Append(" = ");
            sb.Append(fakeSwitchStatement.DictionaryGlobalName);
            sb.Append(".get(");
            sb.Append(this.TranslateExpressionAsString(switchStatement.Condition));
            sb.Append(", ");
            sb.Append(fakeSwitchStatement.DefaultId);
            sb.Append(")\n");
            this.TranslateIfStatement(sb, fakeSwitchStatement.GenerateIfStatementBinarySearchTree());

            // This list of switch statements will be serialized at the end of the function definition as globals.
            sb.SwitchStatements.Add(fakeSwitchStatement);
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
                .Of("PST_tryParseFloat(")
                .Push(this.TranslateExpression(stringValue))
                .Push(", ")
                .Push(this.TranslateExpression(floatOutList))
                .Push(')');
        }

        public override StringBuffer TranslateUtf8BytesToString(Expression bytes)
        {
            return StringBuffer
                .Of("bytes(")
                .Push(this.TranslateExpression(bytes))
                .Push(").decode('utf-8')");
        }

        public override StringBuffer TranslateVariable(Variable variable)
        {
            return StringBuffer.Of(variable.Name);
        }

        public override void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(varDecl.VariableNameToken.Value);
            sb.Append(" = ");
            sb.Append(this.TranslateExpressionAsString(varDecl.Value));
            sb.Append("\n");
        }

        public override void TranslateWhileLoop(TranspilerContext sb, WhileLoop whileLoop)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("while ");
            sb.Append(this.TranslateExpressionAsString(whileLoop.Condition));
            sb.Append(":\n");
            sb.TabDepth++;
            this.TranslateStatements(sb, whileLoop.Code);
            sb.TabDepth--;
        }

        public override void GenerateCodeForFunction(TranspilerContext sb, FunctionDefinition funcDef, bool isStatic)
        {
            this.transpilerCtx.CurrentFunctionDefinition = funcDef;

            sb.Append(sb.CurrentTab);
            sb.Append("def ");
            sb.Append(funcDef.NameToken.Value);
            sb.Append('(');
            int argCount = funcDef.ArgNames.Length;
            for (int i = 0; i < argCount; ++i)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(funcDef.ArgNames[i].Value);
            }
            sb.Append("):\n");
            sb.TabDepth++;
            this.TranslateStatements(sb, funcDef.Code);
            sb.TabDepth--;
            sb.Append("\n");

            foreach (PythonFakeSwitchStatement switchStatement in this.transpilerCtx.SwitchStatements)
            {
                sb.Append(sb.CurrentTab);
                sb.Append(switchStatement.GenerateGlobalDictionaryLookup());
                sb.Append("\n");
            }
            this.transpilerCtx.SwitchStatements.Clear();
            this.transpilerCtx.CurrentFunctionDefinition = null;
        }

        public override void GenerateCodeForClass(TranspilerContext sb, ClassDefinition classDef)
        {
            throw new NotImplementedException();
        }

        public override void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef)
        {
            throw new InvalidOperationException(); // This function should not be called. Python uses lists as structs.
        }
    }
}
