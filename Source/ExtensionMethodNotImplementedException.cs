using Pastel.Parser;

namespace Pastel
{
    internal class ExtensionMethodNotImplementedException : ParserException
    {
        internal ExtensionMethodNotImplementedException(Token throwToken, string message)
            : base(throwToken, message)
        { }
    }
}
