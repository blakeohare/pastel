using System;
using System.Collections.Generic;
using System.Linq;
using Pastel.ParseNodes;

namespace Pastel
{
    internal static class NativeFunctionUtil
    {
        private static Dictionary<NativeFunction, PType> returnTypes;
        private static Dictionary<NativeFunction, PType[]> argTypes;
        private static Dictionary<NativeFunction, bool[]> argTypesRepeated;
        private static Dictionary<string, NativeFunction> nativeFunctionsByName;

        public static NativeFunction? GetNativeFunctionFromName(string name)
        {
            if (name[0] == '$')
            {
                name = name.Substring(1);
            }
            if (nativeFunctionsByName == null) NativeFunctionUtil.Init();
            if (nativeFunctionsByName.ContainsKey(name)) return nativeFunctionsByName[name];
            return null;
        }

        public static PType[] GetNativeFunctionArgTypes(NativeFunction functionId)
        {
            if (returnTypes == null)
            {
                NativeFunctionUtil.Init();
            }
            return argTypes[functionId].ToArray();
        }

        public static PType GetNativeFunctionReturnType(NativeFunction functionId)
        {
            if (returnTypes == null)
            {
                NativeFunctionUtil.Init();
            }

            return returnTypes[functionId];
        }

        public static bool[] GetNativeFunctionIsArgTypeRepeated(NativeFunction functionId)
        {
            if (returnTypes == null)
            {
                NativeFunctionUtil.Init();
            }

            return argTypesRepeated[functionId];
        }

        private static void Init()
        {
            nativeFunctionsByName = new Dictionary<string, NativeFunction>();
            foreach (NativeFunction func in typeof(NativeFunction).GetEnumValues().Cast<NativeFunction>())
            {
                nativeFunctionsByName[func.ToString().ToLower()] = func;
            }

            returnTypes = new Dictionary<NativeFunction, PType>();
            argTypes = new Dictionary<NativeFunction, PType[]>();
            argTypesRepeated = new Dictionary<NativeFunction, bool[]>();

            string[] rows = GetNativeFunctionSignatureManifest().Split('\n');
            foreach (string row in rows)
            {
                string definition = row.Trim();
                if (definition.Length > 0)
                {
                    TokenStream tokens = new Tokenizer("native function manifest", row).Tokenize();
                    PType returnType = TypeParser.Parse(tokens);
                    string name = tokens.Pop().Value;
                    tokens.PopExpected("(");
                    List<PType> argList = new List<PType>();
                    List<bool> argRepeated = new List<bool>();
                    while (!tokens.PopIfPresent(")"))
                    {
                        if (argList.Count > 0) tokens.PopExpected(",");
                        argList.Add(TypeParser.Parse(tokens));
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

                    NativeFunction func = nativeFunctionsByName[name.ToLower()];
                    returnTypes[func] = returnType;
                    argTypes[func] = argList.ToArray();
                    argTypesRepeated[func] = argRepeated.ToArray();
                }
            }
        }

        private static string GetNativeFunctionSignatureManifest()
        {
            return Util.ReadAssemblyFileText(typeof(NativeFunctionUtil).Assembly, "NativeFunctionSignatures.txt");
        }
    }
}
