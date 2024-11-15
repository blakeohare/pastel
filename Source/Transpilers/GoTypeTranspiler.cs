using Pastel.Parser.ParseNodes;
using System;

namespace Pastel.Transpilers
{
    internal class GoTypeTranspiler : AbstractTypeTranspiler
    {
        public override string TranslateType(PType type)
        {
            switch (type.RootValue)
            {
                case "int": return "int";
                case "double": return "float64";
                case "Array": return "[]" + this.TranslateType(type.Generics[0]);
            }

            if (type.IsStruct)
            {
                return "PtrBox_" + type.RootValue;
            }

            throw new NotImplementedException();
        }
    }
}
