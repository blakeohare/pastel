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
                case "string": return "string";
                case "object": return "any"; 

                case "Array":
                case "List":
                    return "[]" + TranslateType(type.Generics[0]);
            }

            if (type.IsStruct)
            {
                return "PtrBox_" + type.RootValue;
            }

            throw new NotImplementedException();
        }
    }
}
