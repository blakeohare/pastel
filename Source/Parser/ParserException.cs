namespace Pastel.Parser
{
    internal class ParserException : UserErrorException
    {
        internal ParserException(Token token, string message)
            : base(InterpretToken(token) + message)
        { }

        private static string InterpretToken(Token token)
        {
            if (token == null) return "";
            return token.FileName + ", Line: " + (token.Line + 1) + ", Col: " + (token.Col + 1) + ", ";
        }
    }
}
