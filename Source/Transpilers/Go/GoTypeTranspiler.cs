using Pastel.Parser.ParseNodes;
using System;

namespace Pastel.Transpilers.Go
{
    internal class GoTypeTranspiler : AbstractTypeTranspiler
    {
        public override string TranslateType(PType type)
        {
            switch (type.RootValue)
            {
                case "bool": return "bool";
                case "int": return "int";
                case "double": return "float64";
                case "char": return "int";
                case "string": return "*pstring";
                case "object": return "any"; 

                case "Array":
                case "List":
                    return "*plist";
            }

            if (type.IsStruct)
            {
                return "PtrBox_" + type.RootValue;
            }

            throw new NotImplementedException();
        }
    }
}
