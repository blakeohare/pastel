using Pastel.Parser.ParseNodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Transpilers
{
    internal class GoTranspiler : AbstractTranspiler
    {
        public GoTranspiler(TranspilerContext transpilerCtx)
            : base(transpilerCtx)
        {
            this.UsesStructDefinitions = true;
            this.HasStructsInSeparateFiles = false;
        }

        public override string PreferredTab => "  ";
        public override string PreferredNewline => "\n";

        public override string HelperCodeResourcePath { get { return "Transpilers/Resources/PastelHelper.go"; } }

        public override string TranslateType(PType type)
        {
            switch (type.RootValue)
            {
                case "int": return "int";
                case "double": return "float64";
                case "Array": return "[]" + this.TranslateType(type.Generics[0]);
            }

            if (type.IsStruct)
            {
                return "PtrBox_" + type.RootValue;
            }

            throw new NotImplementedException();
        }

        protected override void WrapCodeImpl(TranspilerContext ctx, ProjectConfig config, List<string> lines, bool isForStruct)
        {
            List<string> headerLines = new List<string>() { "package main", "" };
            string[] imports = ctx.GetFeatures()
                .Where(f => f.StartsWith("IMPORT:"))
                .Select(f => f.Substring("IMPORT:".Length))
                .OrderBy(v => v)
                .ToArray();

            if (imports.Length > 0)
            {
                if (imports.Length == 1)
                {
                    headerLines.Add("import \"" + imports[0] + "\"");
                }
                else
                {
                    headerLines.Add("import (");
                    foreach (string impt in imports)
                    {
                        headerLines.Add("  \"" + impt + "\"");
                    }
                    headerLines.Add(")");
                }
                headerLines.Add("");
            }
            lines.InsertRange(0, headerLines);
        }

        public override StringBuffer TranslateArrayGet(Expression array, Expression index)
        {
            return this.TranslateExpression(array)
                .Push("[")
                .Push(this.TranslateExpression(index))
                .Push("]");
        }

        public override StringBuffer TranslateArrayJoin(Expression array, Expression sep)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateArrayLength(Expression array)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateArrayNew(PType arrayType, Expression lengthExpression)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateArraySet(Expression array, Expression index, Expression value)
        {
            throw new NotImplementedException();
        }

        public override void TranslateAssignment(TranspilerContext sb, Assignment assignment)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(this.TranslateExpressionAsString(assignment.Target));
            sb.Append(' ').Append(assignment.OpToken.Value).Append(' ');
            sb.Append(this.TranslateExpressionAsString(assignment.Value));
            sb.Append('\n');
        }

        public override StringBuffer TranslateBase64ToBytes(Expression base64String)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateBase64ToString(Expression base64String)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateBooleanConstant(bool value)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateBooleanNot(UnaryOp unaryOp)
        {
            throw new NotImplementedException();
        }

        public override void TranslateBreak(TranspilerContext sb)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateCast(PType type, Expression expression)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateCharConstant(char value)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateCharToString(Expression charValue)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateChr(Expression charCode)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateConstructorInvocation(ConstructorInvocation constructorInvocation)
        {
            StructDefinition sDef = constructorInvocation.StructDefinition;
            string name = sDef.NameToken.Value;
            StringBuffer buf = StringBuffer
                .Of("PtrBox_")
                .Push(name)
                .Push("{ o: ")
                .Push("&S_")
                .Push(name)
                .Push("{ ");
            for (int i = 0; i < sDef.FieldNames.Length; i++)
            {
                if (i > 0) buf.Push(", ");
                buf
                    .Push("f_")
                    .Push(sDef.FieldNames[i].Value)
                    .Push(": ")
                    .Push(this.TranslateExpression(constructorInvocation.Args[i]));
            }
            return buf.Push(" } }");
        }

        public override StringBuffer TranslateCurrentTimeSeconds()
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateDictionaryContainsKey(Expression dictionary, Expression key)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateDictionaryGet(Expression dictionary, Expression key)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateDictionaryKeys(Expression dictionary)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateDictionaryNew(PType keyType, PType valueType)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateDictionaryRemove(Expression dictionary, Expression key)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateDictionarySet(Expression dictionary, Expression key, Expression value)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateDictionarySize(Expression dictionary)
        {
            throw new NotImplementedException();
        }

        public override void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateDictionaryValues(Expression dictionary)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateEmitComment(string value)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStatements(TranspilerContext sb, Statement[] statements)
        {
            for (int i = 0; i < statements.Length; i++)
            {
                this.TranslateStatement(sb, statements[i]);
            }
        }

        public override void TranslateExpressionAsStatement(TranspilerContext sb, Expression expression)
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

        public override StringBuffer TranslateFloatConstant(double value)
        {
            return StringBuffer.Of(CodeUtil.FloatToString(value));
        }

        public override StringBuffer TranslateFloatDivision(Expression floatNumerator, Expression floatDenominator)
        {
            bool wrapA = floatNumerator.ResolvedType.RootValue == "int";
            bool wrapB = floatDenominator.ResolvedType.RootValue == "int";
            return StringBuffer
                .Of("(")
                .Push(wrapA ? "float64(" : "(")
                .Push(this.TranslateExpression(floatNumerator))
                .Push(") / ")
                .Push(wrapB ? "float64(" : "(")
                .Push(this.TranslateExpression(floatDenominator))
                .Push("))");
        }

        public override StringBuffer TranslateFloatToInt(Expression floatExpr)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateFloatToString(Expression floatExpr)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateFunctionInvocation(FunctionReference funcRef, Expression[] args)
        {
            StringBuffer buf = this.TranslateExpression(funcRef)
                .Push("(");
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0) buf.Push(", ");
                buf.Push(this.TranslateExpression(args[i]));
            }
            return buf.Push(")");
        }

        public override StringBuffer TranslateFunctionReference(FunctionReference funcRef)
        {
            return StringBuffer.Of("fn_").Push(funcRef.Function.Name);
        }

        public override StringBuffer TranslateGetFunction(Expression name)
        {
            throw new NotImplementedException();
        }

        public override void TranslateIfStatement(TranspilerContext sb, IfStatement ifStatement)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("if ");
            sb.Append(this.TranslateExpressionAsString(ifStatement.Condition));
            sb.Append(" {\n");
            sb.TabDepth++;
            this.TranslateStatements(sb, ifStatement.IfCode);
            sb.TabDepth--;
            sb.Append(sb.CurrentTab).Append("}");
            if (ifStatement.ElseCode != null && ifStatement.ElseCode.Length > 0)
            {
                sb.Append(" else {\n");
                sb.TabDepth++;
                this.TranslateStatements(sb, ifStatement.ElseCode);
                sb.TabDepth--;
                sb.Append(sb.CurrentTab).Append("}");
            }
            sb.Append("\n");
        }

        public override StringBuffer TranslateInlineIncrement(Expression innerExpression, bool isPrefix, bool isAddition)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateIntBuffer16()
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateIntegerConstant(int value)
        {
            return StringBuffer.Of(value.ToString());
        }

        public override StringBuffer TranslateIntegerDivision(Expression integerNumerator, Expression integerDenominator)
        {
            return StringBuffer
                .Of("((")
                .Push(this.TranslateExpression(integerNumerator))
                .Push(") / (")
                .Push(this.TranslateExpression(integerDenominator))
                .Push("))");
        }

        public override StringBuffer TranslateIntToString(Expression integer)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateIsValidInteger(Expression stringValue)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListAdd(Expression list, Expression item)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListClear(Expression list)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListConcat(Expression list, Expression items)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListGet(Expression list, Expression index)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListInsert(Expression list, Expression index, Expression item)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListJoinChars(Expression list)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListJoinStrings(Expression list, Expression sep)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListNew(PType type)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListPop(Expression list)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListRemoveAt(Expression list, Expression index)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListReverse(Expression list)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListSet(Expression list, Expression index, Expression value)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListShuffle(Expression list)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListSize(Expression list)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateListToArray(Expression list)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMathArcCos(Expression ratio)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMathArcSin(Expression ratio)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMathArcTan(Expression yComponent, Expression xComponent)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMathCos(Expression thetaRadians)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMathLog(Expression value)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMathPow(Expression expBase, Expression exponent)
        {
            this.transpilerCtx.MarkFeatureAsBeingUsed("IMPORT:math");

            return StringBuffer.Of("math.Pow(")
                .Push(this.TranslateExpression(expBase))
                .Push(", ")
                .Push(this.TranslateExpression(exponent))
                .Push(")");
        }

        public override StringBuffer TranslateMathSin(Expression thetaRadians)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMathTan(Expression thetaRadians)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateMultiplyList(Expression list, Expression n)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateNegative(UnaryOp unaryOp)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateNullConstant()
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateOrd(Expression charValue)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateOpChain(OpChain opChain)
        {
            bool isFloat = false;
            bool containsInt = false;
            Expression[] expressions = opChain.Expressions;
            for (int i = 0; i < expressions.Length; i++)
            {
                string type = expressions[i].ResolvedType.RootValue;
                if (type == "double")
                {
                    isFloat = true;
                }
                else if (type == "int")
                {
                    containsInt = true;
                }
            }

            bool doIntToFloatConversion = isFloat && containsInt;

            StringBuffer buf = StringBuffer.Of("(");
            for (int i = 0; i < opChain.Expressions.Length; i++)
            {
                if (i > 0)
                {
                    buf.Push(" ").Push(opChain.Ops[i - 1].Value).Push(" ");
                }
                Expression expr = opChain.Expressions[i];
                bool convertToFloat = doIntToFloatConversion && expr.ResolvedType.RootValue == "int";
                buf
                    .Push(convertToFloat ? "float64(" : "(")
                    .Push(this.TranslateExpression(opChain.Expressions[i]))
                    .Push(")");
            }
            return buf.Push(")");
        }

        public override StringBuffer TranslateParseFloatUnsafe(Expression stringValue)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateParseInt(Expression safeStringValue)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslatePrintStdErr(Expression value)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslatePrintStdOut(Expression value)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateRandomFloat()
        {
            throw new NotImplementedException();
        }

        public override void TranslateReturnStatemnt(TranspilerContext sb, ReturnStatement returnStatement)
        {
            sb.Append(sb.CurrentTab).Append("return");
            if (returnStatement.Expression != null)
            {
                sb.Append(' ');
                sb.Append(this.TranslateExpressionAsString(returnStatement.Expression));
            }
            sb.Append("\n");
        }

        public override StringBuffer TranslateSortedCopyOfIntArray(Expression intArray)
        {
            this.transpilerCtx.MarkFeatureAsBeingUsed("IMPORT:sort");
            return StringBuffer
                .Of("PST_SortedIntArrayCopy(")
                .Push(this.TranslateExpression(intArray))
                .Push(")");
        }

        public override StringBuffer TranslateSortedCopyOfStringArray(Expression stringArray)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringAppend(Expression str1, Expression str2)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringBuffer16()
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringCharAt(Expression str, Expression index)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringCharCodeAt(Expression str, Expression index)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringCompareIsReverse(Expression str1, Expression str2)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringConcatAll(Expression[] strings)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringConcatPair(Expression strLeft, Expression strRight)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringConstant(string value)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringContains(Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringEndsWith(Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringEquals(Expression left, Expression right)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringFromCharCode(Expression charCode)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringIndexOf(Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringIndexOfWithStart(Expression haystack, Expression needle, Expression startIndex)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringLastIndexOf(Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringLength(Expression str)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringReplace(Expression haystack, Expression needle, Expression newNeedle)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringReverse(Expression str)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringSplit(Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringStartsWith(Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringSubstring(Expression str, Expression start, Expression length)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringSubstringIsEqualTo(Expression haystack, Expression startIndex, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringToLower(Expression str)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringToUpper(Expression str)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringToUtf8Bytes(Expression str)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringTrim(Expression str)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringTrimEnd(Expression str)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStringTrimStart(Expression str)
        {
            throw new NotImplementedException();
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
                .Push(".o.f_")
                .Push(fieldName);
        }

        public override void TranslateSwitchStatement(TranspilerContext sb, SwitchStatement switchStatement)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateToCodeString(Expression str)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateTryParseFloat(Expression stringValue, Expression floatOutList)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateUtf8BytesToString(Expression bytes)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateVariable(Variable variable)
        {
            return StringBuffer.Of("v_").Push(variable.Name);
        }

        public override void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl)
        {
            sb
                .Append(sb.CurrentTab)
                .Append("var v_")
                .Append(varDecl.VariableNameToken.Value)
                .Append(' ')
                .Append(this.TranslateType(varDecl.Type))
                .Append(" = ")
                .Append(this.TranslateExpressionAsString(varDecl.Value))
                .Append("\n");
        }

        public override void TranslateWhileLoop(TranspilerContext sb, WhileLoop whileLoop)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("for ");
            sb.Append(this.TranslateExpressionAsString(whileLoop.Condition));
            sb.Append(" {\n");
            sb.TabDepth++;
            this.TranslateStatements(sb, whileLoop.Code);
            sb.TabDepth--;
            sb.Append(sb.CurrentTab).Append("}\n");
        }

        public override void GenerateCodeForFunction(TranspilerContext sb, FunctionDefinition funcDef, bool isStatic)
        {
            sb
                .Append("func fn_")
                .Append(funcDef.Name)
                .Append('(');
            for (int i = 0; i < funcDef.ArgNames.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb
                    .Append("v_")
                    .Append(funcDef.ArgNames[i].Value)
                    .Append(' ')
                    .Append(this.TranslateType(funcDef.ArgTypes[i]));
            }
            sb.Append(')');
            if (funcDef.ReturnType.RootValue != "void")
            {
                sb.Append(" ").Append(this.TranslateType(funcDef.ReturnType));
            }
            sb.Append(" {\n");
            sb.TabDepth++;
            this.TranslateStatements(sb, funcDef.Code);
            sb.TabDepth--;
            sb.Append("}\n\n");
        }

        public override void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef)
        {
            sb
                .Append("type S_")
                .Append(structDef.NameToken.Value)
                .Append(" struct {\n");

            sb.TabDepth++;

            string[] fieldNames = CodeUtil.PadStringsToSameLength(structDef.FieldNames.Select(n => n.Value));
            for (int i = 0; i < fieldNames.Length; i++)
            {
                sb.Append(sb.CurrentTab);
                sb.Append("f_");
                sb.Append(fieldNames[i]);
                sb.Append(" ");
                sb.Append(this.TranslateType(structDef.FieldTypes[i]));
                sb.Append('\n');
            }
            sb.TabDepth--;

            sb
                .Append("}\n")
                .Append("type PtrBox_")
                .Append(structDef.NameToken.Value)
                .Append(" struct {\n");
            sb.TabDepth++;
            sb
                .Append(sb.CurrentTab)
                .Append("o *S_")
                .Append(structDef.NameToken.Value)
                .Append("\n");
            sb.TabDepth--;
            sb
                .Append("}\n");
        }
    }
}
