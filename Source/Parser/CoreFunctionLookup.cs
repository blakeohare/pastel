using Pastel.Parser.ParseNodes;
using System.Collections.Generic;
using System.Xml;

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

            Dictionary<string, CoreFunction> _Base64 = [];
            Dictionary<string, CoreFunction> _Collections = [];
            Dictionary<string, CoreFunction> _Convert = [];
            Dictionary<string, CoreFunction> _DateTime = [];
            Dictionary<string, CoreFunction> _Deprecated = [];
            Dictionary<string, CoreFunction> _Json = [];
            Dictionary<string, CoreFunction> _Math = [];
            Dictionary<string, CoreFunction> _Pastel = [];
            Dictionary<string, CoreFunction> _Random = [];
            Dictionary<string, CoreFunction> _Sorting = [];

            Dictionary<string, CoreFunction> _string = [];
            Dictionary<string, CoreFunction> _stringBuilder = [];
            Dictionary<string, CoreFunction> _array = [];
            Dictionary<string, CoreFunction> _list = [];
            Dictionary<string, CoreFunction> _dictionary = [];

            coreLookup["Base64"] = _Base64;
            coreLookup["Collections"] = _Collections;
            coreLookup["Convert"] = _Convert;
            coreLookup["DateTime"] = _DateTime;
            coreLookup["Deprecated"] = _Deprecated;
            coreLookup["Pastel"] = _Pastel;
            coreLookup["Json"] = _Json;
            coreLookup["Math"] = _Math;
            coreLookup["Random"] = _Random;
            coreLookup["Sorting"] = _Sorting;

            methodLookup["string"] = _string;
            methodLookup["StringBuilder"] = _stringBuilder;
            methodLookup["Array"] = _array;
            methodLookup["List"] = _list;
            methodLookup["Dictionary"] = _dictionary;

            _Base64["toBytes"] = CoreFunction.BASE64_TO_BYTES;
            _Base64["toStringUtf8"] = CoreFunction.BASE64_TO_STRING;
            _Base64["fromBytes"] = CoreFunction.BYTES_TO_BASE64;

            _Collections["concatenateLists"] = CoreFunction.LIST_CONCAT;
            _Collections["multiplyList"] = CoreFunction.MULTIPLY_LIST;

            _Convert["boolToString"] = CoreFunction.BOOL_TO_STRING;
            _Convert["charCodeToChar"] = CoreFunction.CHR;
            _Convert["charCodeToString"] = CoreFunction.STRING_FROM_CHAR_CODE;
            _Convert["charToCharCode"] = CoreFunction.ORD;
            _Convert["charToString"] = CoreFunction.CHAR_TO_STRING;
            _Convert["floatToString"] = CoreFunction.FLOAT_TO_STRING;
            _Convert["intToString"] = CoreFunction.INT_TO_STRING;
            _Convert["isValidInteger"] = CoreFunction.IS_VALID_INTEGER;
            _Convert["parseFloatUnsafe"] = CoreFunction.PARSE_FLOAT_UNSAFE;
            _Convert["parseInt"] = CoreFunction.PARSE_INT;
            _Convert["tryParseFloat"] = CoreFunction.TRY_PARSE_FLOAT;
            _Convert["utf8BytesToString"] = CoreFunction.UTF8_BYTES_TO_STRING;

            _DateTime["currentTimeFloat"] = CoreFunction.CURRENT_TIME_SECONDS;

            _Json["serializeString"] = CoreFunction.TO_CODE_STRING;

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

            _Pastel["emitComment"] = CoreFunction.EMIT_COMMENT;
            _Pastel["invokeExtension"] = CoreFunction.EXTENSIBLE_CALLBACK_INVOKE;
            _Pastel["printStdErr"] = CoreFunction.PRINT_STDERR;
            _Pastel["printStdOut"] = CoreFunction.PRINT_STDOUT;

            _Random["nextFloat"] = CoreFunction.RANDOM_FLOAT;

            _Sorting["getIntegerSortedCopy"] = CoreFunction.SORTED_COPY_OF_INT_ARRAY;
            _Sorting["getLexicalSortedCopy"] = CoreFunction.SORTED_COPY_OF_STRING_ARRAY;

            _string["charCodeAt"] = CoreFunction.STRING_CHAR_CODE_AT;
            _string["contains"] = CoreFunction.STRING_CONTAINS;
            _string["endsWith"] = CoreFunction.STRING_ENDS_WITH;
            _string["indexOf"] = CoreFunction.STRING_INDEX_OF;
            _string["lastIndexOf"] = CoreFunction.STRING_LAST_INDEX_OF;
            _string["replace"] = CoreFunction.STRING_REPLACE;
            _string["reverse"] = CoreFunction.STRING_REVERSE;
            _string["size"] = CoreFunction.STRING_LENGTH;
            _string["split"] = CoreFunction.STRING_SPLIT;
            _string["startsWith"] = CoreFunction.STRING_STARTS_WITH;
            _string["subString"] = CoreFunction.STRING_SUBSTRING;
            _string["subStringIsEqualTo"] = CoreFunction.STRING_SUBSTRING_IS_EQUAL_TO;
            _string["toLower"] = CoreFunction.STRING_TO_LOWER;
            _string["toUpper"] = CoreFunction.STRING_TO_UPPER;
            _string["toUtf8Bytes"] = CoreFunction.STRING_TO_UTF8_BYTES;
            _string["trim"] = CoreFunction.STRING_TRIM;
            _string["trimEnd"] = CoreFunction.STRING_TRIM_END;
            _string["trimStart"] = CoreFunction.STRING_TRIM_START;

            _array["join"] = CoreFunction.ARRAY_JOIN;
            _array["size"] = CoreFunction.ARRAY_LENGTH;

            _list["add"] = CoreFunction.LIST_ADD;
            _list["clear"] = CoreFunction.LIST_CLEAR;
            _list["insert"] = CoreFunction.LIST_INSERT;
            _list["join"] = CoreFunction.LIST_JOIN_STRINGS;
            _list["pop"] = CoreFunction.LIST_POP;
            _list["removeAt"] = CoreFunction.LIST_REMOVE_AT;
            _list["reverse"] = CoreFunction.LIST_REVERSE;
            _list["shuffle"] = CoreFunction.LIST_SHUFFLE;
            _list["size"] = CoreFunction.LIST_SIZE;
            _list["toArray"] = CoreFunction.LIST_TO_ARRAY;

            _dictionary["contains"] = CoreFunction.DICTIONARY_CONTAINS_KEY;
            _dictionary["keys"] = CoreFunction.DICTIONARY_KEYS;
            _dictionary["remove"] = CoreFunction.DICTIONARY_REMOVE;
            _dictionary["size"] = CoreFunction.DICTIONARY_SIZE;
            _dictionary["tryGet"] = CoreFunction.DICTIONARY_TRY_GET;
            _dictionary["values"] = CoreFunction.DICTIONARY_VALUES;

            _stringBuilder["add"] = CoreFunction.STRINGBUILDER_ADD;
            _stringBuilder["clear"] = CoreFunction.STRINGBUILDER_CLEAR;
            _stringBuilder["toString"] = CoreFunction.STRINGBUILDER_TOSTRING;

            // This assumes the left input is a variable and you can apply += on it
            _Deprecated["stringAppend"] = CoreFunction.STRING_APPEND;
            // Replace this in favor of the traditional `str1.compare(str2) -> -1, 0, 1`
            _Deprecated["stringCompareIsReverse"] = CoreFunction.STRING_COMPARE_IS_REVERSE;
            // TODO: Use == and let the static type system deal with such nuances.
            _Deprecated["stringEquals"] = CoreFunction.STRING_EQUALS;
            // TODO: verify this is what == does.
            _Deprecated["strongReferenceEquality"] = CoreFunction.STRONG_REFERENCE_EQUALITY;
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
