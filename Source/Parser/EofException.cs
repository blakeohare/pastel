namespace Pastel.Parser
{
    internal class EofException : UserErrorException
    {
        public EofException(string filename) : base("EOF encountered in " + filename) { }

        public EofException(string filename, string msg) : base("EOF encountered in " + filename + ": " + msg) { }
    }
}
