using Pastel.Parser.ParseNodes;
using System.Collections.Generic;

namespace Pastel.Parser
{
    public class CoreFunctionLookup
    {
        internal static bool IsAnyRootNamespace(string name)
        {
            CoreFunctionLookup.EnsureInitialized();
            return coreLookup.ContainsKey(name);
        }

        private static readonly Dictionary<string, Dictionary<string, CoreFunction>> coreLookup = [];
        private static readonly Dictionary<string, Dictionary<string, CoreFunction>> methodLookup = [];

        private static void EnsureInitialized()
        {
            if (coreLookup.Count > 0) return;

            Dictionary<string, CoreFunction> _Math = [];
            Dictionary<string, CoreFunction> _Core = [];
            Dictionary<string, CoreFunction> _string = [];
            Dictionary<string, CoreFunction> _stringBuilder = [];
            Dictionary<string, CoreFunction> _array = [];
            Dictionary<string, CoreFunction> _list = [];
            Dictionary<string, CoreFunction> _dictionary = [];
            coreLookup["Math"] = _Math;
            coreLookup["Core"] = _Core;
            methodLookup["string"] = _string;
            methodLookup["stringBuilder"] = _stringBuilder;
            methodLookup["Array"] = _array;
            methodLookup["List"] = _list;
            methodLookup["Dictionary"] = _dictionary;

            _Math["abs"] = CoreFunction.MATH_ABS;
            _Math["arcTan"] = CoreFunction.MATH_ARCTAN;
            _Math["arcCos"] = CoreFunction.MATH_ARCCOS;
            _Math["arcSin"] = CoreFunction.MATH_ARCSIN;
            _Math["tan"] = CoreFunction.MATH_TAN;
            _Math["cos"] = CoreFunction.MATH_COS;
            _Math["sin"] = CoreFunction.MATH_SIN;
            _Math["log"] = CoreFunction.MATH_LOG;
            _Math["ceil"] = CoreFunction.MATH_CEIL;
            _Math["floor"] = CoreFunction.MATH_FLOOR;
            _Math["pow"] = CoreFunction.MATH_POW;

            // Copy Math namespace to Core using old casing for compatibility
            foreach (string key in _Math.Keys)
            {
                string upperCaseKey = key.Substring(0, 1).ToUpper() + key.Substring(1);
                _Core[upperCaseKey] = _Math[key];
            }

            _Core["Base64ToBytes"] = CoreFunction.BASE64_TO_BYTES;
            _Core["Base64ToString"] = CoreFunction.BASE64_TO_STRING;
            _Core["BoolToString"] = CoreFunction.BOOL_TO_STRING;
            _Core["BytesToBase64"] = CoreFunction.BYTES_TO_BASE64;
            _Core["CharToString"] = CoreFunction.CHAR_TO_STRING;
            _Core["Chr"] = CoreFunction.CHR;
            _Core["CurrentTimeSeconds"] = CoreFunction.CURRENT_TIME_SECONDS;
            _Core["EmitComment"] = CoreFunction.EMIT_COMMENT;
            _Core["ExtensibleCallbackInvoke"] = CoreFunction.EXTENSIBLE_CALLBACK_INVOKE;
            _Core["FloatToString"] = CoreFunction.FLOAT_TO_STRING;
            _Core["IntToString"] = CoreFunction.INT_TO_STRING;
            _Core["IsValidInteger"] = CoreFunction.IS_VALID_INTEGER;
            _Core["ListConcat"] = CoreFunction.LIST_CONCAT;
            _Core["ListToArray"] = CoreFunction.LIST_TO_ARRAY;
            _Core["MultiplyList"] = CoreFunction.MULTIPLY_LIST;
            _Core["Ord"] = CoreFunction.ORD;
            _Core["ParseFloatUnsafe"] = CoreFunction.PARSE_FLOAT_UNSAFE;
            _Core["ParseInt"] = CoreFunction.PARSE_INT;
            _Core["PrintStdErr"] = CoreFunction.PRINT_STDERR;
            _Core["PrintStdOut"] = CoreFunction.PRINT_STDOUT;
            _Core["RandomFloat"] = CoreFunction.RANDOM_FLOAT;
            _Core["StringAppend"] = CoreFunction.STRING_APPEND;
            _Core["StringCompareIsReverse"] = CoreFunction.STRING_COMPARE_IS_REVERSE;
            _Core["StringEquals"] = CoreFunction.STRING_EQUALS;
            _Core["StringFromCharCode"] = CoreFunction.STRING_FROM_CHAR_CODE;
            _Core["StrongReferenceEquality"] = CoreFunction.STRONG_REFERENCE_EQUALITY;
            _Core["ToCodeString"] = CoreFunction.TO_CODE_STRING;
            _Core["TryParseFloat"] = CoreFunction.TRY_PARSE_FLOAT;
            _Core["Utf8BytesToString"] = CoreFunction.UTF8_BYTES_TO_STRING;

            // TODO: rename these to lexical and numeric sort
            _Core["SortedCopyOfStringArray"] = CoreFunction.SORTED_COPY_OF_STRING_ARRAY;
            _Core["SortedCopyOfIntArray"] = CoreFunction.SORTED_COPY_OF_INT_ARRAY;

            _string["CharCodeAt"] = CoreFunction.STRING_CHAR_CODE_AT;
            _string["Contains"] = CoreFunction.STRING_CONTAINS;
            _string["EndsWith"] = CoreFunction.STRING_ENDS_WITH;
            _string["IndexOf"] = CoreFunction.STRING_INDEX_OF;
            _string["LastIndexOf"] = CoreFunction.STRING_LAST_INDEX_OF;
            _string["Replace"] = CoreFunction.STRING_REPLACE;
            _string["Reverse"] = CoreFunction.STRING_REVERSE;
            _string["Size"] = CoreFunction.STRING_LENGTH;
            _string["Split"] = CoreFunction.STRING_SPLIT;
            _string["StartsWith"] = CoreFunction.STRING_STARTS_WITH;
            _string["SubString"] = CoreFunction.STRING_SUBSTRING;
            _string["SubStringIsEqualTo"] = CoreFunction.STRING_SUBSTRING_IS_EQUAL_TO;
            _string["ToLower"] = CoreFunction.STRING_TO_LOWER;
            _string["ToUpper"] = CoreFunction.STRING_TO_UPPER;
            _string["ToUtf8Bytes"] = CoreFunction.STRING_TO_UTF8_BYTES;
            _string["Trim"] = CoreFunction.STRING_TRIM;
            _string["TrimEnd"] = CoreFunction.STRING_TRIM_END;
            _string["TrimStart"] = CoreFunction.STRING_TRIM_START;

            _array["Join"] = CoreFunction.ARRAY_JOIN;
            _array["Size"] = CoreFunction.ARRAY_LENGTH;

            _list["Add"] = CoreFunction.LIST_ADD;
            _list["Clear"] = CoreFunction.LIST_CLEAR;
            _list["Insert"] = CoreFunction.LIST_INSERT;
            _list["Join"] = CoreFunction.LIST_JOIN_STRINGS;
            _list["Pop"] = CoreFunction.LIST_POP;
            _list["RemoveAt"] = CoreFunction.LIST_REMOVE_AT;
            _list["Reverse"] = CoreFunction.LIST_REVERSE;
            _list["Shuffle"] = CoreFunction.LIST_SHUFFLE;
            _list["Size"] = CoreFunction.LIST_SIZE;


            _dictionary["Contains"] = CoreFunction.DICTIONARY_CONTAINS_KEY;
            _dictionary["Keys"] = CoreFunction.DICTIONARY_KEYS;
            _dictionary["Remove"] = CoreFunction.DICTIONARY_REMOVE;
            _dictionary["Size"] = CoreFunction.DICTIONARY_SIZE;
            _dictionary["TryGet"] = CoreFunction.DICTIONARY_TRY_GET;
            _dictionary["Values"] = CoreFunction.DICTIONARY_VALUES;

            _stringBuilder["Add"] = CoreFunction.STRINGBUILDER_ADD;
            _stringBuilder["Clear"] = CoreFunction.STRINGBUILDER_CLEAR;
            _stringBuilder["ToString"] = CoreFunction.STRINGBUILDER_TOSTRING;
        }

        internal static CoreFunction GetCoreFunction(string rootNs, string field)
        {
            CoreFunctionLookup.EnsureInitialized();
            if (coreLookup.ContainsKey(rootNs) && coreLookup[rootNs].ContainsKey(field))
            {
                return coreLookup[rootNs][field];
            }

            return CoreFunction.NONE;
        }

        internal static CoreFunction DetermineCoreFunctionId(PType rootType, string field)
        {
            string typeName = rootType.RootValue;
            if (methodLookup.ContainsKey(typeName) && methodLookup[typeName].ContainsKey(field))
            {
                return methodLookup[typeName][field];
            }

            return CoreFunction.NONE;
        }
    }
}