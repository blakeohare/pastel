using Pastel.ParseNodes;
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

        public bool UsesStructDefinitions { get; protected set; }
        public bool UsesStringTable { get; protected set; }
        public bool UsesFunctionDeclarations { get; protected set; }
        public bool UsesStructDeclarations { get; protected set; }
        public bool UsesFree { get; protected set; }

        public AbstractTranspiler(string tab, string newLine)
        {
            this.UsesStructDefinitions = true;
            this.UsesFunctionDeclarations = false;
            this.UsesStructDeclarations = false;
            this.UsesStringTable = false;
            this.UsesFree = false;

            this.NewLine = newLine;
            this.TabChar = tab;
            this.Tabs = new string[20];
            this.Tabs[0] = "";
            for (int i = 1; i < 20; ++i)
            {
                this.Tabs[i] = this.Tabs[i - 1] + this.TabChar;
            }
        }

        public abstract void GenerateCode(TranspilerContext ctx, PastelCompiler compiler, Dictionary<string, string> files);

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

        public void TranslateExecutable(TranspilerContext sb, Executable executable)
        {
            string typeName = executable.GetType().Name;
            switch (typeName)
            {
                case "Assignment": this.TranslateAssignment(sb, (Assignment)executable); break;
                case "BreakStatement": this.TranslateBreak(sb); break;
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

                case "ExpressionAsExecutable":
                    ExpressionAsExecutable exprAsExec = (ExpressionAsExecutable)executable;
                    bool omit = false;
                    NativeFunctionInvocation nfi = exprAsExec.Expression as NativeFunctionInvocation;
                    if (nfi != null)
                    {
                        if (nfi.Function == NativeFunction.FREE)
                        {
                            omit = !sb.Transpiler.UsesFree;
                        }
                    }
                    if (!omit)
                    {
                        this.TranslateExpressionAsExecutable(sb, exprAsExec.Expression);
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
                case "NativeFunctionInvocation": this.TranslateNativeFunctionInvocation(sb, (NativeFunctionInvocation)expression); break;
                case "OpChain": this.TranslateOpChain(sb, (OpChain)expression); break;

                case "InlineIncrement":
                    InlineIncrement ii = (InlineIncrement)expression;
                    this.TranslateInlineIncrement(sb, ii.Expression, ii.IsPrefix, ii.IncrementToken.Value == "++");
                    break;

                case "FunctionInvocation":
                    FunctionInvocation funcInvocation = (FunctionInvocation)expression;
                    bool specifyInterpreterScope = false;
                    if (funcInvocation.FirstToken.FileName.StartsWith("LIB:") &&
                        funcInvocation.Root is FunctionReference)
                    {
                        FunctionDefinition funcDef = ((FunctionReference)funcInvocation.Root).Function;
                        if (!funcDef.NameToken.FileName.StartsWith("LIB:"))
                        {
                            specifyInterpreterScope = true;
                        }
                    }

                    if (specifyInterpreterScope)
                    {
                        this.TranslateFunctionInvocationInterpreterScoped(sb, (FunctionReference)funcInvocation.Root, funcInvocation.Args);
                    }
                    else
                    {
                        this.TranslateFunctionInvocationLocallyScoped(sb, (FunctionReference)funcInvocation.Root, funcInvocation.Args);
                    }
                    break;

                case "Variable":
                    Variable v = (Variable)expression;
                    string name = v.Name;
                    char firstChar = name[0];
                    if (firstChar >= 'A' && firstChar <= 'Z' && name.Contains('_') && name.ToUpper() == name)
                    {
                        this.TranslateGlobalVariable(sb, v);
                    }
                    else
                    {
                        this.TranslateVariable(sb, v);
                    }
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

                        default:
                            // TODO: throw an exception (in the parser) if generics exist.
                            this.TranslateConstructorInvocation(sb, constructor, constructor.StructType);
                            break;
                    }
                    break;

                case "DotField":
                    DotField df = (DotField)expression;
                    StructDefinition structDef = df.StructType;
                    if (structDef == null) throw new InvalidOperationException(); // should have been thrown by the compiler
                    string fieldName = df.FieldName.Value;
                    int fieldIndex = structDef.ArgIndexByName[fieldName];
                    this.TranslateStructFieldDereference(sb, df.Root, structDef, fieldName, fieldIndex);
                    break;

                case "InlineConstant":
                    InlineConstant ic = (InlineConstant)expression;
                    switch (ic.ResolvedType.RootValue)
                    {
                        case "bool": this.TranslateBooleanConstant(sb, (bool)ic.Value); break;
                        case "char": this.TranslateCharConstant(sb, ((string)ic.Value)[0]); break;
                        case "double": this.TranslateFloatConstant(sb, (double)ic.Value); break;
                        case "int": this.TranslateIntegerConstant(sb, (int)ic.Value); break;
                        case "null": this.TranslateNullConstant(sb); break;
                        case "string": this.TranslateStringConstant(sb, (string)ic.Value); break;
                        default: throw new NotImplementedException();
                    }
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

        public void TranslateNativeFunctionInvocation(TranspilerContext sb, NativeFunctionInvocation nativeFuncInvocation)
        {
            Expression[] args = nativeFuncInvocation.Args;
            switch (nativeFuncInvocation.Function)
            {
                case NativeFunction.ARRAY_COPY: this.TranslateArrayCopy(sb, args[0], args[1]); break;
                case NativeFunction.ARRAY_GET: this.TranslateArrayGet(sb, args[0], args[1]); break;
                case NativeFunction.ARRAY_JOIN: this.TranslateArrayJoin(sb, args[0], args[1]); break;
                case NativeFunction.ARRAY_LENGTH: this.TranslateArrayLength(sb, args[0]); break;
                case NativeFunction.ARRAY_SET: this.TranslateArraySet(sb, args[0], args[1], args[2]); break;
                case NativeFunction.ARRAY_SORT_FLOAT: this.TranslateArraySortFloat(sb, args[0], args[1]); break;
                case NativeFunction.ARRAY_SORT_INT: this.TranslateArraySortInt(sb, args[0], args[1]); break;
                case NativeFunction.ARRAY_SORT_STRING: this.TranslateArraySortString(sb, args[0], args[1]); break;
                case NativeFunction.BASE64_TO_STRING: this.TranslateBase64ToString(sb, args[0]); break;
                case NativeFunction.CHAR_TO_STRING: this.TranslateCharToString(sb, args[0]); break;
                case NativeFunction.CHR: this.TranslateChr(sb, args[0]); break;
                case NativeFunction.COMMAND_LINE_ARGS: this.TranslateCommandLineArgs(sb); break;
                case NativeFunction.CONVERT_RAW_DICTIONARY_VALUE_COLLECTION_TO_A_REUSABLE_VALUE_LIST: this.TranslateConvertRawDictionaryValueCollectionToAReusableValueList(sb, args[0]); break;
                case NativeFunction.CURRENT_TIME_SECONDS: this.TranslateCurrentTimeSeconds(sb); break;
                case NativeFunction.DICTIONARY_CONTAINS_KEY: this.TranslateDictionaryContainsKey(sb, args[0], args[1]); break;
                case NativeFunction.DICTIONARY_GET: this.TranslateDictionaryGet(sb, args[0], args[1]); break;
                case NativeFunction.DICTIONARY_KEYS: this.TranslateDictionaryKeys(sb, args[0]); break;
                case NativeFunction.DICTINOARY_KEYS_TO_VALUE_LIST: this.TranslateDictionaryKeysToValueList(sb, args[0]); break;
                case NativeFunction.DICTIONARY_LENGTH: this.TranslateDictionaryLength(sb, args[0]); break;
                case NativeFunction.DICTIONARY_NEW: this.TranslateDictionaryNew(sb, nativeFuncInvocation.ResolvedType.Generics[0], nativeFuncInvocation.ResolvedType.Generics[1]); break;
                case NativeFunction.DICTIONARY_REMOVE: this.TranslateDictionaryRemove(sb, args[0], args[1]); break;
                case NativeFunction.DICTIONARY_SET: this.TranslateDictionarySet(sb, args[0], args[1], args[2]); break;
                case NativeFunction.DICTIONARY_VALUES: this.TranslateDictionaryValues(sb, args[0]); break;
                case NativeFunction.DICTIONARY_VALUES_TO_VALUE_LIST: this.TranslateDictionaryValues(sb, args[0]); break;
                case NativeFunction.EMIT_COMMENT: this.TranslateEmitComment(sb, ((InlineConstant)args[0]).Value.ToString()); break;
                case NativeFunction.ENQUEUE_VM_RESUME: this.TranslateVmEnqueueResume(sb, args[0], args[1]); break;
                case NativeFunction.FLOAT_BUFFER_16: this.TranslateFloatBuffer16(sb); break;
                case NativeFunction.FLOAT_DIVISION: this.TranslateFloatDivision(sb, args[0], args[1]); break;
                case NativeFunction.FLOAT_TO_STRING: this.TranslateFloatToString(sb, args[0]); break;
                case NativeFunction.FREE: this.TranslateFree(sb, args[0]); break;
                case NativeFunction.GET_PROGRAM_DATA: this.TranslateGetProgramData(sb); break;
                case NativeFunction.GET_RESOURCE_MANIFEST: this.TranslateGetResourceManifest(sb); break;
                case NativeFunction.INT: this.TranslateFloatToInt(sb, args[0]); break;
                case NativeFunction.INT_BUFFER_16: this.TranslateIntBuffer16(sb); break;
                case NativeFunction.INT_TO_STRING: this.TranslateIntToString(sb, args[0]); break;
                case NativeFunction.INTEGER_DIVISION: this.TranslateIntegerDivision(sb, args[0], args[1]); break;
                case NativeFunction.INVOKE_DYNAMIC_LIBRARY_FUNCTION: this.TranslateInvokeDynamicLibraryFunction(sb, args[0], args[1]); break;
                case NativeFunction.IS_VALID_INTEGER: this.TranslateIsValidInteger(sb, args[0]); break;
                case NativeFunction.LIST_ADD: this.TranslateListAdd(sb, args[0], args[1]); break;
                case NativeFunction.LIST_CLEAR: this.TranslateListClear(sb, args[0]); break;
                case NativeFunction.LIST_CONCAT: this.TranslateListConcat(sb, args[0], args[1]); break;
                case NativeFunction.LIST_GET: this.TranslateListGet(sb, args[0], args[1]); break;
                case NativeFunction.LIST_INSERT: this.TranslateListInsert(sb, args[0], args[1], args[2]); break;
                case NativeFunction.LIST_JOIN_CHARS: this.TranslateListJoinChars(sb, args[0]); break;
                case NativeFunction.LIST_JOIN_STRINGS: this.TranslateListJoinStrings(sb, args[0], args[1]); break;
                case NativeFunction.LIST_LENGTH: this.TranslateListLength(sb, args[0]); break;
                case NativeFunction.LIST_NEW: this.TranslateListNew(sb, nativeFuncInvocation.ResolvedType.Generics[0]); break;
                case NativeFunction.LIST_POP: this.TranslateListPop(sb, args[0]); break;
                case NativeFunction.LIST_REMOVE_AT: this.TranslateListRemoveAt(sb, args[0], args[1]); break;
                case NativeFunction.LIST_REVERSE: this.TranslateListReverse(sb, args[0]); break;
                case NativeFunction.LIST_SET: this.TranslateListSet(sb, args[0], args[1], args[2]); break;
                case NativeFunction.LIST_SHUFFLE: this.TranslateListShuffle(sb, args[0]); break;
                case NativeFunction.LIST_TO_ARRAY: this.TranslateListToArray(sb, args[0]); break;
                case NativeFunction.MATH_ARCCOS: this.TranslateMathArcCos(sb, args[0]); break;
                case NativeFunction.MATH_ARCSIN: this.TranslateMathArcSin(sb, args[0]); break;
                case NativeFunction.MATH_ARCTAN: this.TranslateMathArcTan(sb, args[0], args[1]); break;
                case NativeFunction.MATH_COS: this.TranslateMathCos(sb, args[0]); break;
                case NativeFunction.MATH_LOG: this.TranslateMathLog(sb, args[0]); break;
                case NativeFunction.MATH_POW: this.TranslateMathPow(sb, args[0], args[1]); break;
                case NativeFunction.MATH_SQRT: this.TranslateMathSqrt(sb, args[0]); break;
                case NativeFunction.MATH_SIN: this.TranslateMathSin(sb, args[0]); break;
                case NativeFunction.MATH_TAN: this.TranslateMathTan(sb, args[0]); break;
                case NativeFunction.MULTIPLY_LIST: this.TranslateMultiplyList(sb, args[0], args[1]); break;
                case NativeFunction.ORD: this.TranslateOrd(sb, args[0]); break;
                case NativeFunction.PARSE_FLOAT_UNSAFE: this.TranslateParseFloatUnsafe(sb, args[0]); break;
                case NativeFunction.PARSE_INT: this.TranslateParseInt(sb, args[0]); break;
                case NativeFunction.PRINT_STDERR: this.TranslatePrintStdErr(sb, args[0]); break;
                case NativeFunction.PRINT_STDOUT: this.TranslatePrintStdOut(sb, args[0]); break;
                case NativeFunction.RANDOM_FLOAT: this.TranslateRandomFloat(sb); break;
                case NativeFunction.READ_BYTE_CODE_FILE: this.TranslateReadByteCodeFile(sb); break;
                case NativeFunction.REGISTER_LIBRARY_FUNCTION: this.TranslateRegisterLibraryFunction(sb, args[0], args[1], args[2]); break;
                case NativeFunction.RESOURCE_READ_TEXT_FILE: this.TranslateResourceReadTextFile(sb, args[0]); break;
                case NativeFunction.SET_PROGRAM_DATA: this.TranslateSetProgramData(sb, args[0]); break;
                case NativeFunction.SORTED_COPY_OF_INT_ARRAY: this.TranslateSortedCopyOfIntArray(sb, args[0]); break;
                case NativeFunction.SORTED_COPY_OF_STRING_ARRAY: this.TranslateSortedCopyOfStringArray(sb, args[0]); break;
                case NativeFunction.STRING_APPEND: this.TranslateStringAppend(sb, args[0], args[1]); break;
                case NativeFunction.STRING_BUFFER_16: this.TranslateStringBuffer16(sb); break;
                case NativeFunction.STRING_CHAR_AT: this.TranslateStringCharAt(sb, args[0], args[1]); break;
                case NativeFunction.STRING_CHAR_CODE_AT: this.TranslateStringCharCodeAt(sb, args[0], args[1]); break;
                case NativeFunction.STRING_COMPARE_IS_REVERSE: this.TranslateStringCompareIsReverse(sb, args[0], args[1]); break;
                case NativeFunction.STRING_CONCAT_ALL: if (args.Length == 2) this.TranslateStringConcatPair(sb, args[0], args[1]); else this.TranslateStringConcatAll(sb, args); break;
                case NativeFunction.STRING_CONTAINS: this.TranslateStringContains(sb, args[0], args[1]); break;
                case NativeFunction.STRING_ENDS_WITH: this.TranslateStringEndsWith(sb, args[0], args[1]); break;
                case NativeFunction.STRING_EQUALS: this.TranslateStringEquals(sb, args[0], args[1]); break;
                case NativeFunction.STRING_FROM_CHAR_CODE: this.TranslateStringFromCharCode(sb, args[0]); break;
                case NativeFunction.STRING_INDEX_OF: if (args.Length == 2) this.TranslateStringIndexOf(sb, args[0], args[1]); else this.TranslateStringIndexOfWithStart(sb, args[0], args[1], args[2]); break;
                case NativeFunction.STRING_LENGTH: this.TranslateStringLength(sb, args[0]); break;
                case NativeFunction.STRING_REPLACE: this.TranslateStringReplace(sb, args[0], args[1], args[2]); break;
                case NativeFunction.STRING_REVERSE: this.TranslateStringReverse(sb, args[0]); break;
                case NativeFunction.STRING_SPLIT: this.TranslateStringSplit(sb, args[0], args[1]); break;
                case NativeFunction.STRING_STARTS_WITH: this.TranslateStringStartsWith(sb, args[0], args[1]); break;
                case NativeFunction.STRING_SUBSTRING: this.TranslateStringSubstring(sb, args[0], args[1], args[2]); break;
                case NativeFunction.STRING_SUBSTRING_IS_EQUAL_TO: this.TranslateStringSubstringIsEqualTo(sb, args[0], args[1], args[2]); break;
                case NativeFunction.STRING_TO_LOWER: this.TranslateStringToLower(sb, args[0]); break;
                case NativeFunction.STRING_TO_UPPER: this.TranslateStringToUpper(sb, args[0]); break;
                case NativeFunction.STRING_TRIM: this.TranslateStringTrim(sb, args[0]); break;
                case NativeFunction.STRING_TRIM_END: this.TranslateStringTrimEnd(sb, args[0]); break;
                case NativeFunction.STRING_TRIM_START: this.TranslateStringTrimStart(sb, args[0]); break;
                case NativeFunction.STRONG_REFERENCE_EQUALITY: this.TranslateStrongReferenceEquality(sb, args[0], args[1]); break;
                case NativeFunction.THREAD_SLEEP: this.TranslateThreadSleep(sb, args[0]); break;
                case NativeFunction.TRY_PARSE_FLOAT: this.TranslateTryParseFloat(sb, args[0], args[1]); break;
                case NativeFunction.VM_DETERMINE_LIBRARY_AVAILABILITY: this.TranslateVmDetermineLibraryAvailability(sb, args[0], args[1]); break;
                case NativeFunction.VM_END_PROCESS: this.TranslateVmEndProcess(sb); break;
                case NativeFunction.VM_RUN_LIBRARY_MANIFEST: this.TranslateVmRunLibraryManifest(sb, args[0], args[1]); break;

                default: throw new NotImplementedException(nativeFuncInvocation.Function.ToString());
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

        public abstract void TranslateArrayCopy(TranspilerContext sb, Expression array, Expression length);
        public abstract void TranslateArrayGet(TranspilerContext sb, Expression array, Expression index);
        public abstract void TranslateArrayJoin(TranspilerContext sb, Expression array, Expression sep);
        public abstract void TranslateArrayLength(TranspilerContext sb, Expression array);
        public abstract void TranslateArrayNew(TranspilerContext sb, PType arrayType, Expression lengthExpression);
        public abstract void TranslateArraySet(TranspilerContext sb, Expression array, Expression index, Expression value);
        public abstract void TranslateArraySortFloat(TranspilerContext sb, Expression array, Expression length);
        public abstract void TranslateArraySortInt(TranspilerContext sb, Expression array, Expression length);
        public abstract void TranslateArraySortString(TranspilerContext sb, Expression array, Expression length);
        public abstract void TranslateAssignment(TranspilerContext sb, Assignment assignment);
        public abstract void TranslateBase64ToString(TranspilerContext sb, Expression base64String);
        public abstract void TranslateBooleanConstant(TranspilerContext sb, bool value);
        public abstract void TranslateBooleanNot(TranspilerContext sb, UnaryOp unaryOp);
        public abstract void TranslateBreak(TranspilerContext sb);
        public abstract void TranslateCast(TranspilerContext sb, PType type, Expression expression);
        public abstract void TranslateCharConstant(TranspilerContext sb, char value);
        public abstract void TranslateCharToString(TranspilerContext sb, Expression charValue);
        public abstract void TranslateChr(TranspilerContext sb, Expression charCode);
        public abstract void TranslateCommandLineArgs(TranspilerContext sb);
        public abstract void TranslateConstructorInvocation(TranspilerContext sb, ConstructorInvocation constructorInvocation, StructDefinition structDef);
        public abstract void TranslateConvertRawDictionaryValueCollectionToAReusableValueList(TranspilerContext sb, Expression dictionary);
        public abstract void TranslateCurrentTimeSeconds(TranspilerContext sb);
        public abstract void TranslateDictionaryContainsKey(TranspilerContext sb, Expression dictionary, Expression key);
        public abstract void TranslateDictionaryGet(TranspilerContext sb, Expression dictionary, Expression key);
        public abstract void TranslateDictionaryKeys(TranspilerContext sb, Expression dictionary);
        public abstract void TranslateDictionaryKeysToValueList(TranspilerContext sb, Expression dictionary);
        public abstract void TranslateDictionaryNew(TranspilerContext sb, PType keyType, PType valueType);
        public abstract void TranslateDictionaryRemove(TranspilerContext sb, Expression dictionary, Expression key);
        public abstract void TranslateDictionarySet(TranspilerContext sb, Expression dictionary, Expression key, Expression value);
        public abstract void TranslateDictionaryLength(TranspilerContext sb, Expression dictionary);
        public abstract void TranslateDictionaryValues(TranspilerContext sb, Expression dictionary);
        public abstract void TranslateDictionaryValuesToValueList(TranspilerContext sb, Expression dictionary);
        public abstract void TranslateEmitComment(TranspilerContext sb, string value);
        public abstract void TranslateExpressionAsExecutable(TranspilerContext sb, Expression expression);
        public abstract void TranslateFloatBuffer16(TranspilerContext sb);
        public abstract void TranslateFloatConstant(TranspilerContext sb, double value);
        public abstract void TranslateFloatDivision(TranspilerContext sb, Expression floatNumerator, Expression floatDenominator);
        public abstract void TranslateFloatToInt(TranspilerContext sb, Expression floatExpr);
        public abstract void TranslateFloatToString(TranspilerContext sb, Expression floatExpr);
        public abstract void TranslateFree(TranspilerContext ctx, Expression expression);
        public abstract void TranslateFunctionInvocationInterpreterScoped(TranspilerContext sb, FunctionReference funcRef, Expression[] args);
        public abstract void TranslateFunctionInvocationLocallyScoped(TranspilerContext sb, FunctionReference funcRef, Expression[] args);
        public abstract void TranslateFunctionReference(TranspilerContext sb, FunctionReference funcRef);
        public abstract void TranslateGetProgramData(TranspilerContext sb);
        public abstract void TranslateGetResourceManifest(TranspilerContext sb);
        public abstract void TranslateGlobalVariable(TranspilerContext sb, Variable variable);
        public abstract void TranslateIfStatement(TranspilerContext sb, IfStatement ifStatement);
        public abstract void TranslateInlineIncrement(TranspilerContext sb, Expression innerExpression, bool isPrefix, bool isAddition);
        public abstract void TranslateIntBuffer16(TranspilerContext sb);
        public abstract void TranslateIntegerConstant(TranspilerContext sb, int value);
        public abstract void TranslateIntegerDivision(TranspilerContext sb, Expression integerNumerator, Expression integerDenominator);
        public abstract void TranslateIntToString(TranspilerContext sb, Expression integer);
        public abstract void TranslateInvokeDynamicLibraryFunction(TranspilerContext sb, Expression functionId, Expression argsArray);
        public abstract void TranslateIsValidInteger(TranspilerContext sb, Expression stringValue);
        public abstract void TranslateListAdd(TranspilerContext sb, Expression list, Expression item);
        public abstract void TranslateListClear(TranspilerContext sb, Expression list);
        public abstract void TranslateListConcat(TranspilerContext sb, Expression list, Expression items);
        public abstract void TranslateListGet(TranspilerContext sb, Expression list, Expression index);
        public abstract void TranslateListInsert(TranspilerContext sb, Expression list, Expression index, Expression item);
        public abstract void TranslateListJoinChars(TranspilerContext sb, Expression list);
        public abstract void TranslateListJoinStrings(TranspilerContext sb, Expression list, Expression sep);
        public abstract void TranslateListLength(TranspilerContext sb, Expression list);
        public abstract void TranslateListNew(TranspilerContext sb, PType type);
        public abstract void TranslateListPop(TranspilerContext sb, Expression list);
        public abstract void TranslateListRemoveAt(TranspilerContext sb, Expression list, Expression index);
        public abstract void TranslateListReverse(TranspilerContext sb, Expression list);
        public abstract void TranslateListSet(TranspilerContext sb, Expression list, Expression index, Expression value);
        public abstract void TranslateListShuffle(TranspilerContext sb, Expression list);
        public abstract void TranslateListToArray(TranspilerContext sb, Expression list);
        public abstract void TranslateMathArcCos(TranspilerContext sb, Expression ratio);
        public abstract void TranslateMathArcSin(TranspilerContext sb, Expression ratio);
        public abstract void TranslateMathArcTan(TranspilerContext sb, Expression yComponent, Expression xComponent);
        public abstract void TranslateMathCos(TranspilerContext sb, Expression thetaRadians);
        public abstract void TranslateMathLog(TranspilerContext sb, Expression value);
        public abstract void TranslateMathPow(TranspilerContext sb, Expression expBase, Expression exponent);
        public abstract void TranslateMathSqrt(TranspilerContext sb, Expression value);
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
        public abstract void TranslateReadByteCodeFile(TranspilerContext sb);
        public abstract void TranslateRegisterLibraryFunction(TranspilerContext sb, Expression libRegObj, Expression functionName, Expression functionArgCount);
        public abstract void TranslateResourceReadTextFile(TranspilerContext sb, Expression path);
        public abstract void TranslateReturnStatemnt(TranspilerContext sb, ReturnStatement returnStatement);
        public abstract void TranslateSetProgramData(TranspilerContext sb, Expression programData);
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
        public abstract void TranslateStringTrim(TranspilerContext sb, Expression str);
        public abstract void TranslateStringTrimEnd(TranspilerContext sb, Expression str);
        public abstract void TranslateStringTrimStart(TranspilerContext sb, Expression str);
        public abstract void TranslateStrongReferenceEquality(TranspilerContext sb, Expression left, Expression right);
        public abstract void TranslateThreadSleep(TranspilerContext sb, Expression seconds);
        public abstract void TranslateTryParseFloat(TranspilerContext sb, Expression stringValue, Expression floatOutList);
        public abstract void TranslateStructFieldDereference(TranspilerContext sb, Expression root, StructDefinition structDef, string fieldName, int fieldIndex);
        public abstract void TranslateSwitchStatement(TranspilerContext sb, SwitchStatement switchStatement);
        public abstract void TranslateVariable(TranspilerContext sb, Variable variable);
        public abstract void TranslateVariableDeclaration(TranspilerContext sb, VariableDeclaration varDecl);
        public abstract void TranslateVmDetermineLibraryAvailability(TranspilerContext sb, Expression libraryName, Expression libraryVersion);
        public abstract void TranslateVmEndProcess(TranspilerContext sb);
        public abstract void TranslateVmEnqueueResume(TranspilerContext sb, Expression seconds, Expression executionContextId);
        public abstract void TranslateVmRunLibraryManifest(TranspilerContext sb, Expression libraryName, Expression libRegObj);
        public abstract void TranslateWhileLoop(TranspilerContext sb, WhileLoop whileLoop);

        public abstract void GenerateCodeForStruct(TranspilerContext sb, StructDefinition structDef);
        public abstract void GenerateCodeForFunction(TranspilerContext sb, FunctionDefinition funcDef);
        public abstract void GenerateCodeForGlobalsDefinitions(TranspilerContext sb, IList<VariableDeclaration> globals);

        public virtual void GenerateCodeForStructDeclaration(TranspilerContext sb, string structName)
        {
            throw new NotSupportedException();
        }

        // Overridden in languages that require a function to be declared separately in order for declaration order to not matter, such as C.
        public virtual void GenerateCodeForFunctionDeclaration(TranspilerContext sb, FunctionDefinition funcDef)
        {
            throw new NotSupportedException();
        }

        // Overridden in languages that can't allocate strings in the local scope.
        // For example, strings allocated in C will be reclaimed once the scope ends.
        public virtual void GenerateCodeForStringTable(TranspilerContext sb, StringTableBuilder stringTable)
        {
            throw new NotSupportedException();
        }
    }
}
