using System;
using System.Collections.Generic;
using System.Linq;

namespace Pastel
{
    internal class Tokenizer
    {
        private readonly string content;
        private readonly string filename;
        private readonly int length;
        private readonly int[] lines;
        private readonly int[] columns;
        private readonly List<Token> tokenBuilder = new List<Token>();

        private static readonly HashSet<char> ALPHANUMERIC_CHARS;

        static Tokenizer()
        {
            ALPHANUMERIC_CHARS = new HashSet<char>() { '_' };
            for (int i = 0; i < 26; ++i)
            {
                ALPHANUMERIC_CHARS.Add((char)('a' + i));
                ALPHANUMERIC_CHARS.Add((char)('A' + i));
                if (i < 10) ALPHANUMERIC_CHARS.Add((char)('0' + i));
            }
        }

        public Tokenizer(string file, string content)
        {
            // Adding a dummy \n to the end of the file allows for simpler code
            // - index + 1 can generally be blindly checked, without checking bounds
            // - last line comments will get closed
            // - non-alphanumeric character guarantees that the alphanumeric end will get triggered
            this.content = content + "\n";
            this.filename = file;
            this.length = this.content.Length;
            this.lines = new int[length];
            this.columns = new int[length];
            int line = 1;
            int column = 1;
            for (int i = 0; i < this.length; ++i)
            {
                lines[i] = line;
                columns[i] = column;
                char c = this.content[i];
                if (c == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
            }
            this.tokenBuilder = new List<Token>();
        }

        private enum State
        {
            NORMAL,
            COMMENT_LINE,
            COMMENT_MULTILINE,
            STRING_SINGLE,
            STRING_DOUBLE,
            WORD
        }

        private void PushToken(int start, int length, TokenType type)
        {
            string value = this.content.Substring(start, length);
            if (type == TokenType.ALPHANUMS)
            {
                char c = value[0];
                if (c >= '0' && c <= '9')
                {
                    type = TokenType.NUMBER;
                }
            }
            this.tokenBuilder.Add(new Token(this.filename, value, start, this.lines[start], this.columns[start], type));
        }

        private void PushToken(int index, TokenType type)
        {
            this.PushToken(index, 1, type);
        }

        public TokenStream Tokenize()
        {
            int tokenStart = 0;
            State state = State.NORMAL;
            char c;
            for (int i = 0; i < length; ++i)
            {
                c = content[i];
                switch (state)
                {
                    case State.NORMAL:
                        switch (c)
                        {
                            case ' ':
                            case '\t':
                            case '\n':
                            case '\r':
                                break;

                            case '"':
                                tokenStart = i;
                                state = State.STRING_DOUBLE;
                                break;
                            case '\'':
                                tokenStart = i;
                                state = State.STRING_SINGLE;
                                break;

                            case '/':
                                if (i + 1 < length)
                                {
                                    c = content[i + 1];
                                    if (c == '/')
                                    {
                                        state = State.COMMENT_LINE;
                                    }
                                    else if (c == '*')
                                    {
                                        state = State.COMMENT_MULTILINE;
                                        i++;
                                    }
                                }
                                if (state == State.NORMAL)
                                {
                                    this.PushToken(i, TokenType.PUNCTUATION);
                                }
                                break;

                            default:
                                if (ALPHANUMERIC_CHARS.Contains(c))
                                {
                                    tokenStart = i;
                                    state = State.WORD;
                                }
                                else
                                {
                                    this.PushToken(i, TokenType.PUNCTUATION);
                                }
                                break;
                        }
                        break;

                    case State.COMMENT_LINE:
                        if (c == '\n')
                        {
                            state = State.NORMAL;
                        }
                        break;

                    case State.COMMENT_MULTILINE:
                        if (c == '*' && content[i + 1] == '/')
                        {
                            i++;
                            state = State.NORMAL;
                        }
                        break;

                    case State.STRING_DOUBLE:
                    case State.STRING_SINGLE:
                        if (c == '\\')
                        {
                            i++;
                        }
                        else if (c == (state == State.STRING_SINGLE ? '\'' : '"'))
                        {
                            this.PushToken(tokenStart, i - tokenStart + 1, TokenType.STRING);
                            state = State.NORMAL;
                        }
                        break;

                    case State.WORD:
                        if (!ALPHANUMERIC_CHARS.Contains(c))
                        {
                            this.PushToken(tokenStart, i - tokenStart, TokenType.ALPHANUMS);
                            --i;
                            state = State.NORMAL;
                        }
                        break;

                    default:
                        throw new Exception();
                }
            }

            // Convert all decimals into single tokens.
            // The implementation here is a little cheesy. 
            // Find all the '.' tokens and then consolidate numbers around them
            for (int i = 0; i < this.tokenBuilder.Count; ++i)
            {
                Token token = this.tokenBuilder[i];
                if (token.Value == ".")
                {
                    if (i > 0)
                    {
                        Token prev = this.tokenBuilder[i - 1];
                        if (prev.Type == TokenType.NUMBER && !prev.IsNextWhitespace)
                        {
                            prev.Value += ".";
                            this.tokenBuilder[i] = prev;
                            this.tokenBuilder[i - 1] = null;
                            token = prev;
                        }
                    }

                    if (i + 1 < this.tokenBuilder.Count)
                    {
                        Token next = this.tokenBuilder[i + 1];
                        if (!token.IsNextWhitespace && next.Type == TokenType.NUMBER)
                        {
                            token.Value += next.Value;
                            this.tokenBuilder[i + 1] = null;
                            token.Type = TokenType.NUMBER;
                            i++;
                        }
                    }
                }
            }

            this.tokenBuilder.Add(new Token(this.filename, null, this.length - 1, this.lines[this.length - 1], this.columns[this.length - 1], TokenType.EOF));

            Token[] newTokens = this.tokenBuilder.Where(t => t != null).ToArray();

            return new TokenStream(this.filename, this.content, newTokens);
        }
    }
}
