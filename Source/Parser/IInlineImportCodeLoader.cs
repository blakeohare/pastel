namespace Pastel.Parser
{
    public interface IInlineImportCodeLoader
    {
        string LoadCode(Token throwLocation, string path);
    }
}
