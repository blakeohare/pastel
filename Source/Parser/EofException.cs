using System;

namespace Pastel.Parser
{
    internal class EofException : Exception
    {
        public EofException(string filename) : base("EOF encountered in " + filename) { }
    }
}
