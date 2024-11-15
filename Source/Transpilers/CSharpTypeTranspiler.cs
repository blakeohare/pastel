using Pastel.Parser.ParseNodes;
using System;

namespace Pastel.Transpilers
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
                    return "System.Collections.Generic.List<" + this.TranslateType(type.Generics[0]) + ">";

                case "Dictionary":
                    return "System.Collections.Generic.Dictionary<" + this.TranslateType(type.Generics[0]) + ", " + this.TranslateType(type.Generics[1]) + ">";

                case "Array":
                    return this.TranslateType(type.Generics[0]) + "[]";

                case "Func":
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append("System.Func<");
                    for (int i = 0; i < type.Generics.Length - 1; ++i)
                    {
                        sb.Append(this.TranslateType(type.Generics[i + 1]));
                        sb.Append(", ");
                    }
                    sb.Append(this.TranslateType(type.Generics[0]));
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
