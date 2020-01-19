using System;

namespace Pastel
{
    internal class EofException : Exception
    {
        public EofException(string filename) : base("EOF encountered in " + filename) { }
    }
}
