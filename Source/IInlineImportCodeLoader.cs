namespace Pastel
{
    public interface IInlineImportCodeLoader
    {
        string LoadCode(Token throwLocation, string path);
    }
}
