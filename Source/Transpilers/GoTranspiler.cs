using Pastel.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Transpilers
{
    internal class GoTranspiler : AbstractTranspiler
    {
        public GoTranspiler()
            : base("  ")
        {
            this.UsesStructDefinitions = true;
            this.HasStructsInSeparateFiles = false;
        }

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

        public override void TranslateArrayGet(TranspilerContext sb, Expression array, Expression index)
        {
            this.TranslateExpression(sb, array);
            sb.Append('[');
            this.TranslateExpression(sb, index);
            sb.Append(']');
        }

        public override void TranslateArrayJoin(TranspilerContext sb, Expression array, Expression sep)
        {
            throw new NotImplementedException();
        }

        public override void TranslateArrayLength(TranspilerContext sb, Expression array)
        {
            throw new NotImplementedException();
        }

        public override void TranslateArrayNew(TranspilerContext sb, PType arrayType, Expression lengthExpression)
        {
            throw new NotImplementedException();
        }

        public override void TranslateArraySet(TranspilerContext sb, Expression array, Expression index, Expression value)
        {
            throw new NotImplementedException();
        }

        public override void TranslateAssignment(TranspilerContext sb, Assignment assignment)
        {
            sb.Append(sb.CurrentTab);
            this.TranslateExpression(sb, assignment.Target);
            sb.Append(' ').Append(assignment.OpToken.Value).Append(' ');
            this.TranslateExpression(sb, assignment.Value);
            sb.Append('\n');
        }

        public override void TranslateBase64ToBytes(TranspilerContext sb, Expression base64String)
        {
            throw new NotImplementedException();
        }

        public override void TranslateBase64ToString(TranspilerContext sb, Expression base64String)
        {
            throw new NotImplementedException();
        }

        public override void TranslateBooleanConstant(TranspilerContext sb, bool value)
        {
            throw new NotImplementedException();
        }

        public override void TranslateBooleanNot(TranspilerContext sb, UnaryOp unaryOp)
        {
            throw new NotImplementedException();
        }

        public override void TranslateBreak(TranspilerContext sb)
        {
            throw new NotImplementedException();
        }

        public override void TranslateCast(TranspilerContext sb, PType type, Expression expression)
        {
            throw new NotImplementedException();
        }

        public override void TranslateCharConstant(TranspilerContext sb, char value)
        {
            throw new NotImplementedException();
        }

        public override void TranslateCharToString(TranspilerContext sb, Expression charValue)
        {
            throw new NotImplementedException();
        }

        public override void TranslateChr(TranspilerContext sb, Expression charCode)
        {
            throw new NotImplementedException();
        }

        public override void TranslateConstructorInvocation(TranspilerContext sb, ConstructorInvocation constructorInvocation)
        {
            StructDefinition sDef = constructorInvocation.StructDefinition;
            string name = sDef.NameToken.Value;
            sb.Append("PtrBox_").Append(name).Append("{ o: ");
            sb.Append("&S_").Append(name).Append("{ ");
            for (int i = 0; i < sDef.LocalFieldNames.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append("f_").Append(sDef.LocalFieldNames[i].Value).Append(": ");
                this.TranslateExpression(sb, constructorInvocation.Args[i]);
            }
            sb.Append(" } }");
        }

        public override void TranslateCurrentTimeSeconds(TranspilerContext sb)
        {
            throw new NotImplementedException();
        }

        public override void TranslateDictionaryContainsKey(TranspilerContext sb, Expression dictionary, Expression key)
        {
            throw new NotImplementedException();
        }

        public override void TranslateDictionaryGet(TranspilerContext sb, Expression dictionary, Expression key)
        {
            throw new NotImplementedException();
        }

        public override void TranslateDictionaryKeys(TranspilerContext sb, Expression dictionary)
        {
            throw new NotImplementedException();
        }

        public override void TranslateDictionaryNew(TranspilerContext sb, PType keyType, PType valueType)
        {
            throw new NotImplementedException();
        }

        public override void TranslateDictionaryRemove(TranspilerContext sb, Expression dictionary, Expression key)
        {
            throw new NotImplementedException();
        }

        public override void TranslateDictionarySet(TranspilerContext sb, Expression dictionary, Expression key, Expression value)
        {
            throw new NotImplementedException();
        }

        public override void TranslateDictionarySize(TranspilerContext sb, Expression dictionary)
        {
            throw new NotImplementedException();
        }

        public override void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut)
        {
            throw new NotImplementedException();
        }

        public override void TranslateDictionaryValues(TranspilerContext sb, Expression dictionary)
        {
            throw new NotImplementedException();
        }

        public override void TranslateEmitComment(TranspilerContext sb, string value)
        {
            throw new NotImplementedException();
        }

        public override void TranslateExecutables(TranspilerContext sb, Executable[] executables)
        {
            for (int i = 0; i < executables.Length; i++)
            {
                this.TranslateExecutable(sb, executables[i]);
            }
        }

        public override void TranslateExpressionAsExecutable(TranspilerContext sb, Expression expression)
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

        public override void TranslateFloatConstant(TranspilerContext sb, double value)
        {
            sb.Append(PastelUtil.FloatToString(value));
        }

        public override void TranslateFloatDivision(TranspilerContext sb, Expression floatNumerator, Expression floatDenominator)
        {
            bool wrapA = floatNumerator.ResolvedType.RootValue == "int";
            bool wrapB = floatDenominator.ResolvedType.RootValue == "int";
            sb.Append("(");
            sb.Append(wrapA ? "float64(" : "(");
            this.TranslateExpression(sb, floatNumerator);
            sb.Append(") / ");
            sb.Append(wrapB ? "float64(" : "(");
            this.TranslateExpression(sb, floatDenominator);
            sb.Append("))");
        }

        public override void TranslateFloatToInt(TranspilerContext sb, Expression floatExpr)
        {
            throw new NotImplementedException();
        }

        public override void TranslateFloatToString(TranspilerContext sb, Expression floatExpr)
        {
            throw new NotImplementedException();
        }

        public override void TranslateFunctionInvocation(TranspilerContext sb, FunctionReference funcRef, Expression[] args)
        {
            this.TranslateExpression(sb, funcRef);
            sb.Append('(');
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                this.TranslateExpression(sb, args[i]);
            }
            sb.Append(')');
        }

        public override void TranslateFunctionReference(TranspilerContext sb, FunctionReference funcRef)
        {
            sb.Append("fn_").Append(funcRef.Function.Name);
        }

        public override void TranslateGetFunction(TranspilerContext sb, Expression name)
        {
            throw new NotImplementedException();
        }

        public override void TranslateIfStatement(TranspilerContext sb, IfStatement ifStatement)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("if ");
            this.TranslateExpression(sb, ifStatement.Condition);
            sb.Append(" {\n");
            sb.TabDepth++;
            this.TranslateExecutables(sb, ifStatement.IfCode);
            sb.TabDepth--;
            sb.Append(sb.CurrentTab).Append("}");
            if (ifStatement.ElseCode != null && ifStatement.ElseCode.Length > 0)
            {
                sb.Append(" else {\n");
                sb.TabDepth++;
                this.TranslateExecutables(sb, ifStatement.ElseCode);
                sb.TabDepth--;
                sb.Append(sb.CurrentTab).Append("}");
            }
            sb.Append("\n");
        }

        public override void TranslateInlineIncrement(TranspilerContext sb, Expression innerExpression, bool isPrefix, bool isAddition)
        {
            throw new NotImplementedException();
        }

        public override void TranslateInstanceFieldDereference(TranspilerContext sb, Expression root, ClassDefinition classDef, string fieldName)
        {
            throw new NotImplementedException();
        }

        public override void TranslateIntBuffer16(TranspilerContext sb)
        {
            throw new NotImplementedException();
        }

        public override void TranslateIntegerConstant(TranspilerContext sb, int value)
        {
            sb.Append(value);
        }

        public override void TranslateIntegerDivision(TranspilerContext sb, Expression integerNumerator, Expression integerDenominator)
        {
            sb.Append("((");
            this.TranslateExpression(sb, integerNumerator);
            sb.Append(") / (");
            this.TranslateExpression(sb, integerDenominator);
            sb.Append("))");
        }

        public override void TranslateIntToString(TranspilerContext sb, Expression integer)
        {
            throw new NotImplementedException();
        }

        public override void TranslateIsValidInteger(TranspilerContext sb, Expression stringValue)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListAdd(TranspilerContext sb, Expression list, Expression item)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListClear(TranspilerContext sb, Expression list)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListConcat(TranspilerContext sb, Expression list, Expression items)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListGet(TranspilerContext sb, Expression list, Expression index)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListInsert(TranspilerContext sb, Expression list, Expression index, Expression item)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListJoinChars(TranspilerContext sb, Expression list)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListJoinStrings(TranspilerContext sb, Expression list, Expression sep)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListNew(TranspilerContext sb, PType type)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListPop(TranspilerContext sb, Expression list)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListRemoveAt(TranspilerContext sb, Expression list, Expression index)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListReverse(TranspilerContext sb, Expression list)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListSet(TranspilerContext sb, Expression list, Expression index, Expression value)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListShuffle(TranspilerContext sb, Expression list)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListSize(TranspilerContext sb, Expression list)
        {
            throw new NotImplementedException();
        }

        public override void TranslateListToArray(TranspilerContext sb, Expression list)
        {
            throw new NotImplementedException();
        }

        public override void TranslateMathArcCos(TranspilerContext sb, Expression ratio)
        {
            throw new NotImplementedException();
        }

        public override void TranslateMathArcSin(TranspilerContext sb, Expression ratio)
        {
            throw new NotImplementedException();
        }

        public override void TranslateMathArcTan(TranspilerContext sb, Expression yComponent, Expression xComponent)
        {
            throw new NotImplementedException();
        }

        public override void TranslateMathCos(TranspilerContext sb, Expression thetaRadians)
        {
            throw new NotImplementedException();
        }

        public override void TranslateMathLog(TranspilerContext sb, Expression value)
        {
            throw new NotImplementedException();
        }

        public override void TranslateMathPow(TranspilerContext sb, Expression expBase, Expression exponent)
        {
            sb.MarkFeatureAsBeingUsed("IMPORT:math");
            sb.Append("math.Pow(");
            this.TranslateExpression(sb, expBase);
            sb.Append(", ");
            this.TranslateExpression(sb, exponent);
            sb.Append(')');
        }

        public override void TranslateMathSin(TranspilerContext sb, Expression thetaRadians)
        {
            throw new NotImplementedException();
        }

        public override void TranslateMathTan(TranspilerContext sb, Expression thetaRadians)
        {
            throw new NotImplementedException();
        }

        public override void TranslateMultiplyList(TranspilerContext sb, Expression list, Expression n)
        {
            throw new NotImplementedException();
        }

        public override void TranslateNegative(TranspilerContext sb, UnaryOp unaryOp)
        {
            throw new NotImplementedException();
        }

        public override void TranslateNullConstant(TranspilerContext sb)
        {
            throw new NotImplementedException();
        }

        public override void TranslateOrd(TranspilerContext sb, Expression charValue)
        {
            throw new NotImplementedException();
        }

        public override void TranslateOpChain(TranspilerContext sb, OpChain opChain)
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

            sb.Append('(');
            for (int i = 0; i < opChain.Expressions.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(' ').Append(opChain.Ops[i - 1].Value).Append(' ');
                }
                Expression expr = opChain.Expressions[i];
                bool convertToFloat = doIntToFloatConversion && expr.ResolvedType.RootValue == "int";
                sb.Append(convertToFloat ? "float64(" : "(");
                this.TranslateExpression(sb, opChain.Expressions[i]);
                sb.Append(')');
            }
            sb.Append(')');
        }

        public override void TranslateParseFloatUnsafe(TranspilerContext sb, Expression stringValue)
        {
            throw new NotImplementedException();
        }

        public override void TranslateParseInt(TranspilerContext sb, Expression safeStringValue)
        {
            throw new NotImplementedException();
        }

        public override void TranslatePrintStdErr(TranspilerContext sb, Expression value)
        {
            throw new NotImplementedException();
        }

        public override void TranslatePrintStdOut(TranspilerContext sb, Expression value)
        {
            throw new NotImplementedException();
        }

        public override void TranslateRandomFloat(TranspilerContext sb)
        {
            throw new NotImplementedException();
        }

        public override void TranslateReturnStatemnt(TranspilerContext sb, ReturnStatement returnStatement)
        {
            sb.Append(sb.CurrentTab).Append("return");
            if (returnStatement.Expression != null)
            {
                sb.Append(' ');
                this.TranslateExpression(sb, returnStatement.Expression);
            }
            sb.Append("\n");
        }

        public override void TranslateSortedCopyOfIntArray(TranspilerContext sb, Expression intArray)
        {
            sb.MarkFeatureAsBeingUsed("IMPORT:sort");
            sb.Append("PST_SortedIntArrayCopy(");
            this.TranslateExpression(sb, intArray);
            sb.Append(')');
        }

        public override void TranslateSortedCopyOfStringArray(TranspilerContext sb, Expression stringArray)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringAppend(TranspilerContext sb, Expression str1, Expression str2)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringBuffer16(TranspilerContext sb)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringCharAt(TranspilerContext sb, Expression str, Expression index)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringCharCodeAt(TranspilerContext sb, Expression str, Expression index)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringCompareIsReverse(TranspilerContext sb, Expression str1, Expression str2)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringConcatAll(TranspilerContext sb, Expression[] strings)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringConcatPair(TranspilerContext sb, Expression strLeft, Expression strRight)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringConstant(TranspilerContext sb, string value)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringContains(TranspilerContext sb, Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringEndsWith(TranspilerContext sb, Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringEquals(TranspilerContext sb, Expression left, Expression right)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringFromCharCode(TranspilerContext sb, Expression charCode)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringIndexOf(TranspilerContext sb, Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringIndexOfWithStart(TranspilerContext sb, Expression haystack, Expression needle, Expression startIndex)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringLastIndexOf(TranspilerContext sb, Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringLength(TranspilerContext sb, Expression str)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringReplace(TranspilerContext sb, Expression haystack, Expression needle, Expression newNeedle)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringReverse(TranspilerContext sb, Expression str)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringSplit(TranspilerContext sb, Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringStartsWith(TranspilerContext sb, Expression haystack, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringSubstring(TranspilerContext sb, Expression str, Expression start, Expression length)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringSubstringIsEqualTo(TranspilerContext sb, Expression haystack, Expression startIndex, Expression needle)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringToLower(TranspilerContext sb, Expression str)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringToUpper(TranspilerContext sb, Expression str)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringToUtf8Bytes(TranspilerContext sb, Expression str)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringTrim(TranspilerContext sb, Expression str)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringTrimEnd(TranspilerContext sb, Expression str)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStringTrimStart(TranspilerContext sb, Expression str)
        {
            throw new NotImplementedException();
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
            sb.Append(".o.f_").Append(fieldName);
        }

        public override void TranslateSwitchStatement(TranspilerContext sb, SwitchStatement switchStatement)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override void TranslateUtf8BytesToString(TranspilerContext sb, Expression bytes)
        {
            throw new NotImplementedException();
        }

        public override void TranslateVariable(TranspilerContext sb, Variable variable)
        {
            sb.Append("v_").Append(variable.Name);
        }

        public override void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl)
        {
            sb
                .Append(sb.CurrentTab)
                .Append("var v_")
                .Append(varDecl.VariableNameToken.Value)
                .Append(' ')
                .Append(this.TranslateType(varDecl.Type))
                .Append(" = ");
            this.TranslateExpression(sb, varDecl.Value);
            sb.Append("\n");
        }

        public override void TranslateWhileLoop(TranspilerContext sb, WhileLoop whileLoop)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("for ");
            this.TranslateExpression(sb, whileLoop.Condition);
            sb.Append(" {\n");
            sb.TabDepth++;
            this.TranslateExecutables(sb, whileLoop.Code);
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
            this.TranslateExecutables(sb, funcDef.Code);
            sb.TabDepth--;
            sb.Append("}\n\n");
        }

        public override void GenerateCodeForClass(TranspilerContext sb, ClassDefinition classDef)
        {
            throw new NotImplementedException();
        }

        public override void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef)
        {
            sb
                .Append("type S_")
                .Append(structDef.NameToken.Value)
                .Append(" struct {\n");

            sb.TabDepth++;

            string[] fieldNames = PastelUtil.PadStringsToSameLength(structDef.LocalFieldNames.Select(n => n.Value));
            for (int i = 0; i < fieldNames.Length; i++)
            {
                sb.Append(sb.CurrentTab);
                sb.Append("f_");
                sb.Append(fieldNames[i]);
                sb.Append(" ");
                sb.Append(this.TranslateType(structDef.LocalFieldTypes[i]));
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
