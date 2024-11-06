using System.Collections.Generic;
using System.Linq;

namespace Pastel.Parser
{
    internal class TokenStream
    {
        private Token[] tokens;
        private int index;
        private int length;

        public TokenStream(IList<Token> tokens)
        {
            index = 0;
            this.tokens = tokens.ToArray();
            length = this.tokens.Length;
        }

        public int SnapshotState()
        {
            return index;
        }

        public void RevertState(int index)
        {
            this.index = index;
        }

        public bool IsNext(string token)
        {
            if (index < length)
            {
                return tokens[index].Value == token;
            }
            return false;
        }

        public Token Peek()
        {
            if (index < length)
            {
                return tokens[index];
            }
            return null;
        }

        public Token Pop()
        {
            if (index < length)
            {
                return tokens[index++];
            }
            throw new EofException(tokens[0].FileName);
        }

        public Token PopIdentifier()
        {
            Token token = Pop();
            string value = token.Value;
            char c = value[0];
            if (c >= 'a' && c <= 'z' ||
                c >= 'A' && c <= 'Z' ||
                c >= '0' && c <= '9' ||
                c == '_')
            {
                return token;
            }
            throw new ParserException(token, "Expected identifier. Found '" + value + "'");
        }

        public string PeekValue()
        {
            if (index < length)
            {
                return tokens[index].Value;
            }
            return null;
        }

        public string PeekAhead(int offset)
        {
            if (index + offset < length)
            {
                return tokens[index + offset].Value;
            }
            return null;
        }

        public bool PopIfPresent(string value)
        {
            if (index < length && tokens[index].Value == value)
            {
                index++;
                return true;
            }
            return false;
        }

        public Token PopExpected(string value)
        {
            Token token = Pop();
            if (token.Value != value)
            {
                string message = "Unexpected token. Expected: '" + value + "' but found '" + token.Value + "'.";
                throw new ParserException(token, message);
            }
            return token;
        }

        public bool HasMore
        {
            get
            {
                return index < length;
            }
        }

        public Token PopBitShiftHackIfPresent()
        {
            string next = PeekValue();
            if (next == "<" || next == ">")
            {
                if (index + 1 < length)
                {
                    Token nextToken = tokens[index + 1];
                    if (nextToken.Value == next && !nextToken.HasWhitespacePrefix)
                    {
                        Token output = Pop();
                        Pop();
                        return new Token(
                            output.Value + output.Value,
                            output.FileName,
                            output.Line,
                            output.Col,
                            output.HasWhitespacePrefix);
                    }
                }
            }
            return null;
        }
    }
}
