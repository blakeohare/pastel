using Pastel.Parser;
using Pastel.Parser.ParseNodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Transpilers.Java
{
    internal class JavaTranspiler : CurlyBraceTranspiler
    {
        public JavaTranspiler(TranspilerContext transpilerCtx)
            : base(transpilerCtx, true)
        {
            this.TypeTranspiler = new JavaTypeTranspiler();
            this.Exporter = new JavaExporter();
        }

        public override string HelperCodeResourcePath { get { return "Transpilers/Java/PastelHelper.java"; } }

        private JavaTypeTranspiler JavaTypeTranspiler { get { return (JavaTypeTranspiler)TypeTranspiler; } }

        protected override void WrapCodeImpl(TranspilerContext ctx, ProjectConfig config, List<string> lines, bool isForStruct)
        {
            if (!isForStruct && config.WrappingClassNameForFunctions != null)
            {
                CodeUtil.IndentLines(lines);
                lines.InsertRange(0, new string[] { "public final class " + config.WrappingClassNameForFunctions + " {", "" });
                lines.Add("}");
            }

            List<string> prefixData = new List<string>();

            string nsValue = isForStruct ? config.NamespaceForStructs : config.NamespaceForFunctions;
            if (nsValue != null)
            {
                prefixData.AddRange(new string[] { "package " + nsValue + ";", "" });
            }

            if (config.Imports.Count > 0)
            {
                prefixData.AddRange(
                    config.Imports
                        .OrderBy(t => t)
                        .Select(t => "import " + t + ";")
                        .Concat(new string[] { "" }));
            }

            if (prefixData.Count > 0) lines.InsertRange(0, prefixData);
        }

        public override StringBuffer TranslateFunctionPointerInvocation(FunctionPointerInvocation fpi)
        {
            return StringBuffer.Of("(")
                .Push(TypeTranspiler.TranslateType(fpi.ResolvedType))
                .Push(") TranslationHelper.invokeFunctionPointer(")
                .Push(TranslateExpression(fpi.Root))
                .Push(", new Object[] {")
                .Push(TranslateCommaDelimitedExpressions(fpi.Args))
                .Push("})")
                .WithTightness(ExpressionTightness.UNARY_PREFIX);
        }

        public override StringBuffer TranslatePrintStdErr(Expression value)
        {
            return StringBuffer
                .Of("PlatformTranslationHelper.printStdErr(")
                .Push(TranslateExpression(value))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslatePrintStdOut(Expression value)
        {
            return StringBuffer
                .Of("PlatformTranslationHelper.printStdOut(")
                .Push(TranslateExpression(value))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateArrayGet(Expression array, Expression index)
        {
            return TranslateExpression(array)
                .Push("[")
                .Push(TranslateExpression(index))
                .Push("]")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateArrayJoin(Expression array, Expression sep)
        {
            return StringBuffer
                .Of("String.join(")
                .Push(TranslateExpression(sep))
                .Push(", ")
                .Push(TranslateExpression(array))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateArrayLength(Expression array)
        {
            return TranslateExpression(array)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".length")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateArrayNew(PType arrayType, Expression lengthExpression)
        {
            // In the event of multi-dimensional jagged arrays, the outermost array length goes in the innermost bracket.
            // Unwrap nested arrays in the type and run the code as normal, and then add that many []'s to the end.
            int bracketSuffixCount = 0;
            while (arrayType.RootValue == "Array")
            {
                arrayType = arrayType.Generics[0];
                bracketSuffixCount++;
            }

            StringBuffer buf = StringBuffer.Of("new ");
            if (arrayType.RootValue == "Dictionary")
            {
                buf.Push("HashMap");
            }
            else if (arrayType.RootValue == "List")
            {
                buf.Push("ArrayList");
            }
            else
            {
                buf.Push(TypeTranspiler.TranslateType(arrayType));
            }
            buf
                .Push("[")
                .Push(TranslateExpression(lengthExpression))
                .Push("]");

            while (bracketSuffixCount-- > 0)
            {
                buf.Push("[]");
            }
            return buf.WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
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
            return StringBuffer
                .Of("PST_base64ToBytes(")
                .Push(TranslateExpression(base64String))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateBase64ToString(Expression base64String)
        {
            return StringBuffer.Of("PST_base64ToString(")
                .Push(TranslateExpression(base64String))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateCast(PType type, Expression expression)
        {
            // TODO: No. Get rid of this nonsense.
            DotField dotField = expression as DotField;
            if (dotField != null &&
                dotField.Root.ResolvedType.RootValue == "Value" &&
                dotField.FieldName.Value == "internalValue")
            {
                if (type.RootValue == "int")
                {
                    return TranslateExpression(dotField.Root)
                        .Push(".intValue");
                }
                else if (type.RootValue == "bool")
                {
                    return StringBuffer
                        .Of("(")
                        .Push(TranslateExpression(dotField.Root))
                        .Push(".intValue == 1)");
                }
            }

            return StringBuffer
                .Of("(")
                .Push(TypeTranspiler.TranslateType(type))
                .Push(") ")
                .Push(TranslateExpression(expression).EnsureTightness(ExpressionTightness.UNARY_PREFIX))
                .WithTightness(ExpressionTightness.UNARY_PREFIX);
        }

        public override StringBuffer TranslateCharConstant(char value)
        {
            return StringBuffer
                .Of(CodeUtil.ConvertCharToCharConstantCode(value))
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateCharToString(Expression charValue)
        {
            return StringBuffer
                .Of("\"\" + ")
                .Push(TranslateExpression(charValue).EnsureGreaterTightness(ExpressionTightness.ADDITION))
                .WithTightness(ExpressionTightness.ADDITION);
        }

        public override StringBuffer TranslateChr(Expression charCode)
        {
            return StringBuffer
                .Of("Character.toString((char) ")
                .Push(TranslateExpression(charCode))
                .Push(")");
        }

        public override StringBuffer TranslateConstructorInvocation(ConstructorInvocation constructorInvocation)
        {
            StringBuffer buf = StringBuffer.Of("new ");
            string structType = constructorInvocation.StructDefinition.NameToken.Value;
            if (structType == "ClassValue")
            {
                structType = "org.crayonlang.interpreter.structs.ClassValue";
            }
            buf
                .Push(structType)
                .Push("(");
            Expression[] args = constructorInvocation.Args;
            for (int i = 0; i < args.Length; ++i)
            {
                if (i > 0) buf.Push(", ");
                buf.Push(TranslateExpression(args[i]));
            }
            return buf.
                Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateCurrentTimeSeconds()
        {
            return StringBuffer
                .Of("System.currentTimeMillis() / 1000.0")
                .WithTightness(ExpressionTightness.MULTIPLICATION);
        }

        public override StringBuffer TranslateDictionaryContainsKey(Expression dictionary, Expression key)
        {
            return TranslateExpression(dictionary)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".containsKey(")
                .Push(TranslateExpression(key))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryGet(Expression dictionary, Expression key)
        {
            return TranslateExpression(dictionary)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".get(")
                .Push(TranslateExpression(key))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryKeys(Expression dictionary)
        {
            StringBuffer buf = StringBuffer.Of("PST_convert");
            switch (dictionary.ResolvedType.Generics[0].RootValue)
            {
                case "int": buf.Push("Integer"); break;
                case "string": buf.Push("String"); break;

                default:
                    // TODO: Explicitly disallow dictionaries with non-intenger or non-string keys at compile time.
                    throw new NotImplementedException();
            }
            return buf
                .Push("SetToArray(")
                .Push(TranslateExpression(dictionary).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                .Push(".keySet())")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);

            // TODO: do a simple .keySet().toArray(TranslationHelper.STATIC_INSTANCE_OF_ZERO_LENGTH_INT_OR_STRING_ARRAY);
        }

        public override StringBuffer TranslateDictionaryNew(PType keyType, PType valueType)
        {
            return StringBuffer
                .Of("new HashMap<")
                .Push(JavaTypeTranspiler.TranslateJavaNestedType(keyType))
                .Push(", ")
                .Push(JavaTypeTranspiler.TranslateJavaNestedType(valueType))
                .Push(">()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionaryRemove(Expression dictionary, Expression key)
        {
            return TranslateExpression(dictionary)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".remove(")
                .Push(TranslateExpression(key))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionarySet(Expression dictionary, Expression key, Expression value)
        {
            return TranslateExpression(dictionary)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".put(")
                .Push(TranslateExpression(key))
                .Push(", ")
                .Push(TranslateExpression(value))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateDictionarySize(Expression dictionary)
        {
            return TranslateExpression(dictionary)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".size()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut)
        {
            PType[] dictTypes = dictionary.ResolvedType.Generics;
            PType keyType = dictTypes[0];
            PType valueType = dictTypes[1];
            bool keyTypeIsBoxed = JavaTypeTranspiler.IsJavaPrimitiveTypeBoxed(keyType);
            bool keyExpressionIsSimple = key is Variable || key is InlineConstant;
            string keyVar = null;
            if (!keyExpressionIsSimple)
            {
                keyVar = "_PST_dictKey" + sb.SwitchCounter++;
                sb.Append(sb.CurrentTab);
                sb.Append(TypeTranspiler.TranslateType(keyType));
                sb.Append(' ');
                sb.Append(keyVar);
                sb.Append(" = ");
                sb.Append(TranslateExpressionAsString(key));
                sb.Append(";\n");
            }

            string lookupVar = "_PST_dictLookup" + sb.SwitchCounter++;
            sb.Append(sb.CurrentTab);
            sb.Append(JavaTypeTranspiler.TranslateJavaNestedType(valueType));
            sb.Append(' ');
            sb.Append(lookupVar);
            sb.Append(" = ");
            sb.Append(TranslateExpressionAsString(dictionary));
            sb.Append(".get(");
            if (keyExpressionIsSimple)
            {
                sb.Append(TranslateExpressionAsString(key));
            }
            else
            {
                sb.Append(keyVar);
            }
            sb.Append(");\n");
            sb.Append(sb.CurrentTab);
            sb.Append(varOut.Name);
            sb.Append(" = ");
            sb.Append(lookupVar);
            sb.Append(" == null ? (");

            if (!keyTypeIsBoxed)
            {
                // if the key is not a primitive, then we don't know if this null is a lack of a value or
                // if it's the actual desired value. We must explicitly call .containsKey to be certain.
                // In this specific case, we must do a double-lookup.
                sb.Append(TranslateExpressionAsString(dictionary));
                sb.Append(".containsKey(");
                if (keyExpressionIsSimple) sb.Append(TranslateExpressionAsString(key));
                else sb.Append(keyVar);
                sb.Append(") ? null : (");
                sb.Append(TranslateExpressionAsString(fallbackValue));
                sb.Append(")");
            }
            else
            {
                sb.Append(TranslateExpressionAsString(fallbackValue));
            }
            sb.Append(") : ");
            sb.Append(lookupVar);
            sb.Append(";\n");
        }

        public override StringBuffer TranslateDictionaryValues(Expression dictionary)
        {
            return TranslateExpression(dictionary)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".values()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateExtensibleCallbackInvoke(Expression name, Expression argsArray)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateFloatBuffer16()
        {
            return StringBuffer
                .Of("PST_floatBuffer16")
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
            return TranslateExpression(floatExpr)
                .EnsureTightness(ExpressionTightness.UNARY_PREFIX)
                .Prepend("(int) ")
                .WithTightness(ExpressionTightness.UNARY_PREFIX);
        }

        public override StringBuffer TranslateFloatToString(Expression floatExpr)
        {
            return StringBuffer.Of("Double.toString(")
                .Push(TranslateExpression(floatExpr))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateGetFunction(Expression name)
        {
            return StringBuffer.Of("TranslationHelper.getFunction(")
                .Push(TranslateExpression(name))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateIntBuffer16()
        {
            return StringBuffer
                .Of("PST_intBuffer16")
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateIntegerDivision(Expression integerNumerator, Expression integerDenominator)
        {
            return TranslateExpression(integerNumerator)
                .EnsureTightness(ExpressionTightness.MULTIPLICATION)
                .Push(" / ")
                .Push(TranslateExpression(integerDenominator).EnsureGreaterTightness(ExpressionTightness.MULTIPLICATION))
                .WithTightness(ExpressionTightness.MULTIPLICATION);
        }

        public override StringBuffer TranslateIntToString(Expression integer)
        {
            return StringBuffer
                .Of("Integer.toString(")
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
                .Push(".add(")
                .Push(TranslateExpression(item))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListClear(Expression list)
        {
            return TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".clear()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListConcat(Expression list, Expression items)
        {
            // Fun fact: this is actually not implemented. The only place that this is used is by Value.
            return StringBuffer
                .Of("PST_concatLists(")
                .Push(TranslateExpression(list))
                .Push(", ")
                .Push(TranslateExpression(items))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListGet(Expression list, Expression index)
        {
            return TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".get(")
                .Push(TranslateExpression(index))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListInsert(Expression list, Expression index, Expression item)
        {
            return TranslateExpression(list)
                .Push(".add(")
                .Push(TranslateExpression(index))
                .Push(", ")
                .Push(TranslateExpression(item))
                .Push(")");
        }

        public override StringBuffer TranslateListJoinChars(Expression list)
        {
            return StringBuffer
                .Of("PST_joinChars(")
                .Push(TranslateExpression(list))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListJoinStrings(Expression list, Expression sep)
        {
            return StringBuffer
                .Of("PST_joinList(")
                .Push(TranslateExpression(sep))
                .Push(", ")
                .Push(TranslateExpression(list))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListNew(PType type)
        {
            return StringBuffer
                .Of("new ArrayList<")
                .Push(JavaTypeTranspiler.TranslateJavaNestedType(type))
                .Push(">()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListPop(Expression list)
        {
            bool useInlineListPop =
                list is Variable ||
                list is DotField && ((DotField)list).Root is Variable;

            if (useInlineListPop)
            {
                return TranslateExpression(list)
                    .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                    .Push(".remove(")
                    .Push(TranslateExpression(list).EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE))
                    .Push(".size() - 1)")
                    .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
            }

            return StringBuffer
                .Of("PST_listPop(")
                .Push(TranslateExpression(list))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListRemoveAt(Expression list, Expression index)
        {
            return TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".remove(")
                .Push(TranslateExpression(index))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListReverse(Expression list)
        {
            return StringBuffer
                .Of("java.util.Collections.reverse(")
                .Push(TranslateExpression(list))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListSet(Expression list, Expression index, Expression value)
        {
            return TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".set(")
                .Push(TranslateExpression(index))
                .Push(", ")
                .Push(TranslateExpression(value))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListShuffle(Expression list)
        {
            // This is currently only used and implemented by Value lists.
            return StringBuffer
                .Of("PST_listShuffle(")
                .Push(TranslateExpression(list))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListSize(Expression list)
        {
            return TranslateExpression(list)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".size()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateListToArray(Expression list)
        {
            PType itemType = list.ResolvedType.Generics[0];
            switch (itemType.TypeName)
            {
                case "bool":
                case "byte":
                case "int":
                case "double":
                case "char":
                    string primitiveName = itemType.TypeName;
                    return StringBuffer
                        .Of("PST_listToArray")
                        .Push("" + (char)(primitiveName[0] + 'A' - 'a'))
                        .Push(primitiveName.Substring(1))
                        .Push("(")
                        .Push(TranslateExpression(list))
                        .Push(")")
                        .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);

                case "string":
                    return TranslateExpression(list)
                        .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                        .Push(".toArray(PST_emptyArrayString)")
                        .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);

                case "object":
                    return TranslateExpression(list)
                        .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                        .Push(".toArray()")
                        .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);

                case "List":
                    return TranslateExpression(list)
                        .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                        .Push(".toArray(PST_emptyArrayList)")
                        .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);

                case "Dictionary":
                    return TranslateExpression(list)
                        .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                        .Push(".toArray(PST_emptyArrayMap)")
                        .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);

                case "Array":
                    throw new NotImplementedException("not implemented: java list of arrays to array");

                default:
                    if (itemType.IsStruct)
                    {
                        return TranslateExpression(list)
                            .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                            .Push(".toArray(")
                            .Push(TypeTranspiler.TranslateType(itemType))
                            .Push(".EMPTY_ARRAY)")
                            .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
                    }

                    // I think I covered all the types that are supported.
                    throw new NotImplementedException();
            }
        }

        public override StringBuffer TranslateMathArcCos(Expression ratio)
        {
            return StringBuffer
                .Of("Math.acos(")
                .Push(TranslateExpression(ratio))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathArcSin(Expression ratio)
        {
            return StringBuffer
                .Of("Math.asin(")
                .Push(TranslateExpression(ratio))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathArcTan(Expression yComponent, Expression xComponent)
        {
            return StringBuffer
                .Of("Math.atan2(")
                .Push(TranslateExpression(yComponent))
                .Push(", ")
                .Push(TranslateExpression(xComponent))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathCos(Expression thetaRadians)
        {
            return StringBuffer
                .Of("Math.cos(")
                .Push(TranslateExpression(thetaRadians))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathLog(Expression value)
        {
            return StringBuffer
                .Of("Math.log(")
                .Push(TranslateExpression(value))
                .Push(")");
        }

        public override StringBuffer TranslateMathPow(Expression expBase, Expression exponent)
        {
            return StringBuffer
                .Of("Math.pow(")
                .Push(TranslateExpression(expBase))
                .Push(", ")
                .Push(TranslateExpression(exponent))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathSin(Expression thetaRadians)
        {
            return StringBuffer
                .Of("Math.sin(")
                .Push(TranslateExpression(thetaRadians))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMathTan(Expression thetaRadians)
        {
            return StringBuffer
                .Of("Math.tan(")
                .Push(TranslateExpression(thetaRadians))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateMultiplyList(Expression list, Expression n)
        {
            // TODO: this helper function is not actually implemented yet.
            return StringBuffer
                .Of("PST_multiplyList(")
                .Push(TranslateExpression(list))
                .Push(", ")
                .Push(TranslateExpression(n))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateNullConstant()
        {
            return StringBuffer
                .Of("null")
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateOrd(Expression charValue)
        {
            return TranslateExpression(charValue)
                .EnsureTightness(ExpressionTightness.UNARY_PREFIX)
                .Prepend("(int) ")
                .WithTightness(ExpressionTightness.UNARY_PREFIX);
        }

        public override StringBuffer TranslateParseFloatUnsafe(Expression stringValue)
        {
            return StringBuffer
                .Of("Double.parseDouble(")
                .Push(TranslateExpression(stringValue))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateParseInt(Expression safeStringValue)
        {
            return StringBuffer
                .Of("Integer.parseInt(")
                .Push(TranslateExpression(safeStringValue))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateRandomFloat()
        {
            return StringBuffer
                .Of("PST_random.nextDouble()")
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateSortedCopyOfIntArray(Expression intArray)
        {
            return StringBuffer
                .Of("PST_sortedCopyOfIntArray(")
                .Push(TranslateExpression(intArray))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateSortedCopyOfStringArray(Expression stringArray)
        {
            return StringBuffer
                .Of("PST_sortedCopyOfStringArray(")
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
            return StringBuffer
                .Of("PST_stringBuffer16")
                .WithTightness(ExpressionTightness.ATOMIC);
        }

        public override StringBuffer TranslateStringCharAt(Expression str, Expression index)
        {
            return TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".charAt(")
                .Push(TranslateExpression(index))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringCharCodeAt(Expression str, Expression index)
        {
            return TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".charAt(")
                .Push(TranslateExpression(index))
                .Push(")")
                .Prepend("(int) ")
                .WithTightness(ExpressionTightness.UNARY_PREFIX);
        }

        public override StringBuffer TranslateStringCompareIsReverse(Expression str1, Expression str2)
        {
            return TranslateExpression(str1)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".compareTo(")
                .Push(TranslateExpression(str2))
                .Push(") > 0")
                .WithTightness(ExpressionTightness.INEQUALITY);
        }

        public override StringBuffer TranslateStringConcatAll(Expression[] strings)
        {
            // Java, when presented with sequential inline additions of strings, automatically
            // uses a string builder.
            StringBuffer acc = TranslateExpression(strings[0]);
            for (int i = 1; i < strings.Length; ++i)
            {
                acc
                    .EnsureTightness(ExpressionTightness.ADDITION)
                    .Push(" + ")
                    .Push(TranslateExpression(strings[i]).EnsureGreaterTightness(ExpressionTightness.ADDITION))
                    .WithTightness(ExpressionTightness.ADDITION);
            }
            return acc;
        }

        public override StringBuffer TranslateStringConcatPair(Expression strLeft, Expression strRight)
        {
            return TranslateExpression(strLeft)
                .EnsureTightness(ExpressionTightness.ADDITION)
                .Push(" + ")
                .Push(TranslateExpression(strRight).EnsureGreaterTightness(ExpressionTightness.ADDITION))
                .WithTightness(ExpressionTightness.ADDITION);
        }

        public override StringBuffer TranslateStringContains(Expression haystack, Expression needle)
        {
            return TranslateExpression(haystack)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".contains(")
                .Push(TranslateExpression(needle))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringEndsWith(Expression haystack, Expression needle)
        {
            return TranslateExpression(haystack)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".endsWith(")
                .Push(TranslateExpression(needle))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringEquals(Expression left, Expression right)
        {
            return TranslateExpression(left)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".equals(")
                .Push(TranslateExpression(right))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringFromCharCode(Expression charCode)
        {
            return StringBuffer
                .Of("Character.toString((char) ")
                .Push(TranslateExpression(charCode).EnsureTightness(ExpressionTightness.UNARY_PREFIX))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringIndexOf(Expression haystack, Expression needle)
        {
            return TranslateExpression(haystack)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".indexOf(")
                .Push(TranslateExpression(needle))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringIndexOfWithStart(Expression haystack, Expression needle, Expression startIndex)
        {
            return TranslateExpression(haystack)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".indexOf(")
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
                .Push(".lastIndexOf(")
                .Push(TranslateExpression(needle))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringLength(Expression str)
        {
            return TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".length()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringReplace(Expression haystack, Expression needle, Expression newNeedle)
        {
            return TranslateExpression(haystack)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".replace((CharSequence) ")
                .Push(TranslateExpression(needle).EnsureTightness(ExpressionTightness.UNARY_PREFIX))
                .Push(", (CharSequence) ")
                .Push(TranslateExpression(newNeedle).EnsureTightness(ExpressionTightness.UNARY_PREFIX))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringReverse(Expression str)
        {
            return StringBuffer
                .Of("PST_reverseString(")
                .Push(TranslateExpression(str))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringSplit(Expression haystack, Expression needle)
        {
            return StringBuffer
                .Of("PST_literalStringSplit(")
                .Push(TranslateExpression(haystack))
                .Push(", ")
                .Push(TranslateExpression(needle))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringStartsWith(Expression haystack, Expression needle)
        {
            return TranslateExpression(haystack)
                .Push(".startsWith(")
                .Push(TranslateExpression(needle))
                .Push(")");
        }

        public override StringBuffer TranslateStringSubstring(Expression str, Expression start, Expression length)
        {
            return TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".substring(")
                .Push(TranslateExpression(start))
                .Push(", ")
                .Push(TranslateExpression(start).EnsureTightness(ExpressionTightness.ADDITION))
                .Push(" + ")
                .Push(TranslateExpression(length).EnsureGreaterTightness(ExpressionTightness.ADDITION))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringSubstringIsEqualTo(Expression haystack, Expression startIndex, Expression needle)
        {
            return StringBuffer
                .Of("PST_checkStringInString(")
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
                .Push(".toLowerCase()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringToUpper(Expression str)
        {
            return TranslateExpression(str)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".toUpperCase()")
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
                .Push(".trim()")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringTrimEnd(Expression str)
        {
            return StringBuffer
                .Of("PST_trimSide(")
                .Push(TranslateExpression(str))
                .Push(", false)")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateStringTrimStart(Expression str)
        {
            return StringBuffer
                .Of("PST_trimSide(")
                .Push(TranslateExpression(str))
                .Push(", true)")
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
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateStructFieldDereference(Expression root, StructDefinition structDef, string fieldName, int fieldIndex)
        {
            return TranslateExpression(root)
                .EnsureTightness(ExpressionTightness.SUFFIX_SEQUENCE)
                .Push(".")
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
                .Of("PST_parseFloatOrReturnNull(")
                .Push(TranslateExpression(floatOutList))
                .Push(", ")
                .Push(TranslateExpression(stringValue))
                .Push(")")
                .WithTightness(ExpressionTightness.SUFFIX_SEQUENCE);
        }

        public override StringBuffer TranslateUtf8BytesToString(Expression bytes)
        {
            throw new NotImplementedException();
        }

        public override void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(TypeTranspiler.TranslateType(varDecl.Type));
            sb.Append(' ');
            sb.Append(varDecl.VariableNameToken.Value);
            if (varDecl.Value != null)
            {
                sb.Append(" = ");
                sb.Append(TranslateExpressionAsString(varDecl.Value));
            }
            sb.Append(";\n");
        }

        public override void GenerateCodeForFunction(TranspilerContext sb, FunctionDefinition funcDef, bool isStatic)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("public static ");
            sb.Append(TypeTranspiler.TranslateType(funcDef.ReturnType));
            sb.Append(' ');
            sb.Append(funcDef.NameToken.Value);
            sb.Append('(');
            Token[] argNames = funcDef.ArgNames;
            PType[] argTypes = funcDef.ArgTypes;
            for (int i = 0; i < argTypes.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(TypeTranspiler.TranslateType(argTypes[i]));
                sb.Append(' ');
                sb.Append(argNames[i].Value);
            }
            sb.Append(") {\n");
            sb.TabDepth++;
            TranslateStatements(sb, funcDef.Code);
            sb.TabDepth--;
            sb.Append(sb.CurrentTab);
            sb.Append("}\n");
        }

        public override void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef)
        {
            string[] names = structDef.FieldNames.Select(token => token.Value).ToArray();
            string[] types = structDef.FieldTypes.Select(type => TypeTranspiler.TranslateType(type)).ToArray();

            string name = structDef.NameToken.Value;

            // TODO: This is a Crayon-ism that needs to be removed
            // TODO: also this is dangerously likely to affect other projects. At least add a
            // hacky `if (structDef.NameToken.FileName == blah)` that'll be at least somewhat more
            // likely to not create false positives in the mean time.
            bool isValue = name == "Value";

            sb.Append("public class ");
            sb.Append(name);
            sb.Append(" {\n");
            for (int i = 0; i < names.Length; ++i)
            {
                sb.Append("  public ");
                sb.Append(types[i]);
                sb.Append(' ');
                sb.Append(names[i]);
                sb.Append(";\n");
            }

            sb.Append("  public static final ");
            sb.Append(name);
            sb.Append("[] EMPTY_ARRAY = new ");
            sb.Append(name);
            sb.Append("[0];\n");

            if (isValue)
            {
                // The overhead of having extra fields on each Value is much less than the overhead
                // of Java's casting. Particularly on Android.
                sb.Append("  public int intValue;\n");
            }

            sb.Append("\n  public ");
            sb.Append(structDef.NameToken.Value);
            sb.Append('(');
            for (int i = 0; i < names.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(types[i]);
                sb.Append(' ');
                sb.Append(names[i]);
            }
            sb.Append(") {\n");
            for (int i = 0; i < names.Length; ++i)
            {
                sb.Append("    this.");
                sb.Append(names[i]);
                sb.Append(" = ");
                sb.Append(names[i]);
                sb.Append(";\n");
            }
            sb.Append("  }");

            if (isValue)
            {
                // TODO: Yikes! Crayon Runtime specific hack! Remove this!
                sb.Append("\n\n");
                sb.Append("  public Value(int intValue) {\n");
                sb.Append("    this.type = 3;\n");
                sb.Append("    this.intValue = intValue;\n");
                sb.Append("    this.internalValue = intValue;\n");
                sb.Append("  }\n\n");
                sb.Append("  public Value(boolean boolValue) {\n");
                sb.Append("    this.type = 2;\n");
                sb.Append("    this.intValue = boolValue ? 1 : 0;\n");
                sb.Append("    this.internalValue = boolValue;\n");
                sb.Append("  }");
            }

            sb.Append("\n}");
        }
    }
}
