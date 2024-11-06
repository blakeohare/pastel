namespace Pastel.Parser
{
    public enum TokenType
    {
        WORD,
        PUNCTUATION,
        STRING,
        INTEGER,
        FLOAT,
    }

    public class Token
    {
        public string Value { get; set; }
        public int Line { get; private set; }
        public int Col { get; private set; }
        public string FileName { get; private set; }
        public TokenType Type { get; set; }

        public Token(string value, string filename, int lineIndex, int colIndex, TokenType type)
        {
            this.Value = value;
            this.FileName = filename;
            this.Line = lineIndex;
            this.Col = colIndex;
            this.Type = type;
        }

        public static Token CreateDummyToken(string value)
        {
            return new Token(value, "", 0, 0, TokenType.PUNCTUATION);
        }

        public override string ToString()
        {
            return "Token: '" + Value + "'";
        }
    }
}
