﻿using System.Collections.Generic;
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
            this.index = 0;
            this.tokens = tokens.ToArray();
            this.length = this.tokens.Length;
        }

        public int SnapshotState()
        {
            return this.index;
        }

        public void RevertState(int index)
        {
            this.index = index;
        }

        public bool IsNext(string token)
        {
            if (this.index < this.length)
            {
                return this.tokens[this.index].Value == token;
            }
            return false;
        }

        public Token Peek()
        {
            if (this.index < this.length)
            {
                return this.tokens[this.index];
            }
            return null;
        }

        public Token Pop()
        {
            if (this.index < this.length)
            {
                return this.tokens[this.index++];
            }
            throw new EofException(this.tokens[0].FileName);
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
            throw new UNTESTED_ParserException(
                token, 
                "Expected identifier. Found '" + value + "'");
        }

        public string PeekValue()
        {
            if (this.index < this.length)
            {
                return this.tokens[this.index].Value;
            }
            return null;
        }

        public string PeekAhead(int offset)
        {
            if (this.index + offset < this.length)
            {
                return this.tokens[this.index + offset].Value;
            }
            return null;
        }

        public bool PopIfPresent(string value)
        {
            if (this.index < this.length && this.tokens[this.index].Value == value)
            {
                this.index++;
                return true;
            }
            return false;
        }

        public Token PopExpected(string value)
        {
            Token token = this.Pop();
            if (token.Value != value)
            {
                string message = "Unexpected token. Expected: '" + value + "' but found '" + token.Value + "'.";
                throw new UNTESTED_ParserException(token, message);
            }
            return token;
        }

        public bool HasMore
        {
            get
            {
                return this.index < this.length;
            }
        }

        public Token PopBitShiftHackIfPresent()
        {
            string val1 = this.PeekValue();
            if (val1 == "<<") return this.Pop(); // << is unambiguouos
            if (val1 == ">" && this.index + 1 < this.length) // could be a >>
            {
                Token token1 = this.tokens[this.index];
                Token token2 = this.tokens[this.index + 1];
                if (token2.Value == ">" &&
                    token1.Line == token2.Line &&
                    token1.Col + 1 == token2.Col)
                {
                    this.Pop();
                    this.Pop();
                    return new Token(">>", token1.FileName, token1.Line, token1.Col, TokenType.PUNCTUATION);
                }
            }

            return null;
        }
    }
}
