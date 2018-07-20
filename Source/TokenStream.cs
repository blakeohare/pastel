using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pastel
{
    class TokenStream
    {
        private Token[] tokens;
        private int length;
        private int index = 0;

        public TokenStream(IList<Token> tokens)
        {
            this.tokens = tokens.ToArray();
            this.length = this.tokens.Length;
        }
    }
}
