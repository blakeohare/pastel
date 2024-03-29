﻿using Pastel.Nodes;
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

        public PythonTranspiler()
            : base("  ", "\n")
        {
            this.UsesStructDefinitions = false;
        }

        public override string HelperCodeResourcePath { get { return "Transpilers/Resources/PastelHelper.py"; } }

        public override string TranslateType(PType type)
        {
            throw new InvalidOperationException("Python does not support types.");
        }

        protected override void WrapCodeImpl(TranspilerContext ctx, ProjectConfig config, List<string> lines, bool isForStruct)
        {
            if (config.Imports.Count > 0)
            {
                lines.InsertRange(0,
                    config.Imports
                        .OrderBy(t => t)
                        .Select(t => "import " + t)
                        .Concat(new string[] { "" }));
            }
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
            sb.Append('(');
            this.TranslateExpression(sb, sep);
            sb.Append(").join(");
            this.TranslateExpression(sb, array);
            sb.Append(')');
        }

        public override void TranslateArrayLength(TranspilerContext sb, Expression array)
        {
            sb.Append("len(");
            this.TranslateExpression(sb, array);
            sb.Append(')');
        }

        public override void TranslateArrayNew(TranspilerContext sb, PType arrayType, Expression lengthExpression)
        {
            if (lengthExpression is InlineConstant)
            {
                InlineConstant ic = (InlineConstant)lengthExpression;
                int length = (int)ic.Value;
                switch (length)
                {
                    case 0: sb.Append("[]"); return;
                    case 1: sb.Append("[None]"); return;
                    case 2: sb.Append("[None, None]"); return;
                    default: break;
                }
            }
            sb.Append("(PST_NoneListOfOne * ");
            this.TranslateExpression(sb, lengthExpression);
            sb.Append(")");
        }

        public override void TranslateArraySet(TranspilerContext sb, Expression array, Expression index, Expression value)
        {
            this.TranslateExpression(sb, array);
            sb.Append('[');
            this.TranslateExpression(sb, index);
            sb.Append("] = ");
            this.TranslateExpression(sb, value);
        }

        public override void TranslateAssignment(TranspilerContext sb, Assignment assignment)
        {
            sb.Append(sb.CurrentTab);
            this.TranslateExpression(sb, assignment.Target);
            sb.Append(' ');
            sb.Append(assignment.OpToken.Value);
            sb.Append(' ');
            this.TranslateExpression(sb, assignment.Value);
            sb.Append(this.NewLine);
        }

        public override void TranslateBase64ToBytes(TranspilerContext sb, Expression base64String)
        {
            sb.Append("PST_base64ToBytes(");
            this.TranslateExpression(sb, base64String);
            sb.Append(')');
        }

        public override void TranslateBase64ToString(TranspilerContext sb, Expression base64String)
        {
            sb.Append("PST_base64ToString(");
            this.TranslateExpression(sb, base64String);
            sb.Append(')');
        }

        public override void TranslateBooleanConstant(TranspilerContext sb, bool value)
        {
            sb.Append(value ? "True" : "False");
        }

        public override void TranslateBooleanNot(TranspilerContext sb, UnaryOp unaryOp)
        {
            sb.Append("not (");
            this.TranslateExpression(sb, unaryOp.Expression);
            sb.Append(')');
        }

        public override void TranslateBreak(TranspilerContext sb)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("break");
            sb.Append(this.NewLine);
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
            sb.Append("chr(");
            this.TranslateExpression(sb, charCode);
            sb.Append(')');
        }

        public override void TranslateConstructorInvocation(TranspilerContext sb, ConstructorInvocation constructorInvocation)
        {
            StructDefinition structDef = constructorInvocation.StructDefinition;
            ClassDefinition classDef = constructorInvocation.ClassDefinition;
            if (structDef == null)
            {
                throw new NotImplementedException();
            }
            else
            {
                sb.Append('[');
                int args = structDef.FlatFieldNames.Length;
                for (int i = 0; i < args; ++i)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }
                    this.TranslateExpression(sb, constructorInvocation.Args[i]);
                }
                sb.Append(']');
            }
        }

        public override void TranslateCurrentTimeSeconds(TranspilerContext sb)
        {
            sb.Append("time.time()");
        }

        public override void TranslateDictionaryContainsKey(TranspilerContext sb, Expression dictionary, Expression key)
        {
            sb.Append('(');
            this.TranslateExpression(sb, key);
            sb.Append(" in ");
            this.TranslateExpression(sb, dictionary);
            sb.Append(')');
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
            sb.Append("list(");
            this.TranslateExpression(sb, dictionary);
            sb.Append(".keys())");
        }

        public override void TranslateDictionaryNew(TranspilerContext sb, PType keyType, PType valueType)
        {
            sb.Append("{}");
        }

        public override void TranslateDictionaryRemove(TranspilerContext sb, Expression dictionary, Expression key)
        {
            this.TranslateExpression(sb, dictionary);
            sb.Append(".pop(");
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
            sb.Append("len(");
            this.TranslateExpression(sb, dictionary);
            sb.Append(')');
        }

        public override void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(varOut.Name);
            sb.Append(" = ");
            this.TranslateExpression(sb, dictionary);
            sb.Append(".get(");
            this.TranslateExpression(sb, key);
            sb.Append(", ");
            this.TranslateExpression(sb, fallbackValue);
            sb.Append(")");
            sb.Append(this.NewLine);
        }

        public override void TranslateDictionaryValues(TranspilerContext sb, Expression dictionary)
        {
            sb.Append("list(");
            this.TranslateExpression(sb, dictionary);
            sb.Append(".values())");
        }

        public override void TranslateEmitComment(TranspilerContext sb, string value)
        {
            sb.Append("# ");
            sb.Append(value);
        }

        public override void TranslateExecutables(TranspilerContext sb, Executable[] executables)
        {
            if (executables.Length == 0)
            {
                sb.Append(sb.CurrentTab);
                sb.Append("pass");
                sb.Append(this.NewLine);
            }
            else
            {
                base.TranslateExecutables(sb, executables);
            }
        }

        public override void TranslateExpressionAsExecutable(TranspilerContext sb, Expression expression)
        {
            sb.Append(sb.CurrentTab);
            this.TranslateExpression(sb, expression);
            sb.Append(this.NewLine);
        }

        public override void TranslateExtensibleCallbackInvoke(TranspilerContext sb, Expression name, Expression argsArray)
        {
            throw new NotImplementedException();
        }

        public override void TranslateFloatBuffer16(TranspilerContext sb)
        {
            sb.Append("PST_FloatBuffer16");
        }

        public override void TranslateFloatConstant(TranspilerContext sb, double value)
        {
            sb.Append(PastelUtil.FloatToString(value));
        }

        public override void TranslateFloatDivision(TranspilerContext sb, Expression floatNumerator, Expression floatDenominator)
        {
            sb.Append("(1.0 * (");
            this.TranslateExpression(sb, floatNumerator);
            sb.Append(") / (");
            this.TranslateExpression(sb, floatDenominator);
            sb.Append("))");
        }

        public override void TranslateFloatToInt(TranspilerContext sb, Expression floatExpr)
        {
            sb.Append("int(");
            this.TranslateExpression(sb, floatExpr);
            sb.Append(")");
        }

        public override void TranslateFloatToString(TranspilerContext sb, Expression floatExpr)
        {
            sb.Append("str(");
            this.TranslateExpression(sb, floatExpr);
            sb.Append(')');
        }

        public override void TranslateFunctionInvocation(TranspilerContext sb, FunctionReference funcRef, Expression[] args)
        {
            this.TranslateFunctionReference(sb, funcRef);
            sb.Append('(');
            this.TranslateCommaDelimitedExpressions(sb, args);
            sb.Append(')');
        }

        public override void TranslateFunctionReference(TranspilerContext sb, FunctionReference funcRef)
        {
            sb.Append(funcRef.Function.NameToken.Value);
        }

        public override void TranslateGetFunction(TranspilerContext sb, Expression name)
        {
            sb.Append("TranslationHelper_getFunction(");
            this.TranslateExpression(sb, name);
            sb.Append(')');
        }

        public override void TranslateIfStatement(TranspilerContext sb, IfStatement ifStatement)
        {
            sb.Append(sb.CurrentTab);
            this.TranslateIfStatementNoIndent(sb, ifStatement);
        }

        private void TranslateIfStatementNoIndent(TranspilerContext sb, IfStatement ifStatement)
        {
            sb.Append("if ");
            this.TranslateExpression(sb, ifStatement.Condition);
            sb.Append(':');
            sb.Append(this.NewLine);
            sb.TabDepth++;
            if (ifStatement.IfCode.Length == 0)
            {
                // ideally this should be optimized out at compile-time. TODO: throw instead and do that
                sb.Append(sb.CurrentTab);
                sb.Append("pass");
                sb.Append(this.NewLine);
            }
            else
            {
                this.TranslateExecutables(sb, ifStatement.IfCode);
            }
            sb.TabDepth--;

            Executable[] elseCode = ifStatement.ElseCode;

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
                sb.Append("else:");
                sb.Append(this.NewLine);
                sb.TabDepth++;
                this.TranslateExecutables(sb, elseCode);
                sb.TabDepth--;
            }
        }

        public override void TranslateInlineIncrement(TranspilerContext sb, Expression innerExpression, bool isPrefix, bool isAddition)
        {
            throw new Pastel.ParserException(
                innerExpression.FirstToken,
                "Python does not support ++ or --. Please check all usages with if (@ext_boolean(\"HAS_INCREMENT\")) { ... }");
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

        public override void TranslateIntegerConstant(TranspilerContext sb, int value)
        {
            sb.Append(value);
        }

        public override void TranslateIntegerDivision(TranspilerContext sb, Expression integerNumerator, Expression integerDenominator)
        {
            sb.Append('(');
            this.TranslateExpression(sb, integerNumerator);
            sb.Append(") // (");
            this.TranslateExpression(sb, integerDenominator);
            sb.Append(')');
        }

        public override void TranslateIntToString(TranspilerContext sb, Expression integer)
        {
            sb.Append("str(");
            this.TranslateExpression(sb, integer);
            sb.Append(')');
        }

        public override void TranslateIsValidInteger(TranspilerContext sb, Expression stringValue)
        {
            sb.Append("PST_isValidInteger(");
            this.TranslateExpression(sb, stringValue);
            sb.Append(')');
        }

        public override void TranslateListAdd(TranspilerContext sb, Expression list, Expression item)
        {
            this.TranslateExpression(sb, list);
            sb.Append(".append(");
            this.TranslateExpression(sb, item);
            sb.Append(')');
        }

        public override void TranslateListClear(TranspilerContext sb, Expression list)
        {
            sb.Append("del ");
            this.TranslateExpression(sb, list);
            sb.Append("[:]");
        }

        public override void TranslateListConcat(TranspilerContext sb, Expression list, Expression items)
        {
            this.TranslateExpression(sb, list);
            sb.Append(" + ");
            this.TranslateExpression(sb, items);
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
            sb.Append(".insert(");
            this.TranslateExpression(sb, index);
            sb.Append(", ");
            this.TranslateExpression(sb, item);
            sb.Append(')');
        }

        public override void TranslateListJoinChars(TranspilerContext sb, Expression list)
        {
            sb.Append("''.join(");
            this.TranslateExpression(sb, list);
            sb.Append(')');
        }

        public override void TranslateListJoinStrings(TranspilerContext sb, Expression list, Expression sep)
        {
            this.TranslateExpression(sb, sep);
            sb.Append(".join(");
            this.TranslateExpression(sb, list);
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
            sb.Append("del ");
            this.TranslateExpression(sb, list);
            sb.Append('[');
            this.TranslateExpression(sb, index);
            sb.Append(']');
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
            sb.Append("random.shuffle(");
            this.TranslateExpression(sb, list);
            sb.Append(')');
        }

        public override void TranslateListSize(TranspilerContext sb, Expression list)
        {
            sb.Append("len(");
            this.TranslateExpression(sb, list);
            sb.Append(')');
        }

        public override void TranslateListToArray(TranspilerContext sb, Expression list)
        {
            this.TranslateExpression(sb, list);
            sb.Append("[:]");
        }

        public override void TranslateMathArcCos(TranspilerContext sb, Expression ratio)
        {
            sb.Append("math.acos(");
            this.TranslateExpression(sb, ratio);
            sb.Append(')');
        }

        public override void TranslateMathArcSin(TranspilerContext sb, Expression ratio)
        {
            sb.Append("math.asin(");
            this.TranslateExpression(sb, ratio);
            sb.Append(')');
        }

        public override void TranslateMathArcTan(TranspilerContext sb, Expression yComponent, Expression xComponent)
        {
            sb.Append("math.atan2(");
            this.TranslateExpression(sb, yComponent);
            sb.Append(", ");
            this.TranslateExpression(sb, xComponent);
            sb.Append(')');
        }

        public override void TranslateMathCos(TranspilerContext sb, Expression thetaRadians)
        {
            sb.Append("math.cos(");
            this.TranslateExpression(sb, thetaRadians);
            sb.Append(')');
        }

        public override void TranslateMathLog(TranspilerContext sb, Expression value)
        {
            sb.Append("math.log(");
            this.TranslateExpression(sb, value);
            sb.Append(')');
        }

        public override void TranslateMathPow(TranspilerContext sb, Expression expBase, Expression exponent)
        {
            sb.Append('(');
            this.TranslateExpression(sb, expBase);
            sb.Append(" ** ");
            this.TranslateExpression(sb, exponent);
            sb.Append(')');
        }

        public override void TranslateMathSin(TranspilerContext sb, Expression thetaRadians)
        {
            sb.Append("math.sin(");
            this.TranslateExpression(sb, thetaRadians);
            sb.Append(')');
        }

        public override void TranslateMathTan(TranspilerContext sb, Expression thetaRadians)
        {
            sb.Append("math.tan(");
            this.TranslateExpression(sb, thetaRadians);
            sb.Append(')');
        }

        public override void TranslateMultiplyList(TranspilerContext sb, Expression list, Expression n)
        {
            sb.Append('(');
            this.TranslateExpression(sb, list);
            sb.Append(" * (");
            this.TranslateExpression(sb, n);
            sb.Append("))");
        }

        public override void TranslateNegative(TranspilerContext sb, UnaryOp unaryOp)
        {
            Expression expr = unaryOp.Expression;
            if (expr is InlineConstant || expr is Variable)
            {
                sb.Append('-');
                this.TranslateExpression(sb, expr);
            }
            else
            {
                sb.Append("-(");
                this.TranslateExpression(sb, expr);
                sb.Append(')');
            }
        }

        public override void TranslateNullConstant(TranspilerContext sb)
        {
            sb.Append("None");
        }

        public override void TranslateOrd(TranspilerContext sb, Expression charValue)
        {
            sb.Append("ord(");
            this.TranslateExpression(sb, charValue);
            sb.Append(')');
        }

        public override void TranslateOpChain(TranspilerContext sb, OpChain opChain)
        {
            sb.Append('(');
            Expression[] expressions = opChain.Expressions;
            Pastel.Token[] ops = opChain.Ops;
            for (int i = 0; i < expressions.Length; ++i)
            {
                if (i > 0)
                {
                    // TODO: platform should have an op translator, which would just be a pass-through function for most ops.
                    sb.Append(' ');
                    sb.Append(this.TranslateOp(ops[i - 1].Value));
                    sb.Append(' ');
                }
                this.TranslateExpression(sb, expressions[i]);
            }
            sb.Append(')');
        }

        public override void TranslateParseFloatUnsafe(TranspilerContext sb, Expression stringValue)
        {
            sb.Append("float(");
            this.TranslateExpression(sb, stringValue);
            sb.Append(")");
        }

        public override void TranslateParseInt(TranspilerContext sb, Expression safeStringValue)
        {
            sb.Append("int(");
            this.TranslateExpression(sb, safeStringValue);
            sb.Append(')');
        }

        public override void TranslatePrintStdErr(TranspilerContext sb, Expression value)
        {
            sb.Append("print(");
            this.TranslateExpression(sb, value);
            sb.Append(')');
        }

        public override void TranslatePrintStdOut(TranspilerContext sb, Expression value)
        {
            sb.Append("print(");
            this.TranslateExpression(sb, value);
            sb.Append(')');
        }

        public override void TranslateRandomFloat(TranspilerContext sb)
        {
            sb.Append("random.random()");
        }

        public override void TranslateReturnStatemnt(TranspilerContext sb, ReturnStatement returnStatement)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("return ");
            this.TranslateExpression(sb, returnStatement.Expression);
            sb.Append(this.NewLine);
        }

        public override void TranslateSortedCopyOfIntArray(TranspilerContext sb, Expression intArray)
        {
            sb.Append("PST_sortedCopyOfList(");
            this.TranslateExpression(sb, intArray);
            sb.Append(')');
        }

        public override void TranslateSortedCopyOfStringArray(TranspilerContext sb, Expression stringArray)
        {
            sb.Append("PST_sortedCopyOfList(");
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
            sb.Append("ord(");
            this.TranslateExpression(sb, str);
            sb.Append('[');
            this.TranslateExpression(sb, index);
            sb.Append("])");
        }

        public override void TranslateStringCompareIsReverse(TranspilerContext sb, Expression str1, Expression str2)
        {
            sb.Append('(');
            this.TranslateExpression(sb, str1);
            sb.Append(" > ");
            this.TranslateExpression(sb, str2);
            sb.Append(')');
        }

        public override void TranslateStringConcatAll(TranspilerContext sb, Expression[] strings)
        {
            sb.Append("''.join([");
            for (int i = 0; i < strings.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                this.TranslateExpression(sb, strings[i]);
            }
            sb.Append("])");
        }

        public override void TranslateStringConcatPair(TranspilerContext sb, Expression strLeft, Expression strRight)
        {
            this.TranslateExpression(sb, strLeft);
            sb.Append(" + ");
            this.TranslateExpression(sb, strRight);
        }

        public override void TranslateStringConstant(TranspilerContext sb, string value)
        {
            sb.Append(PastelUtil.ConvertStringValueToCode(value));
        }

        public override void TranslateStringContains(TranspilerContext sb, Expression haystack, Expression needle)
        {
            sb.Append('(');
            this.TranslateExpression(sb, needle);
            sb.Append(" in ");
            this.TranslateExpression(sb, haystack);
            sb.Append(')');
        }

        public override void TranslateStringEndsWith(TranspilerContext sb, Expression haystack, Expression needle)
        {
            this.TranslateExpression(sb, haystack);
            sb.Append(".endswith(");
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
            sb.Append("chr(");
            this.TranslateExpression(sb, charCode);
            sb.Append(')');
        }

        public override void TranslateStringIndexOf(TranspilerContext sb, Expression haystack, Expression needle)
        {
            this.TranslateExpression(sb, haystack);
            sb.Append(".find(");
            this.TranslateExpression(sb, needle);
            sb.Append(')');
        }

        public override void TranslateStringIndexOfWithStart(TranspilerContext sb, Expression haystack, Expression needle, Expression startIndex)
        {
            this.TranslateExpression(sb, haystack);
            sb.Append(".find(");
            this.TranslateExpression(sb, needle);
            sb.Append(", ");
            this.TranslateExpression(sb, startIndex);
            sb.Append(')');
        }

        public override void TranslateStringLastIndexOf(TranspilerContext sb, Expression haystack, Expression needle)
        {
            this.TranslateExpression(sb, haystack);
            sb.Append(".rfind(");
            this.TranslateExpression(sb, needle);
            sb.Append(')');
        }

        public override void TranslateStringLength(TranspilerContext sb, Expression str)
        {
            sb.Append("len(");
            this.TranslateExpression(sb, str);
            sb.Append(')');
        }

        public override void TranslateStringReplace(TranspilerContext sb, Expression haystack, Expression needle, Expression newNeedle)
        {
            this.TranslateExpression(sb, haystack);
            sb.Append(".replace(");
            this.TranslateExpression(sb, needle);
            sb.Append(", ");
            this.TranslateExpression(sb, newNeedle);
            sb.Append(')');
        }

        public override void TranslateStringReverse(TranspilerContext sb, Expression str)
        {
            this.TranslateExpression(sb, str);
            sb.Append("[::-1]");
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
            this.TranslateExpression(sb, haystack);
            sb.Append(".startswith(");
            this.TranslateExpression(sb, needle);
            sb.Append(')');
        }

        public override void TranslateStringSubstring(TranspilerContext sb, Expression str, Expression start, Expression length)
        {
            this.TranslateExpression(sb, str);
            sb.Append('[');
            this.TranslateExpression(sb, start);
            sb.Append(':');
            this.TranslateExpression(sb, start);
            sb.Append(" + ");
            this.TranslateExpression(sb, length);
            sb.Append(']');
        }

        public override void TranslateStringSubstringIsEqualTo(TranspilerContext sb, Expression haystack, Expression startIndex, Expression needle)
        {
            sb.Append("PST_stringCheckSlice(");
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
            sb.Append(".lower()");
        }

        public override void TranslateStringToUpper(TranspilerContext sb, Expression str)
        {
            this.TranslateExpression(sb, str);
            sb.Append(".upper()");
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
            sb.Append(".strip()");
        }

        public override void TranslateStringTrimEnd(TranspilerContext sb, Expression str)
        {
            this.TranslateExpression(sb, str);
            sb.Append(".rstrip()");
        }

        public override void TranslateStringTrimStart(TranspilerContext sb, Expression str)
        {
            this.TranslateExpression(sb, str);
            sb.Append(".lstrip()");
        }

        public override void TranslateStringBuilderAdd(TranspilerContext sb, Expression sbInst, Expression obj)
        {
            this.TranslateExpression(sb, sbInst);
            sb.Append(".append(");
            string t = obj.ResolvedType.RootValue;
            bool isString = t == "string" || t == "char";
            if (isString)
            {
                this.TranslateExpression(sb, obj);
            }
            else
            {
                sb.Append("str(");
                this.TranslateExpression(sb, obj);
                sb.Append(')');
            }
            sb.Append(')');
        }

        public override void TranslateStringBuilderClear(TranspilerContext sb, Expression sbInst)
        {
            this.TranslateListClear(sb, sbInst);
        }

        public override void TranslateStringBuilderNew(TranspilerContext sb)
        {
            sb.Append("[]");
        }

        public override void TranslateStringBuilderToString(TranspilerContext sb, Expression sbInst)
        {
            sb.Append("''.join(");
            this.TranslateExpression(sb, sbInst);
            sb.Append(')');
        }

        public override void TranslateStrongReferenceEquality(TranspilerContext sb, Expression left, Expression right)
        {
            this.TranslateExpression(sb, left);
            sb.Append(" is ");
            this.TranslateExpression(sb, right);
        }

        public override void TranslateStructFieldDereference(TranspilerContext sb, Expression root, StructDefinition structDef, string fieldName, int fieldIndex)
        {
            this.TranslateExpression(sb, root);
            sb.Append('[');
            sb.Append(fieldIndex);
            sb.Append(']');
        }

        public override void TranslateSwitchStatement(TranspilerContext sb, SwitchStatement switchStatement)
        {
            string functionName = sb.CurrentFunctionDefinition.NameToken.Value;
            int switchId = sb.SwitchCounter++;
            PythonFakeSwitchStatement fakeSwitchStatement = PythonFakeSwitchStatement.Build(switchStatement, switchId, functionName);

            sb.Append(sb.CurrentTab);
            sb.Append(fakeSwitchStatement.ConditionVariableName);
            sb.Append(" = ");
            sb.Append(fakeSwitchStatement.DictionaryGlobalName);
            sb.Append(".get(");
            this.TranslateExpression(sb, switchStatement.Condition);
            sb.Append(", ");
            sb.Append(fakeSwitchStatement.DefaultId);
            sb.Append(')');
            sb.Append(this.NewLine);
            this.TranslateIfStatement(sb, fakeSwitchStatement.GenerateIfStatementBinarySearchTree());

            // This list of switch statements will be serialized at the end of the function definition as globals.
            sb.SwitchStatements.Add(fakeSwitchStatement);
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
            sb.Append("PST_tryParseFloat(");
            this.TranslateExpression(sb, stringValue);
            sb.Append(", ");
            this.TranslateExpression(sb, floatOutList);
            sb.Append(')');
        }

        public override void TranslateUtf8BytesToString(TranspilerContext sb, Expression bytes)
        {
            sb.Append("bytes(");
            this.TranslateExpression(sb, bytes);
            sb.Append(").decode('utf-8')");
        }

        public override void TranslateVariable(TranspilerContext sb, Variable variable)
        {
            sb.Append(variable.Name);
        }

        public override void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(varDecl.VariableNameToken.Value);
            sb.Append(" = ");
            this.TranslateExpression(sb, varDecl.Value);
            sb.Append(this.NewLine);
        }

        public override void TranslateWhileLoop(TranspilerContext sb, WhileLoop whileLoop)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("while ");
            this.TranslateExpression(sb, whileLoop.Condition);
            sb.Append(':');
            sb.Append(this.NewLine);
            sb.TabDepth++;
            this.TranslateExecutables(sb, whileLoop.Code);
            sb.TabDepth--;
        }

        public override void GenerateCodeForFunction(TranspilerContext sb, FunctionDefinition funcDef, bool isStatic)
        {
            sb.CurrentFunctionDefinition = funcDef;

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
            sb.Append("):");
            sb.Append(this.NewLine);
            sb.TabDepth++;
            this.TranslateExecutables(sb, funcDef.Code);
            sb.TabDepth--;
            sb.Append(this.NewLine);

            foreach (PythonFakeSwitchStatement switchStatement in sb.SwitchStatements)
            {
                sb.Append(sb.CurrentTab);
                sb.Append(switchStatement.GenerateGlobalDictionaryLookup());
                sb.Append(this.NewLine);
            }
            sb.SwitchStatements.Clear();
            sb.CurrentFunctionDefinition = null;
        }

        public override void GenerateCodeForClass(TranspilerContext sb, ClassDefinition classDef)
        {
            throw new NotImplementedException();
        }

        public override void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef)
        {
            throw new InvalidOperationException("This function should not be called. Python uses lists as structs.");
        }
    }
}
