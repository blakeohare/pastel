using Pastel.Parser;
using Pastel.Parser.ParseNodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Transpilers
{
    internal abstract class AbstractExpressionTranslator
    {
        private TranspilerContext transpilerCtx;

        protected void MarkFeatureAsUsed(string feature)
        {
            this.transpilerCtx.MarkFeatureAsBeingUsed(feature);
        }

        protected AbstractTypeTranspiler TypeTranspiler
        {
            get { return this.transpilerCtx.Transpiler.TypeTranspiler; }
        }

        public AbstractExpressionTranslator(TranspilerContext transpilerCtx)
        {
            this.transpilerCtx = transpilerCtx;
        }

        public string TranslateExpressionAsString(Expression expression)
        {
            return this.TranslateExpression(expression).Flatten();
        }

        public StringBuffer TranslateExpression(Expression expression)
        {
            string typeName = expression.GetType().Name;
            switch (typeName)
            {
                case "CastExpression": return this.TranslateCast(((CastExpression)expression).Type, ((CastExpression)expression).Expression);
                case "FunctionReference": return this.TranslateFunctionReference((FunctionReference)expression);
                case "FunctionPointerInvocation": return this.TranslateFunctionPointerInvocation((FunctionPointerInvocation)expression);
                case "CoreFunctionInvocation": return this.TranslateCoreFunctionInvocation((CoreFunctionInvocation)expression);
                case "OpPair":
                    OpPair opPair = (OpPair)expression;
                    if (opPair.Op == "/")
                    {
                        if (opPair.Left.ResolvedType.RootValue == "int" &&
                            opPair.Right.ResolvedType.RootValue == "int")
                        {
                            return this.TranslateDivideInteger(opPair.Left, opPair.Right);
                        }
                        return this.TranslateDivideFloat(opPair.Left, opPair.Right);
                    }
                    return this.TranslateOpPair(opPair);
                case "OpChain": throw new InvalidOperationException(); // This should have been resolved into more specific actions.

                case "ExtensibleFunctionInvocation":
                    return this.TranslateExtensibleFunctionInvocation((ExtensibleFunctionInvocation)expression);

                case "InlineIncrement":
                    InlineIncrement ii = (InlineIncrement)expression;
                    return this.TranslateInlineIncrement(ii.Expression, ii.IsPrefix, ii.IncrementToken.Value == "++");

                case "FunctionInvocation":
                    FunctionInvocation funcInvocation = (FunctionInvocation)expression;
                    return this.TranslateFunctionInvocation((FunctionReference)funcInvocation.Root, funcInvocation.Args);

                case "Variable":
                    Variable v = (Variable)expression;
                    return this.TranslateVariable(v);

                case "ConstructorInvocation":
                    ConstructorInvocation constructor = (ConstructorInvocation)expression;
                    string rootType = constructor.Type.RootValue;
                    switch (rootType)
                    {
                        case "Array":
                            if (constructor.Type.Generics.Length != 1)
                            {
                                throw new ParserException(constructor.Type.FirstToken, "Array constructor requires exactly 1 generic type.");
                            }
                            return this.TranslateArrayNew(constructor.Type.Generics[0], constructor.Args[0]);

                        case "List":
                            if (constructor.Type.Generics.Length != 1)
                            {
                                throw new ParserException(constructor.Type.FirstToken, "List constructor requires exactly 1 generic type.");
                            }
                            return this.TranslateListNew(constructor.Type.Generics[0]);

                        case "Dictionary":
                            if (constructor.Type.Generics.Length != 2)
                            {
                                throw new ParserException(constructor.Type.FirstToken, "Dictionary constructor requires exactly 2 generic types.");
                            }
                            PType dictionaryKeyType = constructor.Type.Generics[0];
                            PType dictionaryValueType = constructor.Type.Generics[1];
                            return this.TranslateDictionaryNew(dictionaryKeyType, dictionaryValueType);

                        case "StringBuilder":
                            if (constructor.Type.Generics.Length != 0)
                            {
                                throw new ParserException(constructor.Type.FirstToken, "StringBuilder constructor does not have any generics.");
                            }
                            return this.TranslateStringBuilderNew();

                        default:
                            // TODO: throw an exception (in the parser) if generics exist.
                            return this.TranslateConstructorInvocation(constructor);
                    }

                case "DotField":
                    DotField df = (DotField)expression;
                    StructDefinition structDef = df.StructType;
                    string fieldName = df.FieldName.Value;
                    if (structDef != null)
                    {
                        int fieldIndex = structDef.FieldIndexByName[fieldName];
                        return this.TranslateStructFieldDereference(df.Root, structDef, fieldName, fieldIndex);
                    }
                    throw new InvalidOperationException(); // should have been thrown by the compiler

                case "InlineConstant":
                    InlineConstant ic = (InlineConstant)expression;
                    switch (ic.ResolvedType.RootValue)
                    {
                        case "bool": return this.TranslateBooleanConstant((bool)ic.Value);
                        case "char": return this.TranslateCharConstant(((char)ic.Value));
                        case "double": return this.TranslateFloatConstant((double)ic.Value);
                        case "int": return this.TranslateIntegerConstant((int)ic.Value);
                        case "null": return this.TranslateNullConstant();
                        case "string": return this.TranslateStringConstant((string)ic.Value);
                    }
                    throw new NotImplementedException();

                case "UnaryOp":
                    UnaryOp uo = (UnaryOp)expression;
                    if (uo.OpToken.Value == "-") return this.TranslateNegative(uo);
                    return this.TranslateBooleanNot(uo);

                case "StringConcatenation":
                    return this.TranslateStringConcatenation(((StringConcatenation)expression).Expressions.ToArray());
            }
            throw new NotImplementedException(typeName);
        }

        public StringBuffer TranslateStringConcatenation(Expression[] expressions)
        {
            if (expressions.Length <= 2)
            {
                if (expressions.Length < 2)
                {
                    throw new InvalidOperationException(); // should have been optimized out by now
                }

                return this.TranslateStringConcatPair(expressions[0], expressions[1]);
            }

            return this.TranslateStringConcatAll(expressions);
        }

        public virtual StringBuffer TranslateFunctionPointerInvocation(FunctionPointerInvocation fpi)
        {
            StringBuffer sb = this.TranslateExpression(fpi.Root);
            sb.Push("(");
            for (int i = 0; i < fpi.Args.Length; ++i)
            {
                if (i > 0) sb.Push(", ");
                sb.Push(this.TranslateExpression(fpi.Args[i]));
            }
            sb.Push(")");
            return sb;
        }

        public StringBuffer TranslateVariableName(string varName)
        {
            StringBuffer sb = StringBuffer.Of(varName);
            if (this.transpilerCtx.VariablePrefix != null)
            {
                sb.Prepend(this.transpilerCtx.VariablePrefix);
            }
            return sb.WithTightness(ExpressionTightness.ATOMIC);
        }

        public StringBuffer TranslateCoreFunctionInvocation(CoreFunctionInvocation coreFuncInvocation)
        {
            Expression[] args = coreFuncInvocation.Args;
            switch (coreFuncInvocation.Function)
            {
                case CoreFunction.ARRAY_GET: return this.TranslateArrayGet(args[0], args[1]);
                case CoreFunction.ARRAY_JOIN: return this.TranslateArrayJoin(args[0], args[1]);
                case CoreFunction.ARRAY_LENGTH: return this.TranslateArrayLength(args[0]);
                case CoreFunction.ARRAY_SET: return this.TranslateArraySet(args[0], args[1], args[2]);
                case CoreFunction.BASE64_TO_BYTES: return this.TranslateBase64ToBytes(args[0]);
                case CoreFunction.BASE64_TO_STRING: return this.TranslateBase64ToString(args[0]);
                case CoreFunction.BOOL_TO_STRING: return this.TranslateBoolToString(args[0]);
                case CoreFunction.BYTES_TO_BASE64: return this.TranslateBytesToBase64(args[0]);
                case CoreFunction.CHAR_TO_STRING: return this.TranslateCharToString(args[0]);
                case CoreFunction.CHR: return this.TranslateChr(args[0]);
                case CoreFunction.CURRENT_TIME_SECONDS: return this.TranslateCurrentTimeSeconds();
                case CoreFunction.DICTIONARY_CONTAINS_KEY: return this.TranslateDictionaryContainsKey(args[0], args[1]);
                case CoreFunction.DICTIONARY_GET: return this.TranslateDictionaryGet(args[0], args[1]);
                case CoreFunction.DICTIONARY_KEYS: return this.TranslateDictionaryKeys(args[0]);
                case CoreFunction.DICTIONARY_NEW: return this.TranslateDictionaryNew(coreFuncInvocation.ResolvedType.Generics[0], coreFuncInvocation.ResolvedType.Generics[1]);
                case CoreFunction.DICTIONARY_REMOVE: return this.TranslateDictionaryRemove(args[0], args[1]);
                case CoreFunction.DICTIONARY_SET: return this.TranslateDictionarySet(args[0], args[1], args[2]);
                case CoreFunction.DICTIONARY_SIZE: return this.TranslateDictionarySize(args[0]);
                case CoreFunction.DICTIONARY_VALUES: return this.TranslateDictionaryValues(args[0]);
                case CoreFunction.EMIT_COMMENT: return this.TranslateEmitComment(((InlineConstant)args[0]).Value.ToString());
                case CoreFunction.EXTENSIBLE_CALLBACK_INVOKE: return this.TranslateExtensibleCallbackInvoke(args[0], args[1]);
                case CoreFunction.FLOAT_TO_STRING: return this.TranslateFloatToString(args[0]);
                case CoreFunction.GET_FUNCTION: return this.TranslateGetFunction(args[0]);
                case CoreFunction.INT_TO_STRING: return this.TranslateIntToString(args[0]);
                case CoreFunction.IS_VALID_INTEGER: return this.TranslateIsValidInteger(args[0]);
                case CoreFunction.LIST_ADD: return this.TranslateListAdd(args[0], args[1]);
                case CoreFunction.LIST_CLEAR: return this.TranslateListClear(args[0]);
                case CoreFunction.LIST_CONCAT: return this.TranslateListConcat(args[0], args[1]);
                case CoreFunction.LIST_GET: return this.TranslateListGet(args[0], args[1]);
                case CoreFunction.LIST_INSERT: return this.TranslateListInsert(args[0], args[1], args[2]);
                case CoreFunction.LIST_JOIN_CHARS: return this.TranslateListJoinChars(args[0]);
                case CoreFunction.LIST_JOIN_STRINGS: return this.TranslateListJoinStrings(args[0], args[1]);
                case CoreFunction.LIST_NEW: return this.TranslateListNew(coreFuncInvocation.ResolvedType.Generics[0]);
                case CoreFunction.LIST_POP: return this.TranslateListPop(args[0]);
                case CoreFunction.LIST_REMOVE_AT: return this.TranslateListRemoveAt(args[0], args[1]);
                case CoreFunction.LIST_REVERSE: return this.TranslateListReverse(args[0]);
                case CoreFunction.LIST_SET: return this.TranslateListSet(args[0], args[1], args[2]);
                case CoreFunction.LIST_SHUFFLE: return this.TranslateListShuffle(args[0]);
                case CoreFunction.LIST_SIZE: return this.TranslateListSize(args[0]);
                case CoreFunction.LIST_TO_ARRAY: return this.TranslateListToArray(args[0]);
                case CoreFunction.MATH_ABS: return this.TranslateMathAbs(args[0]);
                case CoreFunction.MATH_ARCCOS: return this.TranslateMathArcCos(args[0]);
                case CoreFunction.MATH_ARCSIN: return this.TranslateMathArcSin(args[0]);
                case CoreFunction.MATH_ARCTAN: return this.TranslateMathArcTan(args[0], args[1]);
                case CoreFunction.MATH_CEIL: return this.TranslateMathCeil(args[0]);
                case CoreFunction.MATH_COS: return this.TranslateMathCos(args[0]);
                case CoreFunction.MATH_FLOOR: return this.TranslateMathFloor(args[0]);
                case CoreFunction.MATH_LOG: return this.TranslateMathLog(args[0]);
                case CoreFunction.MATH_POW: return this.TranslateMathPow(args[0], args[1]);
                case CoreFunction.MATH_SIN: return this.TranslateMathSin(args[0]);
                case CoreFunction.MATH_TAN: return this.TranslateMathTan(args[0]);
                case CoreFunction.MULTIPLY_LIST: return this.TranslateMultiplyList(args[0], args[1]);
                case CoreFunction.ORD: return this.TranslateOrd(args[0]);
                case CoreFunction.PARSE_FLOAT_UNSAFE: return this.TranslateParseFloatUnsafe(args[0]);
                case CoreFunction.PARSE_INT: return this.TranslateParseInt(args[0]);
                case CoreFunction.PRINT_STDERR: return this.TranslatePrintStdErr(args[0]);
                case CoreFunction.PRINT_STDOUT: return this.TranslatePrintStdOut(args[0]);
                case CoreFunction.RANDOM_FLOAT: return this.TranslateRandomFloat();
                case CoreFunction.SORTED_COPY_OF_INT_ARRAY: return this.TranslateSortedCopyOfIntArray(args[0]);
                case CoreFunction.SORTED_COPY_OF_STRING_ARRAY: return this.TranslateSortedCopyOfStringArray(args[0]);
                case CoreFunction.STRING_APPEND: return this.TranslateStringAppend(args[0], args[1]);
                case CoreFunction.STRING_CHAR_AT: return this.TranslateStringCharAt(args[0], args[1]);
                case CoreFunction.STRING_CHAR_CODE_AT: return this.TranslateStringCharCodeAt(args[0], args[1]);
                case CoreFunction.STRING_COMPARE_IS_REVERSE: return this.TranslateStringCompareIsReverse(args[0], args[1]);
                case CoreFunction.STRING_CONCAT_ALL: return (args.Length == 2) ? this.TranslateStringConcatPair(args[0], args[1]) : this.TranslateStringConcatAll(args);
                case CoreFunction.STRING_CONTAINS: return this.TranslateStringContains(args[0], args[1]);
                case CoreFunction.STRING_ENDS_WITH: return this.TranslateStringEndsWith(args[0], args[1]);
                case CoreFunction.STRING_EQUALS: return this.TranslateStringEquals(args[0], args[1]);
                case CoreFunction.STRING_FROM_CHAR_CODE: return this.TranslateStringFromCharCode(args[0]);
                case CoreFunction.STRING_INDEX_OF: return (args.Length == 2) ? this.TranslateStringIndexOf(args[0], args[1]) : this.TranslateStringIndexOfWithStart(args[0], args[1], args[2]);
                case CoreFunction.STRING_LAST_INDEX_OF: return this.TranslateStringLastIndexOf(args[0], args[1]);
                case CoreFunction.STRING_LENGTH: return this.TranslateStringLength(args[0]);
                case CoreFunction.STRING_REPLACE: return this.TranslateStringReplace(args[0], args[1], args[2]);
                case CoreFunction.STRING_REVERSE: return this.TranslateStringReverse(args[0]);
                case CoreFunction.STRING_SPLIT: return this.TranslateStringSplit(args[0], args[1]);
                case CoreFunction.STRING_STARTS_WITH: return this.TranslateStringStartsWith(args[0], args[1]);
                case CoreFunction.STRING_SUBSTRING: return this.TranslateStringSubstring(args[0], args[1], args[2]);
                case CoreFunction.STRING_SUBSTRING_IS_EQUAL_TO: return this.TranslateStringSubstringIsEqualTo(args[0], args[1], args[2]);
                case CoreFunction.STRING_TO_LOWER: return this.TranslateStringToLower(args[0]);
                case CoreFunction.STRING_TO_UPPER: return this.TranslateStringToUpper(args[0]);
                case CoreFunction.STRING_TO_UTF8_BYTES: return this.TranslateStringToUtf8Bytes(args[0]);
                case CoreFunction.STRING_TRIM: return this.TranslateStringTrim(args[0]);
                case CoreFunction.STRING_TRIM_END: return this.TranslateStringTrimEnd(args[0]);
                case CoreFunction.STRING_TRIM_START: return this.TranslateStringTrimStart(args[0]);
                case CoreFunction.STRINGBUILDER_ADD: return this.TranslateStringBuilderAdd(args[0], args[1]);
                case CoreFunction.STRINGBUILDER_CLEAR: return this.TranslateStringBuilderClear(args[0]);
                case CoreFunction.STRINGBUILDER_TOSTRING: return this.TranslateStringBuilderToString(args[0]);
                case CoreFunction.STRONG_REFERENCE_EQUALITY: return this.TranslateStrongReferenceEquality(args[0], args[1]);
                case CoreFunction.TO_CODE_STRING: return this.TranslateToCodeString(args[0]);
                case CoreFunction.TRY_PARSE_FLOAT: return this.TranslateTryParseFloat(args[0], args[1]);
                case CoreFunction.UTF8_BYTES_TO_STRING: return this.TranslateUtf8BytesToString(args[0]);

                case CoreFunction.DICTIONARY_TRY_GET:
                    throw new ParserException(coreFuncInvocation.FirstToken, "Dictionary's TryGet method cannot be called like this. It must be assigned to a variable directly. This is due to a restriction in how this can get transpiled to certain languages.");

                default:
                    throw new NotImplementedException(coreFuncInvocation.Function.ToString());
            }
        }

        public StringBuffer TranslateExtensibleFunctionInvocation(ExtensibleFunctionInvocation funcInvocation)
        {
            StringBuffer sb = StringBuffer.Of("");
            Expression[] args = funcInvocation.Args;
            Token throwToken = funcInvocation.FunctionRef.FirstToken;
            string functionName = funcInvocation.FunctionRef.Name;
            Dictionary<string, string> extLookup = this.transpilerCtx.PastelContext.ExtensionSet.ExtensibleFunctionTranslations;

            if (!extLookup.ContainsKey(functionName) || extLookup[functionName] == null)
            {
                string msg = "The extensbile method '" + functionName + "' does not have any registered translation.";
                throw new ExtensionMethodNotImplementedException(throwToken, msg);
            }

            string codeSnippet = extLookup[functionName];

            // Filter down to just the arguments that are used.
            // Put their location and length in this locations lookup. The key
            // is the ordinal for the argument starting from 0.
            Dictionary<int, int[]> locations = new Dictionary<int, int[]>();
            for (int i = 0; i < args.Length; ++i)
            {
                string searchString = "[ARG:" + (i + 1) + "]";
                int argIndex = codeSnippet.IndexOf(searchString);
                if (argIndex != -1)
                {
                    locations[i] = new int[] { argIndex, searchString.Length, argIndex + searchString.Length };
                }
            }
            // Get the arguments in order of their actual appearance.
            int[] argOrdinalsInOrder = locations.Keys.OrderBy(argN => locations[argN][0]).ToArray();
            if (argOrdinalsInOrder.Length == 0)
            {
                // If there aren't any, you're done. Just put the code snippet into the
                // buffer as-is.
                sb.Push(codeSnippet);
            }
            else
            {
                sb.Push(codeSnippet.Substring(0, locations[argOrdinalsInOrder[0]][0]));
                for (int i = 0; i < argOrdinalsInOrder.Length; ++i)
                {
                    int currentArgOrdinal = argOrdinalsInOrder[i];
                    int nextArgOrdinal = i + 1 < argOrdinalsInOrder.Length ? argOrdinalsInOrder[i + 1] : -1;
                    sb.Push(this.TranslateExpression(args[currentArgOrdinal]));
                    int argEndIndex = locations[currentArgOrdinal][2];
                    if (nextArgOrdinal == -1)
                    {
                        // Take the code snippet from the end of the current arg to the end and
                        // add it to the buffer.
                        sb.Push(codeSnippet.Substring(argEndIndex));
                    }
                    else
                    {
                        int nextArgBeginIndex = locations[nextArgOrdinal][0];
                        sb.Push(codeSnippet.Substring(argEndIndex, nextArgBeginIndex - argEndIndex));
                    }
                }
            }
            return sb;
        }

        public abstract StringBuffer TranslateArrayGet(Expression array, Expression index);
        public abstract StringBuffer TranslateArrayJoin(Expression array, Expression sep);
        public abstract StringBuffer TranslateArrayLength(Expression array);
        public abstract StringBuffer TranslateArrayNew(PType arrayType, Expression lengthExpression);
        public abstract StringBuffer TranslateArraySet(Expression array, Expression index, Expression value);
        public abstract StringBuffer TranslateBase64ToBytes(Expression base64String);
        public abstract StringBuffer TranslateBase64ToString(Expression base64String);
        public abstract StringBuffer TranslateBoolToString(Expression value);
        public abstract StringBuffer TranslateBytesToBase64(Expression byteArr);
        public abstract StringBuffer TranslateBooleanConstant(bool value);
        public abstract StringBuffer TranslateBooleanNot(UnaryOp unaryOp);
        public abstract StringBuffer TranslateCast(PType type, Expression expression);
        public abstract StringBuffer TranslateCharConstant(char value);
        public abstract StringBuffer TranslateCharToString(Expression charValue);
        public abstract StringBuffer TranslateChr(Expression charCode);
        public abstract StringBuffer TranslateConstructorInvocation(ConstructorInvocation constructorInvocation);
        public abstract StringBuffer TranslateCurrentTimeSeconds();
        public abstract StringBuffer TranslateDictionaryContainsKey(Expression dictionary, Expression key);
        public abstract StringBuffer TranslateDictionaryGet(Expression dictionary, Expression key);
        public abstract StringBuffer TranslateDictionaryKeys(Expression dictionary);
        public abstract StringBuffer TranslateDictionaryNew(PType keyType, PType valueType);
        public abstract StringBuffer TranslateDictionaryRemove(Expression dictionary, Expression key);
        public abstract StringBuffer TranslateDictionarySet(Expression dictionary, Expression key, Expression value);
        public abstract StringBuffer TranslateDictionarySize(Expression dictionary);
        public abstract StringBuffer TranslateDictionaryValues(Expression dictionary);
        public abstract StringBuffer TranslateDivideFloat(Expression left, Expression right);
        public abstract StringBuffer TranslateDivideInteger(Expression left, Expression right);
        public abstract StringBuffer TranslateEmitComment(string value);
        public abstract StringBuffer TranslateExtensibleCallbackInvoke(Expression name, Expression argsArray);
        public abstract StringBuffer TranslateFloatBuffer16();
        public abstract StringBuffer TranslateFloatConstant(double value);
        public abstract StringBuffer TranslateFloatToString(Expression floatExpr);
        public abstract StringBuffer TranslateFunctionInvocation(FunctionReference funcRef, Expression[] args);
        public abstract StringBuffer TranslateFunctionReference(FunctionReference funcRef);
        public abstract StringBuffer TranslateGetFunction(Expression name);
        public abstract StringBuffer TranslateInlineIncrement(Expression innerExpression, bool isPrefix, bool isAddition);
        public abstract StringBuffer TranslateIntBuffer16();
        public abstract StringBuffer TranslateIntegerConstant(int value);
        public abstract StringBuffer TranslateIntToString(Expression integer);
        public abstract StringBuffer TranslateIsValidInteger(Expression stringValue);
        public abstract StringBuffer TranslateListAdd(Expression list, Expression item);
        public abstract StringBuffer TranslateListClear(Expression list);
        public abstract StringBuffer TranslateListConcat(Expression list, Expression items);
        public abstract StringBuffer TranslateListGet(Expression list, Expression index);
        public abstract StringBuffer TranslateListInsert(Expression list, Expression index, Expression item);
        public abstract StringBuffer TranslateListJoinChars(Expression list);
        public abstract StringBuffer TranslateListJoinStrings(Expression list, Expression sep);
        public abstract StringBuffer TranslateListNew(PType type);
        public abstract StringBuffer TranslateListPop(Expression list);
        public abstract StringBuffer TranslateListRemoveAt(Expression list, Expression index);
        public abstract StringBuffer TranslateListReverse(Expression list);
        public abstract StringBuffer TranslateListSet(Expression list, Expression index, Expression value);
        public abstract StringBuffer TranslateListShuffle(Expression list);
        public abstract StringBuffer TranslateListSize(Expression list);
        public abstract StringBuffer TranslateStringBuilderNew();
        public abstract StringBuffer TranslateListToArray(Expression list);
        public abstract StringBuffer TranslateMathAbs(Expression num);
        public abstract StringBuffer TranslateMathArcCos(Expression ratio);
        public abstract StringBuffer TranslateMathArcSin(Expression ratio);
        public abstract StringBuffer TranslateMathArcTan(Expression yComponent, Expression xComponent);
        public abstract StringBuffer TranslateMathCeil(Expression num);
        public abstract StringBuffer TranslateMathCos(Expression thetaRadians);
        public abstract StringBuffer TranslateMathFloor(Expression num);
        public abstract StringBuffer TranslateMathLog(Expression value);
        public abstract StringBuffer TranslateMathPow(Expression expBase, Expression exponent);
        public abstract StringBuffer TranslateMathSin(Expression thetaRadians);
        public abstract StringBuffer TranslateMathTan(Expression thetaRadians);
        public abstract StringBuffer TranslateMultiplyList(Expression list, Expression n);
        public abstract StringBuffer TranslateNegative(UnaryOp unaryOp);
        public abstract StringBuffer TranslateNullConstant();
        public abstract StringBuffer TranslateOrd(Expression charValue);
        public abstract StringBuffer TranslateOpPair(OpPair opPair);
        public abstract StringBuffer TranslateParseFloatUnsafe(Expression stringValue);
        public abstract StringBuffer TranslateParseInt(Expression safeStringValue);
        public abstract StringBuffer TranslatePrintStdErr(Expression value);
        public abstract StringBuffer TranslatePrintStdOut(Expression value);
        public abstract StringBuffer TranslateRandomFloat();
        public abstract StringBuffer TranslateSortedCopyOfIntArray(Expression intArray);
        public abstract StringBuffer TranslateSortedCopyOfStringArray(Expression stringArray);
        public abstract StringBuffer TranslateStringAppend(Expression str1, Expression str2);
        public abstract StringBuffer TranslateStringBuffer16();
        public abstract StringBuffer TranslateStringBuilderAdd(Expression sbInst, Expression obj);
        public abstract StringBuffer TranslateStringBuilderClear(Expression sbInst);
        public abstract StringBuffer TranslateStringBuilderToString(Expression sbInst);
        public abstract StringBuffer TranslateStringCharAt(Expression str, Expression index);
        public abstract StringBuffer TranslateStringCharCodeAt(Expression str, Expression index);
        public abstract StringBuffer TranslateStringCompareIsReverse(Expression str1, Expression str2);
        public abstract StringBuffer TranslateStringConcatAll(Expression[] strings);
        public abstract StringBuffer TranslateStringConcatPair(Expression strLeft, Expression strRight);
        public abstract StringBuffer TranslateStringConstant(string value);
        public abstract StringBuffer TranslateStringContains(Expression haystack, Expression needle);
        public abstract StringBuffer TranslateStringEndsWith(Expression haystack, Expression needle);
        public abstract StringBuffer TranslateStringEquals(Expression left, Expression right);
        public abstract StringBuffer TranslateStringFromCharCode(Expression charCode);
        public abstract StringBuffer TranslateStringIndexOf(Expression haystack, Expression needle);
        public abstract StringBuffer TranslateStringLastIndexOf(Expression haystack, Expression needle);
        public abstract StringBuffer TranslateStringIndexOfWithStart(Expression haystack, Expression needle, Expression startIndex);
        public abstract StringBuffer TranslateStringLength(Expression str);
        public abstract StringBuffer TranslateStringReplace(Expression haystack, Expression needle, Expression newNeedle);
        public abstract StringBuffer TranslateStringReverse(Expression str);
        public abstract StringBuffer TranslateStringSplit(Expression haystack, Expression needle);
        public abstract StringBuffer TranslateStringStartsWith(Expression haystack, Expression needle);
        public abstract StringBuffer TranslateStringSubstring(Expression str, Expression start, Expression length);
        public abstract StringBuffer TranslateStringSubstringIsEqualTo(Expression haystack, Expression startIndex, Expression needle);
        public abstract StringBuffer TranslateStringToLower(Expression str);
        public abstract StringBuffer TranslateStringToUpper(Expression str);
        public abstract StringBuffer TranslateStringToUtf8Bytes(Expression str);
        public abstract StringBuffer TranslateStringTrim(Expression str);
        public abstract StringBuffer TranslateStringTrimEnd(Expression str);
        public abstract StringBuffer TranslateStringTrimStart(Expression str);
        public abstract StringBuffer TranslateStrongReferenceEquality(Expression left, Expression right);
        public abstract StringBuffer TranslateStructFieldDereference(Expression root, StructDefinition structDef, string fieldName, int fieldIndex);
        public abstract StringBuffer TranslateToCodeString(Expression str);
        public abstract StringBuffer TranslateTryParseFloat(Expression stringValue, Expression floatOutList);
        public abstract StringBuffer TranslateUtf8BytesToString(Expression bytes);
        public abstract StringBuffer TranslateVariable(Variable variable);
    }
}
