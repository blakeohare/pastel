using System.Collections.Generic;
using System.Linq;

namespace Pastel
{
    internal class TokenStreamState
    {
        internal int Index { get; set; }
    }

    class TokenStream
    {
        private AggregatingTokenStream aggregatingTokenStream;

        public TokenStream(string filename, string originalContents, IList<Token> tokens)
        {
            this.aggregatingTokenStream = new AggregatingTokenStream(filename, originalContents, tokens);
        }

        private class AggregatingTokenStream
        {
            private static Dictionary<char, string[]> multiCharTokens = new Dictionary<char, string[]>();

            static AggregatingTokenStream()
            {
                Dictionary<char, List<string>> lookupBuilder = new Dictionary<char, List<string>>();
                foreach (string token in new string[]
                    {
                        "==", "!=", ">=", "<=",
                        "&&", "||",
                        "+=", "-=", "*=", "/=", ">>=", "<<=",
                        "<<", ">>", ">>>",

                        "++", "--", // not supported by Pastel, but should never appear. Recognize them in the tokenizer only for the purpose of clear error messages.
                    })
                {
                    char c = token[0];
                    if (!lookupBuilder.ContainsKey(c))
                    {
                        lookupBuilder[c] = new List<string>();
                    }
                    lookupBuilder[c].Add(token);
                }

                AggregatingTokenStream.multiCharTokens = new Dictionary<char, string[]>();
                foreach (char c in lookupBuilder.Keys)
                {
                    // Sort by length (largest first) so that tokens like >> don't get detected before >>=
                    AggregatingTokenStream.multiCharTokens[c] = lookupBuilder[c].OrderBy(v => -v.Length).ToArray();
                }
            }

            public string Filename { get; private set; }
            private string originalContents;
            private Token[] tokens;
            private int length;
            public int Index { get; set; } = 0;
            private bool enableMultiCharTokens = true;
            private Token cachedToken = null;

            public AggregatingTokenStream(string filename, string contents, IList<Token> tokens)
            {
                this.tokens = tokens.ToArray();
                this.length = this.tokens.Length;
                this.Filename = filename;
                this.originalContents = contents;
            }

            internal void ToggleMultiChar(bool value)
            {
                this.enableMultiCharTokens = value;
                this.cachedToken = null;
            }

            internal void InvalidateCache()
            {
                this.cachedToken = null;
            }

            internal Token Peek()
            {
                if (this.cachedToken != null) return this.cachedToken;
                
                Token nextToken = this.tokens[this.Index];
                string nextValue = nextToken.Value;
                if (this.enableMultiCharTokens)
                {
                    if (nextToken.Type == TokenType.PUNCTUATION &&
                        AggregatingTokenStream.multiCharTokens.ContainsKey(nextValue[0]))
                    {
                        foreach (string mcharToken in AggregatingTokenStream.multiCharTokens[nextValue[0]])
                        {
                            if (this.originalContents.Substring(nextToken.Index, mcharToken.Length) == mcharToken)
                            {
                                this.cachedToken = new Token(this.Filename, mcharToken, nextToken.Index, nextToken.Line, nextToken.Column, nextToken.Type);
                                return this.cachedToken;
                            }
                        }
                    }
                }

                this.cachedToken = this.tokens[this.Index];

                return this.cachedToken;
            }

            internal Token Pop()
            {
                Token token = this.Peek();
                if (token.Type == TokenType.PUNCTUATION && token.Value.Length > 1)
                {
                    if (token.Type == TokenType.EOF) throw new ParserException(token, "EOF reached.");
                    this.Index += token.Value.Length;
                }
                else
                {
                    this.Index++;
                }
                this.cachedToken = null;
                return token;
            }
        }

        public bool HasMore {  get { return this.aggregatingTokenStream.Peek().Type != TokenType.EOF; } }

        public static Token CreateDummyToken(Token nearbyToken, string value)
        {
            char c = value[0];
            TokenType type = TokenType.PUNCTUATION;
            if (c == '"' || c == '\'')
            {
                type = TokenType.STRING;
            }
            else if (c >= '0' && c <= '9')
            {
                type = TokenType.NUMBER;
            }
            else if (
              (c >= 'a' && c <= 'z') ||
              (c >= 'A' && c <= 'Z') ||
              (c >= '0' && c <= '9') ||
              c == '_')
            {
                type = TokenType.ALPHANUMS;
            }

            return new Token(nearbyToken.FileName, value, nearbyToken.Index, nearbyToken.Line, nearbyToken.Column, type);
        }

        public Token CreateDummyToken(string value)
        {
            return TokenStream.CreateDummyToken(this.Peek(), value);
        }

        public void EnableMultiCharTokens() { this.aggregatingTokenStream.ToggleMultiChar(true); }
        public void DisableMultiCharTokens() { this.aggregatingTokenStream.ToggleMultiChar(false); }

        public Token Pop() { return this.aggregatingTokenStream.Pop(); }
        public Token Peek() { return this.aggregatingTokenStream.Peek(); }
        public TokenStreamState SnapshotState() { return new TokenStreamState() { Index = this.aggregatingTokenStream.Index }; }
        public void RestoreState(TokenStreamState state) { this.aggregatingTokenStream.Index = state.Index; this.aggregatingTokenStream.InvalidateCache(); }

        public Token PeekValid()
        {
            Token token = this.aggregatingTokenStream.Peek();
            if (token.Type == TokenType.EOF)
            {
                throw new ParserException(token, "Unexpected EOF");
            }
            return token;
        }

        public bool IsNext(string value)
        {
            Token token = this.aggregatingTokenStream.Peek();
            return token.Value == value;
        }

        public string PeekValue()
        {
            return this.aggregatingTokenStream.Peek().Value;
        }

        public bool PopIfPresent(string value)
        {
            Token token = this.aggregatingTokenStream.Peek();
            if (token.Value == value)
            {
                this.aggregatingTokenStream.Pop();
                return true;
            }
            return false;
        }

        public Token PopExpected(string value)
        {
            Token token = this.aggregatingTokenStream.Peek();
            if (token.Value == value)
            {
                return this.aggregatingTokenStream.Pop();
            }
            throw new ParserException(token, "Expected '" + value + "' but found '" + token.Value + "'");
        }

        public void EnsureNext(string value)
        {
            if (!this.IsNext(value))
            {
                this.PopExpected(value);
            }
        }
    }
}
