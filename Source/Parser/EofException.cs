namespace Pastel.Parser
{
    internal class EofException : UserErrorException
    {
        public EofException(string filename) : base("EOF encountered in " + filename) { }
    }
}
