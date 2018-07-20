using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pastel
{
    class Token
    {
        public string FileName { get; private set; }
        public string Value { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }
        public bool IsAlphanumeric { get; private set; }
        public bool IsNextWhitespace { get; private set; }
        private char nextChar;

        public Token(string filename, string value, int line, int column, bool isAlpha, char nextChar)
        {
            this.FileName = filename;
            this.Value = value;
            this.Line = line;
            this.Column = column;
            this.IsAlphanumeric = isAlpha;
            this.nextChar = nextChar;
        }
    }
}
