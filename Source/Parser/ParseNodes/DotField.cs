using System;
using System.Collections.Generic;

namespace Pastel.Parser.ParseNodes
{
    internal class DotField : Expression
    {
        public Expression Root { get; set; }
        public Token DotToken { get; set; }
        public Token FieldName { get; set; }

        public CoreFunction CoreFunctionId { get; set; }
        public StructDefinition StructType { get; set; }

        public DotField(Expression root, Token dotToken, Token fieldName)
            : base(ExpressionType.DOT_FIELD, root.FirstToken, root.Owner)
        {
            this.Root = root;
            this.DotToken = dotToken;
            this.FieldName = fieldName;
            this.CoreFunctionId = CoreFunction.NONE;
        }

        public override Expression ResolveNamesAndCullUnusedCode(Resolver resolver)
        {
            if (this.Root.Type == ExpressionType.VARIABLE && ((Variable)this.Root).Name == "Core")
            {
                CoreFunction coreFunction = this.GetCoreFunction(this.FieldName.Value);
                return new CoreFunctionReference(this.FirstToken, coreFunction, this.Owner);
            }
            
            this.Root = this.Root.ResolveNamesAndCullUnusedCode(resolver);

            if (this.Root is EnumReference)
            {
                InlineConstant enumValue = ((EnumReference)this.Root).EnumDef.GetValue(this.FieldName);
                return enumValue.CloneWithNewToken(this.FirstToken);
            }

            if (this.Root is EnumReference enumRef)
            {
                EnumDefinition enumDef = enumRef.EnumDef;
                InlineConstant enumValue = enumDef.GetValue(this.FieldName);
                return enumValue;
            }

            return this;
        }

        internal override InlineConstant DoConstantResolution(HashSet<string> cycleDetection, Resolver resolver)
        {
            Variable varRoot = this.Root as Variable;
            if (varRoot == null) throw new ParserException(this.FirstToken, "Not able to resolve this constant.");
            string enumName = varRoot.Name;
            EnumDefinition enumDef = resolver.GetEnumDefinition(enumName);
            if (enumDef == null)
            {
                throw new ParserException(this.FirstToken, "Not able to resolve this constant.");
            }

            return enumDef.GetValue(this.FieldName);
        }

        internal override Expression ResolveType(VariableScope varScope, Resolver resolver)
        {
            Root = Root.ResolveType(varScope, resolver);

            PType rootType = Root.ResolvedType;
            if (rootType.IsStruct)
            {
                string fieldName = FieldName.Value;
                rootType.FinalizeType(resolver);
                this.StructType = rootType.StructDef;
                int fieldIndex;
                if (!this.StructType.FieldIndexByName.TryGetValue(fieldName, out fieldIndex))
                {
                    throw new ParserException(this.FieldName, "The struct '" + this.StructType.NameToken.Value + "' does not have a field called '" + fieldName + "'.");
                }
                this.ResolvedType = this.StructType.FieldTypes[fieldIndex];

                return this;
            }

            this.CoreFunctionId = DetermineCoreFunctionId(this.Root.ResolvedType, this.FieldName.Value);
            if (this.CoreFunctionId != CoreFunction.NONE)
            {
                CoreFunctionReference cfr = new CoreFunctionReference(FirstToken, CoreFunctionId, Root, Owner);
                cfr.ResolvedType = new PType(Root.FirstToken, null, "@CoreFunc");
                return cfr;
            }

            throw new NotImplementedException();
        }

