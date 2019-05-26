using Pastel.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Transpilers
{
    internal class PhpTranspiler : CurlyBraceTranspiler
    {
        public PhpTranspiler() : base("  ", "\n", true)
        {
            this.HasNewLineAtEndOfFile = false;
        }

        public override string HelperCodeResourcePath { get { return "Transpilers/Resources/PastelHelper.php"; } }

        public override string TranslateType(PType type)
        {
            throw new Exception(); // PHP doesn't have strict types.
        }

        protected override void WrapCodeImpl(ProjectConfig config, List<string> lines, bool isForStruct)
        {
            if (isForStruct)
            {
                PastelUtil.IndentLines(this.TabChar, lines);

                lines.InsertRange(0, new string[]
                {
                    "<?php",
                    "",
                });

                lines.Add("?>");
            }
            else
            {
                PastelUtil.IndentLines(this.TabChar + this.TabChar, lines);

                string t = this.TabChar;
                List<string> prefixes = new List<string>()
                {
                    "// ensures array's pointer behavior behaves according to Pastel standards.",
                    "class PastelPtrArray {",
                    t + "var $arr = array();",
                    "}",
                    "function _pastelWrapValue($value) { $o = new PastelPtrArray(); $o->arr = $value; return $o; }",
                    "// redundant-but-pleasantly-named helper methods for external callers",
                    "function pastelWrapList($arr) { return _pastelWrapValue($arr); }",
                    "function pastelWrapDictionary($arr) { return _pastelWrapValue($arr); }",
                    "",
                    "class PastelGeneratedCode {"
                };

                PastelUtil.IndentLines(this.TabChar, prefixes);

                prefixes.InsertRange(0, new string[] {
                    "<?php",
                    ""
                });

                lines.InsertRange(0, prefixes);

                lines.Add(this.TabChar + "}");
                lines.Add("");
                lines.Add("?>");
            }
        }
        public override void TranslateFunctionInvocation(TranspilerContext sb, FunctionReference funcRef, Expression[] args)
        {
            sb.Append("self::");
            base.TranslateFunctionInvocation(sb, funcRef, args);
        }

        public override void TranslateFunctionPointerInvocation(TranspilerContext sb, FunctionPointerInvocation fpi)
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

        public override void TranslateBase64ToString(TranspilerContext sb, Expression base64String)
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
            sb.Append("new ");
            sb.Append(constructorInvocation.StructType.NameToken.Value);
            sb.Append('(');
            Expression[] args = constructorInvocation.Args;
            for (int i = 0; i < args.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                this.TranslateExpression(sb, args[i]);
            }
            sb.Append(')');
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
            throw new NotImplementedException();
        }

        public override void TranslateFloatToString(TranspilerContext sb, Expression floatExpr)
        {
            throw new NotImplementedException();
        }

        public override void TranslateGetFunction(TranspilerContext sb, Expression name)
        {
            throw new NotImplementedException();
        }

        public override void TranslateIntBuffer16(TranspilerContext sb)
        {
            throw new NotImplementedException();
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
            sb.Append("pow(");
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

        public override void TranslateNullConstant(TranspilerContext sb)
        {
            throw new NotImplementedException();
        }

        public override void TranslateOrd(TranspilerContext sb, Expression charValue)
        {
            throw new NotImplementedException();
        }

        public override void TranslateParseFloatUnsafe(TranspilerContext sb, Expression stringValue)
        {
            throw new NotImplementedException();
        }

        public override void TranslateParseInt(TranspilerContext sb, Expression safeStringValue)
        {
            throw new NotImplementedException();
        }

        public override void TranslateRandomFloat(TranspilerContext sb)
        {
            throw new NotImplementedException();
        }

        public override void TranslateSortedCopyOfIntArray(TranspilerContext sb, Expression intArray)
        {
            sb.Append("self::PST_sortedCopyOfIntArray(");
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

        public override void TranslateStrongReferenceEquality(TranspilerContext sb, Expression left, Expression right)
        {
            throw new NotImplementedException();
        }

        public override void TranslateStructFieldDereference(TranspilerContext sb, Expression root, StructDefinition structDef, string fieldName, int fieldIndex)
        {
            this.TranslateExpression(sb, root);
            sb.Append("->");
            sb.Append(fieldName);
        }

        public override void TranslateTryParseFloat(TranspilerContext sb, Expression stringValue, Expression floatOutList)
        {
            throw new NotImplementedException();
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
            sb.Append(';');
            sb.Append(this.NewLine);
        }

        public override void GenerateCodeForFunction(TranspilerContext sb, FunctionDefinition funcDef)
        {
            sb.Append(this.NewLine);
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
            sb.Append(") {");
            sb.Append(this.NewLine);
            sb.TabDepth++;

            this.TranslateExecutables(sb, funcDef.Code);
            
            sb.TabDepth--;
            sb.Append(sb.CurrentTab);
            sb.Append('}');
            sb.Append(this.NewLine);
            sb.Append(this.NewLine);

        }

        public override void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef)
        {
            string name = structDef.NameToken.Value;
            sb.Append(sb.CurrentTab);
            sb.Append("class ");
            sb.Append(name);
            sb.Append(" {");
            sb.Append(this.NewLine);
            sb.TabDepth++;

            string[] fieldNames = structDef.ArgNames.Select(a => a.Value).ToArray();

            foreach (string fieldName in fieldNames)
            {
                sb.Append(sb.CurrentTab);
                sb.Append("var $");
                sb.Append(fieldName);
                sb.Append(';');
                sb.Append(this.NewLine);
            }
            sb.Append(sb.CurrentTab);
            sb.Append("function __construct(");
            for (int i = 0; i < fieldNames.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                sb.Append("$a");
                sb.Append(i);
            }
            sb.Append(") {");
            sb.Append(this.NewLine);
            sb.TabDepth++;
            for (int i = 0; i < fieldNames.Length; ++i)
            {
                sb.Append(sb.CurrentTab);
                sb.Append("$this->");
                sb.Append(fieldNames[i]);
                sb.Append(" = $a");
                sb.Append(i);
                sb.Append(';');
                sb.Append(this.NewLine);
            }
            sb.TabDepth--;
            sb.Append(sb.CurrentTab);
            sb.Append('}');
            sb.Append(this.NewLine);

            sb.TabDepth--;
            sb.Append(sb.CurrentTab);
            sb.Append('}');
            sb.Append(this.NewLine);
        }
    }
}
