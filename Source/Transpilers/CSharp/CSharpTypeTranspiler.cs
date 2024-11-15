using Pastel.Parser.ParseNodes;
using System;

namespace Pastel.Transpilers.CSharp
{
    internal class CSharpTypeTranspiler : AbstractTypeTranspiler
    {
        public override string TranslateType(PType type)
        {
            switch (type.RootValue)
            {
                case "int":
                case "char":
                case "bool":
                case "double":
                case "string":
                case "object":
                case "void":
                    return type.RootValue;

                case "StringBuilder":
                    return "System.Text.StringBuilder";

                case "List":
                    return "System.Collections.Generic.List<" + TranslateType(type.Generics[0]) + ">";

                case "Dictionary":
                    return "System.Collections.Generic.Dictionary<" + TranslateType(type.Generics[0]) + ", " + TranslateType(type.Generics[1]) + ">";

                case "Array":
                    return TranslateType(type.Generics[0]) + "[]";

                case "Func":
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append("System.Func<");
                    for (int i = 0; i < type.Generics.Length - 1; ++i)
                    {
                        sb.Append(TranslateType(type.Generics[i + 1]));
                        sb.Append(", ");
                    }
                    sb.Append(TranslateType(type.Generics[0]));
                    sb.Append('>');
                    return sb.ToString();

                default:
                    if (type.Generics.Length > 0)
                    {
                        throw new NotImplementedException();
                    }
                    return type.TypeName;
            }
        }
    }
}
