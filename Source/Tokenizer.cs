using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pastel
{
    class Tokenizer
    {
        private string content;
        private string filename;
        private int length;
        private int[] lines;
        private int[] columns;
        private List<Token> tokenBuilder = new List<Token>();

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

            int length = this.content.Length;
            this.lines = new int[length];
            this.columns = new int[length];
            int line = 1;
            int column = 1;
            for (int i = 0; i < length; ++i)
            {
                lines[i] = line;
                columns[i] = column;
                char c = content[i];
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

        private void PushToken(int start, int end)
        {
            int length = end - start;
            string value = this.content.Substring(start, length);
            bool isAlpha = ALPHANUMERIC_CHARS.Contains(value[0]);
            this.tokenBuilder.Add(new Token(this.filename, value, this.lines[start], this.columns[start], isAlpha, this.content[end]));
        }

        private void PushToken(int index)
        {
            this.PushToken(index, index + 1);
        }

        TokenStream Tokenize()
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
                                    this.PushToken(i);
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
                                    this.PushToken(i);
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
                            this.PushToken(tokenStart, i);
                            state = State.NORMAL;
                        }
                        break;

                    case State.WORD:
                        if (!ALPHANUMERIC_CHARS.Contains(c))
                        {
                            --i;
                            this.PushToken(tokenStart, i);
                        }
                        break;

                    default:
                        throw new Exception();
                }
            }

            return new TokenStream(this.tokenBuilder);
        }
    }
}
