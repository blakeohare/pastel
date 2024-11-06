using System;

namespace Pastel
{
    internal class UserErrorException : Exception
    {
        public UserErrorException(string msg) : base(msg) { }
    }
}
