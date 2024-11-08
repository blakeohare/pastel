using Pastel.Parser;
using Pastel.Parser.ParseNodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Transpilers
{
    internal class JavaTranspiler : CurlyBraceTranspiler
    {
        public JavaTranspiler(TranspilerContext transpilerCtx)
            : base(transpilerCtx, true)
        { }

        public override string PreferredTab => "  ";
        public override string PreferredNewline => "\n";

        public override string HelperCodeResourcePath { get { return "Transpilers/Resources/PastelHelper.java"; } }

        public override string TranslateType(PType type)
        {
            return TranslateJavaType(type);
        }

        private bool IsJavaPrimitiveTypeBoxed(PType type)
        {
            switch (type.RootValue)
            {
                case "int":
                case "double":
                case "bool":
                case "byte":
                case "object":
                case "char":
                    return true;
                default:
                    return false;
            }
        }

        private string TranslateJavaType(PType type)
        {
            switch (type.RootValue)
            {
                case "void": return "void";
                case "byte": return "byte";
                case "int": return "int";
                case "char": return "char";
                case "double": return "double";
                case "bool": return "boolean";
                case "object": return "Object";
                case "string": return "String";

                case "Array":
                    string innerType = this.TranslateJavaType(type.Generics[0]);
                    return innerType + "[]";

                case "List":
                    return "ArrayList<" + this.TranslateJavaNestedType(type.Generics[0]) + ">";

                case "Dictionary":
                    return "HashMap<" + this.TranslateJavaNestedType(type.Generics[0]) + ", " + this.TranslateJavaNestedType(type.Generics[1]) + ">";

                case "Func":
                    return "java.lang.reflect.Method";

                case "ClassValue":
                    // java.lang.ClassValue collision
                    return "org.crayonlang.interpreter.structs.ClassValue";

                default:
                    if (type.IsStructOrClass)
                    {
                        return type.TypeName;
                    }
                    throw new NotImplementedException();
            }
        }

        private string TranslateJavaNestedType(PType type)
        {
            switch (type.RootValue)
            {
                case "bool": return "Boolean";
                case "byte": return "Byte";
                case "char": return "Character";
                case "double": return "Double";
                case "int": return "Integer";
                default:
                    return this.TranslateJavaType(type);
            }
        }

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
            return StringBuffer.Of("((")
                .Push(this.TranslateType(fpi.ResolvedType))
                .Push(") TranslationHelper.invokeFunctionPointer(")
                .Push(this.TranslateExpression(fpi.Root))
                .Push(", new Object[] {")
                .Push(this.TranslateCommaDelimitedExpressions(fpi.Args))
                .Push("}))");
        }

        public override StringBuffer TranslatePrintStdErr(Expression value)
        {
            return StringBuffer
                .Of("PlatformTranslationHelper.printStdErr(")
                .Push(this.TranslateExpression(value))
                .Push(')');
        }

        public override StringBuffer TranslatePrintStdOut(Expression value)
        {
            return StringBuffer
                .Of("PlatformTranslationHelper.printStdOut(")
                .Push(this.TranslateExpression(value))
                .Push(')');
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
                .Of("String.join(")
                .Push(this.TranslateExpression(sep))
                .Push(", ")
                .Push(this.TranslateExpression(array))
                .Push(')');
        }

        public override StringBuffer TranslateArrayLength(Expression array)
        {
            return this.TranslateExpression(array)
                .Push(".length");
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
                buf.Push(this.TranslateType(arrayType));
            }
            buf
                .Push('[')
                .Push(this.TranslateExpression(lengthExpression))
                .Push(']');

            while (bracketSuffixCount-- > 0)
            {
                buf.Push("[]");
            }
            return buf;
        }

        public override StringBuffer TranslateArraySet(Expression array, Expression index, Expression value)
        {
            return this.TranslateExpression(array)
                .Push('[')
                .Push(this.TranslateExpression(index))
                .Push("] = ")
                .Push(this.TranslateExpression(value));
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
            return StringBuffer.Of("PST_base64ToString(")
                .Push(this.TranslateExpression(base64String))
                .Push(')');
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
                    return this.TranslateExpression(dotField.Root)
                        .Push(".intValue");
                }
                else if (type.RootValue == "bool")
                {
                    return StringBuffer
                        .Of("(")
                        .Push(this.TranslateExpression(dotField.Root))
                        .Push(".intValue == 1)");
                }
            }

            return StringBuffer
                .Of("((")
                .Push(this.TranslateType(type))
                .Push(") ")
                .Push(this.TranslateExpression(expression))
                .Push(')');
        }

        public override StringBuffer TranslateCharConstant(char value)
        {
            return StringBuffer.Of(CodeUtil.ConvertCharToCharConstantCode(value));
        }

        public override StringBuffer TranslateCharToString(Expression charValue)
        {
            return StringBuffer
                .Of("(\"\" + ")
                .Push(this.TranslateExpression(charValue))
                .Push(')');
        }

        public override StringBuffer TranslateChr(Expression charCode)
        {
            return StringBuffer
                .Of("Character.toString((char) ")
                .Push(this.TranslateExpression(charCode))
                .Push(")");
        }

        public override StringBuffer TranslateConstructorInvocation(ConstructorInvocation constructorInvocation)
        {
            if (constructorInvocation.ClassDefinition != null)
            {
                throw new NotImplementedException();
            }
            else
            {
                StringBuffer buf = StringBuffer.Of("new ");
                string structType = constructorInvocation.StructDefinition.NameToken.Value;
                if (structType == "ClassValue")
                {
                    structType = "org.crayonlang.interpreter.structs.ClassValue";
                }
                buf
                    .Push(structType)
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
            return StringBuffer.Of("System.currentTimeMillis() / 1000.0");
        }

        public override StringBuffer TranslateDictionaryContainsKey(Expression dictionary, Expression key)
        {
            return this.TranslateExpression(dictionary)
                .Push(".containsKey(")
                .Push(this.TranslateExpression(key))
                .Push(')');
        }

        public override StringBuffer TranslateDictionaryGet(Expression dictionary, Expression key)
        {
            return this.TranslateExpression(dictionary)
                .Push(".get(")
                .Push(this.TranslateExpression(key))
                .Push(')');
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
                .Push(this.TranslateExpression(dictionary))
                .Push(".keySet())");

            // TODO: do a simple .keySet().toArray(TranslationHelper.STATIC_INSTANCE_OF_ZERO_LENGTH_INT_OR_STRING_ARRAY);
        }

        public override StringBuffer TranslateDictionaryNew(PType keyType, PType valueType)
        {
            return StringBuffer
                .Of("new HashMap<")
                .Push(this.TranslateJavaNestedType(keyType))
                .Push(", ")
                .Push(this.TranslateJavaNestedType(valueType))
                .Push(">()");
        }

        public override StringBuffer TranslateDictionaryRemove(Expression dictionary, Expression key)
        {
            return this.TranslateExpression(dictionary)
                .Push(".remove(")
                .Push(this.TranslateExpression(key))
                .Push(')');
        }

        public override StringBuffer TranslateDictionarySet(Expression dictionary, Expression key, Expression value)
        {
            return this.TranslateExpression(dictionary)
                .Push(".put(")
                .Push(this.TranslateExpression(key))
                .Push(", ")
                .Push(this.TranslateExpression(value))
                .Push(')');
        }

        public override StringBuffer TranslateDictionarySize(Expression dictionary)
        {
            return this.TranslateExpression(dictionary)
                .Push(".size()");
        }

        public override void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut)
        {
            PType[] dictTypes = dictionary.ResolvedType.Generics;
            PType keyType = dictTypes[0];
            PType valueType = dictTypes[1];
            bool keyTypeIsBoxed = this.IsJavaPrimitiveTypeBoxed(keyType);
            bool keyExpressionIsSimple = key is Variable || key is InlineConstant;
            string keyVar = null;
            if (!keyExpressionIsSimple)
            {
                keyVar = "_PST_dictKey" + sb.SwitchCounter++;
                sb.Append(sb.CurrentTab);
                sb.Append(this.TranslateType(keyType));
                sb.Append(' ');
                sb.Append(keyVar);
                sb.Append(" = ");
                sb.Append(this.TranslateExpressionAsString(key));
                sb.Append(";\n");
            }

            string lookupVar = "_PST_dictLookup" + sb.SwitchCounter++;
            sb.Append(sb.CurrentTab);
            sb.Append(this.TranslateJavaNestedType(valueType));
            sb.Append(' ');
            sb.Append(lookupVar);
            sb.Append(" = ");
            sb.Append(this.TranslateExpressionAsString(dictionary));
            sb.Append(".get(");
            if (keyExpressionIsSimple)
            {
                sb.Append(this.TranslateExpressionAsString(key));
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
                sb.Append(this.TranslateExpressionAsString(dictionary));
                sb.Append(".containsKey(");
                if (keyExpressionIsSimple) sb.Append(this.TranslateExpressionAsString(key));
                else sb.Append(keyVar);
                sb.Append(") ? null : (");
                sb.Append(this.TranslateExpressionAsString(fallbackValue));
                sb.Append(")");
            }
            else
            {
                sb.Append(this.TranslateExpressionAsString(fallbackValue));
            }
            sb.Append(") : ");
            sb.Append(lookupVar);
            sb.Append(";\n");
        }

        public override StringBuffer TranslateDictionaryValues(Expression dictionary)
        {
            return this.TranslateExpression(dictionary)
                .Push(".values()");
        }

        public override StringBuffer TranslateExtensibleCallbackInvoke(Expression name, Expression argsArray)
        {
            throw new NotImplementedException();
        }

        public override StringBuffer TranslateFloatBuffer16()
        {
            return StringBuffer.Of("PST_floatBuffer16");
        }

        public override StringBuffer TranslateFloatDivision(Expression floatNumerator, Expression floatDenominator)
        {
            return this.TranslateExpression(floatNumerator)
                .Push(" / ")
                .Push(this.TranslateExpression(floatDenominator));
        }

        public override StringBuffer TranslateFloatToInt(Expression floatExpr)
        {
            return StringBuffer.Of("((int) ")
                .Push(this.TranslateExpression(floatExpr))
                .Push(')');
        }

        public override StringBuffer TranslateFloatToString(Expression floatExpr)
        {
            return StringBuffer.Of("Double.toString(")
                .Push(this.TranslateExpression(floatExpr))
                .Push(')');
        }

        public override StringBuffer TranslateGetFunction(Expression name)
        {
            return StringBuffer.Of("TranslationHelper.getFunction(")
                .Push(this.TranslateExpression(name))
                .Push(')');
        }

        public override StringBuffer TranslateInstanceFieldDereference(Expression root, ClassDefinition classDef, string fieldName)
        {
            return this.TranslateExpression(root)
                .Push('.')
                .Push(fieldName);
        }

        public override StringBuffer TranslateIntBuffer16()
        {
            return StringBuffer.Of("PST_intBuffer16");
        }

        public override StringBuffer TranslateIntegerDivision(Expression integerNumerator, Expression integerDenominator)
        {
            return this.TranslateExpression(integerNumerator)
                .Push(" / ")
                .Push(this.TranslateExpression(integerDenominator));
        }

        public override StringBuffer TranslateIntToString(Expression integer)
        {
            return StringBuffer
                .Of("Integer.toString(")
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
                .Push(".add(")
                .Push(this.TranslateExpression(item))
                .Push(')');
        }

        public override StringBuffer TranslateListClear(Expression list)
        {
            return this.TranslateExpression(list)
                .Push(".clear()");
        }

        public override StringBuffer TranslateListConcat(Expression list, Expression items)
        {
            // Fun fact: this is actually not implemented. The only place that this is used is by Value.
            return StringBuffer
                .Of("PST_concatLists(")
                .Push(this.TranslateExpression(list))
                .Push(", ")
                .Push(this.TranslateExpression(items))
                .Push(')');
        }

        public override StringBuffer TranslateListGet(Expression list, Expression index)
        {
            return this.TranslateExpression(list)
                .Push(".get(")
                .Push(this.TranslateExpression(index))
                .Push(')');
        }

        public override StringBuffer TranslateListInsert(Expression list, Expression index, Expression item)
        {
            return this.TranslateExpression(list)
                .Push(".add(")
                .Push(this.TranslateExpression(index))
                .Push(", ")
                .Push(this.TranslateExpression(item))
                .Push(')');
        }

        public override StringBuffer TranslateListJoinChars(Expression list)
        {
            return StringBuffer
                .Of("PST_joinChars(")
                .Push(this.TranslateExpression(list))
                .Push(')');
        }

        public override StringBuffer TranslateListJoinStrings(Expression list, Expression sep)
        {
            return StringBuffer
                .Of("PST_joinList(")
                .Push(this.TranslateExpression(sep))
                .Push(", ")
                .Push(this.TranslateExpression(list))
                .Push(')');
        }

        public override StringBuffer TranslateListNew(PType type)
        {
            return StringBuffer
                .Of("new ArrayList<")
                .Push(this.TranslateJavaNestedType(type))
                .Push(">()");
        }

        public override StringBuffer TranslateListPop(Expression list)
        {
            bool useInlineListPop =
                (list is Variable) ||
                (list is DotField && ((DotField)list).Root is Variable);

            if (useInlineListPop)
            {
                return this.TranslateExpression(list)
                    .Push(".remove(")
                    .Push(this.TranslateExpression(list))
                    .Push(".size() - 1)");
            }

            return StringBuffer
                .Of("PST_listPop(")
                .Push(this.TranslateExpression(list))
                .Push(')');
        }

        public override StringBuffer TranslateListRemoveAt(Expression list, Expression index)
        {
            return this.TranslateExpression(list)
                .Push(".remove(")
                .Push(this.TranslateExpression(index))
                .Push(')');
        }

        public override StringBuffer TranslateListReverse(Expression list)
        {
            return StringBuffer
                .Of("java.util.Collections.reverse(")
                .Push(this.TranslateExpression(list))
                .Push(')');
        }

        public override StringBuffer TranslateListSet(Expression list, Expression index, Expression value)
        {
            return this.TranslateExpression(list)
                .Push(".set(")
                .Push(this.TranslateExpression(index))
                .Push(", ")
                .Push(this.TranslateExpression(value))
                .Push(')');
        }

        public override StringBuffer TranslateListShuffle(Expression list)
        {
            // This is currently only used and implemented by Value lists.
            return StringBuffer
                .Of("PST_listShuffle(")
                .Push(this.TranslateExpression(list))
                .Push(')');
        }

        public override StringBuffer TranslateListSize(Expression list)
        {
            return this.TranslateExpression(list)
                .Push(".size()");
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
                        .Push((char)(primitiveName[0] + 'A' - 'a'))
                        .Push(primitiveName.Substring(1))
                        .Push('(')
                        .Push(this.TranslateExpression(list))
                        .Push(')');

                case "string":
                    return this.TranslateExpression(list)
                        .Push(".toArray(PST_emptyArrayString)");

                case "object":
                    return this.TranslateExpression(list)
                        .Push(".toArray()");

                case "List":
                    return this.TranslateExpression(list)
                        .Push(".toArray(PST_emptyArrayList)");

                case "Dictionary":
                    return this.TranslateExpression(list)
                        .Push(".toArray(PST_emptyArrayMap)");

                case "Array":
                    throw new NotImplementedException("not implemented: java list of arrays to array");

                default:
                    if (itemType.IsStructOrClass)
                    {
                        return this.TranslateExpression(list)
                            .Push(".toArray(")
                            .Push(this.TranslateType(itemType))
                            .Push(".EMPTY_ARRAY)");
                    }

                    // I think I covered all the types that are supported.
                    throw new NotImplementedException();
            }
        }

        public override StringBuffer TranslateMathArcCos(Expression ratio)
        {
            return StringBuffer
                .Of("Math.acos(")
                .Push(this.TranslateExpression(ratio))
                .Push(')');
        }

        public override StringBuffer TranslateMathArcSin(Expression ratio)
        {
            return StringBuffer
                .Of("Math.asin(")
                .Push(this.TranslateExpression(ratio))
                .Push(')');
        }

        public override StringBuffer TranslateMathArcTan(Expression yComponent, Expression xComponent)
        {
            return StringBuffer
                .Of("Math.atan2(")
                .Push(this.TranslateExpression(yComponent))
                .Push(", ")
                .Push(this.TranslateExpression(xComponent))
                .Push(')');
        }

        public override StringBuffer TranslateMathCos(Expression thetaRadians)
        {
            return StringBuffer
                .Of("Math.cos(")
                .Push(this.TranslateExpression(thetaRadians))
                .Push(')');
        }

        public override StringBuffer TranslateMathLog(Expression value)
        {
            return StringBuffer
                .Of("Math.log(")
                .Push(this.TranslateExpression(value))
                .Push(')');
        }

        public override StringBuffer TranslateMathPow(Expression expBase, Expression exponent)
        {
            return StringBuffer
                .Of("Math.pow(")
                .Push(this.TranslateExpression(expBase))
                .Push(", ")
                .Push(this.TranslateExpression(exponent))
                .Push(')');
        }

        public override StringBuffer TranslateMathSin(Expression thetaRadians)
        {
            return StringBuffer
                .Of("Math.sin(")
                .Push(this.TranslateExpression(thetaRadians))
                .Push(')');
        }

        public override StringBuffer TranslateMathTan(Expression thetaRadians)
        {
            return StringBuffer
                .Of("Math.tan(")
                .Push(this.TranslateExpression(thetaRadians))
                .Push(')');
        }

        public override StringBuffer TranslateMultiplyList(Expression list, Expression n)
        {
            // TODO: this helper function is not actually implemented yet.
            return StringBuffer
                .Of("PST_multiplyList(")
                .Push(this.TranslateExpression(list))
                .Push(", ")
                .Push(this.TranslateExpression(n))
                .Push(')');
        }

        public override StringBuffer TranslateNullConstant()
        {
            return StringBuffer.Of("null");
        }

        public override StringBuffer TranslateOrd(Expression charValue)
        {
            return StringBuffer
                .Of("((int)(")
                .Push(this.TranslateExpression(charValue))
                .Push("))");
        }

        public override StringBuffer TranslateParseFloatUnsafe(Expression stringValue)
        {
            return StringBuffer
                .Of("Double.parseDouble(")
                .Push(this.TranslateExpression(stringValue))
                .Push(')');
        }

        public override StringBuffer TranslateParseInt(Expression safeStringValue)
        {
            return StringBuffer
                .Of("Integer.parseInt(")
                .Push(this.TranslateExpression(safeStringValue))
                .Push(')');
        }

        public override StringBuffer TranslateRandomFloat()
        {
            return StringBuffer.Of("PST_random.nextDouble()");
        }

        public override StringBuffer TranslateSortedCopyOfIntArray(Expression intArray)
        {
            return StringBuffer
                .Of("PST_sortedCopyOfIntArray(")
                .Push(this.TranslateExpression(intArray))
                .Push(')');
        }

        public override StringBuffer TranslateSortedCopyOfStringArray(Expression stringArray)
        {
            return StringBuffer
                .Of("PST_sortedCopyOfStringArray(")
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
            return StringBuffer.Of("PST_stringBuffer16");
        }

        public override StringBuffer TranslateStringCharAt(Expression str, Expression index)
        {
            return this.TranslateExpression(str)
                .Push(".charAt(")
                .Push(this.TranslateExpression(index))
                .Push(')');
        }

        public override StringBuffer TranslateStringCharCodeAt(Expression str, Expression index)
        {
            return StringBuffer
                .Of("((int) ")
                .Push(this.TranslateExpression(str))
                .Push(".charAt(")
                .Push(this.TranslateExpression(index))
                .Push("))");
        }

        public override StringBuffer TranslateStringCompareIsReverse(Expression str1, Expression str2)
        {
            return StringBuffer
                .Of("(")
                .Push(this.TranslateExpression(str1))
                .Push(".compareTo(")
                .Push(this.TranslateExpression(str2))
                .Push(") > 0)");
        }

        public override StringBuffer TranslateStringConcatAll(Expression[] strings)
        {
            // TODO: use a string builder
            StringBuffer buf = this.TranslateExpression(strings[0]);
            for (int i = 1; i < strings.Length; ++i)
            {
                buf
                    .Push(" + ")
                    .Push(this.TranslateExpression(strings[i]));
            }
            return buf;
        }

        public override StringBuffer TranslateStringConcatPair(Expression strLeft, Expression strRight)
        {
            return this.TranslateExpression(strLeft)
                .Push(" + ")
                .Push(this.TranslateExpression(strRight));
        }

        public override StringBuffer TranslateStringContains(Expression haystack, Expression needle)
        {
            return this.TranslateExpression(haystack)
                .Push(".contains(")
                .Push(this.TranslateExpression(needle))
                .Push(')');
        }

        public override StringBuffer TranslateStringEndsWith(Expression haystack, Expression needle)
        {
            return this.TranslateExpression(haystack)
                .Push(".endsWith(")
                .Push(this.TranslateExpression(needle))
                .Push(')');
        }

        public override StringBuffer TranslateStringEquals(Expression left, Expression right)
        {
            return this.TranslateExpression(left)
                .Push(".equals(")
                .Push(this.TranslateExpression(right))
                .Push(')');
        }

        public override StringBuffer TranslateStringFromCharCode(Expression charCode)
        {
            return StringBuffer
                .Of("Character.toString((char) ")
                .Push(this.TranslateExpression(charCode))
                .Push(")");
        }

        public override StringBuffer TranslateStringIndexOf(Expression haystack, Expression needle)
        {
            return this.TranslateExpression(haystack)
                .Push(".indexOf(")
                .Push(this.TranslateExpression(needle))
                .Push(')');
        }

        public override StringBuffer TranslateStringIndexOfWithStart(Expression haystack, Expression needle, Expression startIndex)
        {
            return this.TranslateExpression(haystack)
                .Push(".indexOf(")
                .Push(this.TranslateExpression(needle))
                .Push(", ")
                .Push(this.TranslateExpression(startIndex))
                .Push(')');
        }

        public override StringBuffer TranslateStringLastIndexOf(Expression haystack, Expression needle)
        {
            return this.TranslateExpression(haystack)
                .Push(".lastIndexOf(")
                .Push(this.TranslateExpression(needle))
                .Push(')');
        }

        public override StringBuffer TranslateStringLength(Expression str)
        {
            return this.TranslateExpression(str)
                .Push(".length()");
        }

        public override StringBuffer TranslateStringReplace(Expression haystack, Expression needle, Expression newNeedle)
        {
            return this.TranslateExpression(haystack)
                .Push(".replace((CharSequence) ")
                .Push(this.TranslateExpression(needle))
                .Push(", (CharSequence) ")
                .Push(this.TranslateExpression(newNeedle))
                .Push(')');
        }

        public override StringBuffer TranslateStringReverse(Expression str)
        {
            return StringBuffer
                .Of("PST_reverseString(")
                .Push(this.TranslateExpression(str))
                .Push(')');
        }

        public override StringBuffer TranslateStringSplit(Expression haystack, Expression needle)
        {
            return StringBuffer
                .Of("PST_literalStringSplit(")
                .Push(this.TranslateExpression(haystack))
                .Push(", ")
                .Push(this.TranslateExpression(needle))
                .Push(')');
        }

        public override StringBuffer TranslateStringStartsWith(Expression haystack, Expression needle)
        {
            return this.TranslateExpression(haystack)
                .Push(".startsWith(")
                .Push(this.TranslateExpression(needle))
                .Push(')');
        }

        public override StringBuffer TranslateStringSubstring(Expression str, Expression start, Expression length)
        {
            return this.TranslateExpression(str)
                .Push(".substring(")
                .Push(this.TranslateExpression(start))
                .Push(", ")
                .Push(this.TranslateExpression(start))
                .Push(" + ")
                .Push(this.TranslateExpression(length))
                .Push(')');
        }

        public override StringBuffer TranslateStringSubstringIsEqualTo(Expression haystack, Expression startIndex, Expression needle)
        {
            return StringBuffer
                .Of("PST_checkStringInString(")
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
                .Push(".toLowerCase()");
        }

        public override StringBuffer TranslateStringToUpper(Expression str)
        {
            return this.TranslateExpression(str)
                .Push(".toUpperCase()");
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
                .Push(".trim()");
        }

        public override StringBuffer TranslateStringTrimEnd(Expression str)
        {
            return StringBuffer
                .Of("PST_trimSide(")
                .Push(this.TranslateExpression(str))
                .Push(", false)");
        }

        public override StringBuffer TranslateStringTrimStart(Expression str)
        {
            return StringBuffer
                .Of("PST_trimSide(")
                .Push(this.TranslateExpression(str))
                .Push(", true)");
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
                .Push('.')
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
                .Of("PST_parseFloatOrReturnNull(")
                .Push(this.TranslateExpression(floatOutList))
                .Push(", ")
                .Push(this.TranslateExpression(stringValue))
                .Push(')');
        }

        public override StringBuffer TranslateUtf8BytesToString(Expression bytes)
        {
            throw new NotImplementedException();
        }

        public override void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl)
        {
            sb.Append(sb.CurrentTab);
            sb.Append(this.TranslateType(varDecl.Type));
            sb.Append(' ');
            sb.Append(varDecl.VariableNameToken.Value);
            if (varDecl.Value != null)
            {
                sb.Append(" = ");
                sb.Append(this.TranslateExpressionAsString(varDecl.Value));
            }
            sb.Append(";\n");
        }

        public override void GenerateCodeForFunction(TranspilerContext sb, FunctionDefinition funcDef, bool isStatic)
        {
            sb.Append(sb.CurrentTab);
            sb.Append("public static ");
            sb.Append(this.TranslateType(funcDef.ReturnType));
            sb.Append(' ');
            sb.Append(funcDef.NameToken.Value);
            sb.Append('(');
            Token[] argNames = funcDef.ArgNames;
            PType[] argTypes = funcDef.ArgTypes;
            for (int i = 0; i < argTypes.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(this.TranslateType(argTypes[i]));
                sb.Append(' ');
                sb.Append(argNames[i].Value);
            }
            sb.Append(") {\n");
            sb.TabDepth++;
            this.TranslateStatements(sb, funcDef.Code);
            sb.TabDepth--;
            sb.Append(sb.CurrentTab);
            sb.Append("}\n");
        }

        public override void GenerateCodeForClass(TranspilerContext sb, ClassDefinition classDef)
        {
            throw new NotImplementedException();
        }

        public override void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef)
        {
            string[] flatNames = structDef.FlatFieldNames.Select(token => token.Value).ToArray();
            string[] flatTypes = structDef.FlatFieldTypes.Select(type => this.TranslateType(type)).ToArray();
            string[] localNames = structDef.LocalFieldNames.Select(token => token.Value).ToArray();
            string[] localTypes = structDef.LocalFieldTypes.Select(type => this.TranslateType(type)).ToArray();

            string name = structDef.NameToken.Value;

            // TODO: This is a Crayon-ism that needs to be removed
            // TODO: also this is dangerously likely to affect other projects. At least add a
            // hacky `if (structDef.NameToken.FileName == blah)` that'll be at least somewhat more
            // likely to not create false positives in the mean time.
            bool isValue = name == "Value";

            sb.Append("public class ");
            sb.Append(name);
            if (structDef.Parent != null)
            {
                sb.Append(" extends ");
                sb.Append(structDef.ParentName.Value);
            }
            sb.Append(" {\n");
            for (int i = 0; i < localNames.Length; ++i)
            {
                sb.Append("  public ");
                sb.Append(localTypes[i]);
                sb.Append(' ');
                sb.Append(localNames[i]);
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
            for (int i = 0; i < flatNames.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(flatTypes[i]);
                sb.Append(' ');
                sb.Append(flatNames[i]);
            }
            sb.Append(") {\n");
            if (structDef.Parent != null)
            {
                sb.Append("    super(");
                int parentFieldCount = structDef.Parent.FlatFieldNames.Length;
                for (int i = 0; i < parentFieldCount; ++i)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(flatNames[i]);
                }
                sb.Append(");\n");
            }
            for (int i = 0; i < localNames.Length; ++i)
            {
                sb.Append("    this.");
                sb.Append(localNames[i]);
                sb.Append(" = ");
                sb.Append(localNames[i]);
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
