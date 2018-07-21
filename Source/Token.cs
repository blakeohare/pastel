using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pastel
{
    public enum TokenType
    {
        EOF,
        ALPHANUMS,
        NUMBER,
        PUNCTUATION,
        STRING
    }

    public class Token
    {
        public string FileName { get; private set; }
        public string Value { get; set; }
        public int Line { get; private set; }
        public int Column { get; private set; }
        public TokenType Type { get; set; }
        public bool IsNextWhitespace { get; private set; }
        public int Index { get; private set; }
        
        public Token(string filename, string value, int startIndex, int line, int column, TokenType type)
        {
            this.FileName = filename;
            this.Value = value;
            this.Line = line;
            this.Column = column;
            this.Type = type;
            this.Index = startIndex;
        }

        public static Token CreateDummyToken(string filename, string value, TokenType type)
        {
            char c = value[0];
            bool isAlpha = (c >= 'a' && c <= 'Z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
            return new Token(filename, value, 0, 1, 1, type);
        }

        public override string ToString()
        {
            if (this.Type == TokenType.EOF) return "EOF Token";
            return "Token: " + this.Value;
        }
    }
}