        private CoreFunction GetCoreFunction(string field)
        {
            switch (field)
            {
                case "Abs": return CoreFunction.MATH_ABS;
                case "ArcCos": return CoreFunction.MATH_ARCCOS;
                case "ArcSin": return CoreFunction.MATH_ARCSIN;
                case "ArcTan": return CoreFunction.MATH_ARCTAN;
                case "Base64ToBytes": return CoreFunction.BASE64_TO_BYTES;
                case "Base64ToString": return CoreFunction.BASE64_TO_STRING;
                case "BoolToString": return CoreFunction.BOOL_TO_STRING;
                case "BytesToBase64": return CoreFunction.BYTES_TO_BASE64;
                case "CharToString": return CoreFunction.CHAR_TO_STRING;
                case "Ceil": return CoreFunction.MATH_CEIL;
                case "Chr": return CoreFunction.CHR;
                case "Cos": return CoreFunction.MATH_COS;
                case "CurrentTimeSeconds": return CoreFunction.CURRENT_TIME_SECONDS;
                case "EmitComment": return CoreFunction.EMIT_COMMENT;
                case "ExtensibleCallbackInvoke": return CoreFunction.EXTENSIBLE_CALLBACK_INVOKE;
                case "FloatToString": return CoreFunction.FLOAT_TO_STRING;
                case "Floor": return CoreFunction.MATH_FLOOR;
                case "IntToString": return CoreFunction.INT_TO_STRING;
                case "IsValidInteger": return CoreFunction.IS_VALID_INTEGER;
                case "ListConcat": return CoreFunction.LIST_CONCAT;
                case "ListToArray": return CoreFunction.LIST_TO_ARRAY;
                case "Log": return CoreFunction.MATH_LOG;
                case "MultiplyList": return CoreFunction.MULTIPLY_LIST;
                case "Ord": return CoreFunction.ORD;
                case "ParseFloatUnsafe": return CoreFunction.PARSE_FLOAT_UNSAFE;
                case "ParseInt": return CoreFunction.PARSE_INT;
                case "Pow": return CoreFunction.MATH_POW;
                case "PrintStdErr": return CoreFunction.PRINT_STDERR;
                case "PrintStdOut": return CoreFunction.PRINT_STDOUT;
                case "RandomFloat": return CoreFunction.RANDOM_FLOAT;
                case "Sin": return CoreFunction.MATH_SIN;
                case "StringAppend": return CoreFunction.STRING_APPEND;
                case "StringCompareIsReverse": return CoreFunction.STRING_COMPARE_IS_REVERSE;
                case "StringEquals": return CoreFunction.STRING_EQUALS;
                case "StringFromCharCode": return CoreFunction.STRING_FROM_CHAR_CODE;
                case "StrongReferenceEquality": return CoreFunction.STRONG_REFERENCE_EQUALITY;
                case "Tan": return CoreFunction.MATH_TAN;
                case "ToCodeString": return CoreFunction.TO_CODE_STRING;
                case "TryParseFloat": return CoreFunction.TRY_PARSE_FLOAT;
                case "Utf8BytesToString": return CoreFunction.UTF8_BYTES_TO_STRING;

                // TODO: get this information from the parameter rather than having separate Core function
                case "SortedCopyOfStringArray": return CoreFunction.SORTED_COPY_OF_STRING_ARRAY;
                case "SortedCopyOfIntArray": return CoreFunction.SORTED_COPY_OF_INT_ARRAY;

                default:
                    throw new ParserException(FirstToken, "Invalid Core function: 'Core." + field + "'.");
            }
        }

