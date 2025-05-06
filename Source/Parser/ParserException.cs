namespace Pastel.Parser
{
    internal abstract class ParserException : UserErrorException
    {
        internal ParserException(Token token, string message)
            : base(InterpretToken(token) + message)
        { }

        private static string InterpretToken(Token token)
        {
            if (token == null) return "";
            return token.FileName + ", Line: " + token.Line + ", Col: " + token.Col + ", ";
        }
    }

    internal class UNTESTED_ParserException : ParserException
    {
        internal UNTESTED_ParserException(Token token, string msg)
            : base(token, msg)
        { }
    }
    
    internal class TestedParserException : ParserException
    {
        internal TestedParserException(Token token, string msg)
            : base(token, msg)
        { }
    }
}
