using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser
{
    internal static class Tokenizer
    {
        private static readonly HashSet<string> TWO_CHAR_TOKENS = new HashSet<string>([
            "++", "--",
            "==", "!=",
            "<=", ">=",
            "&&", "||",
            "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=",

            // NOTE: >> is popped differently because it can conflict with type
            // generics, e.g. the last 2 characters of 'List<List<Foo>>'. However
            // << has no such conflict.
            // >>> is not in Pastel as it has too much language-specific nuance.
            "<<",
        ]);

        private static readonly HashSet<char> WHITESPACE = new HashSet<char>([
            ' ', '\n', '\r', '\t',
        ]);

        private static readonly HashSet<char> IDENTIFIER_CHARS = new HashSet<char>(['_']);

        static Tokenizer()
        {
            for (int i = 0; i < 26; i++)
            {
                IDENTIFIER_CHARS.Add((char)('a' + i));
                IDENTIFIER_CHARS.Add((char)('A' + i));
            }
            for (int i = 0; i < 10; i++)
            {
                IDENTIFIER_CHARS.Add((char)('0' + i));
            }
        }

        private enum TokenMode
        {
            NORMAL,
            STRING,
            COMMENT,
            WORD,
        }

        private enum CommentType
        {
            SINGLE_LINE,
            MULTI_LINE,

            UNSET,
        }

        private enum StringType
        {
            SINGLE_QUOTE,
            DOUBLE_QUOTE,

            UNSET,
        }

        private static int FindSuspciousUnclosedStringLine(List<Token> tokensSoFar)
        {
            Token suspiciousToken = null;
            foreach (Token suspiciousCheck in tokensSoFar)
            {
                char c = suspiciousCheck.Value[0];
                if (c == '"' || c == '\'')
                {
                    if (suspiciousCheck.Value.Contains('\n'))
                    {
                        return suspiciousCheck.Line;
                    }
                }
            }
            return -1;
        }

        public static Token[] Tokenize(string filename, string code)
        {
            code = code.Replace("\r\n", "\n") + "\n\n";

            int[] lineByIndex = new int[code.Length];
            int[] colByIndex = new int[code.Length];
            char c;
            int line = 0;
            int col = 0;
            for (int i = 0; i < code.Length; i++)
            {
                c = code[i];
                lineByIndex[i] = line;
                colByIndex[i] = col;
                if (c == '\n')
                {
                    line++;
                    col = 0;
                }
                else
                {
                    col++;
                }
            }

            List<Token> tokens = [];
            int tokenStart = -1;
            CommentType commentType = CommentType.UNSET;
            StringType stringType = StringType.UNSET;
            string c2;
            int length = code.Length;

            TokenMode mode = TokenMode.NORMAL;

            for (int i = 0; i < length; ++i)
            {
                c = code[i];

                switch (mode)
                {
                    case TokenMode.NORMAL:
                        if (IDENTIFIER_CHARS.Contains(c))
                        {
                            mode = TokenMode.WORD;
                            tokenStart = i;
                            i--;
                        }
                        else if (WHITESPACE.Contains(c))
                        {
                            // do nothing
                        }
                        else if (c == '/' && code[i + 1] == '/')
                        {
                            mode = TokenMode.COMMENT;
                            commentType = CommentType.SINGLE_LINE;
                        }
                        else if (c == '/' && code[i + 1] == '*')
                        {
                            mode = TokenMode.COMMENT;
                            commentType = CommentType.MULTI_LINE;
                            i++;
                        }
                        else if (c == '"')
                        {
                            mode = TokenMode.STRING;
                            stringType = StringType.DOUBLE_QUOTE;
                            tokenStart = i;
                        }
                        else if (c == '\'')
                        {
                            mode = TokenMode.STRING;
                            stringType = StringType.SINGLE_QUOTE;
                            tokenStart = i;
                        }
                        else
                        {
                            c2 = code.Substring(i, 2);
                            if (TWO_CHAR_TOKENS.Contains(c2))
                            {
                                tokens.Add(new Token(c2, filename, lineByIndex[i], colByIndex[i], TokenType.PUNCTUATION));
                                i++;
                            }
                            else
                            {
                                tokens.Add(new Token("" + c, filename, lineByIndex[i], colByIndex[i], TokenType.PUNCTUATION));
                            }
                        }
                        break;

                    case TokenMode.COMMENT:
                        if (commentType == CommentType.SINGLE_LINE)
                        {
                            if (c == '\n')
                            {
                                mode = TokenMode.NORMAL;
                            }
                        }
                        else
                        {
                            if (c == '*' && code[i + 1] == '/')
                            {
                                mode = TokenMode.NORMAL;
                                ++i;
                            }
                        }
                        break;

                    case TokenMode.STRING:
                        if (c == '\\')
                        {
                            ++i;
                        }
                        else if ((c == '"' && stringType == StringType.DOUBLE_QUOTE) ||
                            (c == '\'' && stringType == StringType.SINGLE_QUOTE))
                        {
                            string stringValue = code.Substring(tokenStart, i - tokenStart + 1);
                            tokens.Add(new Token(stringValue, filename, lineByIndex[tokenStart], colByIndex[tokenStart], TokenType.STRING));
                            mode = TokenMode.NORMAL;
                        }
                        break;

                    case TokenMode.WORD:
                        if (!IDENTIFIER_CHARS.Contains(c))
                        {
                            string wordValue = code.Substring(tokenStart, i - tokenStart);
                            bool isInteger = wordValue[0] >= '0' && wordValue[0] <= '9';
                            tokens.Add(new Token(wordValue, filename, lineByIndex[tokenStart], colByIndex[tokenStart], isInteger ? TokenType.INTEGER : TokenType.WORD));
                            i--;
                            mode = TokenMode.NORMAL;
                        }
                        break;
                }
            }

            if (mode != TokenMode.NORMAL)
            {
                if (mode == TokenMode.COMMENT)
                {
                    throw new EofException(filename, "This file seems to contain an unclosed comment.");
                }

                if (mode == TokenMode.STRING)
                {
                    string msg = "This file contains an unclosed string.";
                    int susStringLine = FindSuspciousUnclosedStringLine(tokens);
                    if (susStringLine != -1)
                    {
                        msg += " The string on line " + (susStringLine + 1) + " is suspicious.";
                    }
                    throw new EofException(filename, msg);
                }

                throw new EofException(filename);
            }

            // Consolidate decimals into single tokens.
            for (int i = 0; i < tokens.Count; i++)
            {
                Token current = tokens[i];
                if (current != null && current.Value == ".")
                {
                    Token left = i > 0 ? tokens[i - 1] : null;
                    Token right = i + 1 < tokens.Count ? tokens[i + 1] : null;
                    if (left != null && left.Line == current.Line && left.Col + left.Value.Length == current.Col)
                    {
                        if (IsInteger(left.Value))
                        {
                            left.Value += ".";
                            tokens[i - 1] = null;
                            tokens[i] = left;
                            current = left;
                            current.Type = TokenType.FLOAT;
                        }
                    }

                    if (right != null && right.Line == current.Line && current.Col + current.Value.Length == right.Col)
                    {
                        if (IsInteger(right.Value))
                        {
                            current.Value += right.Value;
                            current.Type = TokenType.FLOAT;
                            tokens[i + 1] = null;
                        }
                    }
                }
            }

            return tokens.Where(t => t != null).ToArray();
        }

        private static bool IsInteger(string val)
        {
            for (int i = 0; i < val.Length; i++)
            {
                if (val[i] < '0' || val[i] > '9')
                {
                    return false;
                }
            }
            return true;
        }
    }
}