        private CoreFunction DetermineCoreFunctionId(PType rootType, string field)
        {
            switch (rootType.RootValue)
            {
                case "string":
                    switch (field)
                    {
                        case "CharCodeAt": return CoreFunction.STRING_CHAR_CODE_AT;
                        case "Contains": return CoreFunction.STRING_CONTAINS;
                        case "EndsWith": return CoreFunction.STRING_ENDS_WITH;
                        case "IndexOf": return CoreFunction.STRING_INDEX_OF;
                        case "LastIndexOf": return CoreFunction.STRING_LAST_INDEX_OF;
                        case "Length": throw new ParserException(FieldName, "String uses .Size() for its length.");
                        case "Replace": return CoreFunction.STRING_REPLACE;
                        case "Reverse": return CoreFunction.STRING_REVERSE;
                        case "Size": return CoreFunction.STRING_LENGTH;
                        case "Split": return CoreFunction.STRING_SPLIT;
                        case "StartsWith": return CoreFunction.STRING_STARTS_WITH;
                        case "SubString": return CoreFunction.STRING_SUBSTRING;
                        case "SubStringIsEqualTo": return CoreFunction.STRING_SUBSTRING_IS_EQUAL_TO;
                        case "ToLower": return CoreFunction.STRING_TO_LOWER;
                        case "ToUpper": return CoreFunction.STRING_TO_UPPER;
                        case "ToUtf8Bytes": return CoreFunction.STRING_TO_UTF8_BYTES;
                        case "Trim": return CoreFunction.STRING_TRIM;
                        case "TrimEnd": return CoreFunction.STRING_TRIM_END;
                        case "TrimStart": return CoreFunction.STRING_TRIM_START;
                        default: throw new ParserException(FieldName, "Unresolved string method: " + field);
                    }

                case "Array":
                    switch (field)
                    {
                        case "Join": return CoreFunction.ARRAY_JOIN;
                        case "Length": return CoreFunction.ARRAY_LENGTH;
                        // TODO: deprecate this
                        case "Size": return CoreFunction.ARRAY_LENGTH;
                        default: throw new ParserException(FieldName, "Unresolved Array method: " + field);
                    }

                case "List":
                    switch (field)
                    {
                        case "Add": return CoreFunction.LIST_ADD;
                        case "Clear": return CoreFunction.LIST_CLEAR;
                        case "Insert": return CoreFunction.LIST_INSERT;
                        case "Join":
                            string memberType = rootType.Generics[0].RootValue;
                            switch (memberType)
                            {
                                case "string": return CoreFunction.LIST_JOIN_STRINGS;
                                case "char": return CoreFunction.LIST_JOIN_CHARS;
                                default: throw new ParserException(FieldName, "Unresolved List<" + memberType + "> method: " + field);
                            }

                        case "Pop": return CoreFunction.LIST_POP;
                        case "RemoveAt": return CoreFunction.LIST_REMOVE_AT;
                        case "Reverse": return CoreFunction.LIST_REVERSE;
                        case "Shuffle": return CoreFunction.LIST_SHUFFLE;
                        case "Size": return CoreFunction.LIST_SIZE;
                        default: throw new ParserException(FieldName, "Unresolved List method: " + field);
                    }

                case "Dictionary":
                    switch (field)
                    {
                        case "Add": throw new ParserException(FieldName, "Use bracket notation instead of .Add() to add values to a dictionary.");
                        case "Contains": return CoreFunction.DICTIONARY_CONTAINS_KEY;
                        case "Keys": return CoreFunction.DICTIONARY_KEYS;
                        case "Remove": return CoreFunction.DICTIONARY_REMOVE;
                        case "Size": return CoreFunction.DICTIONARY_SIZE;
                        case "TryGet": return CoreFunction.DICTIONARY_TRY_GET;
                        case "Values": return CoreFunction.DICTIONARY_VALUES;
                        default: throw new ParserException(FieldName, "Unresolved Dictionary method: " + field);
                    }

                case "StringBuilder":
                    switch (field)
                    {
                        case "Add": return CoreFunction.STRINGBUILDER_ADD;
                        case "Clear": return CoreFunction.STRINGBUILDER_CLEAR;
                        case "ToString": return CoreFunction.STRINGBUILDER_TOSTRING;
                        default: throw new ParserException(FieldName, "Unresolved StringBuilder method: " + field);
                    }

                default:
                    throw new ParserException(FieldName, "Unresolved field.");
            }
        }

        internal override Expression ResolveWithTypeContext(Resolver resolver)
        {
            Root = Root.ResolveWithTypeContext(resolver);
            return this;
        }
    }
}
