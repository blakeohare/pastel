using System;
using System.Collections.Generic;
using System.Linq;
using Pastel.Parser;
using Pastel.Parser.ParseNodes;

namespace Pastel
{
    internal static class CoreFunctionUtil
    {
        private static Dictionary<CoreFunction, PType> returnTypes;
        private static Dictionary<CoreFunction, PType[]> argTypes;
        private static Dictionary<CoreFunction, bool[]> argTypesRepeated;

        public static PType[] GetCoreFunctionArgTypes(CoreFunction functionId)
        {
            if (CoreFunctionUtil.returnTypes == null)
            {
                CoreFunctionUtil.Init();
            }
            return CoreFunctionUtil.argTypes[functionId].ToArray();
        }

        public static PType GetCoreFunctionReturnType(CoreFunction functionId)
        {
            if (CoreFunctionUtil.returnTypes == null)
            {
                CoreFunctionUtil.Init();
            }

            return CoreFunctionUtil.returnTypes[functionId];
        }

        public static bool PerformAdditionalTypeResolution(CoreFunctionReference funcRef, Expression[] args)
        {
            PType firstType = args.Length > 0 ? args[0].ResolvedType : PType.VOID;
            switch (funcRef.CoreFunctionId)
            {
                case CoreFunction.MATH_ABS:
                    if (!firstType.IsInteger && !firstType.IsFloat)
                    {
                        throw new TestedParserException(
                            funcRef.FirstToken,
                            "Math.abs() is only applicable to numeric types.");
                    }

                    funcRef.ReturnType = firstType;
                    funcRef.ArgTypes = [firstType];
                    return true;
            }

            return false;
        }

        public static bool[] GetCoreFunctionIsArgTypeRepeated(CoreFunction functionId)
        {
            if (CoreFunctionUtil.returnTypes == null)
            {
                CoreFunctionUtil.Init();
            }

            return CoreFunctionUtil.argTypesRepeated[functionId];
        }

        private static void Init()
        {
            Dictionary<string, CoreFunction> lookup = new Dictionary<string, CoreFunction>();
            foreach (CoreFunction func in typeof(CoreFunction).GetEnumValues().Cast<CoreFunction>())
            {
                lookup[func.ToString()] = func;
            }

            CoreFunctionUtil.returnTypes = new Dictionary<CoreFunction, PType>();
            CoreFunctionUtil.argTypes = new Dictionary<CoreFunction, PType[]>();
            CoreFunctionUtil.argTypesRepeated = new Dictionary<CoreFunction, bool[]>();

            string[] rows = GetCoreFunctionSignatureManifest().Split('\n');
            foreach (string row in rows)
            {
                string definition = row.Trim();
                if (definition.Length > 0)
                {
                    TokenStream tokens = new TokenStream(Tokenizer.Tokenize("core function manifest", row));
                    PType returnType = PType.Parse(tokens);
                    string name = tokens.Pop().Value;
                    tokens.PopExpected("(");
                    List<PType> argList = new List<PType>();
                    List<bool> argRepeated = new List<bool>();
                    while (!tokens.PopIfPresent(")"))
                    {
                        if (argList.Count > 0) tokens.PopExpected(",");
                        argList.Add(PType.Parse(tokens));
                        if (tokens.PopIfPresent("."))
                        {
                            argRepeated.Add(true);
                            tokens.PopExpected(".");
                            tokens.PopExpected(".");
                        }
                        else
                        {
                            argRepeated.Add(false);
                        }
                    }

                    if (tokens.HasMore)
                    {
                        throw new Exception("Invalid entry in the manifest. Stuff at the end: " + row);
                    }

                    CoreFunction func = lookup[name];
                    CoreFunctionUtil.returnTypes[func] = returnType;
                    CoreFunctionUtil.argTypes[func] = argList.ToArray();
                    CoreFunctionUtil.argTypesRepeated[func] = argRepeated.ToArray();
                }
            }
        }

        private static string GetCoreFunctionSignatureManifest()
        {
            return ResourceReader.ReadTextFile("CoreFunctionSignatures.txt");
        }
    }
}
