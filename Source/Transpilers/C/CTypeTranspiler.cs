using Pastel.Parser.ParseNodes;
using System;

namespace Pastel.Transpilers.C
{
    internal class CTypeTranspiler : AbstractTypeTranspiler
    {
        public override string TranslateType(PType type)
        {
            switch (type.RootValue)
            {
                case "int":
                case "char":
                case "double":
                case "void":
                    return type.RootValue;

                case "bool":
                    return "int";

                case "string":
                    return "PString";

                case "object":
                    return "void*";

                case "StringBuilder":
                    return "PStringBuilder";

                case "Array":
                case "List":
                    switch (type.Generics[0].RootValue)
                    {
                        case "int":
                        case "bool":
                            return "PIntList*";
                        case "double":
                            return "PFloatList*";
                        default:
                            return "PPtrList*";
                    }

                case "Dictionary":
                    string keyType = type.Generics[0].RootValue;
                    string valType = type.Generics[1].RootValue;
                    throw new NotImplementedException();

                case "Func":
                    throw new NotImplementedException();

                default:
                    if (type.Generics.Length > 0)
                    {
                        throw new NotImplementedException();
                    }
                    return type.TypeName + "*";
            }
        }
    }
}
