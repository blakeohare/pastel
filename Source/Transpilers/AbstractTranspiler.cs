﻿using Pastel.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel.Transpilers
{
    internal abstract class AbstractTranspiler
    {
        public string TabChar { get; private set; }
        public string[] Tabs { get; set; }
        public string NewLine { get; private set; }

        public bool ClassDefinitionsInSeparateFiles { get; protected set; }
        public bool UsesStructDefinitions { get; protected set; }
        public bool UsesClassDefinitions { get; protected set; }
        public bool UsesFunctionDeclarations { get; protected set; }
        public bool UsesStructDeclarations { get; protected set; }
        public bool HasStructsInSeparateFiles { get; protected set; }
        public bool HasNewLineAtEndOfFile { get; protected set; }

        public virtual string HelperCodeResourcePath { get { return null; } }

        public AbstractTranspiler(string tab, string newLine)
        {
            this.ClassDefinitionsInSeparateFiles = true;
            this.UsesStructDefinitions = true;
            this.UsesClassDefinitions = true;
            this.UsesFunctionDeclarations = false;
            this.UsesStructDeclarations = false;
            this.HasNewLineAtEndOfFile = true;
            this.HasStructsInSeparateFiles = true;

            this.NewLine = newLine;
            this.TabChar = tab;
            this.Tabs = new string[20];
            this.Tabs[0] = "";
            for (int i = 1; i < 20; ++i)
            {
                this.Tabs[i] = this.Tabs[i - 1] + this.TabChar;
            }
        }

        public virtual string TranslateType(PType type)
        {
            throw new InvalidOperationException("This platform does not support types.");
        }

        public virtual void TranslateExecutables(TranspilerContext sb, Executable[] executables)
        {
            for (int i = 0; i < executables.Length; ++i)
            {
                this.TranslateExecutable(sb, executables[i]);
            }
        }

        public virtual string WrapFinalExportedCode(string code, FunctionDefinition[] functions)
        {
            return code;
        }

        public void TranslateExecutable(TranspilerContext sb, Executable executable)
        {
            string typeName = executable.GetType().Name;
            switch (typeName)
            {
                case "Assignment":
                    Assignment asgn = (Assignment)executable;
                    if (asgn.Value is CoreFunctionInvocation &&
                        asgn.Target is Variable &&
                        ((CoreFunctionInvocation)asgn.Value).Function == CoreFunction.DICTIONARY_TRY_GET)
                    {
                        Variable variableOut = (Variable)asgn.Target;
                        Expression[] tryGetArgs = ((CoreFunctionInvocation)asgn.Value).Args;
                        Expression dictionary = tryGetArgs[0];
                        Expression key = tryGetArgs[1];
                        Expression fallbackValue = tryGetArgs[2];
                        this.TranslateDictionaryTryGet(sb, dictionary, key, fallbackValue, variableOut);
                    }
                    else
                    {
                        this.TranslateAssignment(sb, asgn);
                    }
                    break;

                case "BreakStatement": this.TranslateBreak(sb); break;
                case "ExpressionAsExecutable": this.TranslateExpressionAsExecutable(sb, ((ExpressionAsExecutable)executable).Expression); break;
                case "IfStatement": this.TranslateIfStatement(sb, (IfStatement)executable); break;
                case "ReturnStatement": this.TranslateReturnStatemnt(sb, (ReturnStatement)executable); break;
                case "SwitchStatement": this.TranslateSwitchStatement(sb, (SwitchStatement)executable); break;
                case "VariableDeclaration": this.TranslateVariableDeclaration(sb, (VariableDeclaration)executable); break;
                case "WhileLoop": this.TranslateWhileLoop(sb, (WhileLoop)executable); break;
                case "ExecutableBatch":
                    Executable[] execs = ((ExecutableBatch)executable).Executables;
                    for (int i = 0; i < execs.Length; ++i)
                    {
                        this.TranslateExecutable(sb, execs[i]);
                    }
                    break;

                default:
                    throw new NotImplementedException(typeName);
            }
        }

        public void TranslateExpression(TranspilerContext sb, Expression expression)
        {
            string typeName = expression.GetType().Name;
            switch (typeName)
            {
                case "CastExpression": this.TranslateCast(sb, ((CastExpression)expression).Type, ((CastExpression)expression).Expression); break;
                case "FunctionReference": this.TranslateFunctionReference(sb, (FunctionReference)expression); break;
                case "FunctionPointerInvocation": this.TranslateFunctionPointerInvocation(sb, (FunctionPointerInvocation)expression); break;
                case "CoreFunctionInvocation": this.TranslateCoreFunctionInvocation(sb, (CoreFunctionInvocation)expression); break;
                case "OpChain":
                    OpChain oc = (OpChain)expression;
                    if (oc.IsStringConcatenation)
                    {
                        this.TranslateStringConcatenation(sb, oc.Expressions);
                    }
                    else
                    {
                        this.TranslateOpChain(sb, oc);
                    }
                    break;
                case "ExtensibleFunctionInvocation":
                    this.TranslateExtensibleFunctionInvocation(
                        sb,
                        (ExtensibleFunctionInvocation)expression);
                    break;

                case "InlineIncrement":
                    InlineIncrement ii = (InlineIncrement)expression;
                    this.TranslateInlineIncrement(sb, ii.Expression, ii.IsPrefix, ii.IncrementToken.Value == "++");
                    break;

                case "FunctionInvocation":
                    FunctionInvocation funcInvocation = (FunctionInvocation)expression;
                    this.TranslateFunctionInvocation(sb, (FunctionReference)funcInvocation.Root, funcInvocation.Args);
                    break;

                case "Variable":
                    Variable v = (Variable)expression;
                    this.TranslateVariable(sb, v);
                    break;

                case "ConstructorInvocation":
                    ConstructorInvocation constructor = (ConstructorInvocation)expression;
                    string rootType = constructor.Type.RootValue;
                    switch (rootType)
                    {
                        case "Array":
                            if (constructor.Type.Generics.Length != 1)
                            {
                                throw new Pastel.ParserException(constructor.Type.FirstToken, "Array constructor requires exactly 1 generic type.");
                            }
                            this.TranslateArrayNew(sb, constructor.Type.Generics[0], constructor.Args[0]);
                            break;

                        case "List":
                            if (constructor.Type.Generics.Length != 1)
                            {
                                throw new Pastel.ParserException(constructor.Type.FirstToken, "List constructor requires exactly 1 generic type.");
                            }
                            this.TranslateListNew(sb, constructor.Type.Generics[0]);
                            break;

                        case "Dictionary":
                            if (constructor.Type.Generics.Length != 2)
                            {
                                throw new Pastel.ParserException(constructor.Type.FirstToken, "Dictionary constructor requires exactly 2 generic types.");
                            }
                            PType dictionaryKeyType = constructor.Type.Generics[0];
                            PType dictionaryValueType = constructor.Type.Generics[1];
                            this.TranslateDictionaryNew(sb, dictionaryKeyType, dictionaryValueType);
                            break;

                        case "StringBuilder":
                            if (constructor.Type.Generics.Length != 0)
                            {
                                throw new ParserException(constructor.Type.FirstToken, "StringBuilder constructor does not have any generics.");
                            }
                            this.TranslateStringBuilderNew(sb);
                            break;

                        default:
                            // TODO: throw an exception (in the parser) if generics exist.
                            this.TranslateConstructorInvocation(sb, constructor);
                            break;
                    }
                    break;

                case "DotField":
                    DotField df = (DotField)expression;
                    StructDefinition structDef = df.StructType;
                    ClassDefinition classDef = df.ClassType;
                    string fieldName = df.FieldName.Value;
                    if (classDef != null)
                    {
                        this.TranslateInstanceFieldDereference(sb, df.Root, classDef, fieldName);
                    }
                    else if (structDef != null)
                    {
                        int fieldIndex = structDef.FlatFieldIndexByName[fieldName];
                        this.TranslateStructFieldDereference(sb, df.Root, structDef, fieldName, fieldIndex);
                    }
                    else
                    {
                        throw new InvalidOperationException(); // should have been thrown by the compiler
                    }
                    break;

                case "InlineConstant":
                    InlineConstant ic = (InlineConstant)expression;
                    switch (ic.ResolvedType.RootValue)
                    {
                        case "bool": this.TranslateBooleanConstant(sb, (bool)ic.Value); break;
                        case "char": this.TranslateCharConstant(sb, ((char)ic.Value)); break;
                        case "double": this.TranslateFloatConstant(sb, (double)ic.Value); break;
                        case "int": this.TranslateIntegerConstant(sb, (int)ic.Value); break;
                        case "null": this.TranslateNullConstant(sb); break;
                        case "string": this.TranslateStringConstant(sb, (string)ic.Value); break;
                        default: throw new NotImplementedException();
                    }
                    break;

                case "ThisExpression":
                    this.TranslateThis(sb, (ThisExpression)expression);
                    break;

                case "UnaryOp":
                    UnaryOp uo = (UnaryOp)expression;
                    if (uo.OpToken.Value == "-") this.TranslateNegative(sb, uo);
                    else this.TranslateBooleanNot(sb, uo);
                    break;

                case "ForcedParenthesis":
                    sb.Append('(');
                    this.TranslateExpression(sb, ((ForcedParenthesis)expression).Expression);
                    sb.Append(')');
                    break;

                default: throw new NotImplementedException(typeName);
            }
        }

        public void TranslateStringConcatenation(TranspilerContext sb, Expression[] expressions)
        {
            if (expressions.Length == 2)
            {
                this.TranslateStringConcatPair(sb, expressions[0], expressions[1]);
            }
            else
            {
                this.TranslateStringConcatAll(sb, expressions);
            }
        }

        public virtual void TranslateFunctionPointerInvocation(TranspilerContext sb, FunctionPointerInvocation fpi)
        {
            this.TranslateExpression(sb, fpi.Root);
            sb.Append('(');
            for (int i = 0; i < fpi.Args.Length; ++i)
            {
                if (i > 0) sb.Append(", ");
                this.TranslateExpression(sb, fpi.Args[i]);
            }
            sb.Append(')');
        }

        public void TranslateCoreFunctionInvocation(TranspilerContext sb, CoreFunctionInvocation coreFuncInvocation)
        {
            Expression[] args = coreFuncInvocation.Args;
            switch (coreFuncInvocation.Function)
            {
                case CoreFunction.ARRAY_GET: this.TranslateArrayGet(sb, args[0], args[1]); break;
                case CoreFunction.ARRAY_JOIN: this.TranslateArrayJoin(sb, args[0], args[1]); break;
                case CoreFunction.ARRAY_LENGTH: this.TranslateArrayLength(sb, args[0]); break;
                case CoreFunction.ARRAY_SET: this.TranslateArraySet(sb, args[0], args[1], args[2]); break;
                case CoreFunction.BASE64_TO_BYTES: this.TranslateBase64ToBytes(sb, args[0]); break;
                case CoreFunction.BASE64_TO_STRING: this.TranslateBase64ToString(sb, args[0]); break;
                case CoreFunction.CHAR_TO_STRING: this.TranslateCharToString(sb, args[0]); break;
                case CoreFunction.CHR: this.TranslateChr(sb, args[0]); break;
                case CoreFunction.CURRENT_TIME_SECONDS: this.TranslateCurrentTimeSeconds(sb); break;
                case CoreFunction.DICTIONARY_CONTAINS_KEY: this.TranslateDictionaryContainsKey(sb, args[0], args[1]); break;
                case CoreFunction.DICTIONARY_GET: this.TranslateDictionaryGet(sb, args[0], args[1]); break;
                case CoreFunction.DICTIONARY_KEYS: this.TranslateDictionaryKeys(sb, args[0]); break;
                case CoreFunction.DICTIONARY_NEW: this.TranslateDictionaryNew(sb, coreFuncInvocation.ResolvedType.Generics[0], coreFuncInvocation.ResolvedType.Generics[1]); break;
                case CoreFunction.DICTIONARY_REMOVE: this.TranslateDictionaryRemove(sb, args[0], args[1]); break;
                case CoreFunction.DICTIONARY_SET: this.TranslateDictionarySet(sb, args[0], args[1], args[2]); break;
                case CoreFunction.DICTIONARY_SIZE: this.TranslateDictionarySize(sb, args[0]); break;
                case CoreFunction.DICTIONARY_VALUES: this.TranslateDictionaryValues(sb, args[0]); break;
                case CoreFunction.EMIT_COMMENT: this.TranslateEmitComment(sb, ((InlineConstant)args[0]).Value.ToString()); break;
                case CoreFunction.EXTENSIBLE_CALLBACK_INVOKE: this.TranslateExtensibleCallbackInvoke(sb, args[0], args[1]); break;
                case CoreFunction.FLOAT_BUFFER_16: this.TranslateFloatBuffer16(sb); break;
                case CoreFunction.FLOAT_DIVISION: this.TranslateFloatDivision(sb, args[0], args[1]); break;
                case CoreFunction.FLOAT_TO_STRING: this.TranslateFloatToString(sb, args[0]); break;
                case CoreFunction.GET_FUNCTION: this.TranslateGetFunction(sb, args[0]); break;
                case CoreFunction.INT: this.TranslateFloatToInt(sb, args[0]); break;
                case CoreFunction.INT_BUFFER_16: this.TranslateIntBuffer16(sb); break;
                case CoreFunction.INT_TO_STRING: this.TranslateIntToString(sb, args[0]); break;
                case CoreFunction.INTEGER_DIVISION: this.TranslateIntegerDivision(sb, args[0], args[1]); break;
                case CoreFunction.IS_VALID_INTEGER: this.TranslateIsValidInteger(sb, args[0]); break;
                case CoreFunction.LIST_ADD: this.TranslateListAdd(sb, args[0], args[1]); break;
                case CoreFunction.LIST_CLEAR: this.TranslateListClear(sb, args[0]); break;
                case CoreFunction.LIST_CONCAT: this.TranslateListConcat(sb, args[0], args[1]); break;
                case CoreFunction.LIST_GET: this.TranslateListGet(sb, args[0], args[1]); break;
                case CoreFunction.LIST_INSERT: this.TranslateListInsert(sb, args[0], args[1], args[2]); break;
                case CoreFunction.LIST_JOIN_CHARS: this.TranslateListJoinChars(sb, args[0]); break;
                case CoreFunction.LIST_JOIN_STRINGS: this.TranslateListJoinStrings(sb, args[0], args[1]); break;
                case CoreFunction.LIST_NEW: this.TranslateListNew(sb, coreFuncInvocation.ResolvedType.Generics[0]); break;
                case CoreFunction.LIST_POP: this.TranslateListPop(sb, args[0]); break;
                case CoreFunction.LIST_REMOVE_AT: this.TranslateListRemoveAt(sb, args[0], args[1]); break;
                case CoreFunction.LIST_REVERSE: this.TranslateListReverse(sb, args[0]); break;
                case CoreFunction.LIST_SET: this.TranslateListSet(sb, args[0], args[1], args[2]); break;
                case CoreFunction.LIST_SHUFFLE: this.TranslateListShuffle(sb, args[0]); break;
                case CoreFunction.LIST_SIZE: this.TranslateListSize(sb, args[0]); break;
                case CoreFunction.LIST_TO_ARRAY: this.TranslateListToArray(sb, args[0]); break;
                case CoreFunction.MATH_ARCCOS: this.TranslateMathArcCos(sb, args[0]); break;
                case CoreFunction.MATH_ARCSIN: this.TranslateMathArcSin(sb, args[0]); break;
                case CoreFunction.MATH_ARCTAN: this.TranslateMathArcTan(sb, args[0], args[1]); break;
                case CoreFunction.MATH_COS: this.TranslateMathCos(sb, args[0]); break;
                case CoreFunction.MATH_LOG: this.TranslateMathLog(sb, args[0]); break;
                case CoreFunction.MATH_POW: this.TranslateMathPow(sb, args[0], args[1]); break;
                case CoreFunction.MATH_SIN: this.TranslateMathSin(sb, args[0]); break;
                case CoreFunction.MATH_TAN: this.TranslateMathTan(sb, args[0]); break;
                case CoreFunction.MULTIPLY_LIST: this.TranslateMultiplyList(sb, args[0], args[1]); break;
                case CoreFunction.ORD: this.TranslateOrd(sb, args[0]); break;
                case CoreFunction.PARSE_FLOAT_UNSAFE: this.TranslateParseFloatUnsafe(sb, args[0]); break;
                case CoreFunction.PARSE_INT: this.TranslateParseInt(sb, args[0]); break;
                case CoreFunction.PRINT_STDERR: this.TranslatePrintStdErr(sb, args[0]); break;
                case CoreFunction.PRINT_STDOUT: this.TranslatePrintStdOut(sb, args[0]); break;
                case CoreFunction.RANDOM_FLOAT: this.TranslateRandomFloat(sb); break;
                case CoreFunction.SORTED_COPY_OF_INT_ARRAY: this.TranslateSortedCopyOfIntArray(sb, args[0]); break;
                case CoreFunction.SORTED_COPY_OF_STRING_ARRAY: this.TranslateSortedCopyOfStringArray(sb, args[0]); break;
                case CoreFunction.STRING_APPEND: this.TranslateStringAppend(sb, args[0], args[1]); break;
                case CoreFunction.STRING_BUFFER_16: this.TranslateStringBuffer16(sb); break;
                case CoreFunction.STRING_CHAR_AT: this.TranslateStringCharAt(sb, args[0], args[1]); break;
                case CoreFunction.STRING_CHAR_CODE_AT: this.TranslateStringCharCodeAt(sb, args[0], args[1]); break;
                case CoreFunction.STRING_COMPARE_IS_REVERSE: this.TranslateStringCompareIsReverse(sb, args[0], args[1]); break;
                case CoreFunction.STRING_CONCAT_ALL: if (args.Length == 2) this.TranslateStringConcatPair(sb, args[0], args[1]); else this.TranslateStringConcatAll(sb, args); break;
                case CoreFunction.STRING_CONTAINS: this.TranslateStringContains(sb, args[0], args[1]); break;
                case CoreFunction.STRING_ENDS_WITH: this.TranslateStringEndsWith(sb, args[0], args[1]); break;
                case CoreFunction.STRING_EQUALS: this.TranslateStringEquals(sb, args[0], args[1]); break;
                case CoreFunction.STRING_FROM_CHAR_CODE: this.TranslateStringFromCharCode(sb, args[0]); break;
                case CoreFunction.STRING_INDEX_OF: if (args.Length == 2) this.TranslateStringIndexOf(sb, args[0], args[1]); else this.TranslateStringIndexOfWithStart(sb, args[0], args[1], args[2]); break;
                case CoreFunction.STRING_LAST_INDEX_OF: this.TranslateStringLastIndexOf(sb, args[0], args[1]); break;
                case CoreFunction.STRING_LENGTH: this.TranslateStringLength(sb, args[0]); break;
                case CoreFunction.STRING_REPLACE: this.TranslateStringReplace(sb, args[0], args[1], args[2]); break;
                case CoreFunction.STRING_REVERSE: this.TranslateStringReverse(sb, args[0]); break;
                case CoreFunction.STRING_SPLIT: this.TranslateStringSplit(sb, args[0], args[1]); break;
                case CoreFunction.STRING_STARTS_WITH: this.TranslateStringStartsWith(sb, args[0], args[1]); break;
                case CoreFunction.STRING_SUBSTRING: this.TranslateStringSubstring(sb, args[0], args[1], args[2]); break;
                case CoreFunction.STRING_SUBSTRING_IS_EQUAL_TO: this.TranslateStringSubstringIsEqualTo(sb, args[0], args[1], args[2]); break;
                case CoreFunction.STRING_TO_LOWER: this.TranslateStringToLower(sb, args[0]); break;
                case CoreFunction.STRING_TO_UPPER: this.TranslateStringToUpper(sb, args[0]); break;
                case CoreFunction.STRING_TO_UTF8_BYTES: this.TranslateStringToUtf8Bytes(sb, args[0]); break;
                case CoreFunction.STRING_TRIM: this.TranslateStringTrim(sb, args[0]); break;
                case CoreFunction.STRING_TRIM_END: this.TranslateStringTrimEnd(sb, args[0]); break;
                case CoreFunction.STRING_TRIM_START: this.TranslateStringTrimStart(sb, args[0]); break;
                case CoreFunction.STRINGBUILDER_ADD: this.TranslateStringBuilderAdd(sb, args[0], args[1]); break;
                case CoreFunction.STRINGBUILDER_CLEAR: this.TranslateStringBuilderClear(sb, args[0]); break;
                case CoreFunction.STRINGBUILDER_TOSTRING: this.TranslateStringBuilderToString(sb, args[0]); break;
                case CoreFunction.STRONG_REFERENCE_EQUALITY: this.TranslateStrongReferenceEquality(sb, args[0], args[1]); break;
                case CoreFunction.TO_CODE_STRING: this.TranslateToCodeString(sb, args[0]); break;
                case CoreFunction.TRY_PARSE_FLOAT: this.TranslateTryParseFloat(sb, args[0], args[1]); break;
                case CoreFunction.UTF8_BYTES_TO_STRING: this.TranslateUtf8BytesToString(sb, args[0]); break;

                case CoreFunction.DICTIONARY_TRY_GET:
                    throw new ParserException(coreFuncInvocation.FirstToken, "Dictionary's TryGet method cannot be called like this. It must be assigned to a variable directly. This is due to a restriction in how this can get transpiled to certain languages.");

                default: throw new NotImplementedException(coreFuncInvocation.Function.ToString());
            }
        }

        public void TranslateExtensibleFunctionInvocation(TranspilerContext sb, ExtensibleFunctionInvocation funcInvocation)
        {
            Expression[] args = funcInvocation.Args;
            Token throwToken = funcInvocation.FunctionRef.FirstToken;
            string functionName = funcInvocation.FunctionRef.Name;
            Dictionary<string, string> extLookup = sb.ExtensibleFunctionLookup;

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
                sb.Append(codeSnippet);
            }
            else
            {
                sb.Append(codeSnippet.Substring(0, locations[argOrdinalsInOrder[0]][0]));
                for (int i = 0; i < argOrdinalsInOrder.Length; ++i)
                {
                    int currentArgOrdinal = argOrdinalsInOrder[i];
                    int nextArgOrdinal = i + 1 < argOrdinalsInOrder.Length ? argOrdinalsInOrder[i + 1] : -1;
                    sb.Transpiler.TranslateExpression(sb, (Expression)args[currentArgOrdinal]);
                    int argEndIndex = locations[currentArgOrdinal][2];
                    if (nextArgOrdinal == -1)
                    {
                        // Take the code snippet from the end of the current arg to the end and
                        // add it to the buffer.
                        sb.Append(codeSnippet.Substring(argEndIndex));
                    }
                    else
                    {
                        int nextArgBeginIndex = locations[nextArgOrdinal][0];
                        sb.Append(codeSnippet.Substring(argEndIndex, nextArgBeginIndex - argEndIndex));
                    }
                }
            }
        }

        public void TranslateCommaDelimitedExpressions(TranspilerContext sb, IList<Expression> expressions)
        {
            for (int i = 0; i < expressions.Count; ++i)
            {
                if (i > 0) sb.Append(", ");
                this.TranslateExpression(sb, expressions[i]);
            }
        }

        public abstract void TranslateArrayGet(TranspilerContext sb, Expression array, Expression index);
        public abstract void TranslateArrayJoin(TranspilerContext sb, Expression array, Expression sep);
        public abstract void TranslateArrayLength(TranspilerContext sb, Expression array);
        public abstract void TranslateArrayNew(TranspilerContext sb, PType arrayType, Expression lengthExpression);
        public abstract void TranslateArraySet(TranspilerContext sb, Expression array, Expression index, Expression value);
        public abstract void TranslateAssignment(TranspilerContext sb, Assignment assignment);
        public abstract void TranslateBase64ToBytes(TranspilerContext sb, Expression base64String);
        public abstract void TranslateBase64ToString(TranspilerContext sb, Expression base64String);
        public abstract void TranslateBooleanConstant(TranspilerContext sb, bool value);
        public abstract void TranslateBooleanNot(TranspilerContext sb, UnaryOp unaryOp);
        public abstract void TranslateBreak(TranspilerContext sb);
        public abstract void TranslateCast(TranspilerContext sb, PType type, Expression expression);
        public abstract void TranslateCharConstant(TranspilerContext sb, char value);
        public abstract void TranslateCharToString(TranspilerContext sb, Expression charValue);
        public abstract void TranslateChr(TranspilerContext sb, Expression charCode);
        public abstract void TranslateConstructorInvocation(TranspilerContext sb, ConstructorInvocation constructorInvocation);
        public abstract void TranslateCurrentTimeSeconds(TranspilerContext sb);
        public abstract void TranslateDictionaryContainsKey(TranspilerContext sb, Expression dictionary, Expression key);
        public abstract void TranslateDictionaryGet(TranspilerContext sb, Expression dictionary, Expression key);
        public abstract void TranslateDictionaryKeys(TranspilerContext sb, Expression dictionary);
        public abstract void TranslateDictionaryNew(TranspilerContext sb, PType keyType, PType valueType);
        public abstract void TranslateDictionaryRemove(TranspilerContext sb, Expression dictionary, Expression key);
        public abstract void TranslateDictionarySet(TranspilerContext sb, Expression dictionary, Expression key, Expression value);
        public abstract void TranslateDictionarySize(TranspilerContext sb, Expression dictionary);
        public abstract void TranslateDictionaryTryGet(TranspilerContext sb, Expression dictionary, Expression key, Expression fallbackValue, Variable varOut);
        public abstract void TranslateDictionaryValues(TranspilerContext sb, Expression dictionary);
        public abstract void TranslateEmitComment(TranspilerContext sb, string value);
        public abstract void TranslateExtensibleCallbackInvoke(TranspilerContext sb, Expression name, Expression argsArray);
        public abstract void TranslateExpressionAsExecutable(TranspilerContext sb, Expression expression);
        public abstract void TranslateFloatBuffer16(TranspilerContext sb);
        public abstract void TranslateFloatConstant(TranspilerContext sb, double value);
        public abstract void TranslateFloatDivision(TranspilerContext sb, Expression floatNumerator, Expression floatDenominator);
        public abstract void TranslateFloatToInt(TranspilerContext sb, Expression floatExpr);
        public abstract void TranslateFloatToString(TranspilerContext sb, Expression floatExpr);
        public abstract void TranslateFunctionInvocation(TranspilerContext sb, FunctionReference funcRef, Expression[] args);
        public abstract void TranslateFunctionReference(TranspilerContext sb, FunctionReference funcRef);
        public abstract void TranslateGetFunction(TranspilerContext sb, Expression name);
        public abstract void TranslateIfStatement(TranspilerContext sb, IfStatement ifStatement);
        public abstract void TranslateInlineIncrement(TranspilerContext sb, Expression innerExpression, bool isPrefix, bool isAddition);
        public abstract void TranslateInstanceFieldDereference(TranspilerContext sb, Expression root, ClassDefinition classDef, string fieldName);
        public abstract void TranslateIntBuffer16(TranspilerContext sb);
        public abstract void TranslateIntegerConstant(TranspilerContext sb, int value);
        public abstract void TranslateIntegerDivision(TranspilerContext sb, Expression integerNumerator, Expression integerDenominator);
        public abstract void TranslateIntToString(TranspilerContext sb, Expression integer);
        public abstract void TranslateIsValidInteger(TranspilerContext sb, Expression stringValue);
        public abstract void TranslateListAdd(TranspilerContext sb, Expression list, Expression item);
        public abstract void TranslateListClear(TranspilerContext sb, Expression list);
        public abstract void TranslateListConcat(TranspilerContext sb, Expression list, Expression items);
        public abstract void TranslateListGet(TranspilerContext sb, Expression list, Expression index);
        public abstract void TranslateListInsert(TranspilerContext sb, Expression list, Expression index, Expression item);
        public abstract void TranslateListJoinChars(TranspilerContext sb, Expression list);
        public abstract void TranslateListJoinStrings(TranspilerContext sb, Expression list, Expression sep);
        public abstract void TranslateListNew(TranspilerContext sb, PType type);
        public abstract void TranslateListPop(TranspilerContext sb, Expression list);
        public abstract void TranslateListRemoveAt(TranspilerContext sb, Expression list, Expression index);
        public abstract void TranslateListReverse(TranspilerContext sb, Expression list);
        public abstract void TranslateListSet(TranspilerContext sb, Expression list, Expression index, Expression value);
        public abstract void TranslateListShuffle(TranspilerContext sb, Expression list);
        public abstract void TranslateListSize(TranspilerContext sb, Expression list);
        public abstract void TranslateStringBuilderNew(TranspilerContext sb);
        public abstract void TranslateStringBuilderAdd(TranspilerContext sb, Expression sbInst, Expression obj);
        public abstract void TranslateStringBuilderClear(TranspilerContext sb, Expression sbInst);
        public abstract void TranslateStringBuilderToString(TranspilerContext sb, Expression sbInst);
        public abstract void TranslateListToArray(TranspilerContext sb, Expression list);
        public abstract void TranslateMathArcCos(TranspilerContext sb, Expression ratio);
        public abstract void TranslateMathArcSin(TranspilerContext sb, Expression ratio);
        public abstract void TranslateMathArcTan(TranspilerContext sb, Expression yComponent, Expression xComponent);
        public abstract void TranslateMathCos(TranspilerContext sb, Expression thetaRadians);
        public abstract void TranslateMathLog(TranspilerContext sb, Expression value);
        public abstract void TranslateMathPow(TranspilerContext sb, Expression expBase, Expression exponent);
        public abstract void TranslateMathSin(TranspilerContext sb, Expression thetaRadians);
        public abstract void TranslateMathTan(TranspilerContext sb, Expression thetaRadians);
        public abstract void TranslateMultiplyList(TranspilerContext sb, Expression list, Expression n);
        public abstract void TranslateNegative(TranspilerContext sb, UnaryOp unaryOp);
        public abstract void TranslateNullConstant(TranspilerContext sb);
        public abstract void TranslateOrd(TranspilerContext sb, Expression charValue);
        public abstract void TranslateOpChain(TranspilerContext sb, OpChain opChain);
        public abstract void TranslateParseFloatUnsafe(TranspilerContext sb, Expression stringValue);
        public abstract void TranslateParseInt(TranspilerContext sb, Expression safeStringValue);
        public abstract void TranslatePrintStdErr(TranspilerContext sb, Expression value);
        public abstract void TranslatePrintStdOut(TranspilerContext sb, Expression value);
        public abstract void TranslateRandomFloat(TranspilerContext sb);
        public abstract void TranslateReturnStatemnt(TranspilerContext sb, ReturnStatement returnStatement);
        public abstract void TranslateSortedCopyOfIntArray(TranspilerContext sb, Expression intArray);
        public abstract void TranslateSortedCopyOfStringArray(TranspilerContext sb, Expression stringArray);
        public abstract void TranslateStringAppend(TranspilerContext sb, Expression str1, Expression str2);
        public abstract void TranslateStringBuffer16(TranspilerContext sb);
        public abstract void TranslateStringCharAt(TranspilerContext sb, Expression str, Expression index);
        public abstract void TranslateStringCharCodeAt(TranspilerContext sb, Expression str, Expression index);
        public abstract void TranslateStringCompareIsReverse(TranspilerContext sb, Expression str1, Expression str2);
        public abstract void TranslateStringConcatAll(TranspilerContext sb, Expression[] strings);
        public abstract void TranslateStringConcatPair(TranspilerContext sb, Expression strLeft, Expression strRight);
        public abstract void TranslateStringConstant(TranspilerContext sb, string value);
        public abstract void TranslateStringContains(TranspilerContext sb, Expression haystack, Expression needle);
        public abstract void TranslateStringEndsWith(TranspilerContext sb, Expression haystack, Expression needle);
        public abstract void TranslateStringEquals(TranspilerContext sb, Expression left, Expression right);
        public abstract void TranslateStringFromCharCode(TranspilerContext sb, Expression charCode);
        public abstract void TranslateStringIndexOf(TranspilerContext sb, Expression haystack, Expression needle);
        public abstract void TranslateStringLastIndexOf(TranspilerContext sb, Expression haystack, Expression needle);
        public abstract void TranslateStringIndexOfWithStart(TranspilerContext sb, Expression haystack, Expression needle, Expression startIndex);
        public abstract void TranslateStringLength(TranspilerContext sb, Expression str);
        public abstract void TranslateStringReplace(TranspilerContext sb, Expression haystack, Expression needle, Expression newNeedle);
        public abstract void TranslateStringReverse(TranspilerContext sb, Expression str);
        public abstract void TranslateStringSplit(TranspilerContext sb, Expression haystack, Expression needle);
        public abstract void TranslateStringStartsWith(TranspilerContext sb, Expression haystack, Expression needle);
        public abstract void TranslateStringSubstring(TranspilerContext sb, Expression str, Expression start, Expression length);
        public abstract void TranslateStringSubstringIsEqualTo(TranspilerContext sb, Expression haystack, Expression startIndex, Expression needle);
        public abstract void TranslateStringToLower(TranspilerContext sb, Expression str);
        public abstract void TranslateStringToUpper(TranspilerContext sb, Expression str);
        public abstract void TranslateStringToUtf8Bytes(TranspilerContext sb, Expression str);
        public abstract void TranslateStringTrim(TranspilerContext sb, Expression str);
        public abstract void TranslateStringTrimEnd(TranspilerContext sb, Expression str);
        public abstract void TranslateStringTrimStart(TranspilerContext sb, Expression str);
        public abstract void TranslateStrongReferenceEquality(TranspilerContext sb, Expression left, Expression right);
        public abstract void TranslateStructFieldDereference(TranspilerContext sb, Expression root, StructDefinition structDef, string fieldName, int fieldIndex);
        public abstract void TranslateSwitchStatement(TranspilerContext sb, SwitchStatement switchStatement);
        public abstract void TranslateThis(TranspilerContext sb, ThisExpression thisExpr);
        public abstract void TranslateToCodeString(TranspilerContext sb, Expression str);
        public abstract void TranslateTryParseFloat(TranspilerContext sb, Expression stringValue, Expression floatOutList);
        public abstract void TranslateUtf8BytesToString(TranspilerContext sb, Expression bytes);
        public abstract void TranslateVariable(TranspilerContext sb, Variable variable);
        public abstract void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl);
        public abstract void TranslateWhileLoop(TranspilerContext sb, WhileLoop whileLoop);

        public abstract void GenerateCodeForClass(TranspilerContext sb, ClassDefinition classDef);
        public abstract void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef);
        public abstract void GenerateCodeForFunction(TranspilerContext sb, FunctionDefinition funcDef, bool isStatic);

        public virtual void GenerateCodeForStructDeclaration(TranspilerContext sb, string structName)
        {
            throw new NotSupportedException();
        }

        // Overridden in languages that require a function to be declared separately in order for declaration order to not matter, such as C.
        public virtual void GenerateCodeForFunctionDeclaration(TranspilerContext sb, FunctionDefinition funcDef, bool isStatic)
        {
            throw new NotSupportedException();
        }

        internal string WrapCodeForFunctions(TranspilerContext ctx, ProjectConfig config, string code)
        {
            List<string> lines = new List<string>(code.Split('\n').Select(t => t.TrimEnd()));
            WrapCodeImpl(ctx, config, lines, false);
            string output = string.Join(this.NewLine, lines).Trim();
            if (this.HasNewLineAtEndOfFile) output += this.NewLine;
            return output;
        }

        internal string WrapCodeForStructs(TranspilerContext ctx, ProjectConfig config, string code)
        {
            List<string> lines = new List<string>(code.Split('\n').Select(t => t.TrimEnd()));
            WrapCodeImpl(ctx, config, lines, true);
            string output = string.Join(this.NewLine, lines).Trim();
            if (this.HasNewLineAtEndOfFile) output += this.NewLine;
            return output;
        }

        internal string WrapCodeForClasses(TranspilerContext ctx, ProjectConfig config, string code)
        {
            List<string> lines = new List<string>(code.Split('\n').Select(t => t.TrimEnd()));
            WrapCodeImpl(ctx, config, lines, true);
            string output = string.Join(this.NewLine, lines).Trim();
            if (this.HasNewLineAtEndOfFile) output += this.NewLine;
            return output;
        }

        protected abstract void WrapCodeImpl(TranspilerContext ctx, ProjectConfig config, List<string> lines, bool isForStruct);
    }
}
