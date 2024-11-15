using Pastel.Parser.ParseNodes;

namespace Pastel.Transpilers
{
    internal abstract class AbstractTypeTranspiler
    {
        public abstract string TranslateType(PType type);
    }
}
